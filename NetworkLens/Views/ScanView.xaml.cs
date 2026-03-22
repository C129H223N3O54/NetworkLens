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

        // Re-bind if already has devices (e.g. auto-scan ran before Loaded)
        if (_vm.Devices.Count > 0)
        {
            EmptyState.Visibility   = Visibility.Collapsed;
            ResultsPanel.Visibility = Visibility.Visible;
            UpdateResultCount();
        }

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

    private NetworkDevice? GetSelectedDevice()
        => DeviceGrid.SelectedItem as NetworkDevice;

    // ── Context Menu ──────────────────────────────
    private void CtxPortScan_Click(object sender, RoutedEventArgs e)
    {
        var d = GetSelectedDevice();
        if (d == null) return;
        if (Window.GetWindow(this) is MainWindow mw)
            mw.NavigateToPortScan(d);
    }

    private void CtxAddMonitor_Click(object sender, RoutedEventArgs e)
    {
        var d = GetSelectedDevice();
        if (d == null) return;
        if (Window.GetWindow(this) is MainWindow mw)
            mw.NavigateToMonitor(d);
    }

    private async void CtxSetAlias_Click(object sender, RoutedEventArgs e)
    {
        var d = GetSelectedDevice();
        if (d == null) return;

        var dialog = new AliasDialog(d.Alias ?? "", d.DisplayName)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
        {
            d.Alias = dialog.AliasText;
            var svc = new Services.DeviceManager();
            await svc.SaveDeviceAsync(d);
        }
    }

    private void CtxSetCategory_Click(object sender, RoutedEventArgs e)
    {
        var d = GetSelectedDevice();
        if (d == null) return;

        var dialog = new CategoryDialog(d.Category) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true)
        {
            d.Category = dialog.SelectedCategory;
            _ = new Services.DeviceManager().SaveDeviceAsync(d);
        }
    }

    private void CtxToggleFavorite_Click(object sender, RoutedEventArgs e)
    {
        var d = GetSelectedDevice();
        if (d == null) return;
        d.IsFavorite = !d.IsFavorite;
        _ = new Services.DeviceManager().SaveDeviceAsync(d);
    }

    private void CtxCopyIP_Click(object sender, RoutedEventArgs e)
    {
        var d = GetSelectedDevice();
        if (d != null) Clipboard.SetText(d.IpAddress ?? "");
    }

    private void CtxCopyMAC_Click(object sender, RoutedEventArgs e)
    {
        var d = GetSelectedDevice();
        if (d != null) Clipboard.SetText(d.MacAddress ?? "");
    }

    private void CtxPing_Click(object sender, RoutedEventArgs e)
    {
        var d = GetSelectedDevice();
        if (d == null) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/k ping {d.IpAddress} -t",
            UseShellExecute = true
        });
    }

    private void CtxTraceroute_Click(object sender, RoutedEventArgs e)
    {
        var d = GetSelectedDevice();
        if (d == null) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/k tracert {d.IpAddress}",
            UseShellExecute = true
        });
    }
}
