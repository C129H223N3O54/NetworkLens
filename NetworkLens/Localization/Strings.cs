using System.Collections.Generic;

namespace NetworkLens.Localization;

/// <summary>
/// Central translation dictionary.
/// Add new strings here as Key + (de, en) pair.
/// </summary>
internal static class Strings
{
    public static string Get(string key, string lang)
    {
        if (_strings.TryGetValue(key, out var pair))
            return lang == "en" ? pair.en : pair.de;
        return $"[{key}]"; // visible marker for missing translations
    }

    private static readonly Dictionary<string, (string de, string en)> _strings = new()
    {
        // ── Sidebar Navigation ───────────────────
        ["Nav_Scan"]        = ("Netzwerk-Scan",   "Network Scan"),
        ["Nav_PortScan"]    = ("Port-Scanner",    "Port Scanner"),
        ["Nav_NetInfo"]     = ("Netzwerk-Info",   "Network Info"),
        ["Nav_Monitor"]     = ("Live-Monitor",    "Live Monitor"),
        ["Nav_Report"]      = ("Report",          "Report"),
        ["Nav_Settings"]    = ("Einstellungen",   "Settings"),
        ["Nav_Section"]     = ("NAVIGATION",      "NAVIGATION"),

        ["Admin_Badge"]     = ("ADMIN",            "ADMIN"),
        ["Admin_NoAdmin"]   = ("EINGESCHRÄNKT",    "LIMITED"),

        // ── Status Bar ───────────────────────────
        ["Status_Ready"]    = ("Bereit",                "Ready"),
        ["Status_Scanning"] = ("Scan läuft...",         "Scanning..."),
        ["Status_Done"]     = ("Scan abgeschlossen",    "Scan complete"),
        ["Status_Devices"]  = ("Geräte",                "devices"),

        // ── Settings ─────────────────────────────
        ["Settings_Title"]      = ("Einstellungen",                      "Settings"),
        ["Settings_Subtitle"]   = ("NetworkLens konfigurieren",          "Configure NetworkLens"),
        ["Settings_Reset"]      = ("Zurücksetzen",                       "Reset"),
        ["Settings_Save"]       = ("Speichern",                          "Save"),
        ["Settings_Saved"]      = ("Einstellungen gespeichert",          "Settings saved"),

        ["Section_Appearance"]  = ("ERSCHEINUNGSBILD",                   "APPEARANCE"),
        ["Section_Language"]    = ("SPRACHE / LANGUAGE",                 "LANGUAGE / SPRACHE"),
        ["Section_Scan"]        = ("SCAN-EINSTELLUNGEN",                 "SCAN SETTINGS"),
        ["Section_Monitor"]     = ("LIVE-MONITOR",                       "LIVE MONITOR"),
        ["Section_Notify"]      = ("BENACHRICHTIGUNGEN",                 "NOTIFICATIONS"),
        ["Section_Output"]      = ("AUSGABE-PFAD",                       "OUTPUT PATH"),
        ["Section_AppData"]     = ("APP-DATEN",                          "APP DATA"),
        ["Section_About"]       = ("ÜBER",                               "ABOUT"),
        ["Section_Changelog"]   = ("CHANGELOG",                          "CHANGELOG"),

        ["Theme_Label"]         = ("Theme",                              "Theme"),
        ["Theme_Description"]   = ("Zwischen Dark Mode und Light Mode wechseln. Die Änderung wird sofort angewendet.",
                                   "Switch between Dark Mode and Light Mode. Changes apply instantly."),
        ["Theme_Dark"]          = ("Dark Mode",                          "Dark Mode"),
        ["Theme_Light"]         = ("Light Mode",                         "Light Mode"),

        ["Lang_Label"]          = ("Sprache der Oberfläche",             "Interface language"),
        ["Lang_Description"]    = ("Sprache der Anwendung. Die Änderung wird sofort angewendet.",
                                   "Application language. Changes apply instantly."),
        ["Lang_German"]         = ("Deutsch",                            "Deutsch"),
        ["Lang_English"]        = ("English",                            "English"),

        // ── About ────────────────────────────────
        ["About_Idea"]          = ("Idee & Konzept",                     "Idea & Concept"),
        ["About_Development"]   = ("Entwicklung",                        "Development"),
        ["About_AiAssistant"]   = ("KI-Assistent von Anthropic",         "AI assistant by Anthropic"),
        ["About_License"]       = ("Freie Nutzung, Weitergabe und Modifikation erlaubt.",
                                   "Free to use, distribute and modify."),
        ["About_Description"]   = ("Portabler Netzwerkscanner — keine Installation erforderlich.",
                                   "Portable network scanner — no installation required."),
        ["About_OpenGitHub"]    = ("GitHub öffnen",                      "Open GitHub"),

        // ── Common ───────────────────────────────
        ["Btn_Cancel"]          = ("Abbrechen",                          "Cancel"),
        ["Btn_OK"]              = ("OK",                                 "OK"),
        ["Btn_Close"]           = ("Schließen",                          "Close"),
        ["Btn_Save"]            = ("Speichern",                          "Save"),
        ["Btn_Apply"]           = ("Anwenden",                           "Apply"),
        ["Btn_Browse"]          = ("Durchsuchen",                        "Browse"),
        ["Btn_OpenFolder"]      = ("App-Ordner öffnen",                  "Open app folder"),

        // ── Scan View ────────────────────────────
        ["Scan_Title"]          = ("Netzwerk-Scan",                      "Network Scan"),
        ["Scan_Subtitle"]       = ("Geräte im lokalen Subnetz finden",   "Discover devices in your local subnet"),
        ["Scan_SubnetLabel"]    = ("Subnetz (CIDR)",                     "Subnet (CIDR)"),
        ["Scan_AutoDetect"]     = ("Auto-Erkennung",                     "Auto-detect"),
        ["Scan_StartButton"]    = ("Scan starten",                       "Start scan"),
        ["Scan_StopButton"]     = ("Stop",                               "Stop"),
        ["Scan_Search"]         = ("Suchen...",                          "Search..."),
        ["Scan_Export"]         = ("Exportieren",                        "Export"),
        ["Scan_NoResults"]      = ("Noch kein Scan durchgeführt",        "No scan run yet"),
        ["Scan_NoResultsHint"]  = ("Subnetz oben eingeben und auf 'Scan starten' klicken.",
                                   "Enter a subnet above and click 'Start scan'."),

        // ── Scan Table Headers ───────────────────
        ["Col_Status"]          = ("Status",        "Status"),
        ["Col_IP"]              = ("IP-Adresse",    "IP Address"),
        ["Col_Hostname"]        = ("Hostname",      "Hostname"),
        ["Col_Alias"]           = ("Alias",         "Alias"),
        ["Col_MAC"]             = ("MAC-Adresse",   "MAC Address"),
        ["Col_Manufacturer"]    = ("Hersteller",    "Manufacturer"),
        ["Col_Ping"]            = ("Ping",          "Ping"),
        ["Col_Ports"]           = ("Offene Ports",  "Open Ports"),
        ["Col_Category"]        = ("Kategorie",     "Category"),
        ["Col_LastSeen"]        = ("Zuletzt gesehen","Last Seen"),

        // ── Context Menu ─────────────────────────
        ["Ctx_PortScan"]        = ("Port-Scan",                          "Port Scan"),
        ["Ctx_AddMonitor"]      = ("In Monitor aufnehmen",               "Add to Monitor"),
        ["Ctx_SetAlias"]        = ("Alias setzen...",                    "Set alias..."),
        ["Ctx_SetCategory"]     = ("Kategorie...",                       "Category..."),
        ["Ctx_ToggleFav"]       = ("Favorit umschalten",                 "Toggle favorite"),
        ["Ctx_OpenHttp"]        = ("HTTP im Browser öffnen",             "Open in browser (HTTP)"),
        ["Ctx_OpenHttps"]       = ("HTTPS im Browser öffnen",            "Open in browser (HTTPS)"),
        ["Ctx_OpenExplorer"]    = ("Im Explorer öffnen (Netzwerk)",      "Open in Explorer (network)"),
        ["Ctx_OpenFtp"]         = ("FTP öffnen",                         "Open FTP"),
        ["Ctx_OpenTelnet"]      = ("Telnet öffnen",                      "Open Telnet"),
        ["Ctx_OpenSsh"]         = ("SSH öffnen",                         "Open SSH"),
        ["Ctx_Ping"]            = ("Ping (Terminal)",                    "Ping (terminal)"),
        ["Ctx_Traceroute"]      = ("Traceroute",                         "Traceroute"),
        ["Ctx_GeoLocate"]       = ("GeoLocate (Web)",                    "GeoLocate (web)"),
        ["Ctx_CopyIP"]          = ("IP-Adresse kopieren",                "Copy IP address"),
        ["Ctx_CopyMAC"]         = ("MAC-Adresse kopieren",               "Copy MAC address"),

        // ── Monitor View ─────────────────────────
        ["Monitor_Title"]           = ("Live-Monitor",                       "Live Monitor"),
        ["Monitor_Subtitle"]        = ("Kontinuierliche Ping-Überwachung mit Jitter und Packet-Loss",
                                       "Continuous ping monitoring with jitter and packet loss"),
        ["Monitor_Interval"]        = ("Intervall:",                         "Interval:"),
        ["Monitor_Continuous"]      = ("Dauerhaft",                          "Continuous"),
        ["Monitor_StartButton"]     = ("Starten",                            "Start"),
        ["Monitor_StopButton"]      = ("Stop",                               "Stop"),
        ["Monitor_Explanations"]    = ("Erklärungen",                        "Explanations"),
        ["Monitor_AddDevice"]       = ("Gerät hinzufügen:",                  "Add device:"),
        ["Monitor_AddButton"]       = ("Hinzufügen",                         "Add"),
        ["Monitor_NoDevices"]       = ("Kein Gerät überwacht",               "No device monitored"),
        ["Monitor_NoDevicesHint"]   = ("IP-Adresse oben eingeben oder Gerät aus dem Scan-Tab hinzufügen.",
                                       "Enter an IP above or add a device from the Scan tab."),
        ["Monitor_NoDevicesAlert"]  = ("Bitte zuerst Geräte hinzufügen.",    "Please add devices first."),

        ["Monitor_Ping"]            = ("PING",                               "PING"),
        ["Monitor_Jitter"]          = ("JITTER",                             "JITTER"),
        ["Monitor_Min"]             = ("MIN",                                "MIN"),
        ["Monitor_Avg"]             = ("AVG",                                "AVG"),
        ["Monitor_Max"]             = ("MAX",                                "MAX"),
        ["Monitor_PacketLoss"]      = ("PACKET LOSS",                        "PACKET LOSS"),
        ["Monitor_Packets"]         = ("Pakete",                             "packets"),
        ["Monitor_Datapoints"]      = ("Messpunkte",                         "data points"),

        // ── Explanations Panel ───────────────────
        ["Exp_Title"]               = ("BEGRIFFSERKLÄRUNGEN",                "TERMINOLOGY"),
        ["Exp_Close"]               = ("Schließen",                          "Close"),

        ["Exp_PingHeading"]         = ("Ping",                               "Ping"),
        ["Exp_PingText"]            = ("Zeit in ms, die ein Datenpaket zum Gerät und zurück braucht (Round-Trip Time).",
                                       "Time in ms a data packet takes to travel to the device and back (Round-Trip Time)."),
        ["Exp_PingScale"]           = ("🟢 <30ms — Sehr gut\n🟡 30–100ms — Normal\n🔴 >200ms — Langsam",
                                       "🟢 <30ms — Excellent\n🟡 30–100ms — Normal\n🔴 >200ms — Slow"),
        ["Exp_PingWiki"]            = ("Ping – Wikipedia",                   "Ping – Wikipedia"),

        ["Exp_JitterHeading"]       = ("Jitter",                             "Jitter"),
        ["Exp_JitterText"]          = ("Schwankung des Pings. Hoher Jitter = unzuverlässige Verbindung, auch wenn der Durchschnitt gut ist.",
                                       "Variation in ping. High jitter = unstable connection, even if the average is good."),
        ["Exp_JitterScale"]         = ("🟢 <5ms — Stabil\n🟡 5–20ms — Leichte Schwankungen\n🔴 >20ms — Instabil",
                                       "🟢 <5ms — Stable\n🟡 5–20ms — Minor fluctuation\n🔴 >20ms — Unstable"),
        ["Exp_JitterWiki"]          = ("Jitter – Wikipedia",                 "Jitter – Wikipedia"),

        ["Exp_LossHeading"]         = ("Packet Loss",                        "Packet Loss"),
        ["Exp_LossText"]            = ("Pakete ohne Antwort. Verursacht Ruckler, Abbrüche und Aussetzer.",
                                       "Packets that received no reply. Causes stuttering, dropouts and disconnects."),
        ["Exp_LossScale"]           = ("🟢 0% — Perfekt\n🟡 1–5% — Leichte Probleme\n🔴 >5% — Ernsthafte Störung",
                                       "🟢 0% — Perfect\n🟡 1–5% — Minor issues\n🔴 >5% — Serious problem"),
        ["Exp_LossWiki"]            = ("Paketverlust – Wikipedia",           "Packet loss – Wikipedia"),

        // ── Wikipedia article slugs (lang-aware) ──
        ["Wiki_Ping"]               = ("Ping_(Daten%C3%BCbertragung)",       "Ping_(networking_utility)"),
        ["Wiki_Jitter"]             = ("Jitter",                             "Jitter"),
        ["Wiki_PacketLoss"]         = ("Paketverlust",                       "Packet_loss"),

        // ── Port Scan View ───────────────────────
        ["Port_Title"]              = ("Port-Scanner",                       "Port Scanner"),
        ["Port_Subtitle"]           = ("Offene TCP-Ports eines Geräts ermitteln",
                                       "Discover open TCP ports on a device"),
        ["Port_Target"]             = ("Ziel-IP / Hostname",                 "Target IP / hostname"),
        ["Port_Profile"]            = ("Profil",                             "Profile"),
        ["Port_Quick"]              = ("Quick (Top 100)",                    "Quick (Top 100)"),
        ["Port_Standard"]           = ("Standard (Top 1000)",                "Standard (Top 1000)"),
        ["Port_Full"]               = ("Full (1–65535)",                     "Full (1–65535)"),
        ["Port_Custom"]             = ("Benutzerdefiniert",                  "Custom"),
        ["Port_StartScan"]          = ("Scan starten",                       "Start scan"),
        ["Port_NoResults"]          = ("Noch kein Port-Scan durchgeführt",   "No port scan run yet"),

        // ── Network Info View ────────────────────
        ["NetInfo_Title"]           = ("Netzwerk-Info",                      "Network Info"),
        ["NetInfo_Subtitle"]        = ("Informationen über lokales Netzwerk und Verbindungen",
                                       "Local network and connection details"),
        ["NetInfo_Refresh"]         = ("Aktualisieren",                      "Refresh"),
        ["NetInfo_LocalIP"]         = ("Lokale IP",                          "Local IP"),
        ["NetInfo_Gateway"]         = ("Gateway",                            "Gateway"),
        ["NetInfo_Subnet"]          = ("Subnetz",                            "Subnet"),
        ["NetInfo_DNS"]             = ("DNS-Server",                         "DNS Servers"),
        ["NetInfo_PublicIP"]        = ("Öffentliche IP",                     "Public IP"),
        ["NetInfo_WLAN"]            = ("WLAN",                               "WLAN"),
        ["NetInfo_Adapters"]        = ("Adapter",                            "Adapters"),
        ["NetInfo_Connections"]     = ("Aktive Verbindungen",                "Active Connections"),

        // ── Report View ──────────────────────────
        ["Report_Title"]            = ("Report",                             "Report"),
        ["Report_Subtitle"]         = ("Scan-Ergebnisse exportieren und vergleichen",
                                       "Export and compare scan results"),
        ["Report_Html"]             = ("HTML-Report exportieren",            "Export HTML report"),
        ["Report_Csv"]              = ("CSV exportieren",                    "Export CSV"),
        ["Report_Json"]             = ("JSON exportieren",                   "Export JSON"),
        ["Report_Compare"]          = ("Mit vorherigem Scan vergleichen",    "Compare with previous scan"),
        ["Report_NoData"]           = ("Noch kein Scan-Ergebnis vorhanden",  "No scan result available yet"),

        // ── Notifications / Errors ───────────────
        ["Err_Generic"]             = ("Ein unerwarteter Fehler ist aufgetreten:", "An unexpected error occurred:"),
        ["Err_Title"]               = ("NetworkLens — Fehler",               "NetworkLens — Error"),
        ["Notif_NewDevice"]         = ("Neues Gerät erkannt",                "New device detected"),
        ["Notif_DeviceOnline"]      = ("Gerät wieder online",                "Device back online"),
        ["Notif_DeviceOffline"]     = ("Gerät offline",                      "Device offline"),
        ["Notif_NewPort"]           = ("Neuer offener Port",                 "New open port"),

        // ── Footer / Misc ────────────────────────
        ["Footer_Tagline"]          = ("Sideforge · NetworkLens",            "Sideforge · NetworkLens"),
        ["Restart_Required"]        = ("Neustart erforderlich für Admin-Funktionen",
                                       "Restart required for admin features"),
        ["Restart_Now"]             = ("Jetzt neu starten",                  "Restart now"),
    };
}
