using Anarchy.Toolkit.Build.Cli.Core.Service;
using AwesomeAssertions;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Tests.Unit.Build.Cli.Core.Service;

public class AppSettingsTest
{
    private const string Template = """
        {
          "AuthService": {
            "Host": "Base URL of the auth service, see https://wiki.example/auth",
            "ClientId": "OAuth client id"
          },
          "Port": 5000
        }
        """;

    private static JsonObject Parse(string json) => JsonNode.Parse(json)!.AsObject();

    [Fact]
    public void MergeAppSettings_GivenNoExistingFile_PromptsForEveryStringKeyAndCopiesFixedDefaults()
    {
        // Arrange
        var prompted = new List<(string Key, string Hint)>();

        // Act
        var result = AppSettingsMerger.Merge(Parse(Template), existing: null, reset: false, (key, hint, _) => {
            prompted.Add((key, hint));
            return $"value-of-{key}";
        });

        // Assert
        prompted.Should().Equal(
            ("AuthService:Host", "Base URL of the auth service, see https://wiki.example/auth"),
            ("AuthService:ClientId", "OAuth client id"));
        result.Settings["AuthService"]!["Host"]!.GetValue<string>().Should().Be("value-of-AuthService:Host");
        result.Settings["Port"]!.GetValue<int>().Should().Be(5000);
        result.Missing.Should().BeEmpty();
    }

    [Fact]
    public void MergeAppSettings_GivenExistingValues_KeepsThemAndExtraKeysWithoutPrompting()
    {
        // Arrange
        var existing = Parse("""{ "AuthService": { "Host": "https://auth.local/" }, "Port": 6000, "Extra": "mine" }""");
        var prompted = new List<string>();

        // Act
        var result = AppSettingsMerger.Merge(Parse(Template), existing, reset: false, (key, _, _) => {
            prompted.Add(key);
            return "client";
        });

        // Assert
        prompted.Should().Equal("AuthService:ClientId");
        result.Settings["AuthService"]!["Host"]!.GetValue<string>().Should().Be("https://auth.local/");
        result.Settings["AuthService"]!["ClientId"]!.GetValue<string>().Should().Be("client");
        result.Settings["Port"]!.GetValue<int>().Should().Be(6000);
        result.Settings["Extra"]!.GetValue<string>().Should().Be("mine");
    }

    [Fact]
    public void MergeAppSettings_GivenNoAnswer_LeavesKeyOutAndReportsIt()
    {
        // Arrange: a value still equal to its hint is not a real value
        var existing = Parse("""{ "AuthService": { "ClientId": "OAuth client id" } }""");

        // Act
        var result = AppSettingsMerger.Merge(Parse(Template), existing, reset: false, prompt: null);

        // Assert
        result.Missing.Should().Equal("AuthService:Host", "AuthService:ClientId");
        result.Settings["AuthService"]!.AsObject().Should().BeEmpty();
        result.Settings["Port"]!.GetValue<int>().Should().Be(5000);
    }

    [Fact]
    public void MergeAppSettings_GivenNothingAnsweredAndNoExistingSection_DoesNotCreateEmptySection()
    {
        // Act
        var result = AppSettingsMerger.Merge(Parse(Template), existing: null, reset: false, prompt: null);

        // Assert
        result.Settings.ContainsKey("AuthService").Should().BeFalse();
    }

    [Fact]
    public void MergeAppSettings_GivenReset_AsksAgainOfferingCurrentValueAndKeepsItOnBlankAnswer()
    {
        // Arrange
        var existing = Parse("""{ "AuthService": { "Host": "https://old/", "ClientId": "old-client" } }""");
        var offered = new Dictionary<string, string?>();

        // Act
        var result = AppSettingsMerger.Merge(Parse(Template), existing, reset: true, (key, _, current) => {
            offered[key] = current;
            return key == "AuthService:Host" ? "https://new/" : "";
        });

        // Assert
        offered.Should().BeEquivalentTo(new Dictionary<string, string?> {
            ["AuthService:Host"] = "https://old/",
            ["AuthService:ClientId"] = "old-client"
        });
        result.Settings["AuthService"]!["Host"]!.GetValue<string>().Should().Be("https://new/");
        result.Settings["AuthService"]!["ClientId"]!.GetValue<string>().Should().Be("old-client");
        result.Missing.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Database:Password", true)]
    [InlineData("AuthService:ClientSecret", true)]
    [InlineData("Slack:ApiKey", true)]
    [InlineData("AuthService:ClientSecretName", false)]
    [InlineData("AuthService:Host", false)]
    public void IsSensitiveAppSetting_GivenKey_ReturnsExpected(string key, bool expected)
    {
        // Act
        var result = AppSettingsMerger.IsSensitive(key);

        // Assert
        result.Should().Be(expected);
    }
}
