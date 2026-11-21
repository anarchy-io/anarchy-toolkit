using Anarchy.Toolkit.Automation.Core.Service;
using AwesomeAssertions;
using static Anarchy.Toolkit.Automation.Core.Service.ReleaseVersionTasks;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Core.Service;

public class ReleaseNotesTasksTest
{
    [Fact]
    public void Plan_GivenMissingReleaseNotes_CreatesDedicatedFileWithoutReadingReadme()
    {
        var path = Path.Combine(Path.GetTempPath(), "Package", "Package.csproj");
        var project = new Project(path, "Package", "1.0.1", [], "");
        var messages = new Dictionary<string, IReadOnlyList<string>> { [path] = ["fix: problem"] };
        var requested = new List<string>();

        var edits = ReleaseNotesTasks.Plan([project], messages, file => {
            requested.Add(file);
            return null;
        });

        var notes = Path.Combine(Path.GetDirectoryName(path)!, "RELEASE-NOTES.md");
        requested.Should().Equal(notes);
        edits.Should().ContainSingle();
        edits[notes].Should().Be("## Releases\n\n### 1.0.1\n\n- fix: problem\n");
    }

    [Fact]
    public void Plan_GivenExistingVersion_PreservesDedicatedFile()
    {
        var path = Path.Combine(Path.GetTempPath(), "Package", "Package.csproj");
        var project = new Project(path, "Package", "1.0.1", [], "");
        var messages = new Dictionary<string, IReadOnlyList<string>> { [path] = ["fix: problem"] };

        var edits = ReleaseNotesTasks.Plan([project], messages, _ => "## Releases\r\n\r\n### 1.0.1\r\n\r\n- Existing\r\n");

        edits.Should().BeEmpty();
    }

    [Fact]
    public void Plan_GivenExistingHistory_AddsVersionAndPreservesLineEndings()
    {
        var path = Path.Combine(Path.GetTempPath(), "Package", "Package.csproj");
        var project = new Project(path, "Package", "1.0.1", [], "");
        var messages = new Dictionary<string, IReadOnlyList<string>> { [path] = ["fix: problem"] };
        var notes = Path.Combine(Path.GetDirectoryName(path)!, "RELEASE-NOTES.md");

        var edits = ReleaseNotesTasks.Plan([project], messages, _ => "## Releases\r\n\r\n### 1.0.0\r\n\r\n- Old\r\n");

        edits[notes].Should().Be("## Releases\r\n\r\n### 1.0.1\r\n\r\n- fix: problem\r\n\r\n### 1.0.0\r\n\r\n- Old\r\n");
    }
}
