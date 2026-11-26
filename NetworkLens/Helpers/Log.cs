namespace NetworkLens.Helpers;

/// <summary>
/// Minimal file logger (%AppData%\NetworkLens\networklens.log).
/// Never throws — logging must not break the app.
/// The file is rotated once it exceeds ~1 MB (one backup: networklens.log.1).
/// </summary>
internal static class Log
{
    private const long MaxBytes = 1_000_000;
    private static readonly object _lock = new();

    public static string LogPath => Path.Combine(App.AppDataPath, "networklens.log");

    public static void Error(string context, Exception ex) => Write("ERROR", $"{context}: {ex}");

    public static void Warn(string context, Exception ex) =>
        Write("WARN ", $"{context}: {ex.GetType().Name}: {ex.Message}");

    public static void Info(string message) => Write("INFO ", message);

    private static void Write(string level, string message)
    {
        try
        {
            lock (_lock)
            {
                Directory.CreateDirectory(App.AppDataPath);
                var file = new FileInfo(LogPath);
                if (file.Exists && file.Length > MaxBytes)
                    File.Move(LogPath, LogPath + ".1", overwrite: true);

                File.AppendAllText(LogPath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging is best effort.
        }
    }
}
