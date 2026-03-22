using System.Security.Principal;
using System.Windows;
using System.Windows.Input;
using NetworkLens.Models;
using NetworkLens.ViewModels;

namespace NetworkLens;

public partial class MainWindow : Window
{
    private Button? _activeNavButton;
    private Dictionary<Button, FrameworkElement> _navMap = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        SetupNavigation();
        CheckAdminStatus();
        SetActiveNav(NavScan);

        // Wire shared ViewModels into views
        ScanViewPage.SetViewModel(App.ScanVm);
        MonitorViewPage.SetViewModel(App.MonitorVm);

        // Auto-scan on startup if configured
        var settings = ViewModels.SettingsViewModel.LoadStatic();
        if (settings.AutoScanOnStart)
        {
            await Task.Delay(400); // let UI settle
            await App.ScanVm.StartScanAsync();
        }
    }

    // ══════════════════════════════════════════════
    // CROSS-VIEW NAVIGATION HELPERS
    // Called by child views to navigate & pass data
    // ══════════════════════════════════════════════

    public void NavigateToPortScan(NetworkDevice device)
    {
        PortScanViewPage.SetTarget(device.IpAddress ?? "");
        SetActiveNav(NavPorts);
    }

    public void NavigateToMonitor(NetworkDevice device)
    {
        MonitorViewPage.AddDeviceExternal(device);
        SetActiveNav(NavMonitor);
    }

    public void NavigateToReport()
    {
        // Pass the latest scan result to the report view
        if (App.ScanVm.LastScanResult != null)
            ReportViewPage.SetCurrentScan(App.ScanVm.LastScanResult);
        SetActiveNav(NavReport);
    }

    // ══════════════════════════════════════════════
    // NAVIGATION
    // ══════════════════════════════════════════════

    private void SetupNavigation()
    {
        _navMap[NavScan]     = ScanViewPage;
        _navMap[NavPorts]    = PortScanViewPage;
        _navMap[NavInfo]     = NetworkInfoViewPage;
        _navMap[NavMonitor]  = MonitorViewPage;
        _navMap[NavReport]   = ReportViewPage;
        _navMap[NavSettings] = SettingsViewPage;
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            // When navigating to Report, always pass latest scan
            if (btn == NavReport && App.ScanVm.LastScanResult != null)
                ReportViewPage.SetCurrentScan(App.ScanVm.LastScanResult);

            SetActiveNav(btn);
        }
    }

    public void SetActiveNav(Button target)
    {
        if (_activeNavButton != null)
            _activeNavButton.Tag = null;

        foreach (var view in _navMap.Values)
            view.Visibility = Visibility.Collapsed;

        _activeNavButton = target;
        target.Tag = "Active";

        if (_navMap.TryGetValue(target, out var page))
            page.Visibility = Visibility.Visible;
    }

    // Update nav button icons to reflect active state
    private void UpdateNavIcons()
    {
        var icons = new Dictionary<Button, string>
        {
            [NavScan]     = "⬡",
            [NavPorts]    = "⬡",
            [NavInfo]     = "⬡",
            [NavMonitor]  = "⬡",
            [NavReport]   = "⬡",
            [NavSettings] = "⬡"
        };
        // Active button uses accent-colored icon (handled via Tag trigger in XAML)
    }

    // ══════════════════════════════════════════════
    // ADMIN CHECK
    // ══════════════════════════════════════════════

    private void CheckAdminStatus()
    {
        bool isAdmin = IsRunningAsAdmin();
        AdminBadge.Visibility   = isAdmin ? Visibility.Visible   : Visibility.Collapsed;
        NoAdminBadge.Visibility = isAdmin ? Visibility.Collapsed : Visibility.Visible;
        AdminHintPanel.Visibility = isAdmin ? Visibility.Collapsed : Visibility.Visible;
    }

    private static bool IsRunningAsAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    private void BtnRestartAdmin_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName,
                UseShellExecute = true,
                Verb = "runas"
            };
            System.Diagnostics.Process.Start(psi);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Konnte nicht als Admin neu starten:\n{ex.Message}",
                "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ══════════════════════════════════════════════
    // WINDOW CHROME
    // ══════════════════════════════════════════════

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) ToggleMaximize();
        else DragMove();
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        => ToggleMaximize();

    private void BtnClose_Click(object sender, RoutedEventArgs e)
        => Application.Current.Shutdown();

    private void ToggleMaximize()
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    // ══════════════════════════════════════════════
    // STATUS BAR (called by child views)
    // ══════════════════════════════════════════════

    public void UpdateStatusBar(string status, int deviceCount = 0,
        string? duration = null, bool scanning = false)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text  = status;
            DeviceCount.Text = deviceCount > 0 ? $"{deviceCount} Geräte" : "0 Geräte";

            StatusIndicator.Fill = scanning
                ? FindResource("AccentBrush") as System.Windows.Media.Brush
                : deviceCount > 0
                    ? FindResource("StatusOnlineBrush") as System.Windows.Media.Brush
                    : FindResource("TextMutedBrush") as System.Windows.Media.Brush;

            if (duration != null)
            {
                ScanDuration.Text       = $"Scan: {duration}";
                ScanDuration.Visibility = Visibility.Visible;
            }
        });
    }
}
