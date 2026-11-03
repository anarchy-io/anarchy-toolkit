using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Infrastructure.Observability.Serilog.Extension.Observability;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Assertions;
using Anarchy.Toolkit.Tests.Unit.Infrastructure.Observability.Serilog.TestingUtilities;
using AwesomeAssertions;
using Serilog;
using Serilog.Events;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Observability.Serilog.Extension.Observability;

[Collection(EnvironmentVariableCollection.Name)]
public class StandardPropertiesEnricherTests
{
    [Fact]
    public void WithLogRetention_InvalidLogRetention_ThrowsInvalidEnumArgumentException()
    {
        var inMemorySink = new InMemorySink();
        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.Sink(inMemorySink)
            .MinimumLevel.Is(LogEventLevel.Verbose);

        const RetentionPeriod invalidRetention = (RetentionPeriod)(-1);

        loggerConfiguration
            .Invoking(cfg => cfg.Enrich.WithLogRetention(invalidRetention))
            .Should().Throw<UndefinedEnumValueException>()
            .WithMessage("'-1' is not a defined RetentionPeriod value.*")
            .WithParameterName("retentionPeriod");
    }

    [Theory]
    [InlineData(RetentionPeriod.ThreeDays, 3)]
    [InlineData(RetentionPeriod.SevenDays, 7)]
    [InlineData(RetentionPeriod.FifteenDays, 15)]
    [InlineData(RetentionPeriod.ThirtyDays, 30)]
#pragma warning disable CS0618 // Type or member is obsolete; We're intentionally using the NinetyDays value to test the mapping.
    [InlineData(RetentionPeriod.NinetyDays, 90)]
#pragma warning restore CS0618 // Type or member is obsolete; We're intentionally using the NinetyDays value to test the mapping.
    public void WithLogRetention_WithValidArguments_EnrichesLogsWithExpectedProperties(RetentionPeriod retentionPeriod, int expectedNumberOfDays)
    {
        var inMemorySink = new InMemorySink();
        var logger = new LoggerConfiguration()
            .WriteTo.Sink(inMemorySink)
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .Enrich.WithLogRetention(retentionPeriod)
            .CreateLogger();

        logger.Information("Test log message");

        inMemorySink.Should().HaveMessage("Test log message")
            .Which.Should().HaveNumericProperty<int>("retention").Which.Should().Be(expectedNumberOfDays);
    }

    [Theory]
#pragma warning disable xUnit1012 // Cover cases where consumers of package do not have nullable enabled in their project.
    [InlineData(null)]
#pragma warning restore xUnit1012 // Cover cases where consumers of package do not have nullable enabled in their project.
    [InlineData("")]
    [InlineData("  ")]
    public void WithTeam_WithInvalidArguments_ThrowsArgumentException(string invalidTeamName)
    {
        var inMemorySink = new InMemorySink();
        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.Sink(inMemorySink)
            .MinimumLevel.Is(LogEventLevel.Verbose);

        loggerConfiguration
            .Invoking(cfg => cfg.Enrich.WithTeam(invalidTeamName))
            .Should().Throw<ArgumentException>()
            .WithMessage("Team cannot be null or whitespace. (Parameter 'team')")
            .WithParameterName("team");
    }

    [Fact]
    public void WithAppGroup_WithValidArguments_EnrichesLogsWithExpectedProperties()
    {
        const string appGroup = "demo-app";

        var inMemorySink = new InMemorySink();
        var logger = new LoggerConfiguration()
            .WriteTo.Sink(inMemorySink)
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .Enrich.WithAppGroup(appGroup)
            .CreateLogger();

        logger.Information("Test log message");

        inMemorySink.Should().HaveMessage("Test log message")
            .Which.Should().HaveStringProperty("appgroup").Which.Should().Be(appGroup);
    }

    [Theory]
#pragma warning disable xUnit1012 // Cover cases where consumers of package do not have nullable enabled in their project.
    [InlineData(null)]
#pragma warning restore xUnit1012 // Cover cases where consumers of package do not have nullable enabled in their project.
    [InlineData("")]
    [InlineData("  ")]
    public void WithAppGroup_WithInvalidArguments_ThrowsArgumentException(string invalidAppGroup)
    {
        var inMemorySink = new InMemorySink();
        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.Sink(inMemorySink)
            .MinimumLevel.Is(LogEventLevel.Verbose);

        loggerConfiguration
            .Invoking(cfg => cfg.Enrich.WithAppGroup(invalidAppGroup))
            .Should().Throw<ArgumentException>()
            .WithMessage("App Group name cannot be null or whitespace. (Parameter 'appGroup')")
            .WithParameterName("appGroup");
    }

    [Fact]
    public void WithTeam_WithValidArguments_EnrichesLogsWithExpectedProperties()
    {
        const string githubTeamIdentifier = "hosting-deployment";

        var inMemorySink = new InMemorySink();
        var logger = new LoggerConfiguration()
            .WriteTo.Sink(inMemorySink)
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .Enrich.WithTeam(githubTeamIdentifier)
            .CreateLogger();

        logger.Information("Test log message");

        inMemorySink.Should().HaveMessage("Test log message")
            .Which.Should().HaveStringProperty("team").Which.Should().Be(githubTeamIdentifier);
    }

    [Theory]
#pragma warning disable xUnit1012 // Cover cases where consumers of package do not have nullable enabled in their project.
    [InlineData(null)]
#pragma warning restore xUnit1012 // Cover cases where consumers of package do not have nullable enabled in their project.
    [InlineData("")]
    [InlineData("   ")]
    public void WithCostCenter_WithInvalidCostCenterArgumentProvided_ThrowsArgumentException(string invalidCostCenter)
    {
        var configuration = new LoggerConfiguration();

        Action call = () => configuration.Enrich.WithCostCenter(invalidCostCenter);

        call.Should().Throw<ArgumentException>()
            .WithParameterName("costCenter")
            .WithMessage("Cost center cannot be null or whitespace. (Parameter 'costCenter')");
    }

    [Fact]
    public void WithCostCenter_WithCostCenterArgumentProvided_EnrichesLogsWithCostCenterArgument()
    {
        var costCenter = Guid.NewGuid().ToString("N");

        var inMemorySink = new InMemorySink();
        var logger = new LoggerConfiguration()
            .WriteTo.Sink(inMemorySink)
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .Enrich.WithCostCenter(costCenter)
            .CreateLogger();

        logger.Information("YOLO");

        inMemorySink.Should().HaveMessage("YOLO")
                    .Which.Should().HaveStringProperty("CostCenter")
                    .Which.Should().Be(costCenter);
    }

    [Theory]
    [InlineData("  ")]
    [InlineData("hosting-deployment")]
    public void WithCostCenter_WithCostCenterArgumentNotProvided_EnrichesLogsWithCostCenterFromEnvironmentVariable(string costCenter)
    {
        using var _ = new EnvironmentVariableContext("COST_CENTER", costCenter);

        var inMemorySink = new InMemorySink();
        var logger = new LoggerConfiguration()
            .WriteTo.Sink(inMemorySink)
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .Enrich.WithCostCenterFromEnvironment("COST_CENTER")
            .CreateLogger();

        logger.Information("YOLO");

        inMemorySink.Should().HaveMessage("YOLO")
                    .Which.Should().HaveStringProperty("CostCenter")
                    .Which.Should().Be(costCenter);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void WithCostCenter_WithCostCenterArgumentNotProvided_AndEnvironmentVariableNotSet_EnrichesLogsWithEmptyCostCenter(string? unsetCostCenterValue)
    {
        using var _ = new EnvironmentVariableContext("COST_CENTER", unsetCostCenterValue!);

        var configuration = new LoggerConfiguration();

        Action call = () => configuration.Enrich.WithCostCenterFromEnvironment("COST_CENTER");

        call.Should().Throw<InvalidOperationException>()
            .WithMessage("Environment variable 'COST_CENTER' is not set, empty or whitespace.");
    }

    [Fact]
    public void WithCostCenter_WithCustomPropertyName_UsesThatPropertyName()
    {
        var inMemorySink = new InMemorySink();
        var logger = new LoggerConfiguration()
            .WriteTo.Sink(inMemorySink)
            .Enrich.WithCostCenter("ordering", propertyName: "Acme-CostCenter")
            .CreateLogger();

        logger.Information("YOLO");

        inMemorySink.Should().HaveMessage("YOLO")
                    .Which.Should().HaveStringProperty("Acme-CostCenter")
                    .Which.Should().Be("ordering");
    }
}
