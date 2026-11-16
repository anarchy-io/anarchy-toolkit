using Anarchy.Toolkit.Core.Exceptions;
using AWS.Logger;
using AWS.Logger.SeriLog;
using Serilog;
using Serilog.Configuration;
using Serilog.Formatting.Json;

namespace Anarchy.Toolkit.Infrastructure.Observability.Serilog.Extension.Observability;

/// <summary>
/// Serilog sink extensions for shipping logs to AWS CloudWatch.
/// </summary>
/// <example>
/// <code language="csharp">
/// Log.Logger = new LoggerConfiguration()
///     .WriteTo.Ec2CloudWatch("/aws/ec2/my-app", cfg =&gt; cfg.Region = "eu-west-1")
///     .CreateLogger();
/// </code>
/// </example>
public static class LoggerConfigurationExtensions
{
    private const bool DisableLogGroupCreation = true;
    private const string LibraryLogFileName = "/var/log/dotnetapp/aws-logger-error.log";
    private static readonly string LogStreamNameSuffix = Guid.NewGuid().ToString("N") + "-application_logs";
    private const int MaxQueuedMessages = 100_000;

    /// <param name="configuration">The sink configuration.</param>
    extension(LoggerSinkConfiguration configuration)
    {
        /// <summary>
        /// Configures a Serilog sink that writes JSON-formatted logs to AWS CloudWatch from an EC2 instance.
        /// </summary>
        /// <param name="logGroup">The CloudWatch log group to write to (e.g. <c>/aws/ec2/my-app</c>).</param>
        /// <param name="configure">
        /// Optional callback allowing callers to override the default <see cref="AWSLoggerConfig"/>.
        /// </param>
        /// <returns>The updated <see cref="LoggerConfiguration"/>.</returns>
        /// <exception cref="EmptyArgumentException">Thrown when <paramref name="logGroup"/> is null, empty or whitespace.</exception>
        /// <remarks>
        /// Default behavior:
        /// <list type="bullet">
        /// <item>Log group creation disabled</item>
        /// <item>Log stream suffix: <c>{guid}-application_logs</c>, where the GUID is generated once per process</item>
        /// <item>Fallback error log: <c>/var/log/dotnetapp/aws-logger-error.log</c></item>
        /// <item>MaxQueuedMessages: 100,000</item>
        /// <item>Formatter: Serilog <see cref="JsonFormatter"/> with rendered messages</item>
        /// </list>
        /// <paramref name="configure"/> runs after the defaults are applied, so it can override any of them.
        /// </remarks>
        public LoggerConfiguration Ec2CloudWatch(string logGroup, Action<AWSLoggerConfig>? configure = null)
        {
            if (string.IsNullOrWhiteSpace(logGroup))
                throw new EmptyArgumentException(nameof(logGroup), "Log group cannot be null or whitespace.");

            var awsLoggerConfig = new AWSLoggerConfig {
                LogGroup = logGroup,
                DisableLogGroupCreation = DisableLogGroupCreation,
                LibraryLogFileName = LibraryLogFileName,
                LogStreamNameSuffix = LogStreamNameSuffix,
                MaxQueuedMessages = MaxQueuedMessages
            };

            configure?.Invoke(awsLoggerConfig);

            // There are no unit tests reaching AWS: the only way to test delivery is an integration test against AWS
            return configuration.AWSSeriLog(
                awsLoggerConfig,
                textFormatter: new JsonFormatter(renderMessage: true)
            );
        }
    }
}
