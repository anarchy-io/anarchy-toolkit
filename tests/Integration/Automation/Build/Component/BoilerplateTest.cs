using Anarchy.Toolkit.Automation.Build.Component.Tasks;
using AwesomeAssertions;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Tests.Integration.Automation.Build.Component;

public class BoilerplateTest
{
    [Fact]
    public void Create_GivenBuildProject_CreatesUsableScaffold()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-boilerplate-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "build"));
            Directory.CreateDirectory(Path.Combine(root, ".fallout"));
            File.WriteAllText(Path.Combine(root, ".fallout", "parameters.json"), "{\"Configuration\":\"Debug\"}");
            File.WriteAllText(Path.Combine(root, "build", "_build.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");

            BoilerplateTasks.Create(root, "MyApp");

            File.Exists(Path.Combine(root, "MyApp.sln"))
                .Should()
                .BeTrue();

            File.ReadAllText(Path.Combine(root, "MyApp.sln"))
                .Should()
                .Contain("Core.csproj")
                .And.Contain("_build.csproj");

            File.Exists(Path.Combine(root, "Makefile"))
                .Should()
                .BeTrue();

            File.Exists(Path.Combine(root, "scripts", "build.anarchy.sh"))
                .Should()
                .BeTrue();

            File.Exists(Path.Combine(root, "scripts", "build.fallout.sh"))
                .Should()
                .BeFalse();

            File.Exists(Path.Combine(root, "scripts", "make", "targets", "code-quality.mk"))
                .Should()
                .BeTrue();

            File.Exists(Path.Combine(root, "scripts", "console", "banner-compact.sh"))
                .Should()
                .BeTrue();

            File.Exists(Path.Combine(root, "scripts", "console", "banner-wide.sh"))
                .Should()
                .BeTrue();

            File.Exists(Path.Combine(root, "tests", ".gitkeep"))
                .Should()
                .BeTrue();

            File.ReadAllText(Path.Combine(root, ".gitignore"))
                .Should()
                .Contain(".fallout/temp");

            File.ReadAllText(Path.Combine(root, ".editorconfig"))
                .Should()
                .Contain("root = true");

            File.ReadAllText(Path.Combine(root, "global.json"))
                .Should()
                .Contain("10.0.0")
                .And.Contain("Microsoft.Testing.Platform");

            File.ReadAllText(Path.Combine(root, "nuget.config"))
                .Should()
                .Contain("api.nuget.org");

            File.ReadAllText(Path.Combine(root, "Directory.Build.props"))
                .Should()
                .Contain("**\\Queries\\**\\*.sql");

            var parameters = JsonNode.Parse(File.ReadAllText(Path.Combine(root, ".fallout", "parameters.json")))!;
            parameters["Solution"]!.GetValue<string>()
                .Should()
                .Be("MyApp.sln");

            parameters["Configuration"]!.GetValue<string>()
                .Should()
                .Be("Debug");

            File.Exists(Path.Combine(root, ".anarchy-toolkit.json"))
                .Should()
                .BeFalse();

            File.Exists(Path.Combine(root, "src", "Core", "Core.csproj"))
                .Should()
                .BeTrue();

            if (!OperatingSystem.IsWindows())
                File.GetUnixFileMode(Path.Combine(root, "scripts", "build.anarchy.sh"))
                    .Should()
                    .HaveFlag(UnixFileMode.UserExecute);

            var repeat = () => BoilerplateTasks.Create(root, "MyApp");
            repeat.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*overwrite*");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Create_GivenSolutionOnly_AddsBuildWithoutCoreOrOtherFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-boilerplate-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "build"));
            File.WriteAllText(Path.Combine(root, "build", "_build.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");

            BoilerplateTasks.Create(root, "OnlyBuild", new BoilerplateOptions {
                Items = "solution"
            });

            File.ReadAllText(Path.Combine(root, "OnlyBuild.sln"))
                .Should()
                .Contain("_build.csproj")
                .And.NotContain("Core.csproj");

            Directory.Exists(Path.Combine(root, "src"))
                .Should()
                .BeFalse();

            File.Exists(Path.Combine(root, "global.json"))
                .Should()
                .BeFalse();

            File.Exists(Path.Combine(root, "Directory.Build.props"))
                .Should()
                .BeFalse();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
