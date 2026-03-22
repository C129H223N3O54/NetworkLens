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

    // ── Sparkline drawing ─────────────────────────
    private void SparkCanvas_Loaded(object sender, RoutedEventArgs e)
        => DrawSparkline(sender as Canvas);

    private void SparkCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        => DrawSparkline(sender as Canvas);

    private static void DrawSparkline(Canvas? canvas)
    {
        if (canvas == null) return;
        canvas.Children.Clear();

        if (canvas.Tag is not System.Collections.ObjectModel.ObservableCollection<long> history
            || history.Count < 2) return;

        double w = canvas.ActualWidth;
        double h = canvas.ActualHeight;
        if (w < 2 || h < 2) return;

        var values = history.ToList();
        double max = Math.Max(values.Max(), 1);
        double min = values.Min();
        double range = Math.Max(max - min, 1);

        // Build polyline points
        var pts = new PointCollection();
        for (int i = 0; i < values.Count; i++)
        {
            double x = i / (double)(values.Count - 1) * w;
            double y = h - ((values[i] - min) / range * (h - 8) + 4);
            pts.Add(new Point(x, y));
        }

        // Fill polygon (area under curve)
        var fillPts = new PointCollection(pts) { new Point(w, h), new Point(0, h) };
        var fill = new Polygon
        {
            Points = fillPts,
            Fill = new LinearGradientBrush(
                Color.FromArgb(0x44, 0x00, 0xB4, 0xD8),
                Color.FromArgb(0x04, 0x00, 0xB4, 0xD8),
                new Point(0, 0), new Point(0, 1)),
            Stroke = null
        };
        canvas.Children.Add(fill);

        // Line
        var line = new Polyline
        {
            Points = pts,
            Stroke = new SolidColorBrush(Color.FromRgb(0x00, 0xB4, 0xD8)),
            StrokeThickness = 1.5,
            StrokeLineJoin = PenLineJoin.Round
        };
        canvas.Children.Add(line);

        // Last point dot
        if (pts.Count > 0)
        {
            var last = pts[^1];
            var dot = new Ellipse
            {
                Width = 6, Height = 6,
                Fill = new SolidColorBrush(Color.FromRgb(0x00, 0xB4, 0xD8))
            };
            Canvas.SetLeft(dot, last.X - 3);
            Canvas.SetTop(dot, last.Y - 3);
            canvas.Children.Add(dot);
        }
    }

    private void RedrawAllSparklines()
    {
        // Walk visual tree to find all spark canvases and redraw
        foreach (var entry in _vm.Entries)
        {
            // Entries are re-bound through ItemsControl; sparklines redraw on SizeChanged / Loaded
            // For live updates we hook into ObservableCollection via entry
        }
    }
}
