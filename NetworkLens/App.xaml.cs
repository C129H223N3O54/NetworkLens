using System.IO;
using System.Windows;
using NetworkLens.ViewModels;

namespace NetworkLens;

public partial class App : Application
{
    public static string AppDataPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NetworkLens");

    public static string ScansPath   { get; } = Path.Combine(AppDataPath, "scans");
    public static string ConfigPath  { get; } = Path.Combine(AppDataPath, "config.json");
    public static string DevicesPath { get; } = Path.Combine(AppDataPath, "devices.json");

    // ── App-wide shared ViewModels ─────────────────
    // Shared so ScanView, ReportView, MonitorView and PortScanView
    // can all access the same scan state without re-scanning.
    public static ScanViewModel    ScanVm    { get; } = new();
    public static MonitorViewModel MonitorVm { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Ensure app directories exist
        Directory.CreateDirectory(AppDataPath);
        Directory.CreateDirectory(ScansPath);

        // Global unhandled exception handler
        DispatcherUnhandledException += (s, ex) =>
        {
            MessageBox.Show(
                $"Ein unerwarteter Fehler ist aufgetreten:\n\n{ex.Exception.Message}",
                "NetworkLens — Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            ex.Handled = true;
        };
    }
}
