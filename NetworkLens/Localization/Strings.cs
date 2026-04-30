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
        ["Monitor_Interval"]        = ("Intervall:",                        "Interval:"),
        ["Monitor_Continuous"]      = ("Dauerhaft",                          "Continuous"),
        ["Monitor_StartButton"]     = ("Starten",                            "Start"),
        ["Monitor_StopButton"]      = ("Stop",                               "Stop"),
        ["Monitor_Explanations"]    = ("Erklärungen",                        "Explanations"),
        ["Monitor_AddDevice"]       = ("Gerät hinzufügen:",                 "Add device:"),
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

        // ── Footer / Status Bar ──────────────────
        ["Footer_Ready"]            = ("Bereit",                            "Ready"),
        ["Footer_Devices"]          = ("Geräte",                            "devices"),

        // ── Scan View - Extended ─────────────────
        ["Scan_AllSubtitle"]        = ("Alle Geräte im lokalen Netzwerk erkennen", "Discover all devices on the local network"),
        ["Scan_Subnet"]             = ("Subnetz:",                          "Subnet:"),
        ["Scan_Timeout"]            = ("Timeout:",                          "Timeout:"),
        ["Scan_TimeoutDefault"]     = ("1000ms (Standard)",                 "1000ms (default)"),
        ["Scan_AutoDetectShort"]    = ("Auto-detect",                       "Auto-detect"),
        ["Scan_NoScanRun"]          = ("Kein Scan durchgeführt",            "No scan performed"),
        ["Scan_NoScanHint"]         = ("Klicke auf ▶ Scan starten um alle Geräte im Netzwerk zu finden.",
                                       "Click ▶ Start scan to discover all devices on the network."),
        ["Scan_StartShort"]         = ("Scan starten",                      "Start scan"),

        // ── Port Scan View - Extended ────────────
        ["Port_FullSubtitle"]       = ("TCP Connect Scan — offene Ports und Dienste erkennen",
                                       "TCP Connect Scan — discover open ports and services"),
        ["Port_TargetIp"]           = ("Ziel-IP:",                          "Target IP:"),
        ["Port_ProfileLabel"]       = ("Profil:",                           "Profile:"),
        ["Port_ProfileStandard"]    = ("Standard (Top 100)",                "Standard (Top 100)"),
        ["Port_FilterAll"]          = ("Alle",                              "All"),
        ["Port_FilterOpen"]         = ("Offen",                             "Open"),
        ["Port_FilterFiltered"]     = ("Gefiltert",                         "Filtered"),
        ["Port_CopyAsText"]         = ("Als Text kopieren",                 "Copy as text"),
        ["Port_NoPortScan"]         = ("Kein Port-Scan durchgeführt",       "No port scan performed"),
        ["Port_NoPortScanHint"]     = ("IP eingeben und Scan starten.",     "Enter an IP and start the scan."),

        // ── Network Info - Extended ──────────────
        ["NetInfo_FullSubtitle"]    = ("Lokale Adapter, IPs, DNS und aktive Verbindungen",
                                       "Local adapters, IPs, DNS and active connections"),
        ["NetInfo_Refresh"]         = ("Aktualisieren",                     "Refresh"),
        ["NetInfo_LocalIPCard"]     = ("LOKALE IP",                         "LOCAL IP"),
        ["NetInfo_GatewayCard"]     = ("GATEWAY",                           "GATEWAY"),
        ["NetInfo_PublicIPCard"]    = ("ÖFFENTLICHE IP",                    "PUBLIC IP"),
        ["NetInfo_Mask"]            = ("Maske",                             "Mask"),
        ["NetInfo_DHCP"]            = ("DHCP",                              "DHCP"),
        ["NetInfo_PingPrefix"]      = ("Ping",                              "Ping"),
        ["NetInfo_DnsCard"]         = ("DNS-SERVER",                        "DNS SERVERS"),
        ["NetInfo_WlanCard"]        = ("WLAN",                              "WLAN"),
        ["NetInfo_NoWlan"]          = ("Kein WLAN verbunden",               "No WLAN connected"),
        ["NetInfo_OnlyLan"]         = ("Nur LAN-Verbindung erkannt",        "Only LAN connection detected"),
        ["NetInfo_Adapters"]        = ("NETZWERK-ADAPTER",                  "NETWORK ADAPTERS"),
        ["NetInfo_AdapterStats"]    = ("aktiv / gesamt",                    "active / total"),
        ["NetInfo_BadgeActive"]     = ("AKTIV",                             "ACTIVE"),
        ["NetInfo_BadgeInactive"]   = ("INAKTIV",                           "INACTIVE"),
        ["NetInfo_Connections"]     = ("AKTIVE VERBINDUNGEN",               "ACTIVE CONNECTIONS"),
        ["NetInfo_Reload"]          = ("Neu laden",                         "Reload"),
        ["NetInfo_Copy"]            = ("Kopieren",                          "Copy"),
        ["Conn_Protocol"]           = ("Protokoll",                         "Protocol"),
        ["Conn_LocalAddr"]          = ("Lokale Adresse",                    "Local Address"),
        ["Conn_RemoteAddr"]         = ("Remote-Adresse",                    "Remote Address"),
        ["Conn_State"]              = ("Status",                            "State"),
        ["Conn_Process"]            = ("Prozess",                           "Process"),
        ["Conn_PID"]                = ("PID",                               "PID"),
        ["Conn_Listen"]             = ("ABHÖREN",                           "LISTEN"),
        ["Conn_Established"]        = ("VERBUNDEN",                         "ESTABLISHED"),
        ["Conn_TimeWait"]           = ("WARTET",                            "TIME_WAIT"),
        ["Conn_CloseWait"]          = ("SCHLIESST",                         "CLOSE_WAIT"),

        // ── Report - Extended ────────────────────
        ["Report_FullSubtitle"]     = ("Scan-Ergebnisse exportieren und Verlauf vergleichen",
                                       "Export scan results and compare history"),
        ["Report_ExportCurrent"]    = ("AKTUELLEN SCAN EXPORTIEREN",        "EXPORT CURRENT SCAN"),
        ["Report_NoCurrentScan"]    = ("Noch kein Scan durchgeführt. Bitte zuerst einen Netzwerk-Scan starten.",
                                       "No scan performed yet. Please run a network scan first."),
        ["Report_History"]          = ("SCAN-VERLAUF",                      "SCAN HISTORY"),
        ["Report_ClearHistory"]     = ("Verlauf löschen",                   "Clear history"),
        ["Report_DateTime"]         = ("Datum/Zeit",                        "Date/Time"),
        ["Report_Subnet"]           = ("Subnetz",                           "Subnet"),
        ["Report_DeviceCount"]      = ("Geräte",                            "Devices"),
        ["Report_Duration"]         = ("Dauer",                             "Duration"),
        ["Report_HostsChecked"]     = ("Hosts geprüft",                     "Hosts checked"),
        ["Report_Compare"]          = ("SCAN VERGLEICHEN",                  "COMPARE SCANS"),
        ["Report_Before"]           = ("VORHER",                            "BEFORE"),
        ["Report_After"]            = ("NACHHER",                           "AFTER"),
        ["Report_CompareBtn"]       = ("Vergleichen",                       "Compare"),
        ["Report_ChooseScan"]       = ("Scan auswählen...",                 "Select scan..."),

        // ── Settings - Extended ──────────────────
        ["Set_PingTimeout"]         = ("Ping-Timeout pro Host",             "Ping timeout per host"),
        ["Set_PingTimeoutHint"]     = ("Kürzere Timeouts = schnellere Scans, aber evtl. mehr Fehlalarme",
                                       "Shorter timeouts = faster scans, but possibly more false negatives"),
        ["Set_MaxThreads"]          = ("Maximale parallele Threads",        "Maximum parallel threads"),
        ["Set_MaxThreadsHint"]      = ("Mehr Threads = schnellerer Scan, aber höhere CPU-Last",
                                       "More threads = faster scan, but higher CPU load"),
        ["Set_PortTimeout"]         = ("Port-Scan Timeout",                 "Port scan timeout"),
        ["Set_AutoScanStart"]       = ("Auto-Scan beim Start",              "Auto-scan on startup"),
        ["Set_AutoScanHint"]        = ("Netzwerk-Scan beim Programmstart ausführen",
                                       "Run network scan when the application starts"),
        ["Set_DefaultInterval"]     = ("Standard-Intervall",                "Default interval"),
        ["Set_Interval10"]          = ("10 Sekunden (Standard)",            "10 seconds (default)"),
        ["Set_NotifyEnabled"]       = ("Benachrichtigungen aktiv",          "Notifications enabled"),
        ["Set_NotifyNewDevice"]     = ("Neues unbekanntes Gerät im Netzwerk erkannt",
                                       "New unknown device detected on the network"),
        ["Set_NotifyOffline"]       = ("Überwachtes Gerät offline gegangen",
                                       "Monitored device went offline"),
        ["Set_NotifyOnline"]        = ("Gerät wieder online gegangen",      "Device came back online"),
        ["Set_NotifyNewPort"]       = ("Neuer offener Port erkannt",        "New open port detected"),
        ["Set_NotifySound"]         = ("Sound bei Alert abspielen",         "Play sound on alert"),
        ["Set_OutputHint"]          = ("HTML-, CSV- und JSON-Reports werden hier gespeichert.",
                                       "HTML, CSV and JSON reports will be saved here."),
        ["Set_AppConfig"]           = ("Konfiguration",                     "Configuration"),
        ["Set_AppDevices"]          = ("Geräte-Daten",                      "Device data"),
        ["Set_AppScans"]            = ("Scan-Verlauf",                      "Scan history"),

        // Generic timeout values
        ["Time_500ms"]              = ("500 ms (Standard)",                 "500 ms (default)"),
        ["Time_1000ms"]             = ("1000 ms (Standard)",                "1000 ms (default)"),
        ["Time_100Default"]         = ("100 (Standard)",                    "100 (default)"),

        // ── Dialog buttons / labels ──────────────
        ["Dlg_Save"]                = ("Speichern",                          "Save"),
        ["Dlg_Cancel"]              = ("Abbrechen",                          "Cancel"),
        ["Dlg_Device"]              = ("Gerät",                              "Device"),
        ["Dlg_Alias"]               = ("Alias",                              "Alias"),
        ["Dlg_Category"]            = ("Kategorie auswählen",                "Choose category"),

        // ── Status messages ──────────────────────
        ["Stat_DevicesFound"]       = ("{0} Geräte gefunden",                "{0} devices found"),
        ["Stat_ScanComplete"]       = ("Scan abgeschlossen — {0} Geräte",    "Scan complete — {0} devices"),
        ["Stat_Error"]              = ("Fehler: {0}",                        "Error: {0}"),
        ["Stat_ScanError"]          = ("Fehler beim Scan",                   "Scan error"),
        ["Stat_NoDnsFound"]         = ("Keine DNS-Server gefunden",          "No DNS servers found"),
        ["Stat_PingError"]          = ("Ping: Fehler",                       "Ping: error"),

        // ── Report messages ──────────────────────
        ["Rpt_NoSavedScans"]        = ("Noch keine gespeicherten Scans.",    "No saved scans yet."),
        ["Rpt_NewDevices"]          = ("{0} neue Geräte",                    "{0} new devices"),
        ["Rpt_DisappearedDevices"]  = ("{0} verschwunden",                   "{0} disappeared"),
        ["Rpt_ChangedDevices"]      = ("{0} geändert",                       "{0} changed"),

        // ── Graph window hint ────────────────────
        ["Graph_Hint"]              = ("Fenster verschiebbar · Größe änderbar · Monitor läuft im Hintergrund weiter",
                                       "Movable · Resizable · Monitor keeps running in the background"),

        // ── MessageBox titles & bodies ───────────
        ["Msg_Error"]               = ("Fehler",                             "Error"),
        ["Msg_Warning"]             = ("Warnung",                            "Warning"),
        ["Msg_Info"]                = ("Hinweis",                            "Info"),
        ["Msg_Success"]             = ("Erfolg",                             "Success"),
        ["Msg_Confirm"]             = ("Bestätigen",                         "Confirm"),

        ["Msg_TtlError"]            = ("Fehler beim Abrufen",                "Failed to retrieve"),
        ["Msg_AddDevicesFirst"]     = ("Bitte zuerst Geräte hinzufügen.",    "Please add devices first."),
        ["Msg_MonitorTitle"]        = ("Monitor",                            "Monitor"),
        ["Msg_ExportError"]         = ("Fehler beim Exportieren:\n{0}",     "Export error:\n{0}"),
        ["Msg_GenericError"]        = ("Fehler:\n{0}",                      "Error:\n{0}"),
        ["Msg_SelectTwoScans"]      = ("Bitte zwei Scans auswählen.",        "Please select two scans."),
        ["Msg_CompareTitle"]        = ("Vergleich",                          "Compare"),
        ["Msg_SettingsSaved"]       = ("Einstellungen gespeichert.",         "Settings saved."),
        ["Msg_SettingsSaveError"]   = ("Fehler beim Speichern:\n{0}",       "Save error:\n{0}"),
        ["Msg_SettingsLoadError"]   = ("Einstellungen konnten nicht gespeichert werden:\n{0}",
                                       "Settings could not be saved:\n{0}"),

        // ── Tooltips ─────────────────────────────
        ["Tip_MaxPing"]             = ("Höchster gemessener Ping-Wert. Gibt an wie schlecht die Verbindung im schlimmsten Fall war.",
                                       "Highest measured ping value. Indicates the worst-case connection quality."),
        ["Tip_MinPing"]             = ("Niedrigster gemessener Ping-Wert.",
                                       "Lowest measured ping value."),
        ["Tip_AvgPing"]             = ("Durchschnittlicher Ping über alle Messungen.",
                                       "Average ping across all measurements."),

        // ── Toast notifications ──────────────────
        ["Toast_NewDeviceTitle"]    = ("Unbekanntes Gerät",                  "Unknown device"),
        ["Toast_OfflineTitle"]      = ("Gerät offline",                      "Device offline"),
        ["Toast_OnlineTitle"]       = ("Gerät online",                       "Device online"),
        ["Toast_NewPortTitle"]      = ("Neuer offener Port",                 "New open port"),

        // ── Restart prompt ───────────────────────
        ["Restart_Title"]           = ("Neustart erforderlich",              "Restart required"),
        ["Restart_Message"]         = ("Diese Funktion benötigt Administrator-Rechte. NetworkLens jetzt als Administrator neu starten?",
                                       "This feature requires administrator rights. Restart NetworkLens as administrator now?"),
        ["Restart_Failed"]          = ("Neustart fehlgeschlagen.",           "Restart failed."),

        // ── Combobox interval values ─────────────
        ["Int_1Sec"]                = ("1 Sekunde",                          "1 second"),
        ["Int_5Sec"]                = ("5 Sekunden",                         "5 seconds"),
        ["Int_30Sec"]               = ("30 Sekunden",                        "30 seconds"),
        ["Int_60Sec"]               = ("60 Sekunden",                        "60 seconds"),
        ["Int_Continuous"]          = ("Dauerhaft",                          "Continuous"),

        // Generic timeout/threads values
        ["TO_500ms"]                = ("500 ms",                             "500 ms"),
        ["TO_1000ms"]               = ("1000 ms",                            "1000 ms"),
        ["TO_2000ms"]               = ("2000 ms",                            "2000 ms"),
        ["TO_3000ms"]               = ("3000 ms",                            "3000 ms"),
        ["TO_200ms"]                = ("200 ms",                             "200 ms"),
        ["TO_1000msDefault"]        = ("1000 ms (Standard)",                 "1000 ms (default)"),
        ["TO_500msDefault"]         = ("500 ms (Standard)",                  "500 ms (default)"),
        ["Th_50"]                   = ("50",                                 "50"),
        ["Th_100Default"]           = ("100 (Standard)",                     "100 (default)"),
        ["Th_200"]                  = ("200",                                "200"),
        ["Th_500Aggressive"]        = ("500 (aggressiv)",                    "500 (aggressive)"),

        // ── Final touches ───────────────────────
        ["Cat_Title"]               = ("Kategorie wählen",                  "Choose category"),
        ["Cat_Apply"]               = ("Übernehmen",                        "Apply"),

        ["Adm_LimitedTitle"]        = ("⚠ Eingeschränkter Modus",          "⚠ Limited mode"),
        ["Adm_LimitedHint"]         = ("Einige Features benötigen Admin-Rechte.",
                                       "Some features require administrator rights."),

        ["Tip_AvgPingLong"]         = ("Durchschnittlicher Ping über alle bisherigen Messungen. Aussagekräftiger als ein einzelner Wert.",
                                       "Average ping across all measurements so far. More meaningful than a single value."),

        ["Rpt_OpenHtml"]            = ("HTML-Report öffnen",                "Open HTML report"),
        ["Rpt_NoChanges"]           = ("✓ Keine Änderungen zwischen den beiden Scans erkannt.",
                                       "✓ No changes detected between the two scans."),

        ["Scan_Progress"]           = ("{0} / {1} Hosts geprüft — {2} online",
                                       "{0} / {1} hosts checked — {2} online"),

        // ── HTML Report ──────────────────────────
        ["Rep_Title"]               = ("Report",                              "Report"),
        ["Rep_SubnetLabel"]         = ("Subnetz",                             "Subnet"),
        ["Rep_Duration"]            = ("Scan-Dauer",                          "Scan duration"),
        ["Rep_StatTotal"]           = ("Geräte gesamt",                       "Total devices"),
        ["Rep_StatOnline"]          = ("Online",                              "Online"),
        ["Rep_StatOffline"]         = ("Offline",                             "Offline"),
        ["Rep_StatNew"]             = ("Neu erkannt",                         "Newly detected"),
        ["Rep_StatHosts"]           = ("Hosts geprüft",                       "Hosts checked"),
        ["Rep_SearchPlaceholder"]   = ("Suchen (IP, Name, MAC ...)",          "Search (IP, name, MAC ...)"),
        ["Rep_ClearSearch"]         = ("Suche leeren",                        "Clear search"),
        ["Rep_AllStatus"]           = ("Alle Status",                         "All states"),
        ["Rep_StatusSlow"]          = ("Langsam",                             "Slow"),
        ["Rep_RowCount"]            = ("Einträge",                            "entries"),
        ["Rep_ThemeToggle"]         = ("Theme wechseln",                      "Toggle theme"),
        ["Rep_ColIP"]               = ("IP",                                  "IP"),
        ["Rep_ColHostname"]         = ("Hostname",                            "Hostname"),
        ["Rep_ColAlias"]            = ("Alias",                               "Alias"),
        ["Rep_ColMAC"]              = ("MAC",                                 "MAC"),
        ["Rep_ColManufacturer"]     = ("Hersteller",                          "Manufacturer"),
        ["Rep_ColPing"]             = ("Ping",                                "Ping"),
        ["Rep_ColPorts"]            = ("Ports",                               "Ports"),
        ["Rep_ColCategory"]         = ("Kategorie",                           "Category"),
        ["Rep_ColLastSeen"]         = ("Zuletzt gesehen",                     "Last seen"),
        ["Rep_Footer"]              = ("Erstellt mit",                        "Generated by"),
        ["Rep_BadgeNew"]            = ("NEU",                                 "NEW"),
        ["Rep_NoMatches"]           = ("Keine passenden Einträge",            "No matching entries"),

        // ── CSV Header ───────────────────────────
        ["Csv_Header"]              = ("IP-Adresse,Hostname,Alias,MAC-Adresse,Hersteller,Status,Ping (ms),Offene Ports,Kategorie,Zuletzt gesehen",
                                       "IP Address,Hostname,Alias,MAC Address,Manufacturer,Status,Ping (ms),Open Ports,Category,Last Seen"),
    };
}
