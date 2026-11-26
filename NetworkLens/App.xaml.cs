using NetworkLens.Helpers;
using NetworkLens.Localization;
using NetworkLens.ViewModels;

namespace NetworkLens;

public partial class App : Application
{
    public static string AppDataPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NetworkLens");

    public static string ScansPath   { get; } = Path.Combine(AppDataPath, "scans");
    public static string ConfigPath  { get; } = Path.Combine(AppDataPath, "config.json");
    public static string DevicesPath { get; } = Path.Combine(AppDataPath, "devices.json");

    // Created in OnStartup — after the language is known and the data folders exist.
    // (As static initializers they ran before OnStartup, so initial texts were always German.)
    public static ScanViewModel    ScanVm    { get; private set; } = null!;
    public static MonitorViewModel MonitorVm { get; private set; } = null!;

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
        RegisterGlobalExceptionHandlers();

        base.OnStartup(e);

        // Register code page encodings (for OEM CP 850 used by netstat etc.)
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        Directory.CreateDirectory(AppDataPath);
        Directory.CreateDirectory(ScansPath);

        // Load saved theme preference
        ApplyTheme(Views.SettingsView.LoadThemePreference());

        // Detect / load language preference (auto-detect Windows lang on first start)
        LocalizationManager.Instance.Language = LocalizationManager.DetectInitialLanguage();

        ScanVm    = new ScanViewModel();
        MonitorVm = new MonitorViewModel();
    }

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += (_, ex) =>
        {
            Log.Error("Unhandled UI exception", ex.Exception);
            MessageBox.Show(
                $"{LocalizationManager.Instance.T("Err_Generic")}\n\n{ex.Exception.Message}\n\n{Log.LogPath}",
                LocalizationManager.Instance.T("Err_Title"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            ex.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
        {
            if (ex.ExceptionObject is Exception exception)
                Log.Error("Unhandled exception (terminating)", exception);
        };

        TaskScheduler.UnobservedTaskException += (_, ex) =>
        {
            Log.Error("Unobserved task exception", ex.Exception);
            ex.SetObserved();
        };
    }
}
