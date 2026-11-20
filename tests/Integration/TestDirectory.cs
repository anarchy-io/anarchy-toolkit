namespace Anarchy.Toolkit.Tests.Integration;

internal static class TestDirectory
{
    /// <summary>
    /// Deletes a temporary test directory, including the read-only files Git writes on Windows (its objects), which
    /// <see cref="Directory.Delete(string, bool)"/> refuses to remove there.
    /// </summary>
    public static void Delete(string path)
    {
        if (!Directory.Exists(path)) return;
        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            var attributes = File.GetAttributes(file);
            if (attributes.HasFlag(FileAttributes.ReadOnly)) File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
        }

        Directory.Delete(path, true);
    }
}
