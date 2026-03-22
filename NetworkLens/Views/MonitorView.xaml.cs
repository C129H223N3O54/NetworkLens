using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using NetworkLens.Models;
using NetworkLens.ViewModels;

namespace NetworkLens.Views;

public partial class MonitorView : UserControl
{
    private MonitorViewModel _vm = null!;

    public MonitorView()
    {
        InitializeComponent();
    }

    // ── Called by MainWindow with shared VM ───────
    public void SetViewModel(MonitorViewModel vm)
    {
        _vm = vm;
        if (IsLoaded) BindVm();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        _vm ??= new MonitorViewModel();
        BindVm();
    }

    private void BindVm()
    {
        MonitorCards.ItemsSource = _vm.Entries;
        _vm.Entries.CollectionChanged += (_, _) => UpdateVisibility();
        _vm.PropertyChanged += (_, pe) =>
        {
            if (pe.PropertyName == nameof(MonitorViewModel.IsRunning))
                Dispatcher.Invoke(() =>
                {
                    BtnStart.IsEnabled = !_vm.IsRunning;
                    BtnStop.IsEnabled  = _vm.IsRunning;
                });
        };
        UpdateVisibility();
    }

    // ── Add / Remove devices ─────────────────────
    private void BtnAddDevice_Click(object sender, RoutedEventArgs e)
    {
        var ip = TxtAddIp.Text.Trim();
        if (!string.IsNullOrEmpty(ip)) AddDevice(ip);
    }

    private void TxtAddIp_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) BtnAddDevice_Click(sender, new RoutedEventArgs());
    }

    private void AddDevice(string ip)
    {
        var device = new NetworkDevice { IpAddress = ip, Hostname = ip };
        _vm.AddDevice(device);
        TxtAddIp.Clear();
        UpdateVisibility();
    }

    public void AddDeviceExternal(NetworkDevice device)
    {
        _vm.AddDevice(device);
        UpdateVisibility();
    }

    private void BtnRemoveDevice_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is MonitorEntry entry)
        {
            _vm.Entries.Remove(entry);
            UpdateVisibility();
        }
    }

    // ── Monitor control ───────────────────────────
    private async void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Entries.Count == 0)
        {
            MessageBox.Show("Bitte zuerst Geräte zur Überwachung hinzufügen.",
                "Monitor", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        await _vm.StartAsync();
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e) => _vm.Stop();

    private void CmbInterval_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_vm == null) return;
        _vm.IntervalSeconds = CmbInterval.SelectedIndex switch
        {
            0 => 5,
            2 => 30,
            3 => 60,
            _ => 10
        };
    }

    private void UpdateVisibility()
    {
        bool hasEntries = _vm.Entries.Count > 0;
        EmptyState.Visibility = hasEntries ? Visibility.Collapsed : Visibility.Visible;
        CardsScroll.Visibility = hasEntries ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── Live Graph drawing ────────────────────────
    private void SparkCanvas_Loaded(object sender, RoutedEventArgs e)
        => DrawGraph(sender as Canvas);

    private void SparkCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        => DrawGraph(sender as Canvas);

    private static void DrawGraph(Canvas? canvas)
    {
        if (canvas == null) return;
        canvas.Children.Clear();

        // The Tag is now the MonitorEntry, not the collection directly
        MonitorEntry? entry = canvas.Tag as MonitorEntry;
        if (entry == null) return;

        var pingHistory = entry.PingHistory.ToList();
        if (pingHistory.Count < 2) return;

        double w = canvas.ActualWidth;
        double h = canvas.ActualHeight;
        if (w < 4 || h < 4) return;

        // Compute jitter history from ping history
        var jitterHistory = new List<double>();
        for (int i = 1; i < pingHistory.Count; i++)
            jitterHistory.Add(Math.Abs(pingHistory[i] - pingHistory[i - 1]));

        double pingMax  = Math.Max(pingHistory.Max(), 1);
        double pingMin  = Math.Max(pingHistory.Min() - 5, 0);
        double pingRange = Math.Max(pingMax - pingMin, 1);

        double jitterMax = jitterHistory.Count > 0
            ? Math.Max(jitterHistory.Max(), 1) : 1;

        // ── Grid lines ──────────────────────────
        int gridLines = 3;
        for (int i = 0; i <= gridLines; i++)
        {
            double y = h / gridLines * i;
            var line = new System.Windows.Shapes.Line
            {
                X1 = 0, X2 = w, Y1 = y, Y2 = y,
                Stroke = new SolidColorBrush(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF)),
                StrokeThickness = 1,
                StrokeDashArray = new System.Windows.Media.DoubleCollection { 3, 3 }
            };
            canvas.Children.Add(line);

            // Y-axis label
            double val = pingMax - (pingMax - pingMin) / gridLines * i;
            var lbl = new System.Windows.Controls.TextBlock
            {
                Text       = $"{val:F0}ms",
                FontSize   = 9,
                Foreground = new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF)),
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };
            Canvas.SetLeft(lbl, 2);
            Canvas.SetTop(lbl, y + 1);
            canvas.Children.Add(lbl);
        }

        // ── Helper: build point list ─────────────
        static PointCollection BuildPoints(IList<double> values, double min, double range,
            double w, double h, double vPad = 6)
        {
            var pts = new PointCollection();
            int n = values.Count;
            for (int i = 0; i < n; i++)
            {
                double x = i / (double)(n - 1) * w;
                double y = h - vPad - ((values[i] - min) / range * (h - vPad * 2));
                y = Math.Max(vPad, Math.Min(h - vPad, y));
                pts.Add(new Point(x, y));
            }
            return pts;
        }

        // ── Ping area fill ───────────────────────
        var pingVals = pingHistory.Select(p => (double)p).ToList();
        var pingPts  = BuildPoints(pingVals, pingMin, pingRange, w, h);

        var fillPts = new PointCollection(pingPts)
        {
            new Point(w, h),
            new Point(0, h)
        };
        var pingFill = new System.Windows.Shapes.Polygon
        {
            Points = fillPts,
            Fill   = new LinearGradientBrush(
                Color.FromArgb(0x50, 0x00, 0xB4, 0xD8),
                Color.FromArgb(0x05, 0x00, 0xB4, 0xD8),
                new Point(0, 0), new Point(0, 1)),
            Stroke = null
        };
        canvas.Children.Add(pingFill);

        // ── Ping line ────────────────────────────
        var pingLine = new System.Windows.Shapes.Polyline
        {
            Points          = pingPts,
            Stroke          = new SolidColorBrush(Color.FromRgb(0x00, 0xB4, 0xD8)),
            StrokeThickness = 1.8,
            StrokeLineJoin  = PenLineJoin.Round
        };
        canvas.Children.Add(pingLine);

        // ── Jitter line (scaled to same canvas) ──
        if (jitterHistory.Count >= 2)
        {
            var jPts = BuildPoints(jitterHistory, 0, jitterMax, w, h);
            var jLine = new System.Windows.Shapes.Polyline
            {
                Points          = jPts,
                Stroke          = new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xD6, 0x00)),
                StrokeThickness = 1.2,
                StrokeLineJoin  = PenLineJoin.Round,
                StrokeDashArray = new System.Windows.Media.DoubleCollection { 4, 2 }
            };
            canvas.Children.Add(jLine);
        }

        // ── Last ping dot ────────────────────────
        if (pingPts.Count > 0)
        {
            var last = pingPts[^1];
            var dot  = new System.Windows.Shapes.Ellipse
            {
                Width  = 7, Height = 7,
                Fill   = new SolidColorBrush(Color.FromRgb(0x00, 0xB4, 0xD8)),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color       = Color.FromRgb(0x00, 0xB4, 0xD8),
                    BlurRadius  = 8,
                    ShadowDepth = 0,
                    Opacity     = 0.8
                }
            };
            Canvas.SetLeft(dot, last.X - 3.5);
            Canvas.SetTop(dot,  last.Y - 3.5);
            canvas.Children.Add(dot);
        }
    }
}
