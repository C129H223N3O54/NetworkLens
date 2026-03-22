using System.Collections.ObjectModel;
using System.Text.Json;
using NetworkLens.Models;

namespace NetworkLens.Services;

public class DeviceManager
{
    private static readonly JsonSerializerOptions _jsonOpts = new() { WriteIndented = true };

    // ── Load saved devices as dictionary keyed by MAC or IP ──
    public async Task<Dictionary<string, NetworkDevice>> LoadDevicesAsync()
    {
        try
        {
            if (!File.Exists(App.DevicesPath))
                return new Dictionary<string, NetworkDevice>();

            var json = await File.ReadAllTextAsync(App.DevicesPath);
            var list = JsonSerializer.Deserialize<List<NetworkDevice>>(json);
            if (list == null) return new Dictionary<string, NetworkDevice>();

            var dict = new Dictionary<string, NetworkDevice>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in list)
            {
                if (!string.IsNullOrEmpty(d.MacAddress))
                    dict[d.MacAddress] = d;
                else if (!string.IsNullOrEmpty(d.IpAddress))
                    dict[d.IpAddress] = d;
            }
            return dict;
        }
        catch { return new Dictionary<string, NetworkDevice>(); }
    }

    // ── Merge scan results with saved data and persist ──
    public async Task MergeAndSaveAsync(IEnumerable<NetworkDevice> scannedDevices)
    {
        try
        {
            var existing = await LoadDevicesAsync();

            foreach (var device in scannedDevices)
            {
                var key = device.MacAddress ?? device.IpAddress ?? "";
                if (string.IsNullOrEmpty(key)) continue;

                if (existing.TryGetValue(key, out var saved))
                {
                    // Update last seen, keep user data
                    saved.LastSeen    = device.LastSeen;
                    saved.IpAddress   = device.IpAddress;
                    saved.Hostname    = device.Hostname;
                    saved.Manufacturer = device.Manufacturer;
                }
                else
                {
                    existing[key] = device;
                }
            }

            var json = JsonSerializer.Serialize(existing.Values.ToList(), _jsonOpts);
            await File.WriteAllTextAsync(App.DevicesPath, json);
        }
        catch { /* best effort */ }
    }

    // ── Save a single device update (alias, category, etc.) ──
    public async Task SaveDeviceAsync(NetworkDevice device)
    {
        var existing = await LoadDevicesAsync();
        var key = device.MacAddress ?? device.IpAddress ?? "";
        if (!string.IsNullOrEmpty(key))
        {
            existing[key] = device;
            var json = JsonSerializer.Serialize(existing.Values.ToList(), _jsonOpts);
            await File.WriteAllTextAsync(App.DevicesPath, json);
        }
    }
}
