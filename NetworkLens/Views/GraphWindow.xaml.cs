using System.ComponentModel;
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

        // Keep stats live even when minimized — unsubscribed again in OnClosed,
        // otherwise closed windows would keep redrawing and never be collected.
        entry.PropertyChanged += Entry_PropertyChanged;
    }

    private void Entry_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MonitorEntry.PingHistory)) return;
        Dispatcher.InvokeAsync(() =>
        {
            UpdateStats();
            Redraw();
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        _entry.PropertyChanged -= Entry_PropertyChanged;
        base.OnClosed(e);
    }

    private void UpdateStats()
    {
        TxtPing.Text        = _entry.LastPing >= 0 ? _entry.LastPing.ToString() : "—";
        TxtJitter.Text      = $"{_entry.Jitter:F1}";
        TxtJitterIcon.Text  = _entry.JitterIcon;
        TxtMin.Text         = _entry.MinPingText;
        TxtAvg.Text         = $"{_entry.AvgPing:F0}";
        TxtMax.Text         = _entry.MaxPing.ToString();
        TxtLoss.Text        = $"{_entry.PacketLoss:F1}";
        TxtLossCount.Text   = $"{_entry.LostPings} / {_entry.TotalPings}";
        TxtPointCount.Text  = $"{_entry.PingHistory.Count} {NetworkLens.Localization.LocalizationManager.Instance.T("Mon_DataPoints")}";
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
