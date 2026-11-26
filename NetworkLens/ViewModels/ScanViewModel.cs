using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Windows.Data;
using System.Windows.Threading;
using NetworkLens.Helpers;
using NetworkLens.Localization;
using NetworkLens.Models;
using NetworkLens.Services;

namespace NetworkLens.ViewModels;

public class ScanViewModel : BaseViewModel
{
    private const int MaxSavedScans = 50;
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private readonly NetworkScanner _scanner = new();
    private readonly MacLookup _macLookup = new();
    private readonly DeviceManager _deviceManager = new();
    private readonly AlertService _alerts = new();
    private readonly Dispatcher _dispatcher;
    private CancellationTokenSource? _cts;

    private static LocalizationManager L => LocalizationManager.Instance;

    // ── Observable Data ────────────────────────────
    // Only ever modified on the UI thread (results are marshalled via the dispatcher).
    public ObservableCollection<NetworkDevice> Devices { get; } = new();

    private string _subnet = "192.168.1.0/24";
    public string Subnet
    {
        get => _subnet;
        set => Set(ref _subnet, value);
    }

    private bool _isScanning;
    public bool IsScanning
    {
        get => _isScanning;
        set
        {
            Set(ref _isScanning, value);
            OnPropertyChanged(nameof(CanScan));
            OnPropertyChanged(nameof(CanStop));
        }
    }

    public bool CanScan => !IsScanning;
    public bool CanStop => IsScanning;

    private int _progress;
    public int Progress
    {
        get => _progress;
        set => Set(ref _progress, value);
    }

    private string _progressLabel;
    public string ProgressLabel
    {
        get => _progressLabel;
        set => Set(ref _progressLabel, value);
    }

    private string _progressDetail = "";
    public string ProgressDetail
    {
        get => _progressDetail;
        set => Set(ref _progressDetail, value);
    }

    private string _statusText;
    public string StatusText
    {
        get => _statusText;
        set => Set(ref _statusText, value);
    }

    private TimeSpan _scanDuration;
    public TimeSpan ScanDuration
    {
        get => _scanDuration;
        set { Set(ref _scanDuration, value); OnPropertyChanged(nameof(ScanDurationText)); }
    }

    public string ScanDurationText => ScanDuration.TotalSeconds >= 1
        ? $"{ScanDuration.TotalSeconds:F1}s"
        : $"{ScanDuration.TotalMilliseconds:F0}ms";

    private int _newDeviceCount;
    public int NewDeviceCount
    {
        get => _newDeviceCount;
        set => Set(ref _newDeviceCount, value);
    }

    // ── Search / Filter ────────────────────────────
    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            Set(ref _searchText, value);
            DevicesView?.Refresh();
        }
    }

    public ICollectionView? DevicesView { get; }

    // ── Last scan result (for Report export) ──────
    public ScanResult? LastScanResult { get; private set; }

    // ── Commands ───────────────────────────────────
    public AsyncRelayCommand StartScanCommand { get; }
    public RelayCommand StopScanCommand { get; }
    public RelayCommand AutoDetectSubnetCommand { get; }

    public ScanViewModel()
    {
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        _progressLabel = L.T("Status_Ready");
        _statusText = L.T("Status_Ready");

        DevicesView = CollectionViewSource.GetDefaultView(Devices);
        DevicesView.Filter = FilterDevice;
        DevicesView.SortDescriptions.Add(
            new SortDescription(nameof(NetworkDevice.IpSort), ListSortDirection.Ascending));

        StartScanCommand = new AsyncRelayCommand(StartScanAsync, () => CanScan);
        StopScanCommand = new RelayCommand(StopScan, () => CanStop);
        AutoDetectSubnetCommand = new RelayCommand(AutoDetectSubnet);
    }

    // ── Scan ───────────────────────────────────────
    public async Task StartScanAsync()
    {
        if (IsScanning) return;

        // Validate input before touching any state
        var subnet = Subnet?.Trim() ?? "";
        if (!IpHelper.TryGetHostRange(subnet, out uint first, out uint last))
        {
            StatusText = ProgressLabel = string.Format(L.T("Err_SubnetInvalid"), subnet);
            return;
        }
        long hostCount = IpHelper.HostCount(first, last);
        if (hostCount > NetworkScanner.MaxHosts)
        {
            StatusText = ProgressLabel = string.Format(L.T("Err_SubnetTooLargeFmt"), hostCount, NetworkScanner.MaxHosts);
            return;
        }

        IsScanning = true;
        Progress = 0;
        ProgressDetail = "";
        NewDeviceCount = 0;
        Devices.Clear();

        var settings = SettingsStore.Current;
        _scanner.Configure(settings.ScanTimeoutMs, settings.MaxParallelThreads);

        _cts = new CancellationTokenSource();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        int total = (int)hostCount, online = 0, lastReportedPct = -1;

        try
        {
            ProgressLabel = string.Format(L.T("Scan_ScanningFmt"), subnet);
            StatusText = string.Format(L.T("Scan_ScanningStatusFmt"), subnet);

            var previousDevices = await _deviceManager.LoadDevicesAsync();

            await _scanner.ScanAsync(
                subnet,
                onHostFound: device =>
                {
                    // Runs on a worker thread: enrich the device, then hand it to the UI thread.
                    bool isNew = false;
                    var key = !string.IsNullOrEmpty(device.MacAddress) ? device.MacAddress : device.IpAddress ?? "";
                    if (previousDevices.TryGetValue(key, out var saved))
                    {
                        device.Alias      = saved.Alias;
                        device.Category   = saved.Category;
                        device.Notes      = saved.Notes;
                        device.IsFavorite = saved.IsFavorite;
                    }
                    else if (!string.IsNullOrEmpty(device.MacAddress))
                    {
                        device.IsNew = isNew = true;
                        _alerts.NotifyNewDevice(device);
                    }

                    device.Manufacturer = _macLookup.Lookup(device.MacAddress);
                    Interlocked.Increment(ref online);

                    // Non-blocking hand-off; no lock is held while waiting for the UI thread.
                    _dispatcher.InvokeAsync(() =>
                    {
                        Devices.Add(device);
                        if (isNew) NewDeviceCount++;
                    });
                },
                onProgress: (scanned, totalHosts) =>
                {
                    // Throttle UI updates to percent steps instead of one per host.
                    int pct = totalHosts > 0 ? (int)(scanned * 100L / totalHosts) : 100;
                    if (Interlocked.Exchange(ref lastReportedPct, pct) == pct && scanned != totalHosts)
                        return;

                    _dispatcher.InvokeAsync(() =>
                    {
                        Progress = pct;
                        ProgressDetail = string.Format(L.T("Scan_Progress"), scanned, totalHosts, Volatile.Read(ref online));
                    });
                },
                cancellationToken: _cts.Token);

            // Let all queued UI hand-offs run before taking the snapshot.
            await _dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);

            sw.Stop();
            ScanDuration = sw.Elapsed;

            var snapshot = Devices.ToList();
            await _deviceManager.MergeAndSaveAsync(snapshot);

            var scanResult = new ScanResult
            {
                Subnet            = subnet,
                TotalHostsScanned = total,
                Duration          = sw.Elapsed,
                Devices           = snapshot,
                NewDeviceCount    = NewDeviceCount,
                Timestamp         = DateTime.Now
            };
            LastScanResult = scanResult;
            await SaveScanHistoryAsync(scanResult);

            StatusText = string.Format(L.T("Stat_ScanComplete"), Devices.Count);
            ProgressLabel = L.T("Status_Done");
            Progress = 100;
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            ScanDuration = sw.Elapsed;
            StatusText = L.T("Scan_Cancelled");
            ProgressLabel = L.T("Common_Cancelled");
        }
        catch (Exception ex)
        {
            Log.Error("Network scan", ex);
            StatusText = string.Format(L.T("Stat_Error"), ex.Message);
            ProgressLabel = L.T("Stat_ScanError");
        }
        finally
        {
            IsScanning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    public void StopScan() => _cts?.Cancel();

    private void AutoDetectSubnet()
    {
        Subnet = IpHelper.GetLocalSubnetCidr() ?? "192.168.1.0/24";
    }

    // ── Save scan snapshot ─────────────────────────
    private static async Task SaveScanHistoryAsync(ScanResult scan)
    {
        try
        {
            var path = Path.Combine(App.ScansPath, $"scan_{scan.FilenameSlug}.json");
            var json = await Task.Run(() => JsonSerializer.Serialize(scan, JsonOpts));
            await AtomicFile.WriteAllTextAsync(path, json);

            // Keep only the newest scans
            foreach (var old in Directory.GetFiles(App.ScansPath, "scan_*.json")
                         .OrderByDescending(f => f, StringComparer.Ordinal)
                         .Skip(MaxSavedScans))
            {
                try { File.Delete(old); }
                catch (Exception ex) { Log.Warn("Delete old scan", ex); }
            }
        }
        catch (Exception ex)
        {
            Log.Warn("SaveScanHistory", ex);
        }
    }

    // ── Filter ─────────────────────────────────────
    private bool FilterDevice(object obj)
    {
        if (obj is not NetworkDevice d) return false;
        var q = SearchText;
        if (string.IsNullOrWhiteSpace(q)) return true;

        return Contains(d.IpAddress) || Contains(d.Hostname) || Contains(d.Alias)
            || Contains(d.MacAddress) || Contains(d.Manufacturer);

        bool Contains(string? field) =>
            field != null && field.Contains(q, StringComparison.OrdinalIgnoreCase);
    }
}
