using Anarchy.Toolkit.Web.MockServer.Service;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Web.MockServer.Service;

public class ExpectationLoaderTest
{
    [Fact]
    public void SplitGlob_GivenGlobAtFilesystemRoot_PreservesRoot()
    {
        var root = Path.GetPathRoot(Path.GetTempPath())!;
        var glob = Path.Combine(root, "*.json");

        var (directory, pattern) = ExpectationLoader.SplitGlob(glob);

        directory.Should().Be(root);
        pattern.Should().Be("*.json");
    }

    [Fact]
    public void Resolve_GivenAbsoluteGlob_FindsNestedExpectation()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var nested = Path.Combine(directory, "nested");
        Directory.CreateDirectory(nested);
        var file = Path.Combine(nested, "expectation.json");
        try
        {
            File.WriteAllText(file, "{}");
            var (files, missing) = ExpectationLoader.Resolve([Path.Combine(directory, "**", "*.json")]);

            files.Should().Equal(file);
            missing.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
