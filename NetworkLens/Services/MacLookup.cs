using System.Reflection;

namespace NetworkLens.Services;

/// <summary>
/// Looks up manufacturer names from MAC address OUI prefixes.
/// The database is loaded from the embedded resource oui.txt on first use
/// (not at app start). Release builds replace oui.txt with the full IEEE list.
///
/// Format of oui.txt (one entry per line):
///   AA-BB-CC   (hex)   Manufacturer Name
/// or
///   AA-BB-CC   Manufacturer Name
/// </summary>
public class MacLookup
{
    private static readonly Lazy<Dictionary<string, string>> Db =
        new(LoadDatabase, LazyThreadSafetyMode.ExecutionAndPublication);

    private static Dictionary<string, string> LoadDatabase()
    {
        var db = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            var resourceName = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("oui.txt", StringComparison.OrdinalIgnoreCase));

            using var stream = resourceName == null ? null : asm.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                string? line;
                while ((line = reader.ReadLine()) != null)
                    ParseLine(line, db);
            }
        }
        catch (Exception ex)
        {
            Helpers.Log.Warn("MacLookup.LoadDatabase", ex);
        }

        AddMinimalFallback(db);
        return db;
    }

    private static void ParseLine(string line, Dictionary<string, string> db)
    {
        // Cheap pre-check before allocating: "AA-BB-CC" / "AA:BB:CC" / "AA.BB.CC"
        if (line.Length < 10 || line[0] == '#') return;
        if (!IsSeparator(line[2]) || !IsSeparator(line[5])) return;

        var prefix = string.Create(8, line, static (span, src) =>
        {
            for (int i = 0; i < 8; i++)
            {
                var c = src[i];
                span[i] = IsSeparator(c) ? '-' : char.ToUpperInvariant(c);
            }
        });
        if (!Uri.IsHexDigit(prefix[0]) || !Uri.IsHexDigit(prefix[7])) return;

        // Manufacturer name: after the "(hex)" marker, after a tab, or after the prefix.
        int hex = line.IndexOf("(hex)", StringComparison.OrdinalIgnoreCase);
        string name;
        if (hex >= 0)
        {
            name = line[(hex + 5)..].Trim();
        }
        else
        {
            int tab = line.IndexOf('\t');
            name = tab >= 0 ? line[(tab + 1)..].Trim() : line[8..].Trim();
        }

        if (name.Length > 0)
            db.TryAdd(prefix, name);
    }

    private static bool IsSeparator(char c) => c is '-' or ':' or '.';

    /// <summary>Minimal hardcoded fallback for common vendors.</summary>
    private static void AddMinimalFallback(Dictionary<string, string> db)
    {
        var entries = new Dictionary<string, string>
        {
            ["00-00-0C"] = "Cisco",
            ["00-1A-A0"] = "Dell",
            ["00-50-56"] = "VMware",
            ["00-0C-29"] = "VMware",
            ["00-15-5D"] = "Microsoft (Hyper-V)",
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
            db.TryAdd(k, v);
    }

    /// <summary>
    /// Returns manufacturer name for a given MAC address, or null if unknown.
    /// Accepts formats: AA:BB:CC:DD:EE:FF / AA-BB-CC-DD-EE-FF / AABBCCDDEEFF
    /// </summary>
    public string? Lookup(string? mac)
    {
        if (string.IsNullOrWhiteSpace(mac)) return null;

        var clean = mac.Trim().Replace(':', '-').Replace('.', '-').ToUpperInvariant();

        // Compact format AABBCCDDEEFF
        if (!clean.Contains('-') && clean.Length >= 6)
            clean = $"{clean[0..2]}-{clean[2..4]}-{clean[4..6]}";

        if (clean.Length < 8) return null;
        return Db.Value.TryGetValue(clean[..8], out var name) ? name : null;
    }
}
