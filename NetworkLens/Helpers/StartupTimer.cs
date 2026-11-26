using System.Diagnostics;

namespace NetworkLens.Helpers;

/// <summary>
/// Records how long each startup phase takes, measured from the moment Windows
/// created the process (so .NET runtime start-up and single-file extraction are
/// included). Writes one line to networklens.log per start, e.g.
///   Startup: OnStartup=410ms, Services=455ms, MainWindow=780ms, FirstFrame=1020ms
/// Start with "--startup-benchmark" to exit automatically after the first frame.
/// </summary>
internal static class StartupTimer
{
    private static readonly List<string> Marks = new();
    private static DateTime? _processStart;
    private static bool _completed;

    public static bool BenchmarkMode { get; set; }

    private static double ElapsedMs
    {
        get
        {
            try
            {
                _processStart ??= Process.GetCurrentProcess().StartTime;
                return (DateTime.Now - _processStart.Value).TotalMilliseconds;
            }
            catch
            {
                return -1;
            }
        }
    }

    public static void Mark(string phase)
    {
        if (_completed) return;
        Marks.Add($"{phase}={ElapsedMs:F0}ms");
    }

    /// <summary>Called once the main window rendered its first frame.</summary>
    public static void Complete()
    {
        if (_completed) return;
        Mark("FirstFrame");
        _completed = true;
        Log.Info("Startup: " + string.Join(", ", Marks));
    }
}
