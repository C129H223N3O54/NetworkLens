using System.Windows.Input;
using NetworkLens.Helpers;
using NetworkLens.Localization;
using NetworkLens.Models;
using NetworkLens.Services;
using NetworkLens.ViewModels;

namespace NetworkLens.Views;

public partial class ScanView : UserControl
{
    private ScanViewModel _vm = null!;
    private bool _wired;
    private readonly DeviceManager _deviceManager = new();

    private static LocalizationManager L => LocalizationManager.Instance;

    public ScanView()
    {
        InitializeComponent();
        Loaded += ScanView_Loaded;
    }

    // ── Called by MainWindow with the app-wide shared VM ──
    public void SetViewModel(ScanViewModel vm)
    {
        if (_wired) return;
        _vm = vm;
        WireViewModel();
    }

    private void ScanView_Loaded(object sender, RoutedEventArgs e)
    {
        // Fallback if MainWindow has not injected the shared VM yet
        if (!_wired && App.ScanVm != null)
            SetViewModel(App.ScanVm);
    }

    private void WireViewModel()
    {
        _wired = true;

        TxtSubnet.Text = _vm.Subnet;
        TxtSubnet.TextChanged += (_, _) => _vm.Subnet = TxtSubnet.Text;

        // Auto-detect the subnet in the background — enumerating all network
        // adapters can be slow (e.g. Hyper-V hosts) and must not delay app start.
        _ = DetectSubnetAsync(TxtSubnet.Text);

        DeviceGrid.ItemsSource = _vm.DevicesView;

        // Devices are only modified on the UI thread
        _vm.Devices.CollectionChanged += (_, _) =>
        {
            if (_vm.Devices.Count > 0 && ResultsPanel.Visibility != Visibility.Visible)
            {
                EmptyState.Visibility   = Visibility.Collapsed;
                ResultsPanel.Visibility = Visibility.Visible;
            }
            UpdateResultCount();
            UpdateMainWindowStatus();
        };

        _vm.PropertyChanged += Vm_PropertyChanged;
    }

    private async Task DetectSubnetAsync(string initialText)
    {
        try
        {
            var detected = await Task.Run(() => IpHelper.GetLocalSubnetCidr());
            // Don't overwrite what the user typed in the meantime
            if (!string.IsNullOrEmpty(detected) && TxtSubnet.Text == initialText)
                TxtSubnet.Text = detected;
        }
        catch (Exception ex)
        {
            Log.Warn("Subnet auto-detect", ex);
        }
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.InvokeAsync(() => Vm_PropertyChanged(sender, e));
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(ScanViewModel.IsScanning):
                BtnScan.IsEnabled  = _vm.CanScan;
                BtnStop.IsEnabled  = _vm.CanStop;
                ProgressPanel.Visibility = _vm.IsScanning ? Visibility.Visible : Visibility.Collapsed;

                if (!_vm.IsScanning && _vm.Devices.Count > 0)
                {
                    EmptyState.Visibility   = Visibility.Collapsed;
                    ResultsPanel.Visibility = Visibility.Visible;
                }
                break;

            case nameof(ScanViewModel.Progress):
                ScanProgress.Value   = _vm.Progress;
                ProgressPercent.Text = $"{_vm.Progress}%";
                break;

            case nameof(ScanViewModel.ProgressLabel):
                ProgressLabel.Text = _vm.ProgressLabel;
                break;

            case nameof(ScanViewModel.ProgressDetail):
                ProgressDetail.Text = _vm.ProgressDetail;
                break;

            case nameof(ScanViewModel.NewDeviceCount):
                NewDeviceBadge.Visibility = _vm.NewDeviceCount > 0 ? Visibility.Visible : Visibility.Collapsed;
                NewDeviceText.Text = string.Format(L.T("Scan_NewBadgeFmt"), _vm.NewDeviceCount);
                break;

            case nameof(ScanViewModel.StatusText):
                UpdateResultCount();
                UpdateMainWindowStatus();
                break;
        }
    }

    private void UpdateResultCount()
    {
        TxtResultCount.Text = string.Format(L.T("Stat_DevicesFound"), _vm.Devices.Count);
    }

    private void UpdateMainWindowStatus()
    {
        if (Window.GetWindow(this) is MainWindow mw)
            mw.UpdateStatusBar(_vm.StatusText, _vm.Devices.Count,
                _vm.IsScanning ? null : _vm.ScanDurationText, _vm.IsScanning);
    }

    // ── Scan Controls ──────────────────────────────
    private async void BtnScan_Click(object sender, RoutedEventArgs e)
    {
        if (!_wired) return;

        EmptyState.Visibility    = Visibility.Collapsed;
        ResultsPanel.Visibility  = Visibility.Collapsed;
        ProgressPanel.Visibility = Visibility.Visible;
        NewDeviceBadge.Visibility = Visibility.Collapsed;

        await _vm.StartScanAsync();

        ProgressPanel.Visibility = Visibility.Collapsed;
        bool hasDevices = _vm.Devices.Count > 0;
        EmptyState.Visibility   = hasDevices ? Visibility.Collapsed : Visibility.Visible;
        ResultsPanel.Visibility = hasDevices ? Visibility.Visible : Visibility.Collapsed;

        // Invalid input never starts a scan — still show the reason in the status bar.
        UpdateMainWindowStatus();
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e) => _vm?.StopScan();

    private void BtnAutoDetect_Click(object sender, RoutedEventArgs e)
    {
        var subnet = IpHelper.GetLocalSubnetCidr() ?? "192.168.1.0/24";
        TxtSubnet.Text = subnet;
        if (_wired) _vm.Subnet = subnet;
    }

    // ── Search ─────────────────────────────────────
    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_wired) _vm.SearchText = TxtSearch.Text;
    }

    // ── Export ─────────────────────────────────────
    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mw)
            mw.NavigateToReport();
    }

    // ── Grid ──────────────────────────────────────
    private void DeviceGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    // Select row on right-click and show programmatic context menu
    private void DeviceGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var row = FindVisualParent<DataGridRow>(e.OriginalSource as DependencyObject);
        if (row?.Item is not NetworkDevice device) return;

        DeviceGrid.SelectedItem = device;
        row.IsSelected = true;

        e.Handled = true;
        ShowContextMenu(device);
    }

    private void ShowContextMenu(NetworkDevice device)
    {
        var menu = new ContextMenu();
        // Use theme brushes so context menu follows Light/Dark mode
        menu.SetResourceReference(ContextMenu.BackgroundProperty, "BgCardBrush");
        menu.SetResourceReference(ContextMenu.BorderBrushProperty, "BorderBrush");

        void Add(string header, Action action)
        {
            var mi = new MenuItem { Header = header };
            mi.SetResourceReference(MenuItem.ForegroundProperty, "TextPrimaryBrush");
            mi.Click += (_, _) =>
            {
                try { action(); }
                catch (Exception ex) { Log.Warn($"Context menu '{header}'", ex); }
            };
            menu.Items.Add(mi);
        }

        void Sep() => menu.Items.Add(new Separator());

        var ip = device.IpAddress;

        Add(L.T("Ctx_PortScan"), () => { if (Window.GetWindow(this) is MainWindow mw) mw.NavigateToPortScan(device); });
        Add(L.T("Ctx_AddMonitor"), () => { if (Window.GetWindow(this) is MainWindow mw) mw.NavigateToMonitor(device); });
        Add(L.T("Ctx_ShowDetails"), () => ShowAll(device));
        Add(L.T("Ctx_ShowTtl"), () => _ = FetchAndShowTtlAsync(device));
        Sep();
        Add(L.T("Ctx_SetAlias"), () =>
        {
            var dlg = new AliasDialog(device.Alias ?? "", device.DisplayName) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
            {
                device.Alias = dlg.AliasText;
                _ = _deviceManager.SaveDeviceAsync(device);
            }
        });
        Add(L.T("Ctx_SetCategory"), () =>
        {
            var dlg = new CategoryDialog(device.Category) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
            {
                device.Category = dlg.SelectedCategory;
                _ = _deviceManager.SaveDeviceAsync(device);
            }
        });
        Add(L.T("Ctx_ToggleFav"), () =>
        {
            device.IsFavorite = !device.IsFavorite;
            _ = _deviceManager.SaveDeviceAsync(device);
        });
        Sep();
        Add(L.T("Ctx_OpenHttp"), () => ShellHelper.OpenUrl($"http://{ShellHelper.NormalizeIp(ip)}"));
        Add(L.T("Ctx_OpenHttps"), () => ShellHelper.OpenUrl($"https://{ShellHelper.NormalizeIp(ip)}"));
        Add(L.T("Ctx_OpenExplorer"), () => ShellHelper.OpenNetworkShare(ip));
        Add(L.T("Ctx_OpenFtp"), () => ShellHelper.OpenUrl($"ftp://{ShellHelper.NormalizeIp(ip)}"));
        Add(L.T("Ctx_OpenTelnet"), () => ShellHelper.RunConsoleTool("telnet", ip));
        Add(L.T("Ctx_OpenSsh"), () => ShellHelper.RunConsoleTool("ssh", ip));
        Add(L.T("Ctx_Ping"), () => ShellHelper.RunConsoleTool("ping", ip, "-t"));
        Add(L.T("Ctx_Traceroute"), () => ShellHelper.RunConsoleTool("tracert", ip));
        Add(L.T("Ctx_GeoLocate"), () => ShellHelper.OpenUrl($"https://www.iplocation.net/?query={ShellHelper.NormalizeIp(ip)}"));
        Sep();
        Add(L.T("Ctx_CopyIP"), () => Clipboard.SetText(device.IpAddress ?? ""));
        Add(L.T("Ctx_CopyMAC"), () => Clipboard.SetText(device.MacAddress ?? ""));

        menu.PlacementTarget = DeviceGrid;
        menu.IsOpen = true;
    }

    private static async Task FetchAndShowTtlAsync(NetworkDevice device)
    {
        string info;
        try
        {
            using var ping = new System.Net.NetworkInformation.Ping();
            var reply = await ping.SendPingAsync(device.IpAddress!, 1000);
            info = reply.Status == System.Net.NetworkInformation.IPStatus.Success
                ? string.Format(L.T("Scan_TtlInfoFmt"), reply.Options?.Ttl ?? 0)
                : L.T("Scan_NoReply");
        }
        catch (Exception ex)
        {
            Log.Warn("TTL ping", ex);
            info = L.T("Msg_TtlError");
        }
        ShowInfo("TTL", info, device.IpAddress);
    }

    private static void ShowAll(NetworkDevice d)
    {
        var info = $"{L.T("Det_IP")}:   {d.IpAddress}\n" +
                   $"{L.T("Det_Hostname")}:     {d.Hostname     ?? "—"}\n" +
                   $"{L.T("Det_Alias")}:        {d.Alias        ?? "—"}\n" +
                   $"{L.T("Det_MAC")}:  {d.MacAddress   ?? "—"}\n" +
                   $"{L.T("Scan_DetailManufacturer")}:   {d.Manufacturer ?? "—"}\n" +
                   $"{L.T("Det_Ping")}:         {(d.ResponseTime >= 0 ? d.ResponseTime + " ms" : "—")}\n" +
                   $"{L.T("Det_Category")}:    {L.T("Cat_" + d.Category)}\n" +
                   $"{L.T("Det_OpenPorts")}: {d.OpenPortCount}\n" +
                   $"{L.T("Det_LastSeen")}: {d.LastSeen:dd.MM.yyyy HH:mm:ss}\n" +
                   $"{L.T("Scan_DetailNewlyFound")}:  {(d.IsNew ? L.T("Scan_DetailYes") : L.T("Scan_DetailNo"))}";
        ShowInfo(L.T("Det_AllTitle"), info, d.IpAddress);
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T t) return t;
            child = System.Windows.Media.VisualTreeHelper.GetParent(child);
        }
        return null;
    }

    private static void ShowInfo(string title, string content, string? ip)
    {
        MessageBox.Show(content, $"{title}  —  {ip}",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
