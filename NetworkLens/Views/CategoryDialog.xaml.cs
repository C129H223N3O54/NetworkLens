using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NetworkLens.Models;

namespace NetworkLens.Views;

public partial class CategoryDialog : Window
{
    public DeviceCategory SelectedCategory { get; private set; }

    private static readonly (DeviceCategory Cat, string Icon, string LabelKey)[] _categories =
    {
        (DeviceCategory.Unknown,  "❓", "Cat_Unknown"),
        (DeviceCategory.PC,       "🖥",  "Cat_PC"),
        (DeviceCategory.Laptop,   "💻", "Cat_Laptop"),
        (DeviceCategory.Phone,    "📱", "Cat_Phone"),
        (DeviceCategory.Tablet,   "📱", "Cat_Tablet"),
        (DeviceCategory.Server,   "🖳",  "Cat_Server"),
        (DeviceCategory.NAS,      "💾", "Cat_NAS"),
        (DeviceCategory.Printer,  "🖨",  "Cat_Printer"),
        (DeviceCategory.Router,   "📡", "Cat_Router"),
        (DeviceCategory.IoT,      "⚡", "Cat_IoT"),
        (DeviceCategory.TV,       "📺", "Cat_TV"),
        (DeviceCategory.Console,  "🎮", "Cat_Console"),
        (DeviceCategory.Other,    "⬡",  "Cat_Other"),
    };

    public CategoryDialog(DeviceCategory current)
    {
        InitializeComponent();
        SelectedCategory = current;
        BuildButtons();
    }

    private void BuildButtons()
    {
        var L = NetworkLens.Localization.LocalizationManager.Instance;
        foreach (var (cat, icon, labelKey) in _categories)
        {
            var label = L.T(labelKey);
            var isSelected = cat == SelectedCategory;

            var btn = new Border
            {
                Width  = 110, Height = 80,
                Margin = new Thickness(4),
                CornerRadius = new CornerRadius(8),
                Background = isSelected
                    ? new SolidColorBrush(Color.FromArgb(0x33, 0xE8, 0x60, 0x0A))
                    : new SolidColorBrush(Color.FromRgb(0x1A, 0x1D, 0x21)),
                BorderThickness = new Thickness(isSelected ? 1.5 : 1),
                BorderBrush = isSelected
                    ? new SolidColorBrush(Color.FromRgb(0xE8, 0x60, 0x0A))
                    : new SolidColorBrush(Color.FromRgb(0x2D, 0x37, 0x48)),
                Cursor = Cursors.Hand,
                Tag = cat
            };

            var sp = new StackPanel
            {
                VerticalAlignment   = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            sp.Children.Add(new TextBlock
            {
                Text              = icon,
                FontSize          = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin            = new Thickness(0, 0, 0, 4)
            });
            sp.Children.Add(new TextBlock
            {
                Text              = label,
                FontSize          = 11,
                FontFamily        = new FontFamily("Verdana"),
                Foreground        = isSelected
                    ? new SolidColorBrush(Color.FromRgb(0xF0, 0xF2, 0xF5))
                    : new SolidColorBrush(Color.FromRgb(0x8A, 0x9B, 0xB0)),
                TextWrapping      = TextWrapping.Wrap,
                TextAlignment     = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            btn.Child = sp;

            btn.MouseLeftButtonUp += (s, _) =>
            {
                if (s is Border b && b.Tag is DeviceCategory c)
                {
                    SelectedCategory = c;
                    RefreshSelection();
                }
            };

            CategoryPanel.Children.Add(btn);
        }
    }

    private void RefreshSelection()
    {
        foreach (Border b in CategoryPanel.Children)
        {
            if (b.Tag is not DeviceCategory cat) continue;
            bool selected = cat == SelectedCategory;
            b.Background = selected
                ? new SolidColorBrush(Color.FromArgb(0x33, 0xE8, 0x60, 0x0A))
                : new SolidColorBrush(Color.FromRgb(0x1A, 0x1D, 0x21));
            b.BorderBrush = selected
                ? new SolidColorBrush(Color.FromRgb(0xE8, 0x60, 0x0A))
                : new SolidColorBrush(Color.FromRgb(0x2D, 0x37, 0x48));
            b.BorderThickness = new Thickness(selected ? 1.5 : 1);

            if (b.Child is StackPanel sp && sp.Children.Count > 1
                && sp.Children[1] is TextBlock lbl)
            {
                lbl.Foreground = selected
                    ? new SolidColorBrush(Color.FromRgb(0xF0, 0xF2, 0xF5))
                    : new SolidColorBrush(Color.FromRgb(0x8A, 0x9B, 0xB0));
            }
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
