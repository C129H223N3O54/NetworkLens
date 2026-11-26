using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using NetworkLens.Models;
using NetworkLens.Services;

namespace NetworkLens.ViewModels;

public class MonitorEntry : BaseViewModel
{
    public const int HistoryLength = 60;

    public NetworkDevice Device { get; set; } = null!;
    public ObservableCollection<long> PingHistory { get; } = new();

    private DeviceStatus _status = DeviceStatus.Unknown;
    public DeviceStatus Status
    {
        get => _status;
        set { Set(ref _status, value); OnPropertyChanged(nameof(StatusIcon)); }
    }

    public string StatusIcon => Status switch
    {
        DeviceStatus.Online  => "🟢",
        DeviceStatus.Slow    => "🟡",
        DeviceStatus.Offline => "🔴",
        _                    => "⚪"
    };

    private long _lastPing = -1;
    public long LastPing
    {
        get => _lastPing;
        set => Set(ref _lastPing, value);
    }

    private long _minPing = long.MaxValue;
    public long MinPing
    {
        get => _minPing;
        set { Set(ref _minPing, value); OnPropertyChanged(nameof(MinPingText)); }
    }

    /// <summary>"—" until the first successful ping (instead of 9223372036854775807).</summary>
    public string MinPingText => MinPing == long.MaxValue ? "—" : MinPing.ToString();

    private long _maxPing = 0;
    public long MaxPing
    {
        get => _maxPing;
        set => Set(ref _maxPing, value);
    }

    private double _avgPing;
    public double AvgPing
    {
        get => _avgPing;
        set => Set(ref _avgPing, value);
    }

    private double _jitter;
    public double Jitter
    {
        get => _jitter;
        set { Set(ref _jitter, value); OnPropertyChanged(nameof(JitterIcon)); }
    }

    public string JitterIcon => Jitter switch
    {
        < 5  => "🟢",
        < 20 => "🟡",
        _    => "🔴"
    };

    private double _packetLoss;
    public double PacketLoss
    {
        get => _packetLoss;
        set => Set(ref _packetLoss, value);
    }

    private int _totalPings;
    public int TotalPings
    {
        get => _totalPings;
        set => Set(ref _totalPings, value);
    }

    private int _lostPings;
    public int LostPings
    {
        get => _lostPings;
        set => Set(ref _lostPings, value);
    }

    /// <summary>Records one ping result. Must be called on the UI thread.</summary>
    public void AddPingResult(long ms)
    {
        TotalPings++;
        if (ms < 0)
        {
            LostPings++;
            Status = DeviceStatus.Offline;
        }
        else
        {
            LastPing = ms;
            Status = ms > 200 ? DeviceStatus.Slow : DeviceStatus.Online;

            if (ms < MinPing) MinPing = ms;
            if (ms > MaxPing) MaxPing = ms;

            // Rolling window: one remove + one add instead of clearing and
            // re-adding all 60 values (= 61 collection events) per ping.
            if (PingHistory.Count >= HistoryLength) PingHistory.RemoveAt(0);
            PingHistory.Add(ms);
            OnPropertyChanged(nameof(PingHistory)); // triggers graph redraw

            if (PingHistory.Count > 1)
            {
                double sum = 0, jitterSum = 0;
                for (int i = 0; i < PingHistory.Count; i++)
                {
                    sum += PingHistory[i];
                    if (i > 0) jitterSum += Math.Abs(PingHistory[i] - PingHistory[i - 1]);
                }
                AvgPing = sum / PingHistory.Count;
                Jitter = jitterSum / (PingHistory.Count - 1);
            }
        }

        PacketLoss = TotalPings > 0 ? (double)LostPings / TotalPings * 100 : 0;
    }
}

public class MonitorViewModel : BaseViewModel
{
    public ObservableCollection<MonitorEntry> Entries { get; } = new();

    private const int PingTimeoutMs = 2000;

    private readonly AlertService _alerts = new();

    // Track previous online state for change detection
    private readonly Dictionary<string, bool> _previousOnline = new();

    private int _intervalSeconds = SettingsStore.Current.MonitorIntervalSeconds;
    public int IntervalSeconds
    {
        get => _intervalSeconds;
        set => Set(ref _intervalSeconds, value);
    }

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set => Set(ref _isRunning, value);
    }

    private CancellationTokenSource? _cts;

    public AsyncRelayCommand StartCommand { get; }
    public RelayCommand StopCommand { get; }

    public MonitorViewModel()
    {
        StartCommand = new AsyncRelayCommand(StartAsync, () => !IsRunning);
        StopCommand = new RelayCommand(Stop, () => IsRunning);
    }

    public void AddDevice(NetworkDevice device)
    {
        if (string.IsNullOrWhiteSpace(device.IpAddress)) return;
        if (Entries.Any(e => string.Equals(e.Device.IpAddress, device.IpAddress, StringComparison.OrdinalIgnoreCase))) return;
        Entries.Add(new MonitorEntry { Device = device, Status = device.Status });
    }

    public async Task StartAsync()
    {
        if (IsRunning) return;
        IsRunning = true;
        _cts = new CancellationTokenSource();

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                await PingAllAsync(_cts.Token);
                if (IntervalSeconds > 0)
                    await Task.Delay(TimeSpan.FromSeconds(IntervalSeconds), _cts.Token);
                else
                    await Task.Delay(250, _cts.Token); // "continuous": short pause so the UI stays responsive
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            IsRunning = false;
        }
    }

    private async Task PingAllAsync(CancellationToken ct)
    {
        // Snapshot: the user may add/remove entries while pings are in flight.
        var tasks = Entries.ToList().Select(async entry =>
        {
            var ip = entry.Device.IpAddress ?? "";
            bool wasOnline = _previousOnline.GetValueOrDefault(ip, true);

            long ms;
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ip, PingTimeoutMs);
                ms = reply.Status == IPStatus.Success ? reply.RoundtripTime : -1;
            }
            catch
            {
                ms = -1;
            }
            ct.ThrowIfCancellationRequested();

            // Continuation runs on the UI thread (captured context).
            bool isOnline = ms >= 0;
            entry.AddPingResult(ms);

            if (wasOnline && !isOnline)
                _alerts.NotifyDeviceOffline(entry.Device);
            else if (!wasOnline && isOnline)
                _alerts.NotifyDeviceOnline(entry.Device);

            _previousOnline[ip] = isOnline;
        });
        await Task.WhenAll(tasks);
    }

    public void Stop() => _cts?.Cancel();
}
