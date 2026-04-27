using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NetworkLens.Models;
using NetworkLens.ViewModels;

namespace NetworkLens.Views;

public partial class ScanView : UserControl
{
    private ScanViewModel _vm = null!;
    private bool _vmSet = false;

    public ScanView()
    {
        InitializeComponent();
        Loaded += ScanView_Loaded;
    }

    // ── Called by MainWindow with the app-wide shared VM ──
    public void SetViewModel(ScanViewModel vm)
    {
        _vm = vm;
        _vmSet = true;
        WireViewModel();
    }

    private void ScanView_Loaded(object sender, RoutedEventArgs e)
    {
        // Fallback: create own VM if MainWindow hasn't injected one yet
        if (!_vmSet)
        {
            _vm = new ScanViewModel();
            WireViewModel();
        }
    }

    private void WireViewModel()
    {
        // Auto-detect subnet on startup
        var detected = Services.IpHelper.GetLocalSubnetCidr();
        if (!string.IsNullOrEmpty(detected))
        {
            TxtSubnet.Text  = detected;
            _vm.Subnet      = detected;
        }
        else
        {
            TxtSubnet.Text = _vm.Subnet;
        }

        // Subnet textbox → VM
        TxtSubnet.TextChanged += (_, _) => _vm.Subnet = TxtSubnet.Text;

        // Bind device grid to VM's filtered view
        DeviceGrid.ItemsSource = _vm.DevicesView;

        // Show results immediately when first device found
        _vm.Devices.CollectionChanged += (_, _) =>
        {
            Dispatcher.Invoke(() =>
            {
                if (_vm.Devices.Count > 0 && ResultsPanel.Visibility != Visibility.Visible)
                {
                    EmptyState.Visibility   = Visibility.Collapsed;
                    ResultsPanel.Visibility = Visibility.Visible;
                }
                UpdateResultCount();
                UpdateMainWindowStatus();
            });
        };

        _vm.PropertyChanged += Vm_PropertyChanged;
    }

    private void Vm_PropertyChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(ScanViewModel.IsScanning):
                    BtnScan.IsEnabled  = _vm.CanScan;
                    BtnStop.IsEnabled  = _vm.CanStop;
                    ProgressPanel.Visibility = _vm.IsScanning
                        ? Visibility.Visible : Visibility.Collapsed;

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
                    if (_vm.NewDeviceCount > 0)
                    {
                        NewDeviceBadge.Visibility = Visibility.Visible;
                        NewDeviceText.Text = $"{_vm.NewDeviceCount} neue";
                    }
                    break;

                case nameof(ScanViewModel.StatusText):
                    UpdateResultCount();
                    UpdateMainWindowStatus();
                    break;
            }
        });
    }

    private void UpdateResultCount()
    {
        TxtResultCount.Text = $"{_vm.Devices.Count} Geräte gefunden";
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
        if (_vm == null) return;

        EmptyState.Visibility   = Visibility.Collapsed;
        ResultsPanel.Visibility = Visibility.Collapsed;
        ProgressPanel.Visibility = Visibility.Visible;
        NewDeviceBadge.Visibility = Visibility.Collapsed;

        await _vm.StartScanAsync();

        ProgressPanel.Visibility = Visibility.Collapsed;

        if (_vm.Devices.Count > 0)
        {
            EmptyState.Visibility   = Visibility.Collapsed;
            ResultsPanel.Visibility = Visibility.Visible;
        }
        else
        {
            EmptyState.Visibility   = Visibility.Visible;
            ResultsPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e) => _vm?.StopScan();

    private void BtnAutoDetect_Click(object sender, RoutedEventArgs e)
    {
        var subnet = Services.IpHelper.GetLocalSubnetCidr() ?? "192.168.1.0/24";
        TxtSubnet.Text = subnet;
        if (_vm != null) _vm.Subnet = subnet;
    }

    // ── Search ─────────────────────────────────────
    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_vm != null) _vm.SearchText = TxtSearch.Text;
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
            mi.Click += (_, _) => action();
            menu.Items.Add(mi);
        }

        void Sep() => menu.Items.Add(new Separator());

        Add("Port-Scan",                    () => { if (Window.GetWindow(this) is MainWindow mw) mw.NavigateToPortScan(device); });
        Add("In Monitor aufnehmen",         () => { if (Window.GetWindow(this) is MainWindow mw) mw.NavigateToMonitor(device); });
        Sep();
        Add("Alias setzen...",              async () => {
            var dlg = new AliasDialog(device.Alias ?? "", device.DisplayName) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true) { device.Alias = dlg.AliasText; await new Services.DeviceManager().SaveDeviceAsync(device); }
        });
        Add("Kategorie...",                 () => {
            var dlg = new CategoryDialog(device.Category) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true) { device.Category = dlg.SelectedCategory; _ = new Services.DeviceManager().SaveDeviceAsync(device); }
        });
        Add("Favorit umschalten",           async () => { device.IsFavorite = !device.IsFavorite; await new Services.DeviceManager().SaveDeviceAsync(device); });
        Sep();
        // ── Öffnen mit ──
        Add("HTTP im Browser öffnen",       () => OpenUrl($"http://{device.IpAddress}"));
        Add("HTTPS im Browser öffnen",      () => OpenUrl($"https://{device.IpAddress}"));
        Add("Im Explorer öffnen (Netzwerk)",() => OpenExplorer(device.IpAddress));
        Add("FTP öffnen",                   () => OpenUrl($"ftp://{device.IpAddress}"));
        Add("Telnet öffnen",                () => OpenTerminal($"telnet {device.IpAddress}"));
        Add("SSH öffnen",                   () => OpenTerminal($"ssh {device.IpAddress}"));
        Add("Ping (Terminal)",              () => OpenTerminal($"ping {device.IpAddress} -t"));
        Add("Traceroute",                   () => OpenTerminal($"tracert {device.IpAddress}"));
        Add("GeoLocate (Web)",              () => OpenUrl($"https://www.iplocation.net/?query={device.IpAddress}"));
        Sep();
        Add("IP-Adresse kopieren",          () => Clipboard.SetText(device.IpAddress  ?? ""));
        Add("MAC-Adresse kopieren",         () => Clipboard.SetText(device.MacAddress ?? ""));

        menu.PlacementTarget = DeviceGrid;
        menu.IsOpen = true;
    }


    private static void OpenExplorer(string? ip)
    {
        if (ip == null) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "explorer.exe", Arguments = $@"\\{ip}", UseShellExecute = true
        });
    }

    private static void OpenTerminal(string command)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "cmd.exe", Arguments = $"/k {command}", UseShellExecute = true
        });
    }

    private static void FetchAndShowTtl(NetworkDevice device)
    {
        Task.Run(async () =>
        {
            try
            {
                using var ping = new System.Net.NetworkInformation.Ping();
                var reply = await ping.SendPingAsync(device.IpAddress!, 1000);
                string info = reply.Status == System.Net.NetworkInformation.IPStatus.Success
                    ? $"TTL: {reply.Options?.Ttl ?? 0}\n\nTypischer Rückschluss:\n64  → Linux / macOS\n128 → Windows\n255 → Router / Switch"
                    : "Keine Antwort erhalten";
                System.Windows.Application.Current.Dispatcher.Invoke(
                    () => ShowInfo("TTL", info, device.IpAddress));
            }
            catch { System.Windows.Application.Current.Dispatcher.Invoke(
                    () => ShowInfo("TTL", "Fehler beim Abrufen", device.IpAddress)); }
        });
    }

    private static void ShowAll(NetworkDevice d)
    {
        var info = $"IP-Adresse:   {d.IpAddress}\n" +
                   $"Hostname:     {d.Hostname     ?? "—"}\n" +
                   $"Alias:        {d.Alias        ?? "—"}\n" +
                   $"MAC-Adresse:  {d.MacAddress   ?? "—"}\n" +
                   $"Hersteller:   {d.Manufacturer ?? "—"}\n" +
                   $"Ping:         {(d.ResponseTime >= 0 ? d.ResponseTime + " ms" : "—")}\n" +
                   $"Kategorie:    {d.Category}\n" +
                   $"Offene Ports: {d.OpenPortCount}\n" +
                   $"Zuletzt ges.: {d.LastSeen:dd.MM.yyyy HH:mm:ss}\n" +
                   $"Neu erkannt:  {(d.IsNew ? "Ja" : "Nein")}";
        ShowInfo("Alle Details", info, d.IpAddress);
    }

    private NetworkDevice? GetSelectedDevice()
    {
        // Primary: use SelectedItem
        if (DeviceGrid.SelectedItem is NetworkDevice d) return d;

        // Fallback: find item under mouse via ContextMenu PlacementTarget
        if (DeviceGrid.ContextMenu?.PlacementTarget is DataGrid grid)
        {
            var row = FindVisualParent<DataGridRow>(
                Mouse.DirectlyOver as DependencyObject);
            if (row?.Item is NetworkDevice rd) return rd;
        }
        return null;
    }

    private static T? FindVisualParent<T>(DependencyObject? child)
        where T : DependencyObject
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

    // ── Open computer submenu ─────────────────────
    private void CtxOpenHttp_Click(object sender, RoutedEventArgs e)
    {
        var d = GetSelectedDevice();
        if (d != null) OpenUrl($"http://{d.IpAddress}");
    }

    private void CtxOpenHttps_Click(object sender, RoutedEventArgs e)
    {
        var d = GetSelectedDevice();
        if (d != null) OpenUrl($"https://{d.IpAddress}");
    }

    private void CtxOpenExplorer_Click(object sender, RoutedEventArgs e)
    {
        var d = GetSelectedDevice();
        if (d == null) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName         = "explorer.exe",
            Arguments        = $@"\\{d.IpAddress}",
            UseShellExecute  = true
        });
    }

    private void CtxOpenFtp_Click(object sender, RoutedEventArgs e)
    {
        var d = GetSelectedDevice();
        if (d != null) OpenUrl($"ftp://{d.IpAddress}");
    }

    private void CtxOpenTelnet_Click(object sender, RoutedEventArgs e)
    {
        var d = GetSelectedDevice();
        if (d == null) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName        = "cmd.exe",
            Arguments       = $"/k telnet {d.IpAddress}",
            UseShellExecute = true
        });
    }

    private void CtxOpenSsh_Click(object sender, RoutedEventArgs e)
    {
        var d = GetSelectedDevice();
        if (d == null) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName        = "cmd.exe",
            Arguments       = $"/k ssh {d.IpAddress}",
            UseShellExecute = true
        });
    }

    private void CtxGeoLocate_Click(object sender, RoutedEventArgs e)
    {
        var d = GetSelectedDevice();
        if (d != null) OpenUrl($"https://www.iplocation.net/?query={d.IpAddress}");
    }

    private void CtxPing_Click(object sender, RoutedEventArgs e)
    {
        var d = GetSelectedDevice();
        if (d == null) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName        = "cmd.exe",
            Arguments       = $"/k ping {d.IpAddress} -t",
            UseShellExecute = true
        });
    }

    private void CtxTraceroute_Click(object sender, RoutedEventArgs e)
    {
        var d = GetSelectedDevice();
        if (d == null) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName        = "cmd.exe",
            Arguments       = $"/k tracert {d.IpAddress}",
            UseShellExecute = true
        });
    }

    private static void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch { }
    }
}