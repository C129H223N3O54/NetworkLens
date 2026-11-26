using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace NetworkLens.Services;

public static class IpHelper
{
    // ── CIDR → host range ───────────────────────────

    /// <summary>
    /// Parses "192.168.1.0/24" (or a single IP, treated as /32) into the first and
    /// last scannable host address. Network and broadcast address are excluded for
    /// prefixes up to /30; /31 and /32 return all addresses.
    /// </summary>
    public static bool TryGetHostRange(string? cidr, out uint first, out uint last)
    {
        first = last = 0;
        if (!ParseCidr(cidr, out uint ip, out uint mask, out int prefix)) return false;

        uint network   = ip & mask;
        uint broadcast = network | ~mask;

        if (prefix >= 31)
        {
            first = network;
            last  = broadcast;
        }
        else
        {
            first = network + 1;
            last  = broadcast - 1;
        }
        return true;
    }

    public static long HostCount(uint first, uint last) => (long)last - first + 1;

    /// <summary>Lazily enumerates all addresses in the range (no big list in memory).</summary>
    public static IEnumerable<IPAddress> EnumerateHosts(uint first, uint last)
    {
        for (ulong i = first; i <= last; i++)
            yield return UintToIp((uint)i);
    }

    // ── Primary interface / local subnet ────────────

    /// <summary>
    /// Returns the interface that is most likely the "real" LAN connection:
    /// up, not loopback/tunnel, with an IPv4 address — preferring adapters that
    /// have a default gateway (skips Hyper-V vEthernet, VPN and host-only adapters).
    /// </summary>
    public static (NetworkInterface Nic, UnicastIPAddressInformation Address)? GetPrimaryInterface()
    {
        try
        {
            var candidates = new List<(NetworkInterface Nic, UnicastIPAddressInformation Addr, bool HasGateway)>();

            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback
                    or NetworkInterfaceType.Tunnel) continue;

                var props = nic.GetIPProperties();
                var addr = props.UnicastAddresses.FirstOrDefault(a =>
                    a.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !a.Address.ToString().StartsWith("169.254.", StringComparison.Ordinal));
                if (addr == null) continue;

                bool hasGateway = props.GatewayAddresses.Any(g =>
                    g.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !g.Address.Equals(IPAddress.Any));

                candidates.Add((nic, addr, hasGateway));
            }

            var best = candidates.FirstOrDefault(c => c.HasGateway);
            if (best.Nic == null) best = candidates.FirstOrDefault();
            return best.Nic == null ? null : (best.Nic, best.Addr);
        }
        catch
        {
            return null;
        }
    }

    public static string? GetLocalSubnetCidr()
    {
        var primary = GetPrimaryInterface();
        if (primary == null) return null;

        var addr = primary.Value.Address;
        int prefix = addr.PrefixLength;
        if (prefix is < 8 or > 30)
            prefix = addr.IPv4Mask != null ? MaskToCidrPrefix(addr.IPv4Mask.ToString()) : 24;
        if (prefix is < 8 or > 30) return null;

        uint mask    = prefix == 0 ? 0 : 0xFFFFFFFF << (32 - prefix);
        uint netAddr = IpToUint(addr.Address) & mask;
        return $"{IpToString(netAddr)}/{prefix}";
    }

    // ── CIDR parsing ─────────────────────────────────
    public static bool ParseCidr(string? cidr, out uint ip, out uint mask) =>
        ParseCidr(cidr, out ip, out mask, out _);

    public static bool ParseCidr(string? cidr, out uint ip, out uint mask, out int prefix)
    {
        ip = 0; mask = 0; prefix = 0;
        if (string.IsNullOrWhiteSpace(cidr)) return false;

        var parts = cidr.Trim().Split('/');
        if (parts.Length > 2) return false;

        if (!IPAddress.TryParse(parts[0], out var ipAddr) ||
            ipAddr.AddressFamily != AddressFamily.InterNetwork) return false;

        prefix = 32;
        if (parts.Length == 2 && (!int.TryParse(parts[1], out prefix) || prefix is < 0 or > 32))
            return false;

        ip   = IpToUint(ipAddr);
        mask = prefix == 0 ? 0 : 0xFFFFFFFF << (32 - prefix);
        return true;
    }

    public static int MaskToCidrPrefix(string mask)
    {
        if (!IPAddress.TryParse(mask, out var addr)) return 24;
        int count = 0;
        foreach (var b in addr.GetAddressBytes())
            count += System.Numerics.BitOperations.PopCount(b);
        return count;
    }

    public static uint IpToUint(IPAddress addr)
    {
        var b = addr.GetAddressBytes();
        return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
    }

    public static IPAddress UintToIp(uint ip) =>
        new(new[] { (byte)(ip >> 24), (byte)(ip >> 16), (byte)(ip >> 8), (byte)ip });

    public static string IpToString(uint ip) =>
        $"{(ip >> 24) & 0xFF}.{(ip >> 16) & 0xFF}.{(ip >> 8) & 0xFF}.{ip & 0xFF}";
}
