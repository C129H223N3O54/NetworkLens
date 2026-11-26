namespace NetworkLens.Helpers;

/// <summary>
/// Writes files via a temp file + rename so a crash or power loss
/// mid-write can never leave a truncated / corrupt JSON file behind.
/// </summary>
internal static class AtomicFile
{
    public static async Task WriteAllTextAsync(string path, string content)
    {
        var tmp = PrepareTemp(path);
        await File.WriteAllTextAsync(tmp, content).ConfigureAwait(false);
        File.Move(tmp, path, overwrite: true);
    }

    public static void WriteAllText(string path, string content)
    {
        var tmp = PrepareTemp(path);
        File.WriteAllText(tmp, content);
        File.Move(tmp, path, overwrite: true);
    }

    private static string PrepareTemp(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        return path + ".tmp";
    }
}
