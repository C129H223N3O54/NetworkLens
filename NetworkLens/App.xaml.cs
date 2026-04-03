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

    public static ScanViewModel    ScanVm    { get; } = new();
    public static MonitorViewModel MonitorVm { get; } = new();

    // ── Theme ─────────────────────────────────────
    private static bool _isDark = true;
    public static bool IsDarkTheme => _isDark;

    public static void ApplyTheme(bool dark)
    {
        _isDark = dark;
        var uri = new Uri(dark
            ? "Resources/DarkTheme.xaml"
            : "Resources/LightTheme.xaml",
            UriKind.Relative);

        var dict = Current.Resources.MergedDictionaries;
        dict.Clear();
        dict.Add(new ResourceDictionary { Source = uri });
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Directory.CreateDirectory(AppDataPath);
        Directory.CreateDirectory(ScansPath);

        // Load saved theme preference
        bool dark = Views.SettingsView.LoadThemePreference();
        ApplyTheme(dark);

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
