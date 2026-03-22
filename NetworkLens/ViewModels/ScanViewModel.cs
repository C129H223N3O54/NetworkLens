using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using NetworkLens.Models;
using NetworkLens.Services;
using NetworkLens.ViewModels;

namespace NetworkLens.ViewModels;

public class ScanViewModel : BaseViewModel
{
    private readonly NetworkScanner _scanner;
    private readonly MacLookup _macLookup;
    private readonly DeviceManager _deviceManager;
    private readonly AlertService _alerts;
    private CancellationTokenSource? _cts;

    // ── Observable Data ────────────────────────────
    public ObservableCollection<NetworkDevice> Devices { get; } = new();

    private readonly object _devicesLock = new();

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

    private string _progressLabel = "Bereit";
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

    private string _statusText = "Bereit";
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

    public ICollectionView? DevicesView { get; private set; }

    // ── Last scan result (for Report export) ──────
    public ScanResult? LastScanResult { get; private set; }

    // ── Commands ───────────────────────────────────
    public AsyncRelayCommand StartScanCommand { get; }
    public RelayCommand StopScanCommand { get; }
    public RelayCommand AutoDetectSubnetCommand { get; }

    public ScanViewModel()
    {
        _scanner = new NetworkScanner();
        _macLookup = new MacLookup();
        _deviceManager = new DeviceManager();
        _alerts = new AlertService(SettingsViewModel.LoadStatic());

        BindingOperations.EnableCollectionSynchronization(Devices, _devicesLock);

        DevicesView = CollectionViewSource.GetDefaultView(Devices);
        DevicesView.Filter = FilterDevice;

        StartScanCommand = new AsyncRelayCommand(StartScanAsync, () => CanScan);
        StopScanCommand = new RelayCommand(StopScan, () => CanStop);
        AutoDetectSubnetCommand = new RelayCommand(AutoDetectSubnet);
    }

    // ── Scan ───────────────────────────────────────
    public async Task StartScanAsync()
    {
        if (IsScanning) return;

        IsScanning = true;
        Progress = 0;
        NewDeviceCount = 0;
        Devices.Clear();

        _cts = new CancellationTokenSource();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            ProgressLabel = $"Scanne {Subnet} ...";
            StatusText = $"Scanne {Subnet}";

            var previousDevices = await _deviceManager.LoadDevicesAsync();

            int total = 0, done = 0, online = 0;

            await _scanner.ScanAsync(
                Subnet,
                onHostFound: device =>
                {
                    // Restore alias/category from device manager
                    if (previousDevices.TryGetValue(device.MacAddress ?? device.IpAddress ?? "", out var saved))
                    {
                        device.Alias    = saved.Alias;
                        device.Category = saved.Category;
                        device.Notes    = saved.Notes;
                        device.IsFavorite = saved.IsFavorite;
                    }
                    else if (!string.IsNullOrEmpty(device.MacAddress))
                    {
                        device.IsNew = true;
                        System.Threading.Interlocked.Increment(ref _newDeviceCount);
                        OnPropertyChanged(nameof(NewDeviceCount));
                        _alerts.NotifyNewDevice(device);
                    }

                    // Manufacturer lookup
                    device.Manufacturer = _macLookup.Lookup(device.MacAddress);

                    lock (_devicesLock)
                        System.Windows.Application.Current.Dispatcher.Invoke(
                            () => Devices.Add(device));

                    System.Threading.Interlocked.Increment(ref online);
                },
                onProgress: (scanned, totalHosts) =>
                {
                    total = totalHosts;
                    done = scanned;
                    var pct = total > 0 ? (int)((double)done / total * 100) : 0;
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        Progress = pct;
                        ProgressDetail = $"{done} / {total} Hosts geprüft — {online} online";
                    });
                },
                cancellationToken: _cts.Token
            );

            sw.Stop();
            ScanDuration = sw.Elapsed;

            // Save to device manager
            await _deviceManager.MergeAndSaveAsync(Devices);

            // Build scan result snapshot
            var scanResult = new ScanResult
            {
                Subnet            = Subnet,
                TotalHostsScanned = total,
                Duration          = sw.Elapsed,
                Devices           = Devices.ToList(),
                NewDeviceCount    = NewDeviceCount,
                Timestamp         = DateTime.Now
            };
            LastScanResult = scanResult;
            await SaveScanHistoryAsync(scanResult);

            StatusText = $"Scan abgeschlossen — {Devices.Count} Geräte";
            ProgressLabel = "Scan abgeschlossen";
            Progress = 100;
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            ScanDuration = sw.Elapsed;
            StatusText = "Scan abgebrochen";
            ProgressLabel = "Abgebrochen";
        }
        catch (Exception ex)
        {
            StatusText = $"Fehler: {ex.Message}";
            ProgressLabel = "Fehler beim Scan";
        }
        finally
        {
            IsScanning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    public void StopScan()
    {
        _cts?.Cancel();
    }

    private void AutoDetectSubnet()
    {
        Subnet = IpHelper.GetLocalSubnetCidr() ?? "192.168.1.0/24";
    }

    // ── Save scan snapshot ─────────────────────────
    private static async Task SaveScanHistoryAsync(ScanResult scan)
    {
        try
        {
            var path = System.IO.Path.Combine(App.ScansPath,
                $"scan_{scan.FilenameSlug}.json");
            var json = System.Text.Json.JsonSerializer.Serialize(scan,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(path, json);

            // Keep only last 50 scans
            var files = System.IO.Directory.GetFiles(App.ScansPath, "scan_*.json")
                .OrderByDescending(f => f).Skip(50).ToArray();
            foreach (var f in files)
                try { System.IO.File.Delete(f); } catch { }
        }
        catch { /* best effort */ }
    }

    // ── Filter ─────────────────────────────────────
    private bool FilterDevice(object obj)
    {
        if (obj is not NetworkDevice d) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var q = SearchText.ToLowerInvariant();
        return (d.IpAddress?.ToLower().Contains(q) ?? false)
            || (d.Hostname?.ToLower().Contains(q) ?? false)
            || (d.Alias?.ToLower().Contains(q) ?? false)
            || (d.MacAddress?.ToLower().Contains(q) ?? false)
            || (d.Manufacturer?.ToLower().Contains(q) ?? false);
    }
}
