using Anarchy.Toolkit.Infrastructure.Observability.Serilog.Extension.Observability;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Assertions;
using AwesomeAssertions;
using AWS.Logger;
using Serilog;
using Serilog.Events;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Observability.Serilog.Extension.Observability;

public class LoggerConfigurationExtensionTests
{
    // Keeps the AWS CloudWatch sink from reaching real AWS (and from blocking process exit on its 30s flush timeout).
    private static void KeepAwsSinkOffline(AWSLoggerConfig config)
    {
        config.ServiceUrl = "http://127.0.0.1:1";
        config.FlushTimeout = TimeSpan.FromMilliseconds(100);
        config.LibraryLogErrors = false;
    }

    [Theory]
#pragma warning disable xUnit1012 // Cover cases where consumers of package do not have nullable enabled in their project.
    [InlineData(null)]
#pragma warning restore xUnit1012 // Cover cases where consumers of package do not have nullable enabled in their project.
    [InlineData("")]
    [InlineData("   ")]
    public void EC2CloudWatch_WhenLogGroupIsNullEmptyOrWhiteSpace_ThrowsArgumentException(string logGroup)
    {
        new LoggerConfiguration()
            .WriteTo.Invoking(cfg => cfg.Ec2CloudWatch(logGroup))
            .Should().Throw<ArgumentException>()
            .WithParameterName("logGroup");
    }

    [Fact]
    public void EC2CloudWatch_WithLogGroup_KeepsOtherSinksWorking()
    {
        var inMemorySink = new InMemorySink();
        using var logger = new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .WriteTo.Sink(inMemorySink)
            .WriteTo.Ec2CloudWatch("/aws/ec2/test-app", KeepAwsSinkOffline)
            .CreateLogger();

        logger.Information("test message");

        inMemorySink.Should().HaveMessage("test message");
    }
}
