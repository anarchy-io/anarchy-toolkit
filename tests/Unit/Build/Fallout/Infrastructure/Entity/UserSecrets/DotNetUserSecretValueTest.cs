using Anarchy.Toolkit.Build.Fallout.Infrastructure.Entity.UserSecrets;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Build.Fallout.Infrastructure.Entity.UserSecrets;

public class DotNetUserSecretValueTest
{
    [Fact]
    public void PlainValue_GivenString_LogsItAsIs()
    {
        // Act
        DotNetUserSecretValue value = "oracle-vanessa-main";

        // Assert
        value.Value.Should().Be("oracle-vanessa-main");
        value.LogValue.Should().Be("oracle-vanessa-main");
    }

    [Fact]
    public void SensitiveValue_GivenNotPromptedYet_IsEmpty()
    {
        // Act
        var value = new PromptedSensitiveDotNetUserSecretValue("Oracle password");

        // Assert
        value.Value.Should().BeEmpty();
        value.LogValue.Should().BeEmpty();
    }

    [Fact]
    public void ProjectUserSecrets_AcceptPlainAndPromptedValues()
    {
        // Act
        var project = new DotNetUserSecretsProject {
            UserSecrets = {
                { "OracleSettings:OracleHost", "oracle-vanessa-main" },
                { "OracleSettings:OraclePassword", new PromptedSensitiveDotNetUserSecretValue("Oracle password") },
            },
        };

        // Assert
        project.UserSecrets.Values.OfType<IPromptedUserSecretValue>().Should().ContainSingle();
    }
}
