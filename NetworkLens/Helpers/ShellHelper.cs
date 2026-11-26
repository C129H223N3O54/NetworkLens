using System.Diagnostics;
using System.Net;

namespace NetworkLens.Helpers;

/// <summary>
/// Central place for starting external programs, files and URLs.
///
/// Security notes:
/// • System tools are always started via their absolute path in System32 / the
///   Windows directory. Starting "netstat" or "cmd.exe" by name lets Windows search
///   the application directory first — a portable exe in the Downloads folder could
///   then run a planted netstat.exe (CWE-427), possibly elevated after "restart as admin".
/// • Console commands run with System32 as working directory, so cmd.exe cannot
///   resolve ping/tracert/telnet from the current (download) directory either.
/// • IP arguments are validated with IPAddress.TryParse before they reach cmd.exe.
/// • Only http, https and ftp URLs are opened.
/// </summary>
internal static class ShellHelper
{
    public static string SystemExe(string exe) => Path.Combine(Environment.SystemDirectory, exe);

    private static string WindowsExe(string exe) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), exe);

    /// <summary>Returns the normalized IP string or null if the input is not a valid IP address.</summary>
    public static string? NormalizeIp(string? value) =>
        IPAddress.TryParse(value?.Trim(), out var ip) ? ip.ToString() : null;

    public static void OpenUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;
        if (uri.Scheme is not ("http" or "https" or "ftp")) return;
        TryStart(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }, "OpenUrl");
    }

    public static void OpenFile(string path)
    {
        if (!File.Exists(path)) return;
        TryStart(new ProcessStartInfo(path) { UseShellExecute = true }, "OpenFile");
    }

    public static void OpenFolder(string path)
    {
        if (!Directory.Exists(path)) return;
        TryStart(new ProcessStartInfo(WindowsExe("explorer.exe"), $"\"{path}\"")
        {
            UseShellExecute = false
        }, "OpenFolder");
    }

    /// <summary>Opens \\ip in Explorer (SMB shares).</summary>
    public static void OpenNetworkShare(string? ip)
    {
        var safeIp = NormalizeIp(ip);
        if (safeIp == null) return;
        TryStart(new ProcessStartInfo(WindowsExe("explorer.exe"), $@"\\{safeIp}")
        {
            UseShellExecute = false
        }, "OpenNetworkShare");
    }

    /// <summary>
    /// Opens a console window running a network tool against an IP,
    /// e.g. RunConsoleTool("ping", ip, "-t").
    /// </summary>
    public static void RunConsoleTool(string tool, string? ip, string extraArgs = "")
    {
        var safeIp = NormalizeIp(ip);
        if (safeIp == null) return;
        if (tool is not ("ping" or "tracert" or "telnet" or "ssh")) return;

        var args = string.IsNullOrWhiteSpace(extraArgs)
            ? $"/k {tool} {safeIp}"
            : $"/k {tool} {safeIp} {extraArgs}";

        TryStart(new ProcessStartInfo(SystemExe("cmd.exe"), args)
        {
            UseShellExecute  = true,
            WorkingDirectory = Environment.SystemDirectory
        }, "RunConsoleTool");
    }

    private static void TryStart(ProcessStartInfo psi, string context)
    {
        try
        {
            using var _ = Process.Start(psi);
        }
        catch (Exception ex)
        {
            Log.Warn(context, ex);
        }
    }
}
