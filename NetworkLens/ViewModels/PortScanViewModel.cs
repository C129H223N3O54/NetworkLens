using System.Collections.Concurrent;
using System.Windows.Threading;
using NetworkLens.Helpers;
using NetworkLens.Localization;
using NetworkLens.Models;
using NetworkLens.Services;

namespace NetworkLens.ViewModels;

public enum PortScanProfile { Quick, Standard, Full, Custom }

public class PortScanViewModel : BaseViewModel
{
    private const int MaxParallelConnects = 200;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromMilliseconds(200);

    private readonly PortScanner _portScanner = new();
    private readonly ConcurrentQueue<PortResult> _pending = new();
    private readonly List<PortResult> _results = new();
    private readonly DispatcherTimer _flushTimer;
    private CancellationTokenSource? _cts;
    private int _totalPorts;

    private static LocalizationManager L => LocalizationManager.Instance;

    /// <summary>
    /// All results so far (UI thread only). Results arrive from many worker threads and
    /// are flushed in batches every 200 ms instead of one UI update per port — a full
    /// scan (65 535 ports) previously rebuilt the whole grid 65 535 times.
    /// </summary>
    public IReadOnlyList<PortResult> Results => _results;

    /// <summary>Raised on the UI thread after a batch of results was added (or cleared).</summary>
    public event EventHandler? ResultsChanged;

    public int OpenCount { get; private set; }
    public int FilteredCount { get; private set; }

    private string? _targetIp;
    public string? TargetIp
    {
        get => _targetIp;
        set => Set(ref _targetIp, value);
    }

    private PortScanProfile _profile = PortScanProfile.Quick;
    public PortScanProfile Profile
    {
        get => _profile;
        set => Set(ref _profile, value);
    }

    private string _customPorts = "80,443,8080,8443";
    public string CustomPorts
    {
        get => _customPorts;
        set => Set(ref _customPorts, value);
    }

    private bool _isScanning;
    public bool IsScanning
    {
        get => _isScanning;
        set { Set(ref _isScanning, value); OnPropertyChanged(nameof(CanScan)); }
    }

    public bool CanScan => !IsScanning && !string.IsNullOrWhiteSpace(TargetIp);

    private int _progress;
    public int Progress
    {
        get => _progress;
        set => Set(ref _progress, value);
    }

    private string _statusText;
    public string StatusText
    {
        get => _statusText;
        set => Set(ref _statusText, value);
    }

    public AsyncRelayCommand StartScanCommand { get; }
    public RelayCommand StopScanCommand { get; }

    public PortScanViewModel()
    {
        _statusText = L.T("Status_Ready");
        _flushTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = FlushInterval };
        _flushTimer.Tick += (_, _) => Flush();

        StartScanCommand = new AsyncRelayCommand(StartScanAsync, () => CanScan);
        StopScanCommand = new RelayCommand(() => _cts?.Cancel(), () => IsScanning);
    }

    public async Task StartScanAsync()
    {
        if (IsScanning || string.IsNullOrWhiteSpace(TargetIp)) return;

        var ports = GetPortsForProfile();
        if (ports.Length == 0)
        {
            StatusText = L.T("Port_NoValidPorts");
            return;
        }

        IsScanning = true;
        ClearResults();
        Progress = 0;
        _totalPorts = ports.Length;
        _cts = new CancellationTokenSource();

        _portScanner.Configure(SettingsStore.Current.PortScanTimeoutMs, MaxParallelConnects);
        StatusText = string.Format(L.T("Port_ScanningFmt"), ports.Length, TargetIp);
        _flushTimer.Start();

        try
        {
            await _portScanner.ScanAsync(TargetIp!, ports, _pending.Enqueue, _cts.Token);
            Flush();
            StatusText = string.Format(L.T("Port_DoneFmt"), OpenCount);
        }
        catch (OperationCanceledException)
        {
            Flush();
            StatusText = L.T("Common_Cancelled");
        }
        catch (Exception ex)
        {
            Log.Warn($"Port scan {TargetIp}", ex);
            Flush();
            StatusText = string.Format(L.T("Stat_Error"), ex.Message);
        }
        finally
        {
            _flushTimer.Stop();
            IsScanning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void ClearResults()
    {
        while (_pending.TryDequeue(out _)) { }
        _results.Clear();
        OpenCount = FilteredCount = 0;
        ResultsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Moves queued results into <see cref="Results"/> (UI thread).</summary>
    private void Flush()
    {
        if (_pending.IsEmpty) return;

        while (_pending.TryDequeue(out var r))
        {
            _results.Add(r);
            if (r.Status == PortStatus.Open) OpenCount++;
            else if (r.Status == PortStatus.Filtered) FilteredCount++;
        }

        Progress = _totalPorts > 0 ? (int)(_results.Count * 100L / _totalPorts) : 100;
        if (IsScanning)
            StatusText = string.Format(L.T("Port_ProgressFmt"), _results.Count, _totalPorts, OpenCount);

        ResultsChanged?.Invoke(this, EventArgs.Empty);
    }

    private int[] GetPortsForProfile() => Profile switch
    {
        PortScanProfile.Quick    => TopPorts.Quick,
        PortScanProfile.Standard => TopPorts.Standard,
        PortScanProfile.Full     => Enumerable.Range(1, 65535).ToArray(),
        PortScanProfile.Custom   => ParsePortList(CustomPorts),
        _                        => TopPorts.Quick
    };

    /// <summary>
    /// Parses "80,443 8000-8100;22" into a sorted, distinct port list.
    /// Reversed ranges are swapped and everything is clamped to 1–65535, so input like
    /// "9000-80" or "1-999999999" can no longer crash the app or allocate gigabytes.
    /// </summary>
    public static int[] ParsePortList(string? text)
    {
        var ports = new SortedSet<int>();
        var parts = (text ?? "").Split(new[] { ',', ';', ' ' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            int dash = part.IndexOf('-');
            if (dash > 0)
            {
                if (!int.TryParse(part[..dash], out int lo) || !int.TryParse(part[(dash + 1)..], out int hi))
                    continue;
                if (lo > hi) (lo, hi) = (hi, lo);
                lo = Math.Max(lo, 1);
                hi = Math.Min(hi, 65535);
                for (int p = lo; p <= hi; p++) ports.Add(p);
            }
            else if (int.TryParse(part, out int single) && single is >= 1 and <= 65535)
            {
                ports.Add(single);
            }
        }
        return ports.ToArray();
    }
}

public static class TopPorts
{
    public static readonly int[] Quick = new[]
    {
        21, 22, 23, 25, 53, 80, 110, 135, 139, 143,
        443, 445, 3306, 3389, 5900, 8080, 8443, 1433, 27017, 6379
    }.Distinct().ToArray();

    public static readonly int[] Standard = new[]
    {
        21, 22, 23, 25, 53, 67, 68, 80, 110, 111,
        119, 123, 135, 137, 138, 139, 143, 161, 162, 194,
        389, 443, 445, 465, 514, 515, 587, 631, 636, 993,
        995, 1080, 1194, 1433, 1521, 1723, 1883, 2049, 2375, 2376,
        3000, 3306, 3389, 3690, 4000, 4443, 5000, 5001, 5060, 5432,
        5900, 5985, 5986, 6379, 6881, 7070, 8000, 8008, 8080, 8081,
        8083, 8086, 8088, 8443, 8883, 8888, 9000, 9090, 9200, 9300,
        10000, 27017, 32400, 49152, 51820, 500, 4500, 1701, 1293
    }.Distinct().ToArray();
}
