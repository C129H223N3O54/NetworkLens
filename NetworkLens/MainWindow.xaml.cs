using System.Security.Principal;
using System.Windows;
using System.Windows.Input;
using NetworkLens.Helpers;
using NetworkLens.Models;
using NetworkLens.Services;

namespace NetworkLens;

public partial class MainWindow : Window
{
    private Button? _activeNavButton;

    // Host element per navigation button + a factory that creates the page on first use.
    private readonly Dictionary<Button, FrameworkElement> _hosts = new();
    private readonly Dictionary<Button, Action> _ensurePage = new();

    // Pages other than the scan view are created lazily (on first navigation),
    // which keeps app start-up down to building a single view.
    private Views.PortScanView? _portScanPage;
    private Views.NetworkInfoView? _networkInfoPage;
    private Views.MonitorView? _monitorPage;
    private Views.ReportView? _reportPage;
    private Views.SettingsView? _settingsPage;

    private Views.PortScanView PortScanPage => _portScanPage ??= Mount(new Views.PortScanView(), PortScanHost);
    private Views.NetworkInfoView NetworkInfoPage => _networkInfoPage ??= Mount(new Views.NetworkInfoView(), NetworkInfoHost);
    private Views.MonitorView MonitorPage => _monitorPage ??= Mount(new Views.MonitorView(), MonitorHost);
    private Views.ReportView ReportPage => _reportPage ??= Mount(new Views.ReportView(), ReportHost);
    private Views.SettingsView SettingsPage => _settingsPage ??= Mount(new Views.SettingsView(), SettingsHost);

    private static T Mount<T>(T page, ContentControl host) where T : FrameworkElement
    {
        host.Content = page;
        return page;
    }

    public MainWindow()
    {
        InitializeComponent();
        StartupTimer.Mark("MainWindow");

        TxtSidebarVersion.Text = $"Sideforge · NetworkLens {App.VersionText}";

        Loaded += MainWindow_Loaded;
        ContentRendered += MainWindow_ContentRendered;

        // Refresh status bar text when language changes
        Localization.LocalizationManager.Instance.LanguageChanged += (_, _) =>
            Dispatcher.Invoke(() => UpdateStatusBar(_lastStatus, _lastDeviceCount));
    }

    private string _lastStatus = "";
    private int _lastDeviceCount = 0;

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        SetupNavigation();
        CheckAdminStatus();
        SetActiveNav(NavScan);

        // Wire the shared ViewModel into the (always present) scan view
        ScanViewPage.SetViewModel(App.ScanVm);

        // Auto-scan on startup if configured
        if (SettingsStore.Current.AutoScanOnStart && !StartupTimer.BenchmarkMode)
        {
            await Task.Delay(400); // let UI settle
            await App.ScanVm.StartScanAsync();
        }
    }

    private void MainWindow_ContentRendered(object? sender, EventArgs e)
    {
        StartupTimer.Complete();
        if (StartupTimer.BenchmarkMode)
            Application.Current.Shutdown();
    }

    // ══════════════════════════════════════════════
    // CROSS-VIEW NAVIGATION HELPERS
    // Called by child views to navigate & pass data
    // ══════════════════════════════════════════════

    public void NavigateToPortScan(NetworkDevice device)
    {
        PortScanPage.SetTarget(device.IpAddress ?? "");
        SetActiveNav(NavPorts);
    }

    public void NavigateToMonitor(NetworkDevice device)
    {
        // The monitor view binds to App.MonitorVm when it is created/loaded
        App.MonitorVm.AddDevice(device);
        SetActiveNav(NavMonitor);
    }

    public void NavigateToReport()
    {
        // Pass the latest scan result to the report view
        if (App.ScanVm.LastScanResult != null)
            ReportPage.SetCurrentScan(App.ScanVm.LastScanResult);
        SetActiveNav(NavReport);
    }

    // ══════════════════════════════════════════════
    // NAVIGATION
    // ══════════════════════════════════════════════

    private void SetupNavigation()
    {
        _hosts[NavScan]     = ScanViewPage;
        _hosts[NavPorts]    = PortScanHost;
        _hosts[NavInfo]     = NetworkInfoHost;
        _hosts[NavMonitor]  = MonitorHost;
        _hosts[NavReport]   = ReportHost;
        _hosts[NavSettings] = SettingsHost;

        _ensurePage[NavPorts]    = () => _ = PortScanPage;
        _ensurePage[NavInfo]     = () => _ = NetworkInfoPage;
        _ensurePage[NavMonitor]  = () => _ = MonitorPage;
        _ensurePage[NavReport]   = () => _ = ReportPage;
        _ensurePage[NavSettings] = () => _ = SettingsPage;
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            // When navigating to Report, always pass latest scan
            if (btn == NavReport && App.ScanVm.LastScanResult != null)
                ReportPage.SetCurrentScan(App.ScanVm.LastScanResult);

            SetActiveNav(btn);
        }
    }

    public void SetActiveNav(Button target)
    {
        if (_activeNavButton != null)
            _activeNavButton.Tag = null;

        foreach (var host in _hosts.Values)
            host.Visibility = Visibility.Collapsed;

        _activeNavButton = target;
        target.Tag = "Active";

        if (_ensurePage.TryGetValue(target, out var ensure))
            ensure();

        if (_hosts.TryGetValue(target, out var page))
            page.Visibility = Visibility.Visible;
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
            var exe = Environment.ProcessPath
                      ?? throw new InvalidOperationException("Process path unavailable");
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = true,
                Verb = "runas"
            };
            using (System.Diagnostics.Process.Start(psi)) { }
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            // Includes the user cancelling the UAC prompt (Win32Exception 1223)
            Log.Warn("Restart as admin", ex);
            var L = Localization.LocalizationManager.Instance;
            MessageBox.Show(string.Format(L.T("Msg_AdminRestartFail"), ex.Message),
                L.T("Msg_Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
        _lastStatus      = status;
        _lastDeviceCount = deviceCount;

        void Apply()
        {
            StatusText.Text  = status;
            DeviceCount.Text = $"{deviceCount} {Localization.LocalizationManager.Instance.T("Footer_Devices")}";

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
        }

        if (Dispatcher.CheckAccess()) Apply();
        else Dispatcher.InvokeAsync(Apply);
    }
}
