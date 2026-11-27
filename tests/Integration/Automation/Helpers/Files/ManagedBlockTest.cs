using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Integration.Automation.Helpers.Files;

public class ManagedBlockTest
{
    [Fact]
    public void Create_GivenTrackedFileThatTheNewGitignoreIgnores_WarnsAboutIt()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-gitignore-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "src"));
            File.WriteAllText(Path.Combine(root, "src", "appsettings.Development.json"), "{}");
            if (LocalProcessTasks.Run(root, "git", ["init", "-q"]).ExitCode != 0) return;
            LocalProcessTasks.Require(root, "git", "add", "src/appsettings.Development.json");

            var messages = new List<string>();
            SolutionScaffolder.Create(root, null, new ScaffoldOptions { Items = ".gitignore", Log = messages.Add });

            messages.Should().ContainSingle(message => message.Contains("'src/appsettings.Development.json' is tracked"));
        }
        finally
        {
            TestDirectory.Delete(root);
        }
    }
}
