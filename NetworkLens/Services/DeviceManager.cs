using System.Text.Json;
using NetworkLens.Helpers;
using NetworkLens.Models;

namespace NetworkLens.Services;

/// <summary>
/// Persists user data per device (alias, category, favorite, notes) in devices.json.
/// All file access is serialized through one gate and written atomically, so a
/// context-menu edit during a running scan can no longer clobber or corrupt the file.
/// </summary>
public class DeviceManager
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
    private static readonly SemaphoreSlim FileGate = new(1, 1);

    private static string KeyOf(NetworkDevice d) =>
        !string.IsNullOrEmpty(d.MacAddress) ? d.MacAddress : d.IpAddress ?? "";

    // ── Load saved devices as dictionary keyed by MAC or IP ──
    public async Task<Dictionary<string, NetworkDevice>> LoadDevicesAsync()
    {
        await FileGate.WaitAsync().ConfigureAwait(false);
        try
        {
            return await ReadUnsafeAsync().ConfigureAwait(false);
        }
        finally
        {
            FileGate.Release();
        }
    }

    // ── Merge scan results with saved data and persist ──
    public async Task MergeAndSaveAsync(IEnumerable<NetworkDevice> scannedDevices)
    {
        var snapshot = scannedDevices.ToList();

        await FileGate.WaitAsync().ConfigureAwait(false);
        try
        {
            var existing = await ReadUnsafeAsync().ConfigureAwait(false);

            foreach (var device in snapshot)
            {
                var key = KeyOf(device);
                if (key.Length == 0) continue;

                if (existing.TryGetValue(key, out var saved))
                {
                    // Update last seen, keep user data
                    saved.LastSeen     = device.LastSeen;
                    saved.IpAddress    = device.IpAddress;
                    saved.Hostname     = device.Hostname;
                    saved.Manufacturer = device.Manufacturer;
                }
                else
                {
                    existing[key] = device;
                }
            }

            await WriteUnsafeAsync(existing).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Warn("DeviceManager.MergeAndSave", ex);
        }
        finally
        {
            FileGate.Release();
        }
    }

    // ── Save a single device update (alias, category, etc.) ──
    public async Task SaveDeviceAsync(NetworkDevice device)
    {
        var key = KeyOf(device);
        if (key.Length == 0) return;

        await FileGate.WaitAsync().ConfigureAwait(false);
        try
        {
            var existing = await ReadUnsafeAsync().ConfigureAwait(false);
            existing[key] = device;
            await WriteUnsafeAsync(existing).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Warn("DeviceManager.SaveDevice", ex);
        }
        finally
        {
            FileGate.Release();
        }
    }

    // ── Internals (caller must hold FileGate) ──
    private static async Task<Dictionary<string, NetworkDevice>> ReadUnsafeAsync()
    {
        var dict = new Dictionary<string, NetworkDevice>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (!File.Exists(App.DevicesPath)) return dict;

            var json = await File.ReadAllTextAsync(App.DevicesPath).ConfigureAwait(false);
            var list = JsonSerializer.Deserialize<List<NetworkDevice>>(json);
            if (list == null) return dict;

            foreach (var d in list)
            {
                var key = KeyOf(d);
                if (key.Length > 0) dict[key] = d;
            }
        }
        catch (Exception ex)
        {
            Log.Warn("DeviceManager.Read", ex);
        }
        return dict;
    }

    private static Task WriteUnsafeAsync(Dictionary<string, NetworkDevice> devices)
    {
        var json = JsonSerializer.Serialize(devices.Values.ToList(), JsonOpts);
        return AtomicFile.WriteAllTextAsync(App.DevicesPath, json);
    }
}
