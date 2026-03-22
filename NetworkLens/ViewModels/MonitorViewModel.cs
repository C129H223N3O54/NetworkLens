using System.Collections.ObjectModel;
using NetworkLens.Models;
using NetworkLens.Services;

namespace NetworkLens.ViewModels;

public class MonitorEntry : BaseViewModel
{
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
        set => Set(ref _minPing, value);
    }

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

            var history = PingHistory.ToList();
            history.Add(ms);
            if (history.Count > 60) history = history.TakeLast(60).ToList();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                PingHistory.Clear();
                foreach (var h in history) PingHistory.Add(h);
                // Notify graph to redraw
                OnPropertyChanged(nameof(PingHistory));
            });

            if (history.Count > 1)
            {
                AvgPing = history.Average();
                var diffs = history.Skip(1).Zip(history, (a, b) => Math.Abs(a - b));
                Jitter = diffs.Any() ? diffs.Average() : 0;
            }
        }

        PacketLoss = TotalPings > 0 ? (double)LostPings / TotalPings * 100 : 0;
    }
}

public class MonitorViewModel : BaseViewModel
{
    public ObservableCollection<MonitorEntry> Entries { get; } = new();

    private readonly AlertService _alerts =
        new AlertService(SettingsViewModel.LoadStatic());

    // Track previous online state for change detection
    private readonly Dictionary<string, bool> _previousOnline = new();

    private int _intervalSeconds = 10;
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
        if (Entries.Any(e => e.Device.IpAddress == device.IpAddress)) return;
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
                await Task.Delay(TimeSpan.FromSeconds(IntervalSeconds), _cts.Token);
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
        var tasks = Entries.Select(async entry =>
        {
            var ip = entry.Device.IpAddress ?? "";
            bool wasOnline = _previousOnline.GetValueOrDefault(ip, true);

            try
            {
                using var ping = new System.Net.NetworkInformation.Ping();
                var reply = await ping.SendPingAsync(ip, 2000);
                long ms = reply.Status == System.Net.NetworkInformation.IPStatus.Success
                    ? reply.RoundtripTime : -1;

                bool isOnline = ms >= 0;
                entry.AddPingResult(ms);

                // Fire alerts on state change
                if (wasOnline && !isOnline)
                    _alerts.NotifyDeviceOffline(entry.Device);
                else if (!wasOnline && isOnline)
                    _alerts.NotifyDeviceOnline(entry.Device);

                _previousOnline[ip] = isOnline;
            }
            catch
            {
                entry.AddPingResult(-1);
                if (wasOnline) _alerts.NotifyDeviceOffline(entry.Device);
                _previousOnline[ip] = false;
            }
        });
        await Task.WhenAll(tasks);
    }

    public void Stop() => _cts?.Cancel();
}
