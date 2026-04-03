using System.Windows;
using System.Windows.Controls;
using NetworkLens.Helpers;
using NetworkLens.Models;
using NetworkLens.ViewModels;

namespace NetworkLens.Views;

public partial class SettingsView : UserControl
{
    private AppSettings _settings = new();
    private bool _loading = false;

    public SettingsView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        _settings = SettingsViewModel.LoadStatic();
        PopulateUi();

        // Show app data paths
        TxtPathConfig.Text  = App.ConfigPath;
        TxtPathDevices.Text = App.DevicesPath;
        TxtPathScans.Text   = App.ScansPath;
    }

    private void PopulateUi()
    {
        _loading = true;

        CmbScanTimeout.SelectedIndex = _settings.ScanTimeoutMs switch
        {
            500  => 0,
            2000 => 2,
            3000 => 3,
            _    => 1
        };
        CmbMaxThreads.SelectedIndex = _settings.MaxParallelThreads switch
        {
            50  => 0,
            200 => 2,
            500 => 3,
            _   => 1
        };
        CmbPortTimeout.SelectedIndex = _settings.PortScanTimeoutMs switch
        {
            200  => 0,
            1000 => 2,
            2000 => 3,
            _    => 1
        };
        CmbMonitorInterval.SelectedIndex = _settings.MonitorIntervalSeconds switch
        {
            1  => 0,
            5  => 1,
            30 => 3,
            60 => 4,
            0  => 5,   // Dauerhaft
            _  => 2    // 10s default
        };

        ChkAutoScan.IsChecked    = _settings.AutoScanOnStart;
        ChkNotifications.IsChecked = _settings.NotificationsEnabled;
        TxtOutputPath.Text       = _settings.ReportOutputPath;

        ChkAlertNewDevice.IsChecked = _settings.Alerts.NewDeviceAlert;
        ChkAlertOffline.IsChecked   = _settings.Alerts.DeviceOfflineAlert;
        ChkAlertOnline.IsChecked    = _settings.Alerts.DeviceOnlineAlert;
        ChkAlertNewPort.IsChecked   = _settings.Alerts.NewOpenPortAlert;
        ChkAlertSound.IsChecked     = _settings.Alerts.PlaySound;

        UpdateThemeButtons();
        _loading = false;
    }

    private void CollectSettings()
    {
        _settings.ScanTimeoutMs = CmbScanTimeout.SelectedIndex switch
        {
            0 => 500,
            2 => 2000,
            3 => 3000,
            _ => 1000
        };
        _settings.MaxParallelThreads = CmbMaxThreads.SelectedIndex switch
        {
            0 => 50,
            2 => 200,
            3 => 500,
            _ => 100
        };
        _settings.PortScanTimeoutMs = CmbPortTimeout.SelectedIndex switch
        {
            0 => 200,
            2 => 1000,
            3 => 2000,
            _ => 500
        };
        _settings.MonitorIntervalSeconds = CmbMonitorInterval.SelectedIndex switch
        {
            0 => 1,
            1 => 5,
            3 => 30,
            4 => 60,
            5 => 0,    // Dauerhaft
            _ => 10    // 10s default
        };

        _settings.AutoScanOnStart    = ChkAutoScan.IsChecked == true;
        _settings.NotificationsEnabled = ChkNotifications.IsChecked == true;
        _settings.ReportOutputPath   = TxtOutputPath.Text.Trim();

        _settings.Alerts.NewDeviceAlert    = ChkAlertNewDevice.IsChecked == true;
        _settings.Alerts.DeviceOfflineAlert = ChkAlertOffline.IsChecked == true;
        _settings.Alerts.DeviceOnlineAlert  = ChkAlertOnline.IsChecked == true;
        _settings.Alerts.NewOpenPortAlert   = ChkAlertNewPort.IsChecked == true;
        _settings.Alerts.PlaySound          = ChkAlertSound.IsChecked == true;
    }

    // ── Buttons ───────────────────────────────────
    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        CollectSettings();
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                _settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(App.ConfigPath, json);
            MessageBox.Show("Einstellungen gespeichert.", "NetworkLens",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Speichern:\n{ex.Message}", "Fehler",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        var r = MessageBox.Show("Alle Einstellungen auf Standard zurücksetzen?",
            "Zurücksetzen", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r != MessageBoxResult.Yes) return;

        _settings = new AppSettings();
        PopulateUi();
    }

    private void BtnBrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        // WPF-nativer Ordner-Dialog über SaveFileDialog-Trick
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title            = "Ausgabe-Ordner für Reports wählen",
            FileName         = "Ordner wählen",
            Filter           = "Ordner|*.thisDoesNotExist",
            CheckFileExists  = false,
            CheckPathExists  = true,
            InitialDirectory = TxtOutputPath.Text
        };
        if (dlg.ShowDialog() == true)
            TxtOutputPath.Text = System.IO.Path.GetDirectoryName(dlg.FileName) ?? TxtOutputPath.Text;
    }

    private void BtnOpenAppData_Click(object sender, RoutedEventArgs e)
        => ExportHelper.OpenFolder(App.AppDataPath);

    private void BtnThemeDark_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        App.ApplyTheme(dark: true);
        UpdateThemeButtons();
        SaveThemePreference(true);
    }

    private void BtnThemeLight_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        App.ApplyTheme(dark: false);
        UpdateThemeButtons();
        SaveThemePreference(false);
    }

    private void UpdateThemeButtons()
    {
        bool dark = App.IsDarkTheme;
        // Highlight active button
        BtnThemeDark.Background  = dark
            ? new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x00, 0xB4, 0xD8))
            : new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x22, 0x26, 0x2C));
        BtnThemeLight.Background = dark
            ? new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x22, 0x26, 0x2C))
            : new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x00, 0x77, 0xA8));
    }

    private static void SaveThemePreference(bool dark)
    {
        try
        {
            var prefs = new System.Collections.Generic.Dictionary<string, object>
                { ["darkTheme"] = dark };
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(App.AppDataPath, "theme.json"),
                System.Text.Json.JsonSerializer.Serialize(prefs));
        }
        catch { }
    }

    public static bool LoadThemePreference()
    {
        try
        {
            var path = System.IO.Path.Combine(App.AppDataPath, "theme.json");
            if (!System.IO.File.Exists(path)) return true; // default dark
            var json = System.IO.File.ReadAllText(path);
            var doc  = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("darkTheme").GetBoolean();
        }
        catch { return true; }
    }

    private void BtnGitHub_Click(object sender, RoutedEventArgs e)
        => ExportHelper.OpenUrl("https://github.com/C129H223N3O54/NetworkLens");

    private void BtnReleases_Click(object sender, RoutedEventArgs e)
        => ExportHelper.OpenUrl("https://github.com/C129H223N3O54/NetworkLens/releases");
}
