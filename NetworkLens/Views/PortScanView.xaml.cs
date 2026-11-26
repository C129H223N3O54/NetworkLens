using System.Text;
using NetworkLens.Localization;
using NetworkLens.Models;
using NetworkLens.ViewModels;

namespace NetworkLens.Views;

public partial class PortScanView : UserControl
{
    private PortScanViewModel? _vm;
    private PortStatus? _activeFilter;
    private string? _pendingTarget;

    private static LocalizationManager L => LocalizationManager.Instance;

    public PortScanView()
    {
        InitializeComponent();
        Loaded += (_, _) => SetupViewModel();
    }

    // ── Called externally when navigating from ScanView ──
    public void SetTarget(string ip)
    {
        TxtTargetIp.Text = ip;
        if (_vm != null) _vm.TargetIp = ip;
        else _pendingTarget = ip;
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e) { }

    private void SetupViewModel()
    {
        if (_vm != null) return; // Loaded can fire more than once
        var vm = new PortScanViewModel();
        _vm = vm;

        if (_pendingTarget != null) vm.TargetIp = _pendingTarget;

        TxtTargetIp.TextChanged += (_, _) => vm.TargetIp = TxtTargetIp.Text.Trim();
        TxtCustomPorts.TextChanged += (_, _) => vm.CustomPorts = TxtCustomPorts.Text;

        vm.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(PortScanViewModel.IsScanning):
                    BtnScan.IsEnabled = vm.CanScan;
                    BtnStop.IsEnabled = vm.IsScanning;
                    ProgressPanel.Visibility = vm.IsScanning ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case nameof(PortScanViewModel.Progress):
                    ScanProgress.Value = vm.Progress;
                    TxtProgressPct.Text = $"{vm.Progress}%";
                    break;
                case nameof(PortScanViewModel.StatusText):
                    TxtProgressLabel.Text = vm.StatusText;
                    UpdateOpenCount();
                    break;
            }
        };

        // One grid refresh per result batch (raised on the UI thread)
        vm.ResultsChanged += (_, _) =>
        {
            RefreshGrid();
            UpdateOpenCount();
        };

        // Filter pill clicks
        PillAll.MouseLeftButtonUp      += (_, _) => SetFilter(null);
        PillOpen.MouseLeftButtonUp     += (_, _) => SetFilter(PortStatus.Open);
        PillFiltered.MouseLeftButtonUp += (_, _) => SetFilter(PortStatus.Filtered);
    }

    private void SetFilter(PortStatus? f)
    {
        _activeFilter = f;

        var accent = FindResource("AccentBrush") as Brush;
        var deep   = FindResource("BgDeepBrush") as Brush;

        PillAll.Background      = f == null ? accent : deep;
        PillOpen.Background     = f == PortStatus.Open ? accent : deep;
        PillFiltered.Background = f == PortStatus.Filtered ? accent : deep;

        RefreshGrid();
    }

    private void RefreshGrid()
    {
        if (_vm == null) return;

        PortGrid.ItemsSource = _vm.Results
            .Where(r => _activeFilter == null || r.Status == _activeFilter)
            .OrderBy(r => r.Port)
            .ToList();

        bool hasData = _vm.Results.Count > 0;
        EmptyState.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;
        PortGrid.Visibility   = hasData ? Visibility.Visible   : Visibility.Collapsed;
    }

    private void UpdateOpenCount()
    {
        if (_vm == null) return;
        int total = _vm.Results.Count;
        TxtOpenCount.Text = total > 0
            ? string.Format(L.T("Port_StatusFmt"), _vm.OpenCount, _vm.FilteredCount, total)
            : L.T("Port_WaitingResult");
    }

    // ── Full-scan warning ─────────────────────────
    private async void BtnScan_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null) return;

        if (CmbProfile.SelectedIndex == 2) // Full scan
        {
            var result = MessageBox.Show(
                L.T("Port_FullScanWarn"),
                L.T("Port_FullScanTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
        }

        _vm.Profile = CmbProfile.SelectedIndex switch
        {
            0 => PortScanProfile.Quick,
            2 => PortScanProfile.Full,
            3 => PortScanProfile.Custom,
            _ => PortScanProfile.Standard
        };
        _vm.TargetIp    = TxtTargetIp.Text.Trim();
        _vm.CustomPorts = TxtCustomPorts.Text;

        EmptyState.Visibility = Visibility.Collapsed;
        PortGrid.Visibility   = Visibility.Collapsed;

        await _vm.StartScanAsync();
        TxtProgressLabel.Text = _vm.StatusText;
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e) => _vm?.StopScanCommand.Execute(null);

    private void CmbProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LblCustomPorts == null) return;
        bool isCustom = CmbProfile.SelectedIndex == 3;
        LblCustomPorts.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
        TxtCustomPorts.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BtnCopyResults_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var sb = new StringBuilder();
        sb.AppendLine($"Port-Scan: {TxtTargetIp.Text}");
        sb.AppendLine(new string('-', 50));
        foreach (var r in _vm.Results.Where(r => r.Status == PortStatus.Open).OrderBy(r => r.Port))
            sb.AppendLine($"{r.Port,-8} {r.DisplayService,-20} {r.Banner}");
        Clipboard.SetText(sb.ToString());
    }
}
