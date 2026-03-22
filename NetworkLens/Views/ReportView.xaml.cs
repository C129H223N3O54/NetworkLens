using System.IO;
using System.Windows;
using System.Windows.Controls;
using NetworkLens.Models;
using NetworkLens.Services;
using NetworkLens.Helpers;
using NetworkLens.ViewModels;

namespace NetworkLens.Views;

public partial class ReportView : UserControl
{
    private ScanResult? _currentScan;
    private List<ScanResult> _history = new();
    private readonly ReportGenerator _generator = new();

    public ReportView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        LoadHistory();
    }

    // ── Called by ScanView after scan completes ──
    public void SetCurrentScan(ScanResult scan)
    {
        _currentScan = scan;
        TxtNoScan.Visibility    = Visibility.Collapsed;
        ExportButtons.Visibility = Visibility.Visible;
    }

    // ── History ───────────────────────────────────
    private void LoadHistory()
    {
        try
        {
            _history.Clear();

            if (!Directory.Exists(App.ScansPath)) return;

            foreach (var file in Directory.GetFiles(App.ScansPath, "*.json")
                         .OrderByDescending(f => f))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var scan = System.Text.Json.JsonSerializer.Deserialize<ScanResult>(json);
                    if (scan != null) _history.Add(scan);
                }
                catch { }
            }

            if (_history.Count > 0)
            {
                TxtNoHistory.Visibility = Visibility.Collapsed;
                HistoryGrid.Visibility  = Visibility.Visible;
                HistoryGrid.ItemsSource = _history;
                ComparePanel.Visibility = _history.Count >= 2 ? Visibility.Visible : Visibility.Collapsed;
                CmbBefore.ItemsSource   = _history;
                CmbAfter.ItemsSource    = _history;
                if (_history.Count >= 2)
                {
                    CmbBefore.SelectedIndex = 1; // older
                    CmbAfter.SelectedIndex  = 0; // newer
                }
            }
        }
        catch { }
    }

    private void HistoryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    // ── Export ────────────────────────────────────
    private async void BtnExportHtml_Click(object sender, RoutedEventArgs e)
    {
        if (_currentScan == null) return;
        var path = GetOutputPath();
        try
        {
            var file = await _generator.GenerateHtmlAsync(_currentScan, path);
            TxtLastExport.Text = $"Gespeichert: {file}";
            TxtLastExport.Visibility = Visibility.Visible;
            ExportHelper.OpenFile(file);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Exportieren:\n{ex.Message}", "Fehler",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnExportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (_currentScan == null) return;
        var path = GetOutputPath();
        try
        {
            var file = await _generator.GenerateCsvAsync(_currentScan, path);
            TxtLastExport.Text = $"Gespeichert: {file}";
            TxtLastExport.Visibility = Visibility.Visible;
            ExportHelper.OpenFolder(path);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler:\n{ex.Message}", "Fehler",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnExportJson_Click(object sender, RoutedEventArgs e)
    {
        if (_currentScan == null) return;
        var path = GetOutputPath();
        try
        {
            var file = await _generator.GenerateJsonAsync(_currentScan, path);
            TxtLastExport.Text = $"Gespeichert: {file}";
            TxtLastExport.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler:\n{ex.Message}", "Fehler",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string GetOutputPath()
    {
        var settings = SettingsViewModel.LoadStatic();
        var path = settings.ReportOutputPath;
        ExportHelper.EnsureDir(path);
        return path;
    }

    // ── Scan Comparison ───────────────────────────
    private void BtnCompare_Click(object sender, RoutedEventArgs e)
    {
        var before = CmbBefore.SelectedItem as ScanResult;
        var after  = CmbAfter.SelectedItem as ScanResult;

        if (before == null || after == null)
        {
            MessageBox.Show("Bitte zwei Scans auswählen.", "Vergleich",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var diff = ScanComparer.Compare(before, after);
        ShowDiff(diff);
    }

    private void ShowDiff(ScanDiff diff)
    {
        DiffPanel.Visibility = Visibility.Visible;

        bool hasChanges = diff.NewDevices.Count > 0 || diff.DisappearedDevices.Count > 0;
        TxtNoDiff.Visibility = hasChanges ? Visibility.Collapsed : Visibility.Visible;

        if (diff.NewDevices.Count > 0)
        {
            NewDevSection.Visibility = Visibility.Visible;
            TxtNewCount.Text = $"{diff.NewDevices.Count} neue Geräte";
            NewDevList.ItemsSource = diff.NewDevices;
        }
        else
        {
            NewDevSection.Visibility = Visibility.Collapsed;
        }

        if (diff.DisappearedDevices.Count > 0)
        {
            GoneDevSection.Visibility = Visibility.Visible;
            TxtGoneCount.Text = $"{diff.DisappearedDevices.Count} verschwunden";
            GoneDevList.ItemsSource = diff.DisappearedDevices;
        }
        else
        {
            GoneDevSection.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnClearHistory_Click(object sender, RoutedEventArgs e)
    {
        var r = MessageBox.Show("Scan-Verlauf wirklich löschen?", "Verlauf löschen",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r != MessageBoxResult.Yes) return;

        try
        {
            foreach (var f in Directory.GetFiles(App.ScansPath, "*.json"))
                File.Delete(f);
            LoadHistory();
        }
        catch { }
    }
}
