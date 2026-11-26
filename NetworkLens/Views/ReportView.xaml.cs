using System.Text.Json;
using NetworkLens.Helpers;
using NetworkLens.Models;
using NetworkLens.Services;

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

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadHistoryAsync();
    }

    // ── Called by MainWindow when navigating to the report tab ──
    public async void SetCurrentScan(ScanResult scan)
    {
        _currentScan = scan;
        TxtNoScan.Visibility     = Visibility.Collapsed;
        ExportButtons.Visibility = Visibility.Visible;

        // Previously the history was only read once at app start, so scans made
        // during the session never showed up in history / compare.
        await LoadHistoryAsync();
    }

    // ── History ───────────────────────────────────
    private async Task LoadHistoryAsync()
    {
        try
        {
            // Reading and deserializing up to 50 scan files happens off the UI thread.
            _history = await Task.Run(() => ReadHistory());

            bool any = _history.Count > 0;
            TxtNoHistory.Visibility = any ? Visibility.Collapsed : Visibility.Visible;
            HistoryGrid.Visibility  = any ? Visibility.Visible : Visibility.Collapsed;
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
        catch (Exception ex)
        {
            Log.Warn("ReportView.LoadHistory", ex);
        }
    }

    private static List<ScanResult> ReadHistory()
    {
        var list = new List<ScanResult>();
        if (!Directory.Exists(App.ScansPath)) return list;

        foreach (var file in Directory.GetFiles(App.ScansPath, "scan_*.json")
                     .OrderByDescending(f => f, StringComparer.Ordinal))
        {
            try
            {
                var scan = JsonSerializer.Deserialize<ScanResult>(File.ReadAllText(file));
                if (scan != null) list.Add(scan);
            }
            catch (Exception ex)
            {
                Log.Warn($"Read scan {Path.GetFileName(file)}", ex);
            }
        }
        return list;
    }

    private void HistoryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    // ── Export ────────────────────────────────────
    private async void BtnExportHtml_Click(object sender, RoutedEventArgs e)
    {
        if (_currentScan == null) return;
        try
        {
            var path = SettingsStore.ResolveReportPath();
            var file = await _generator.GenerateHtmlAsync(_currentScan, path);
            TxtLastExport.Text = string.Format(Localization.LocalizationManager.Instance.T("Rpt_SavedFmt"), file);
            TxtLastExport.Visibility = Visibility.Visible;
            ShellHelper.OpenFile(file);
        }
        catch (Exception ex)
        {
            Log.Warn("Export HTML", ex);
            var L1 = Localization.LocalizationManager.Instance;
            MessageBox.Show(string.Format(L1.T("Msg_ExportError"), ex.Message), L1.T("Msg_Error"),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnExportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (_currentScan == null) return;
        try
        {
            var path = SettingsStore.ResolveReportPath();
            var file = await _generator.GenerateCsvAsync(_currentScan, path);
            TxtLastExport.Text = string.Format(Localization.LocalizationManager.Instance.T("Rpt_SavedFmt"), file);
            TxtLastExport.Visibility = Visibility.Visible;
            ShellHelper.OpenFolder(path);
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(Localization.LocalizationManager.Instance.T("Msg_GenericError"), ex.Message), Localization.LocalizationManager.Instance.T("Msg_Error"),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnExportJson_Click(object sender, RoutedEventArgs e)
    {
        if (_currentScan == null) return;
        try
        {
            var path = SettingsStore.ResolveReportPath();
            var file = await _generator.GenerateJsonAsync(_currentScan, path);
            TxtLastExport.Text = string.Format(Localization.LocalizationManager.Instance.T("Rpt_SavedFmt"), file);
            TxtLastExport.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(Localization.LocalizationManager.Instance.T("Msg_GenericError"), ex.Message), Localization.LocalizationManager.Instance.T("Msg_Error"),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Scan Comparison ───────────────────────────
    private void BtnCompare_Click(object sender, RoutedEventArgs e)
    {
        var before = CmbBefore.SelectedItem as ScanResult;
        var after  = CmbAfter.SelectedItem as ScanResult;

        if (before == null || after == null)
        {
            var L2 = Localization.LocalizationManager.Instance;
            MessageBox.Show(L2.T("Msg_SelectTwoScans"), L2.T("Msg_CompareTitle"),
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
            TxtNewCount.Text = string.Format(Localization.LocalizationManager.Instance.T("Rpt_NewDevices"), diff.NewDevices.Count);
            NewDevList.ItemsSource = diff.NewDevices;
        }
        else
        {
            NewDevSection.Visibility = Visibility.Collapsed;
        }

        if (diff.DisappearedDevices.Count > 0)
        {
            GoneDevSection.Visibility = Visibility.Visible;
            TxtGoneCount.Text = string.Format(Localization.LocalizationManager.Instance.T("Rpt_DisappearedDevices"), diff.DisappearedDevices.Count);
            GoneDevList.ItemsSource = diff.DisappearedDevices;
        }
        else
        {
            GoneDevSection.Visibility = Visibility.Collapsed;
        }
    }

    private async void BtnClearHistory_Click(object sender, RoutedEventArgs e)
    {
        var r = MessageBox.Show(Localization.LocalizationManager.Instance.T("Rep_ConfirmClear"), Localization.LocalizationManager.Instance.T("Rep_ClearTitle"),
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r != MessageBoxResult.Yes) return;

        try
        {
            foreach (var f in Directory.GetFiles(App.ScansPath, "scan_*.json"))
                File.Delete(f);
        }
        catch (Exception ex)
        {
            Log.Warn("Clear history", ex);
        }
        await LoadHistoryAsync();
    }
}
