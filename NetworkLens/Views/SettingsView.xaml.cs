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
            5  => 0,
            30 => 2,
            60 => 3,
            _  => 1
        };

        ChkAutoScan.IsChecked    = _settings.AutoScanOnStart;
        ChkNotifications.IsChecked = _settings.NotificationsEnabled;
        TxtOutputPath.Text       = _settings.ReportOutputPath;

        ChkAlertNewDevice.IsChecked = _settings.Alerts.NewDeviceAlert;
        ChkAlertOffline.IsChecked   = _settings.Alerts.DeviceOfflineAlert;
        ChkAlertOnline.IsChecked    = _settings.Alerts.DeviceOnlineAlert;
        ChkAlertNewPort.IsChecked   = _settings.Alerts.NewOpenPortAlert;
        ChkAlertSound.IsChecked     = _settings.Alerts.PlaySound;

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
            0 => 5,
            2 => 30,
            3 => 60,
            _ => 10
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
        using var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Ausgabe-Ordner für Reports auswählen",
            SelectedPath = TxtOutputPath.Text
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            TxtOutputPath.Text = dlg.SelectedPath;
    }

    private void BtnOpenAppData_Click(object sender, RoutedEventArgs e)
        => ExportHelper.OpenFolder(App.AppDataPath);

    private void BtnGitHub_Click(object sender, RoutedEventArgs e)
        => ExportHelper.OpenUrl("https://github.com/YOUR_USERNAME/NetworkLens");

    private void BtnReleases_Click(object sender, RoutedEventArgs e)
        => ExportHelper.OpenUrl("https://github.com/YOUR_USERNAME/NetworkLens/releases");
}
