using Anarchy.Toolkit.Core.Exceptions;
using AWS.Logger;
using AWS.Logger.SeriLog;
using Serilog;
using Serilog.Configuration;
using Serilog.Formatting.Json;

namespace Anarchy.Toolkit.Infrastructure.Observability.Serilog.Extension.Observability;

public static class LoggerConfigurationExtensions
{
    private const bool _disableLogGroupCreation = true;
    private const string _libraryLogFileName = "/var/log/dotnetapp/aws-logger-error.log";
    private static readonly string _logStreamNameSuffix = Guid.NewGuid().ToString("N") + "-application_logs";
    private const int _maxQueuedMessages = 100_000;

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
        /// <exception cref="ArgumentException">Thrown when <paramref name="logGroup"/> is null, empty or whitespace.</exception>
        /// <remarks>
        /// Default behavior:
        /// <list type="bullet">
        /// <item>Log group creation disabled</item>
        /// <item>Log stream suffix: <c>application_logs</c></item>
        /// <item>Fallback error log: <c>/var/log/dotnetapp/aws-logger-error.log</c></item>
        /// <item>MaxQueuedMessages: 100,000</item>
        /// </list>
        /// </remarks>
        public LoggerConfiguration Ec2CloudWatch(string logGroup, Action<AWSLoggerConfig>? configure = null)
        {
            if (string.IsNullOrWhiteSpace(logGroup))
                throw new EmptyArgumentException(nameof(logGroup), "Log group cannot be null or whitespace.");

            var awsLoggerConfig = new AWSLoggerConfig {
                LogGroup = logGroup,
                DisableLogGroupCreation = _disableLogGroupCreation,
                LibraryLogFileName = _libraryLogFileName,
                LogStreamNameSuffix = _logStreamNameSuffix,
                MaxQueuedMessages = _maxQueuedMessages
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
