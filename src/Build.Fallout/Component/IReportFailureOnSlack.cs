using Anarchy.Toolkit.Build.Fallout.Component.Tasks;
using Anarchy.Toolkit.Build.Fallout.Core.Entity.Build;
using Anarchy.Toolkit.Build.Fallout.Infrastructure.Extension;
using Fallout.Common;
using Fallout.Common.CI.TeamCity;
using Fallout.Common.Execution;
using Fallout.Common.Tools.Slack;
using System.Text;
using Environment = Anarchy.Toolkit.Build.Fallout.Core.ValueSet.Build.Environment;
using Logger = Anarchy.Toolkit.Build.Fallout.Infrastructure.Helper.Logger;

namespace Anarchy.Toolkit.Build.Fallout.Component
{
    public interface IReportFailureOnSlack : IVersionedBuild
    {
        [Parameter("Slack webhook URL")]
        string? SlackWebhookUrl => TryGetValue(() => SlackWebhookUrl);

        /// <summary>
        /// The name of the application or service, used in failure reporting.
        /// </summary>
        string ApplicationName { get; }

        /// <summary>
        /// The Slack webhook URL used to report failures on Slack. These should be KMS-encrypted per environment the
        /// build runs in (i.e. one for Testing, one for Acceptance, one for Production).
        /// </summary>
        IReadOnlyDictionary<Environment, Secret> EncryptedSlackWebhookUrls { get; }

        /// <summary>
        /// A target to report failures on Slack.
        /// <para>
        /// Reporting failures to Slack requires the <see cref="EncryptedSlackWebhookUrls"/> and
        /// <see cref="ApplicationName"/> properties to be overridden by the build. If the application name is not set, or if a
        /// webhook URL cannot be found for the current environment (<see cref="IDefaultBuild.BuildEnvironment"/>) then nothing
        /// will be reported to Slack.
        /// </para>
        /// <para>
        /// To customize the message reported to slack, override <see cref="ConfigureFailureSlackMessage"/>.
        /// </para>
        /// </summary>
        Target ReportFailure =>
            _ => _
                .Unlisted()
                .AssuredAfterFailure()
                .TryTriggeredBy<ICodeDeployBuild>(t => t.BuildReleaseCodeDeploy, t => t.ValidateDeploymentAcceptance, t => t.DeployArtifacts)
                .TryTriggeredBy<IAwsLambdaBuild>(t => t.BuildRelease)
                .OnlyWhenDynamic(() => IsFailing)
                .Executes(
                     () => {
                         if (FalloutBuild.IsLocalBuild && string.IsNullOrEmpty(SlackWebhookUrl))
                         {
                             Logger.Trace("Build is a local build and a webhook URL parameter (--slack-webhook-url) has not been provided. Failure will not be reported on Slack.");
                             return;
                         }

                         if (string.IsNullOrEmpty(ApplicationName))
                         {
                             Logger.Trace("Build failed, but no application name has been provided.");
                             return;
                         }

                         var slackWebhookUrl = this.As<IReportFailureOnSlack>().GetSlackWebhookUrl();
                         if (string.IsNullOrEmpty(slackWebhookUrl))
                         {
                             Logger.Trace("Build failed, but no Slack webhook URL has been configured for the environment.");
                             return;
                         }

                         string failureSummary = GetFailureSummary();

                         SlackTasks.SendSlackMessage(
                             m => ConfigureFailureSlackMessage(m, failureSummary),
                             webhook: slackWebhookUrl
                         );

                         Logger.Info("Reported failure to Slack");
                     }
                 );

        /// <summary>
        /// Builds a message to send to Slack to report a build failure. This calls <see cref="GetBuildDescription"/> to label 'what kind of a build' it is that has failed.
        /// </summary>
        SlackMessage ConfigureFailureSlackMessage(SlackMessage message, string error)
        {
            string description = GetBuildDescription();

            Uri? buildUrl = null;
            string action;

            if (Host is TeamCity teamCity)
            {
                var buildId = teamCity.BuildId;
                buildUrl = new Uri(new Uri(teamCity.ServerUrl), $"viewLog.html?tab=buildLog&buildId={buildId}");
                action = teamCity.BuildConfiguration;
            }
            else
            {
                action = "Build";
            }

            return message.SetUsername("Fallout Build")
                          .SetAttachments(
                               new SlackMessageAttachment()
                                  .SetColor("danger")
                                  .SetFallback($"Fallout build failed for {ApplicationName} (build {BuildNumber}) with message:{System.Environment.NewLine}{error}")
                                  .SetTitle($":teamcity-new: {ApplicationName}: {action} #{BuildNumber ?? "(unknown version)"} failed")
                                  .SetTitleLink(buildUrl?.ToString())
                                  .AddFields(
                                       new SlackMessageField()
                                          .SetTitle(description)
                                          .SetValue("```" + error + "```")
                                   )
                           );
        }

        /// <summary>
        /// Constructs build description out of failed targets.
        /// <c>Release to Production</c>.
        /// </summary>
        string GetBuildDescription() =>
            string.Join("; ", FailedTargets.Select(ft => ft.Description ?? ft.Name));

        /// <summary>
        /// Constructs build failure summay out of failed targets
        /// <example>
        /// Target "Target1" failed:
        /// Exception: something bad happened
        /// Exception2: something else happened
        ///
        /// Target "Target2" failed.
        /// </example>
        /// </summary>
        string GetFailureSummary()
        {
            if (!FailedTargets.Any()) return "";

            var stringBuilder = new StringBuilder();

            var firstTarget = FailedTargets.First();
            WriteTargetSummay(firstTarget);

            foreach (var failedTarget in FailedTargets.Skip(1))
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine();
                WriteTargetSummay(failedTarget);
            }

            return stringBuilder.ToString();

            void WriteTargetSummay(ExecutableTarget target)
            {
                stringBuilder.Append($"Target {target.Name} failed");

                if (target.SummaryInformation.Any())
                {
                    stringBuilder.AppendLine(":");
                    stringBuilder.AppendJoin(System.Environment.NewLine, target.SummaryInformation.Select(x => $"{x.Key}: {x.Value}"));
                }
                else
                {
                    stringBuilder.AppendLine(".");
                }
            }
        }
    }
}
