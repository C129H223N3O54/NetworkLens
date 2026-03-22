using NetworkLens.Models;

namespace NetworkLens.Services;

public record ScanDiff(
    List<NetworkDevice> NewDevices,
    List<NetworkDevice> DisappearedDevices,
    List<(NetworkDevice Before, NetworkDevice After)> ChangedDevices
);

public class ScanComparer
{
    /// <summary>
    /// Compare two scan results and return the differences.
    /// Key is MAC address if available, otherwise IP.
    /// </summary>
    public static ScanDiff Compare(ScanResult before, ScanResult after)
    {
        var beforeMap = BuildMap(before.Devices);
        var afterMap  = BuildMap(after.Devices);

        var newDevices  = new List<NetworkDevice>();
        var disappeared = new List<NetworkDevice>();
        var changed     = new List<(NetworkDevice, NetworkDevice)>();

        // New devices
        foreach (var (key, device) in afterMap)
        {
            if (!beforeMap.ContainsKey(key))
                newDevices.Add(device);
        }

        // Disappeared devices
        foreach (var (key, device) in beforeMap)
        {
            if (!afterMap.ContainsKey(key))
                disappeared.Add(device);
        }

        // Changed devices (IP changed, new ports, etc.)
        foreach (var (key, afterDev) in afterMap)
        {
            if (!beforeMap.TryGetValue(key, out var beforeDev)) continue;

            bool ipChanged   = beforeDev.IpAddress != afterDev.IpAddress;
            bool portsChanged = beforeDev.OpenPortCount != afterDev.OpenPortCount;

            if (ipChanged || portsChanged)
                changed.Add((beforeDev, afterDev));
        }

        return new ScanDiff(newDevices, disappeared, changed);
    }

    private static Dictionary<string, NetworkDevice> BuildMap(IEnumerable<NetworkDevice> devices)
    {
        var map = new Dictionary<string, NetworkDevice>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in devices)
        {
            var key = !string.IsNullOrEmpty(d.MacAddress) ? d.MacAddress : d.IpAddress;
            if (!string.IsNullOrEmpty(key))
                map[key] = d;
        }
        return map;
    }
}
