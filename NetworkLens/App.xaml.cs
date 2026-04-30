// Build: 2026-04-30 19:07:36 (v1.3.5 bilingual reports)
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

        var newTheme = new ResourceDictionary { Source = uri };

        // Update each brush directly in Application.Resources
        // so DynamicResource bindings pick up the changes
        foreach (var key in newTheme.Keys)
        {
            Current.Resources[key] = newTheme[key];
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Register code page encodings (for OEM CP 850 used by netstat etc.)
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        Directory.CreateDirectory(AppDataPath);
        Directory.CreateDirectory(ScansPath);

        // Load saved theme preference
        bool dark = Views.SettingsView.LoadThemePreference();
        ApplyTheme(dark);

        // Detect / load language preference (auto-detect Windows lang on first start)
        Localization.LocalizationManager.Instance.Language =
            Localization.LocalizationManager.DetectInitialLanguage();

        DispatcherUnhandledException += (s, ex) =>
        {
            MessageBox.Show(
                $"{Localization.LocalizationManager.Instance.T("Err_Generic")}\n\n{ex.Exception.Message}",
                Localization.LocalizationManager.Instance.T("Err_Title"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            ex.Handled = true;
        };
    }
}
