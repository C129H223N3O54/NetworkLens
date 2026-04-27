using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NetworkLens.Models;
using NetworkLens.ViewModels;

namespace NetworkLens.Views;

// Extended PortResult for UI binding extras
public class PortResultUi : PortResult
{
    public Brush StatusBgColor => Status switch
    {
        PortStatus.Open     => new SolidColorBrush(Color.FromArgb(0x22, 0x74, 0xA7, 0x32)),
        PortStatus.Filtered => new SolidColorBrush(Color.FromArgb(0x22, 0xBA, 0x75, 0x17)),
        _                   => new SolidColorBrush(Color.FromArgb(0x18, 0x4A, 0x55, 0x68))
    };
    public Color StatusColorRaw => Status switch
    {
        PortStatus.Open     => Color.FromRgb(0x74, 0xA7, 0x32),
        PortStatus.Filtered => Color.FromRgb(0xBA, 0x75, 0x17),
        _                   => Color.FromRgb(0x4A, 0x55, 0x68)
    };
}

public partial class PortScanView : UserControl
{
    private PortScanViewModel _vm = null!;
    private PortStatus? _activeFilter = null;

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
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e) { }

    private void SetupViewModel()
    {
        _vm = new PortScanViewModel();

        TxtTargetIp.TextChanged += (_, _) => _vm.TargetIp = TxtTargetIp.Text.Trim();
        TxtCustomPorts.TextChanged += (_, _) => _vm.CustomPorts = TxtCustomPorts.Text;

        _vm.PropertyChanged += (_, e) =>
        {
            Dispatcher.Invoke(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(PortScanViewModel.IsScanning):
                        BtnScan.IsEnabled = _vm.CanScan;
                        BtnStop.IsEnabled = _vm.IsScanning;
                        ProgressPanel.Visibility = _vm.IsScanning ? Visibility.Visible : Visibility.Collapsed;
                        break;
                    case nameof(PortScanViewModel.Progress):
                        ScanProgress.Value = _vm.Progress;
                        TxtProgressPct.Text = $"{_vm.Progress}%";
                        break;
                    case nameof(PortScanViewModel.StatusText):
                        TxtProgressLabel.Text = _vm.StatusText;
                        UpdateOpenCount();
                        break;
                }
            });
        };

        // Live-add results as they come in
        _vm.Results.CollectionChanged += (_, _) =>
        {
            Dispatcher.Invoke(() =>
            {
                RefreshGrid();
                UpdateOpenCount();
            });
        };

        // Filter pill clicks
        PillAll.MouseLeftButtonUp      += (_, _) => SetFilter(null);
        PillOpen.MouseLeftButtonUp     += (_, _) => SetFilter(PortStatus.Open);
        PillFiltered.MouseLeftButtonUp += (_, _) => SetFilter(PortStatus.Filtered);
    }

    private void SetFilter(PortStatus? f)
    {
        _activeFilter = f;

        var accent   = FindResource("AccentBrush") as Brush;
        var deep     = FindResource("BgDeepBrush") as Brush;
        var border   = FindResource("BorderBrush") as Brush;

        PillAll.Background      = f == null ? accent : deep;
        PillOpen.Background     = f == PortStatus.Open ? accent : deep;
        PillFiltered.Background = f == PortStatus.Filtered ? accent : deep;

        RefreshGrid();
    }

    private void RefreshGrid()
    {
        var filtered = _vm.Results
            .Cast<PortResult>()
            .Where(r => _activeFilter == null || r.Status == _activeFilter)
            .Select(r => new PortResultUi
            {
                Port         = r.Port,
                Status       = r.Status,
                ServiceName  = r.ServiceName,
                Banner       = r.Banner,
                ResponseTime = r.ResponseTime
            })
            .OrderBy(r => r.Port)
            .ToList();

        PortGrid.ItemsSource = filtered;

        bool hasData = _vm.Results.Count > 0;
        EmptyState.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;
        PortGrid.Visibility   = hasData ? Visibility.Visible   : Visibility.Collapsed;
    }

    private void UpdateOpenCount()
    {
        int open     = _vm.Results.Count(r => r.Status == PortStatus.Open);
        int filtered = _vm.Results.Count(r => r.Status == PortStatus.Filtered);
        int total    = _vm.Results.Count;
        TxtOpenCount.Text = total > 0
            ? $"{open} offen  ·  {filtered} gefiltert  ·  {total} geprüft"
            : "Warte auf Ergebnis…";
    }

    // ── Full-scan warning ─────────────────────────
    private async void BtnScan_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null) return;

        if (CmbProfile.SelectedIndex == 2) // Full scan
        {
            var result = MessageBox.Show(
                "Full-Scan prüft alle 65.535 Ports.\nDas kann mehrere Minuten dauern.\n\nFortfahren?",
                "Full Port-Scan", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
        }

        _vm.Profile = CmbProfile.SelectedIndex switch
        {
            0 => PortScanProfile.Quick,
            2 => PortScanProfile.Full,
            3 => PortScanProfile.Custom,
            _ => PortScanProfile.Standard
        };
        _vm.TargetIp   = TxtTargetIp.Text.Trim();
        _vm.CustomPorts = TxtCustomPorts.Text;

        EmptyState.Visibility = Visibility.Collapsed;
        PortGrid.Visibility   = Visibility.Collapsed;

        await _vm.StartScanAsync();
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
        var sb = new StringBuilder();
        sb.AppendLine($"Port-Scan: {TxtTargetIp.Text}");
        sb.AppendLine(new string('-', 50));
        foreach (var r in _vm.Results.Where(r => r.Status == PortStatus.Open).OrderBy(r => r.Port))
            sb.AppendLine($"{r.Port,-8} {r.DisplayService,-20} {r.Banner}");
        Clipboard.SetText(sb.ToString());
    }
}
