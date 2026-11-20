using Anarchy.Toolkit.Automation.Build.Component.Tasks;
using AwesomeAssertions;
using Fallout.Common.Tools.DotNet;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Build.Component;

public class LocalAutomationTasksTest
{
    [Fact]
    public void DependencyUpgrade_RejectsInvalidVersionLockBeforeStartingTools()
    {
        ((Action)(() => DependencyUpgradeTasks.Run(".", "test.sln", "invalid", false, false, false,
                verbosity: DotNetVerbosity.quiet)))
            .Should()
            .Throw<ArgumentException>();
    }
}
