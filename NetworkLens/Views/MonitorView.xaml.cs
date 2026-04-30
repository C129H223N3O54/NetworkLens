using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using NetworkLens.Models;
using NetworkLens.ViewModels;

namespace NetworkLens.Views;

public partial class MonitorView : UserControl
{
    private MonitorViewModel _vm = null!;
    // Track open graph windows per entry
    private readonly Dictionary<MonitorEntry, GraphWindow> _graphWindows = new();

    public MonitorView() => InitializeComponent();

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
        _vm.Entries.CollectionChanged += (_, args) =>
        {
            Dispatcher.Invoke(UpdateVisibility);
            if (args.NewItems == null) return;
            foreach (MonitorEntry e in args.NewItems) HookEntryRedraw(e);
        };
        _vm.PropertyChanged += (_, pe) =>
        {
            if (pe.PropertyName == nameof(MonitorViewModel.IsRunning))
                Dispatcher.Invoke(() =>
                {
                    BtnStart.IsEnabled = !_vm.IsRunning;
                    BtnStop.IsEnabled  = _vm.IsRunning;
                });
        };
        foreach (var entry in _vm.Entries) HookEntryRedraw(entry);
        UpdateVisibility();
    }

    private void HookEntryRedraw(MonitorEntry entry)
    {
        entry.PropertyChanged += (_, pe) =>
        {
            if (pe.PropertyName != nameof(MonitorEntry.PingHistory)) return;
            Dispatcher.Invoke(() =>
            {
                RedrawEntryGraph(entry);
                // Update graph window if open
                if (_graphWindows.TryGetValue(entry, out var win) && win.IsVisible)
                    win.Redraw();
            });
        };
    }

    // Add / Remove
    private void BtnAddDevice_Click(object sender, RoutedEventArgs e)
    {
        var ip = TxtAddIp.Text.Trim();
        if (string.IsNullOrEmpty(ip)) return;
        _vm.AddDevice(new NetworkDevice { IpAddress = ip, Hostname = ip });
        TxtAddIp.Clear();
        UpdateVisibility();
    }

    private void TxtAddIp_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) BtnAddDevice_Click(sender, new RoutedEventArgs());
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
            if (_graphWindows.TryGetValue(entry, out var win)) win.Close();
            UpdateVisibility();
        }
    }

    // Monitor control
    private async void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Entries.Count == 0)
        {
            var L = Localization.LocalizationManager.Instance;
            MessageBox.Show(L.T("Msg_AddDevicesFirst"), L.T("Msg_MonitorTitle"),
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        await _vm.StartAsync();
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e) => _vm.Stop();

    private void BtnWikiLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string key)
        {
            var loc = Localization.LocalizationManager.Instance;
            var slug = loc.T(key);
            var url = loc.WikiPrefix + slug;
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
    }

    private void BtnShowInfo_Click(object sender, RoutedEventArgs e)
    {
        InfoPanel.Visibility = InfoPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void BtnCloseInfo_Click(object sender, RoutedEventArgs e)
    {
        InfoPanel.Visibility = Visibility.Collapsed;
    }

    private void CmbInterval_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_vm == null) return;
        _vm.IntervalSeconds = CmbInterval.SelectedIndex switch
        {
            0 => 1,
            1 => 5,
            3 => 30,
            4 => 60,
            5 => 0,
            _ => 10
        };
    }

    private void UpdateVisibility()
    {
        bool has = _vm.Entries.Count > 0;
        EmptyState.Visibility  = has ? Visibility.Collapsed : Visibility.Visible;
        CardsScroll.Visibility = has ? Visibility.Visible   : Visibility.Collapsed;
    }

    // Graph canvas events
    private void SparkCanvas_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Canvas c && c.Tag is MonitorEntry entry) DrawGraph(c, entry);
    }

    private void SparkCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is Canvas c && c.Tag is MonitorEntry entry) DrawGraph(c, entry);
    }

    // Double-click on graph border -> open resizable window
    private void SparkCanvas_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount < 2) return;
        e.Handled = true;

        // Find the MonitorEntry by walking up the visual tree
        var el = sender as DependencyObject;
        MonitorEntry? entry = null;

        // Try Tag on sender first
        if (sender is FrameworkElement fe && fe.Tag is MonitorEntry me)
            entry = me;

        // Walk visual tree as fallback
        if (entry == null)
        {
            var current = e.OriginalSource as DependencyObject;
            while (current != null && entry == null)
            {
                if (current is Canvas c && c.Tag is MonitorEntry me2) entry = me2;
                if (current is Border b && b.Tag is MonitorEntry me3) entry = me3;
                current = VisualTreeHelper.GetParent(current);
            }
        }

        if (entry == null) return;

        // Reuse existing window or create new
        if (_graphWindows.TryGetValue(entry, out var existing) && existing.IsVisible)
        {
            existing.Activate();
            return;
        }

        var win = new GraphWindow(entry);
        _graphWindows[entry] = win;
        win.Show();
    }

    private void RedrawEntryGraph(MonitorEntry entry)
    {
        for (int i = 0; i < _vm.Entries.Count; i++)
        {
            if (_vm.Entries[i] != entry) continue;
            var container = MonitorCards.ItemContainerGenerator
                .ContainerFromIndex(i) as FrameworkElement;
            if (container == null) break;
            var canvas = FindChild<Canvas>(container, "SparkCanvas");
            if (canvas != null) DrawGraph(canvas, entry);
            break;
        }
    }

    private static T? FindChild<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        int n = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < n; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T fe && fe.Name == name) return fe;
            var result = FindChild<T>(child, name);
            if (result != null) return result;
        }
        return null;
    }

    // Core graph drawing (static, reused by GraphWindow too)
    public static void DrawGraph(Canvas canvas, MonitorEntry entry, bool large = false)
    {
        canvas.Children.Clear();

        var ping = entry.PingHistory.ToList();
        if (ping.Count < 2) return;

        double w = canvas.ActualWidth;
        double h = canvas.ActualHeight;
        if (w < 4 || h < 4) return;

        var jitter = new List<double>();
        for (int i = 1; i < ping.Count; i++)
            jitter.Add(Math.Abs(ping[i] - ping[i - 1]));

        double pMax   = Math.Max(ping.Max(), 1);
        double pMin   = Math.Max(ping.Min() - 5, 0);
        double pRange = Math.Max(pMax - pMin, 1);
        double jMax   = jitter.Count > 0 ? Math.Max(jitter.Max(), 1) : 1;

        int grid = large ? 5 : 3;
        for (int i = 0; i <= grid; i++)
        {
            double y = h / grid * i;
            canvas.Children.Add(new Line
            {
                X1 = 0, X2 = w, Y1 = y, Y2 = y,
                Stroke = new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 3, 3 }
            });
            double val = pMax - (pMax - pMin) / grid * i;
            var lbl = new TextBlock
            {
                Text = $"{val:F0}ms", FontSize = large ? 10 : 9,
                Foreground = new SolidColorBrush(Color.FromArgb(0x77, 0xFF, 0xFF, 0xFF)),
                FontFamily = new FontFamily("Verdana")
            };
            Canvas.SetLeft(lbl, 3); Canvas.SetTop(lbl, y + 1);
            canvas.Children.Add(lbl);
        }

        static PointCollection Pts(IList<double> vals, double min, double range,
            double w, double h, double pad = 8)
        {
            var pc = new PointCollection();
            int n = vals.Count;
            for (int i = 0; i < n; i++)
            {
                double x = i / (double)(n - 1) * w;
                double y = h - pad - (vals[i] - min) / range * (h - pad * 2);
                pc.Add(new Point(x, Math.Max(pad, Math.Min(h - pad, y))));
            }
            return pc;
        }

        var pVals = ping.Select(p => (double)p).ToList();
        var pPts  = Pts(pVals, pMin, pRange, w, h);

        canvas.Children.Add(new Polygon
        {
            Points = new PointCollection(pPts) { new Point(w, h), new Point(0, h) },
            Fill = new LinearGradientBrush(
                Color.FromArgb(0x55, 0xE8, 0x60, 0x0A),
                Color.FromArgb(0x05, 0xE8, 0x60, 0x0A),
                new Point(0, 0), new Point(0, 1))
        });
        canvas.Children.Add(new Polyline
        {
            Points = pPts,
            Stroke = new SolidColorBrush(Color.FromRgb(0xE8, 0x60, 0x0A)),
            StrokeThickness = large ? 2 : 1.8,
            StrokeLineJoin = PenLineJoin.Round
        });

        if (jitter.Count >= 2)
        {
            canvas.Children.Add(new Polyline
            {
                Points = Pts(jitter, 0, jMax, w, h),
                Stroke = new SolidColorBrush(Color.FromArgb(0xDD, 0xBA, 0x75, 0x17)),
                StrokeThickness = large ? 1.5 : 1.2,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            });
        }

        if (pPts.Count > 0)
        {
            double r = large ? 5 : 3.5;
            var last = pPts[^1];
            var dot = new Ellipse
            {
                Width = r * 2, Height = r * 2,
                Fill = new SolidColorBrush(Color.FromRgb(0xE8, 0x60, 0x0A)),
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(0xE8, 0x60, 0x0A),
                    BlurRadius = 8, ShadowDepth = 0, Opacity = 0.9
                }
            };
            Canvas.SetLeft(dot, last.X - r);
            Canvas.SetTop(dot, last.Y - r);
            canvas.Children.Add(dot);
        }
    }
}
