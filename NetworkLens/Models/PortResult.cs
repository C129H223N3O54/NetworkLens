using System.Text.Json.Serialization;
using System.Windows.Media;

namespace NetworkLens.Models;

public enum PortStatus { Open, Closed, Filtered, Unknown }

public class PortResult
{
    public int Port { get; set; }
    public PortStatus Status { get; set; } = PortStatus.Unknown;
    public string? ServiceName { get; set; }
    public string? Banner { get; set; }
    public long ResponseTime { get; set; }

    // ── Well-known service names ───────────────────
    public static string GetServiceName(int port) => port switch
    {
        20    => "FTP-Data",
        21    => "FTP",
        22    => "SSH",
        23    => "Telnet",
        25    => "SMTP",
        53    => "DNS",
        67    => "DHCP",
        68    => "DHCP",
        80    => "HTTP",
        110   => "POP3",
        111   => "RPC",
        119   => "NNTP",
        123   => "NTP",
        135   => "RPC/DCOM",
        137   => "NetBIOS-NS",
        138   => "NetBIOS-DGM",
        139   => "NetBIOS-SSN",
        143   => "IMAP",
        161   => "SNMP",
        162   => "SNMP-Trap",
        194   => "IRC",
        389   => "LDAP",
        443   => "HTTPS",
        445   => "SMB",
        465   => "SMTPS",
        500   => "IKE/IPSec",
        514   => "Syslog",
        515   => "LPD/LPR",
        587   => "SMTP-Submit",
        631   => "IPP (Print)",
        636   => "LDAPS",
        993   => "IMAPS",
        995   => "POP3S",
        1080  => "SOCKS",
        1194  => "OpenVPN",
        1433  => "MSSQL",
        1521  => "Oracle DB",
        1723  => "PPTP VPN",
        1883  => "MQTT",
        2049  => "NFS",
        2375  => "Docker",
        2376  => "Docker TLS",
        3000  => "Dev Server",
        3306  => "MySQL",
        3389  => "RDP",
        3690  => "SVN",
        4000  => "Dev Server",
        4443  => "HTTPS-Alt",
        5000  => "Dev Server",
        5001  => "Dev Server",
        5060  => "SIP",
        5432  => "PostgreSQL",
        5900  => "VNC",
        5985  => "WinRM HTTP",
        5986  => "WinRM HTTPS",
        6379  => "Redis",
        6881  => "BitTorrent",
        7070  => "RTSP",
        8000  => "HTTP-Alt",
        8008  => "HTTP-Alt",
        8080  => "HTTP-Proxy",
        8081  => "HTTP-Alt",
        8083  => "MQTT-WebSocket",
        8086  => "InfluxDB",
        8088  => "HTTP-Alt",
        8443  => "HTTPS-Alt",
        8883  => "MQTT TLS",
        8888  => "HTTP-Alt",
        9000  => "Dev Server",
        9090  => "Prometheus",
        9200  => "Elasticsearch",
        9300  => "Elasticsearch",
        10000 => "Webmin",
        27017 => "MongoDB",
        32400 => "Plex",
        49152 => "Windows RPC",
        51820 => "WireGuard",
        _     => ""
    };

    // UI helpers
    [JsonIgnore] public Brush StatusColor => Status switch
    {
        PortStatus.Open     => new SolidColorBrush(Color.FromRgb(0x74, 0xA7, 0x32)),
        PortStatus.Filtered => new SolidColorBrush(Color.FromRgb(0xBA, 0x75, 0x17)),
        PortStatus.Closed   => new SolidColorBrush(Color.FromRgb(0x4A, 0x55, 0x68)),
        _                   => new SolidColorBrush(Color.FromRgb(0x4A, 0x55, 0x68))
    };

    [JsonIgnore] public string StatusText => Status switch
    {
        PortStatus.Open     => "Offen",
        PortStatus.Filtered => "Gefiltert",
        PortStatus.Closed   => "Geschlossen",
        _                   => "Unbekannt"
    };

    [JsonIgnore] public string DisplayService => !string.IsNullOrEmpty(ServiceName)
        ? ServiceName : GetServiceName(Port);
}
