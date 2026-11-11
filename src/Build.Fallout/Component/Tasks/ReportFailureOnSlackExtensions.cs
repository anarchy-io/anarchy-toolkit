namespace Anarchy.Toolkit.Build.Fallout.Component.Tasks
{
    internal static class ReportFailureOnSlackExtensions
    {
        public static string? GetSlackWebhookUrl(this IReportFailureOnSlack build)
        {
            return build.SlackWebhookUrl ?? DecryptWebhookUrl();

            string? DecryptWebhookUrl()
            {
                var urls = build.EncryptedSlackWebhookUrls;

                if (urls == null
                 || build.BuildEnvironment == null
                 || !urls.TryGetValue(build.BuildEnvironment, out var encrypted))
                    return null;

                return build.DecryptSecret(encrypted);
            }
        }
    }
}
