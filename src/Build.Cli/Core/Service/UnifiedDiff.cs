using System.Text;

namespace Anarchy.Toolkit.Build.Cli.Core.Service;

/// <summary>
/// Creates unified diffs (as <c>diff -u</c> prints them) between two texts, line by line.
/// </summary>
public static class UnifiedDiff
{
    private const int Context = 3;

    /// <summary>
    /// The unified diff that turns <paramref name="before"/> into <paramref name="after"/>.
    /// </summary>
    /// <param name="path">The file name shown in the <c>---</c> and <c>+++</c> headers.</param>
    /// <param name="before">The current text.</param>
    /// <param name="after">The new text.</param>
    /// <returns>The diff, or an empty string when the texts have the same lines.</returns>
    public static string Create(string path, string before, string after)
    {
        var a = Lines(before);
        var b = Lines(after);
        var edits = Edits(a, b);
        if (edits.All(edit => edit.Kind == ' ')) return "";

        var output = new StringBuilder().Append("--- a/").Append(path).Append('\n').Append("+++ b/").Append(path).Append('\n');
        var ranges = new List<(int Start, int End)>();
        for (var i = 0; i < edits.Count; i++)
        {
            if (edits[i].Kind == ' ') continue;
            var (start, end) = (Math.Max(0, i - Context), Math.Min(edits.Count, i + Context + 1));
            if (ranges.Count > 0 && start <= ranges[^1].End) ranges[^1] = (ranges[^1].Start, end);
            else ranges.Add((start, end));
        }
        foreach (var (start, end) in ranges)
        {
            var hunk = edits.GetRange(start, end - start);
            var oldStart = hunk.FirstOrDefault(edit => edit.Kind != '+').OldLine;
            var newStart = hunk.FirstOrDefault(edit => edit.Kind != '-').NewLine;
            var oldCount = hunk.Count(edit => edit.Kind != '+');
            var newCount = hunk.Count(edit => edit.Kind != '-');
            if (oldCount == 0) oldStart = hunk[0].OldLine - 1;
            if (newCount == 0) newStart = hunk[0].NewLine - 1;
            output.Append($"@@ -{oldStart},{oldCount} +{newStart},{newCount} @@\n");
            foreach (var edit in hunk) output.Append(edit.Kind).Append(edit.Text).Append('\n');
        }
        return output.ToString();
    }

    private readonly record struct Edit(char Kind, string Text, int OldLine, int NewLine);

    private static List<Edit> Edits(string[] a, string[] b)
    {
        var lengths = new int[a.Length + 1, b.Length + 1];
        for (var i = a.Length - 1; i >= 0; i--)
            for (var j = b.Length - 1; j >= 0; j--)
                lengths[i, j] = a[i] == b[j] ? lengths[i + 1, j + 1] + 1 : Math.Max(lengths[i + 1, j], lengths[i, j + 1]);

        var edits = new List<Edit>(a.Length + b.Length);
        int x = 0, y = 0;
        while (x < a.Length || y < b.Length)
        {
            if (x < a.Length && y < b.Length && a[x] == b[y])
                edits.Add(new Edit(' ', a[x++], x, ++y));
            else if (x < a.Length && (y == b.Length || lengths[x + 1, y] >= lengths[x, y + 1]))
                edits.Add(new Edit('-', a[x++], x, y + 1));
            else
                edits.Add(new Edit('+', b[y++], x + 1, y));
        }
        return edits;
    }

    private static string[] Lines(string text)
    {
        var lines = text.ReplaceLineEndings("\n").Split('\n');
        return lines.Length > 0 && lines[^1].Length == 0 ? lines[..^1] : lines;
    }
}
