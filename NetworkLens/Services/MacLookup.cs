using System.Reflection;

namespace NetworkLens.Services;

/// <summary>
/// Looks up manufacturer names from MAC address OUI prefixes.
/// The database is loaded from the embedded resource oui.txt.
///
/// Format of oui.txt (one entry per line):
///   AA-BB-CC   (hex)   Manufacturer Name
/// or
///   AA-BB-CC   Manufacturer Name
/// </summary>
public class MacLookup
{
    private static readonly Dictionary<string, string> _db = new(StringComparer.OrdinalIgnoreCase);
    private static bool _loaded = false;
    private static readonly object _lock = new();

    public MacLookup()
    {
        EnsureLoaded();
    }

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        lock (_lock)
        {
            if (_loaded) return;
            LoadDatabase();
            _loaded = true;
        }
    }

    private static void LoadDatabase()
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            // Try embedded resource first
            var resourceName = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("oui.txt", StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                LoadMinimalFallback();
                return;
            }

            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream == null) { LoadMinimalFallback(); return; }

            using var reader = new StreamReader(stream);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                ParseLine(line);
            }
        }
        catch
        {
            LoadMinimalFallback();
        }
    }

    private static void ParseLine(string line)
    {
        // Skip comments and empty lines
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) return;

        // IEEE format: "00-00-0C   (hex)\t\tCisco Systems, Inc"
        // Or simple:   "00:00:0C\tCisco Systems"
        try
        {
            // Normalize separators
            var normalized = line.Replace(':', '-').Replace('.', '-').ToUpperInvariant();

            // Extract prefix (first 8 chars like "AA-BB-CC")
            if (normalized.Length < 8) return;
            var prefix = normalized[..8]; // e.g. "AA-BB-CC"

            // Validate format
            if (prefix[2] != '-' || prefix[5] != '-') return;

            // Find manufacturer name (after the (hex) marker if present, or after tab)
            int nameStart = line.IndexOf("(hex)", StringComparison.OrdinalIgnoreCase);
            string name;
            if (nameStart >= 0)
            {
                name = line[(nameStart + 5)..].Trim();
            }
            else
            {
                var tabIdx = line.IndexOf('\t');
                name = tabIdx >= 0 ? line[(tabIdx + 1)..].Trim() : line[9..].Trim();
            }

            if (!string.IsNullOrWhiteSpace(name) && !_db.ContainsKey(prefix))
                _db[prefix] = name;
        }
        catch { }
    }

    /// <summary>Minimal hardcoded fallback for common vendors.</summary>
    private static void LoadMinimalFallback()
    {
        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["00-00-0C"] = "Cisco",
            ["00-1A-A0"] = "Dell",
            ["00-50-56"] = "VMware",
            ["00-0C-29"] = "VMware",
            ["B8-27-EB"] = "Raspberry Pi",
            ["DC-A6-32"] = "Raspberry Pi",
            ["E4-5F-01"] = "Raspberry Pi",
            ["00-1B-63"] = "Apple",
            ["00-26-B9"] = "Dell",
            ["00-1C-C0"] = "Apple",
            ["3C-D9-2B"] = "HP",
            ["00-1F-29"] = "Sony",
            ["08-ED-B9"] = "Huawei",
            ["00-E0-4C"] = "Realtek",
            ["00-1D-7E"] = "Cisco-Linksys",
            ["00-0D-B9"] = "PC Engines",
            ["00-08-9B"] = "Synology",
            ["00-11-32"] = "Synology",
            ["18-C0-4D"] = "Synology",
            ["00-50-43"] = "QNAP",
            ["24-5E-BE"] = "QNAP",
            ["FC-23-CD"] = "Samsung",
            ["04-5D-4B"] = "Apple",
            ["10-9A-DD"] = "Apple",
            ["A4-C3-F0"] = "Google",
            ["54-60-09"] = "Google",
            ["F4-F5-D8"] = "Google",
        };

        foreach (var (k, v) in entries)
            _db.TryAdd(k, v);
    }

    /// <summary>
    /// Returns manufacturer name for a given MAC address, or null if unknown.
    /// Accepts formats: AA:BB:CC:DD:EE:FF / AA-BB-CC-DD-EE-FF / AABBCCDDEEFF
    /// </summary>
    public string? Lookup(string? mac)
    {
        if (string.IsNullOrWhiteSpace(mac)) return null;

        try
        {
            // Normalize to XX-XX-XX format
            var clean = mac.Replace(":", "-").Replace(".", "-").ToUpperInvariant().Trim();

            // Handle compact format AABBCCDDEEFF
            if (!clean.Contains('-') && clean.Length >= 6)
            {
                clean = $"{clean[0..2]}-{clean[2..4]}-{clean[4..6]}";
            }

            if (clean.Length < 8) return null;
            var prefix = clean[..8];

            return _db.TryGetValue(prefix, out var name) ? name : null;
        }
        catch { return null; }
    }

    public int DatabaseSize => _db.Count;
}
