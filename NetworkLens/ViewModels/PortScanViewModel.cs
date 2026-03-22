using System.Collections.ObjectModel;
using NetworkLens.Models;
using NetworkLens.Services;

namespace NetworkLens.ViewModels;

public enum PortScanProfile { Quick, Standard, Full, Custom }

public class PortScanViewModel : BaseViewModel
{
    private readonly PortScanner _portScanner;
    private CancellationTokenSource? _cts;

    public ObservableCollection<PortResult> Results { get; } = new();

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

    private string _statusText = "Bereit";
    public string StatusText
    {
        get => _statusText;
        set => Set(ref _statusText, value);
    }

    public AsyncRelayCommand StartScanCommand { get; }
    public RelayCommand StopScanCommand { get; }

    public PortScanViewModel()
    {
        _portScanner = new PortScanner();
        StartScanCommand = new AsyncRelayCommand(StartScanAsync, () => CanScan);
        StopScanCommand = new RelayCommand(() => _cts?.Cancel(), () => IsScanning);
    }

    public async Task StartScanAsync()
    {
        if (string.IsNullOrWhiteSpace(TargetIp)) return;

        IsScanning = true;
        Results.Clear();
        Progress = 0;
        _cts = new CancellationTokenSource();

        var ports = GetPortsForProfile();
        StatusText = $"Scanne {ports.Length} Ports auf {TargetIp} ...";

        try
        {
            int done = 0;
            await _portScanner.ScanAsync(
                TargetIp!,
                ports,
                onResult: result =>
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        Results.Add(result);
                        done++;
                        Progress = (int)((double)done / ports.Length * 100);
                        StatusText = $"{done}/{ports.Length} Ports — {Results.Count(r => r.Status == PortStatus.Open)} offen";
                    });
                },
                cancellationToken: _cts.Token
            );

            StatusText = $"Fertig — {Results.Count(r => r.Status == PortStatus.Open)} offene Ports";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Abgebrochen";
        }
        finally
        {
            IsScanning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private int[] GetPortsForProfile()
    {
        return Profile switch
        {
            PortScanProfile.Quick => TopPorts.Quick,
            PortScanProfile.Standard => TopPorts.Standard,
            PortScanProfile.Full => Enumerable.Range(1, 65535).ToArray(),
            PortScanProfile.Custom => ParseCustomPorts(),
            _ => TopPorts.Quick
        };
    }

    private int[] ParseCustomPorts()
    {
        var result = new List<int>();
        foreach (var part in CustomPorts.Split(',', ';', ' '))
        {
            var t = part.Trim();
            if (t.Contains('-'))
            {
                var rng = t.Split('-');
                if (rng.Length == 2 && int.TryParse(rng[0], out int lo) && int.TryParse(rng[1], out int hi))
                    result.AddRange(Enumerable.Range(lo, hi - lo + 1));
            }
            else if (int.TryParse(t, out int p))
            {
                result.Add(p);
            }
        }
        return result.Distinct().Where(p => p > 0 && p <= 65535).OrderBy(p => p).ToArray();
    }
}

public static class TopPorts
{
    public static readonly int[] Quick = new[]
    {
        21, 22, 23, 25, 53, 80, 110, 135, 139, 143,
        443, 445, 3306, 3389, 5900, 8080, 8443, 1433, 27017, 6379
    };

    public static readonly int[] Standard = new[]
    {
        21, 22, 23, 25, 53, 67, 68, 80, 110, 111,
        119, 123, 135, 137, 138, 139, 143, 161, 162, 194,
        389, 443, 445, 465, 514, 515, 587, 631, 636, 993,
        995, 1080, 1194, 1433, 1521, 1723, 1883, 2049, 2375, 2376,
        3000, 3306, 3389, 3690, 4000, 4443, 5000, 5001, 5060, 5432,
        5900, 5985, 5986, 6379, 6881, 7070, 8000, 8008, 8080, 8081,
        8083, 8086, 8088, 8443, 8883, 8888, 9000, 9090, 9200, 9300,
        10000, 27017, 32400, 49152, 51820, 500, 4500, 1701, 1194, 1293
    };
}
