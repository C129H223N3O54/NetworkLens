using System.Windows;
using System.Windows.Input;
using NetworkLens.ViewModels;

namespace NetworkLens.Views;

public partial class GraphWindow : Window
{
    private readonly MonitorEntry _entry;

    public GraphWindow(MonitorEntry entry)
    {
        InitializeComponent();
        _entry = entry;

        TxtTitle.Text = entry.Device.DisplayName;
        TxtIp.Text    = entry.Device.IpAddress ?? "";

        UpdateStats();

        // Keep stats live even when minimized
        entry.PropertyChanged += (_, pe) =>
        {
            if (pe.PropertyName == nameof(MonitorEntry.PingHistory))
                Dispatcher.Invoke(() => { UpdateStats(); Redraw(); });
        };
    }

    private void UpdateStats()
    {
        TxtPing.Text        = _entry.LastPing >= 0 ? _entry.LastPing.ToString() : "—";
        TxtJitter.Text      = $"{_entry.Jitter:F1}";
        TxtJitterIcon.Text  = _entry.JitterIcon;
        TxtMin.Text         = _entry.MinPing == long.MaxValue ? "—" : _entry.MinPing.ToString();
        TxtAvg.Text         = $"{_entry.AvgPing:F0}";
        TxtMax.Text         = _entry.MaxPing.ToString();
        TxtLoss.Text        = $"{_entry.PacketLoss:F1}";
        TxtLossCount.Text   = $"{_entry.LostPings} / {_entry.TotalPings}";
        TxtPointCount.Text  = $"{_entry.PingHistory.Count} Messpunkte";
    }

    public void Redraw()
    {
        MonitorView.DrawGraph(GraphCanvas, _entry, large: true);
    }

    private void GraphCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Redraw();
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
}
