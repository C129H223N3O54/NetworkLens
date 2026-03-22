using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace NetworkLens.Models;

public enum DeviceStatus { Unknown, Online, Slow, Offline }
public enum DeviceCategory { Unknown, PC, Laptop, Phone, Tablet, Server, NAS, Printer, Router, IoT, TV, Console, Other }

public class NetworkDevice : INotifyPropertyChanged
{
    // ── Core ────────────────────────────────────────
    private string? _ipAddress;
    public string? IpAddress
    {
        get => _ipAddress;
        set { _ipAddress = value; OnPropertyChanged(); OnPropertyChanged(nameof(IpSort)); }
    }

    private string? _hostname;
    public string? Hostname
    {
        get => _hostname;
        set { _hostname = value; OnPropertyChanged(); }
    }

    private string? _macAddress;
    public string? MacAddress
    {
        get => _macAddress;
        set { _macAddress = value; OnPropertyChanged(); }
    }

    private string? _manufacturer;
    public string? Manufacturer
    {
        get => _manufacturer;
        set { _manufacturer = value; OnPropertyChanged(); }
    }

    // ── Status / Timing ────────────────────────────
    private DeviceStatus _status = DeviceStatus.Unknown;
    public DeviceStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(StatusColorRaw));
            OnPropertyChanged(nameof(StatusSort));
        }
    }

    private long _responseTime = -1;
    public long ResponseTime
    {
        get => _responseTime;
        set
        {
            _responseTime = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PingColor));
        }
    }

    // ── User Data ──────────────────────────────────
    private string? _alias;
    public string? Alias
    {
        get => _alias;
        set { _alias = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
    }

    private DeviceCategory _category = DeviceCategory.Unknown;
    public DeviceCategory Category
    {
        get => _category;
        set { _category = value; OnPropertyChanged(); OnPropertyChanged(nameof(CategoryIcon)); }
    }

    private string? _notes;
    public string? Notes
    {
        get => _notes;
        set { _notes = value; OnPropertyChanged(); }
    }

    private bool _isFavorite;
    public bool IsFavorite
    {
        get => _isFavorite;
        set { _isFavorite = value; OnPropertyChanged(); OnPropertyChanged(nameof(FavoriteIcon)); }
    }

    // ── Port Info ──────────────────────────────────
    private int _openPortCount;
    public int OpenPortCount
    {
        get => _openPortCount;
        set { _openPortCount = value; OnPropertyChanged(); }
    }

    public List<PortResult> OpenPorts { get; set; } = new();

    // ── Timestamps ────────────────────────────────
    public DateTime FirstSeen { get; set; } = DateTime.Now;
    public DateTime LastSeen { get; set; } = DateTime.Now;
    public bool IsNew { get; set; } = false;

    // ── Computed Properties (UI Bindings) ─────────

    [JsonIgnore] public string DisplayName => !string.IsNullOrWhiteSpace(Alias) ? Alias
        : !string.IsNullOrWhiteSpace(Hostname) ? Hostname
        : IpAddress ?? "Unbekannt";

    [JsonIgnore] public string FavoriteIcon => IsFavorite ? "⭐" : "☆";

    [JsonIgnore] public string CategoryIcon => Category switch
    {
        DeviceCategory.PC       => "🖥",
        DeviceCategory.Laptop   => "💻",
        DeviceCategory.Phone    => "📱",
        DeviceCategory.Tablet   => "📱",
        DeviceCategory.Server   => "🖳",
        DeviceCategory.NAS      => "💾",
        DeviceCategory.Printer  => "🖨",
        DeviceCategory.Router   => "📡",
        DeviceCategory.IoT      => "⚡",
        DeviceCategory.TV       => "📺",
        DeviceCategory.Console  => "🎮",
        _                       => "❓"
    };

    [JsonIgnore] public Brush StatusColor => Status switch
    {
        DeviceStatus.Online  => new SolidColorBrush(Color.FromRgb(0x00, 0xE6, 0x76)),
        DeviceStatus.Slow    => new SolidColorBrush(Color.FromRgb(0xFF, 0xD6, 0x00)),
        DeviceStatus.Offline => new SolidColorBrush(Color.FromRgb(0xFF, 0x17, 0x44)),
        _                    => new SolidColorBrush(Color.FromRgb(0x4A, 0x55, 0x68))
    };

    [JsonIgnore] public Color StatusColorRaw => Status switch
    {
        DeviceStatus.Online  => Color.FromRgb(0x00, 0xE6, 0x76),
        DeviceStatus.Slow    => Color.FromRgb(0xFF, 0xD6, 0x00),
        DeviceStatus.Offline => Color.FromRgb(0xFF, 0x17, 0x44),
        _                    => Color.FromRgb(0x4A, 0x55, 0x68)
    };

    [JsonIgnore] public Brush PingColor => ResponseTime switch
    {
        < 0    => new SolidColorBrush(Color.FromRgb(0x4A, 0x55, 0x68)),
        < 50   => new SolidColorBrush(Color.FromRgb(0x00, 0xE6, 0x76)),
        < 200  => new SolidColorBrush(Color.FromRgb(0xFF, 0xD6, 0x00)),
        _      => new SolidColorBrush(Color.FromRgb(0xFF, 0x17, 0x44))
    };

    // For sorting
    [JsonIgnore] public int StatusSort => Status switch
    {
        DeviceStatus.Online  => 0,
        DeviceStatus.Slow    => 1,
        DeviceStatus.Offline => 2,
        _                    => 3
    };

    /// <summary>Numeric sort key for IP (e.g. 192.168.1.10 → 3232235786)</summary>
    [JsonIgnore] public long IpSort
    {
        get
        {
            if (string.IsNullOrEmpty(IpAddress)) return 0;
            try
            {
                var parts = IpAddress.Split('.');
                if (parts.Length != 4) return 0;
                return (long.Parse(parts[0]) << 24) | (long.Parse(parts[1]) << 16)
                     | (long.Parse(parts[2]) << 8) | long.Parse(parts[3]);
            }
            catch { return 0; }
        }
    }

    // ── INotifyPropertyChanged ─────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public override string ToString() => $"{IpAddress} ({DisplayName})";
}
