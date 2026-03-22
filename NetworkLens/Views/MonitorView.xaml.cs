using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    private Popup?       _graphPopup;
    private Canvas?      _popupCanvas;
    private MonitorEntry? _popupEntry;

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
            if (pe.PropertyName == nameof(MonitorEntry.PingHistory))
                Dispatcher.Invoke(() => RedrawEntryGraph(entry));
        };
    }

    // Add / Remove
    private void BtnAddDevice_Click(object sender, RoutedEventArgs e)
    {
        var ip = TxtAddIp.Text.Trim();
        if (!string.IsNullOrEmpty(ip))
        {
            _vm.AddDevice(new NetworkDevice { IpAddress = ip, Hostname = ip });
            TxtAddIp.Clear();
            UpdateVisibility();
        }
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
            UpdateVisibility();
        }
    }

    // Monitor control
    private async void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Entries.Count == 0)
        {
            MessageBox.Show("Bitte zuerst Geraete hinzufuegen.", "Monitor",
                MessageBoxButton.OK, MessageBoxImage.Information);
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
            0 => 1,    // 1s
            1 => 5,    // 5s
            3 => 30,   // 30s
            4 => 60,   // 60s
            5 => 0,    // Dauerhaft
            _ => 10    // 10s (default, index 2)
        };
    }

    private void UpdateVisibility()
    {
        bool has = _vm.Entries.Count > 0;
        EmptyState.Visibility  = has ? Visibility.Collapsed : Visibility.Visible;
        CardsScroll.Visibility = has ? Visibility.Visible   : Visibility.Collapsed;
    }

    // Graph events
    private void SparkCanvas_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Canvas c && c.Tag is MonitorEntry entry) DrawGraph(c, entry);
    }

    private void SparkCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is Canvas c && c.Tag is MonitorEntry entry) DrawGraph(c, entry);
    }

    private void SparkCanvas_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount < 2) return;
        // Walk up to find the Canvas with the MonitorEntry tag
        var el = e.OriginalSource as DependencyObject;
        while (el != null)
        {
            if (el is Canvas c && c.Tag is MonitorEntry entry)
            {
                ShowGraphPopup(entry);
                e.Handled = true;
                return;
            }
            el = System.Windows.Media.VisualTreeHelper.GetParent(el);
        }
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
        // Also update popup if open for this entry
        if (_popupEntry == entry && _popupCanvas != null)
            DrawGraph(_popupCanvas, entry, large: true);
    }

    private static T? FindChild<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < n; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T fe && fe.Name == name) return fe;
            var result = FindChild<T>(child, name);
            if (result != null) return result;
        }
        return null;
    }

    // Popup
    private void ShowGraphPopup(MonitorEntry entry)
    {
        _popupEntry = entry;

        var innerCanvas = new Canvas { Height = 260 };
        _popupCanvas = innerCanvas;

        var legend = new StackPanel { Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 8) };
        AddLegendItem(legend, Color.FromRgb(0x00, 0xB4, 0xD8), "Ping");
        AddLegendItem(legend, Color.FromRgb(0xFF, 0xD6, 0x00), "Jitter", 12);

        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock
        {
            Text = $"{entry.Device.DisplayName}  ({entry.Device.IpAddress})",
            FontSize = 15, FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF2, 0xF5)),
            Margin = new Thickness(0, 0, 0, 6)
        });
        panel.Children.Add(legend);
        panel.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x11, 0x14, 0x16)),
            CornerRadius = new CornerRadius(6),
            Child = innerCanvas,
            Margin = new Thickness(0, 0, 0, 10)
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Klicken zum Schlie\u00dfen",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x55, 0x68)),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x16, 0x1A, 0x1F)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xB4, 0xD8)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Width = 620,
            Child = panel,
            Effect = new DropShadowEffect { Color = Colors.Black,
                BlurRadius = 32, ShadowDepth = 0, Opacity = 0.85 }
        };

        _graphPopup = new Popup
        {
            Child = border, IsOpen = false,
            Placement = PlacementMode.Center,
            PlacementTarget = this,
            AllowsTransparency = true,
            StaysOpen = false,
            PopupAnimation = PopupAnimation.Fade
        };

        _graphPopup.Opened += (_, _) =>
        {
            border.UpdateLayout();
            DrawGraph(innerCanvas, entry, large: true);
        };
        _graphPopup.IsOpen = true;
        border.MouseLeftButtonUp += (_, _) => _graphPopup.IsOpen = false;
    }

    private static void AddLegendItem(StackPanel panel, Color c, string label, double leftMargin = 0)
    {
        if (leftMargin > 0)
            panel.Children.Add(new System.Windows.Shapes.Ellipse
            {
                Width = 0, Margin = new Thickness(leftMargin, 0, 0, 0)
            });
        panel.Children.Add(new System.Windows.Shapes.Ellipse
        {
            Width = 8, Height = 8,
            Fill = new SolidColorBrush(c),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(leftMargin, 0, 4, 0)
        });
        panel.Children.Add(new TextBlock
        {
            Text = label, FontSize = 11,
            Foreground = new SolidColorBrush(c),
            VerticalAlignment = VerticalAlignment.Center
        });
    }

    // Core graph renderer
    public static void DrawGraph(Canvas canvas, MonitorEntry entry, bool large = false)
    {
        canvas.Children.Clear();

        var pingHistory = entry.PingHistory.ToList();
        if (pingHistory.Count < 2) return;

        double w = canvas.ActualWidth;
        double h = canvas.ActualHeight;
        if (w < 4 || h < 4) return;

        var jitter = new List<double>();
        for (int i = 1; i < pingHistory.Count; i++)
            jitter.Add(Math.Abs(pingHistory[i] - pingHistory[i - 1]));

        double pingMax   = Math.Max(pingHistory.Max(), 1);
        double pingMin   = Math.Max(pingHistory.Min() - 5, 0);
        double pingRange = Math.Max(pingMax - pingMin, 1);
        double jMax      = jitter.Count > 0 ? Math.Max(jitter.Max(), 1) : 1;

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
            double val = pingMax - (pingMax - pingMin) / grid * i;
            var lbl = new TextBlock
            {
                Text = $"{val:F0}ms", FontSize = large ? 10 : 9,
                Foreground = new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)),
                FontFamily = new FontFamily("Segoe UI")
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

        var pVals = pingHistory.Select(p => (double)p).ToList();
        var pPts  = Pts(pVals, pingMin, pingRange, w, h);

        // Filled area
        var fp = new PointCollection(pPts) { new Point(w, h), new Point(0, h) };
        canvas.Children.Add(new Polygon
        {
            Points = fp,
            Fill = new LinearGradientBrush(
                Color.FromArgb(0x55, 0x00, 0xB4, 0xD8),
                Color.FromArgb(0x05, 0x00, 0xB4, 0xD8),
                new Point(0, 0), new Point(0, 1))
        });

        // Ping line
        canvas.Children.Add(new Polyline
        {
            Points = pPts,
            Stroke = new SolidColorBrush(Color.FromRgb(0x00, 0xB4, 0xD8)),
            StrokeThickness = large ? 2 : 1.8,
            StrokeLineJoin = PenLineJoin.Round
        });

        // Jitter line
        if (jitter.Count >= 2)
        {
            canvas.Children.Add(new Polyline
            {
                Points = Pts(jitter, 0, jMax, w, h),
                Stroke = new SolidColorBrush(Color.FromArgb(0xDD, 0xFF, 0xD6, 0x00)),
                StrokeThickness = large ? 1.5 : 1.2,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            });
        }

        // Last point dot
        if (pPts.Count > 0)
        {
            var last = pPts[^1];
            double r = large ? 5 : 3.5;
            var dot = new Ellipse
            {
                Width = r * 2, Height = r * 2,
                Fill = new SolidColorBrush(Color.FromRgb(0x00, 0xB4, 0xD8)),
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(0x00, 0xB4, 0xD8),
                    BlurRadius = 8, ShadowDepth = 0, Opacity = 0.9
                }
            };
            Canvas.SetLeft(dot, last.X - r);
            Canvas.SetTop(dot,  last.Y - r);
            canvas.Children.Add(dot);
        }
    }
}
