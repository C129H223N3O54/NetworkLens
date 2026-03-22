using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace NetworkLens.Services;

public static class IpHelper
{
    // ── CIDR → list of all IPs ──────────────────────
    public static List<string> GetAllIpsInSubnet(string cidr)
    {
        var result = new List<string>();

        try
        {
            if (!ParseCidr(cidr, out uint baseIp, out uint mask))
                return result;

            uint network = baseIp & mask;
            uint broadcast = network | ~mask;

            // Skip network address and broadcast
            for (uint ip = network + 1; ip < broadcast; ip++)
            {
                result.Add(IpToString(ip));
            }
        }
        catch { }

        return result;
    }

    // ── Local subnet detection ──────────────────────
    public static string? GetLocalSubnetCidr()
    {
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback
                    or NetworkInterfaceType.Tunnel) continue;

                foreach (var addr in nic.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;

                    var ip = addr.Address.ToString();
                    var mask = addr.IPv4Mask?.ToString();
                    if (mask == null) continue;

                    // Skip link-local (169.254.x.x)
                    if (ip.StartsWith("169.254.")) continue;

                    var prefix = MaskToCidrPrefix(mask);
                    if (prefix < 8 || prefix > 30) continue;

                    // Calculate network address
                    uint ipUint   = IpToUint(addr.Address);
                    uint maskUint = IpToUint(addr.IPv4Mask!);
                    uint netAddr  = ipUint & maskUint;

                    return $"{IpToString(netAddr)}/{prefix}";
                }
            }
        }
        catch { }
        return null;
    }

    public static string? GetLocalIpAddress()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 80);
            return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString();
        }
        catch { return null; }
    }

    public static string? GetBroadcastAddress(string cidr)
    {
        try
        {
            if (!ParseCidr(cidr, out uint baseIp, out uint mask)) return null;
            uint broadcast = (baseIp & mask) | ~mask;
            return IpToString(broadcast);
        }
        catch { return null; }
    }

    // ── CIDR parsing ─────────────────────────────────
    public static bool ParseCidr(string cidr, out uint ip, out uint mask)
    {
        ip = 0; mask = 0;
        try
        {
            var parts = cidr.Trim().Split('/');
            if (parts.Length != 2) return false;

            if (!IPAddress.TryParse(parts[0], out var ipAddr)) return false;
            if (!int.TryParse(parts[1], out int prefix)) return false;
            if (prefix < 0 || prefix > 32) return false;

            ip   = IpToUint(ipAddr);
            mask = prefix == 0 ? 0 : (0xFFFFFFFF << (32 - prefix));
            return true;
        }
        catch { return false; }
    }

    public static int MaskToCidrPrefix(string mask)
    {
        if (!IPAddress.TryParse(mask, out var addr)) return 24;
        var bytes = addr.GetAddressBytes();
        int count = 0;
        foreach (var b in bytes)
        {
            byte bb = b;
            while (bb != 0) { count += bb & 1; bb >>= 1; }
        }
        return count;
    }

    public static uint IpToUint(IPAddress addr)
    {
        var b = addr.GetAddressBytes();
        return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
    }

    public static string IpToString(uint ip)
    {
        return $"{(ip >> 24) & 0xFF}.{(ip >> 16) & 0xFF}.{(ip >> 8) & 0xFF}.{ip & 0xFF}";
    }

    public static int GetSubnetSize(string cidr)
    {
        if (ParseCidr(cidr, out _, out uint mask))
        {
            uint hostBits = ~mask;
            return (int)(hostBits - 1); // -2 for network + broadcast
        }
        return 254;
    }
}
