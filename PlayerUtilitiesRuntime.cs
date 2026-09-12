using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace REPOJP.StageRoles;

internal static class BaseUpgradeHistory
{
    internal const string PropertyKey = "RS.BUD.History";
    private static string? _remotePayload;
    private static IReadOnlyList<UpgradeDrawRecord> _remoteRecords = Array.Empty<UpgradeDrawRecord>();
    private static IReadOnlyList<UpgradeDrawRecord>? _previewRecords;
    private static string? _previewPayload;
    private static object? _previewRoom, _previewStats;
    internal static bool IsHost => PhotonNetwork.CurrentRoom == null || PhotonNetwork.IsMasterClient;
    internal static IReadOnlyList<UpgradeDrawRecord> Local => StatsManager.instance?.runStats != null
        ? DrawHistoryStore.Read(StatsManager.instance.runStats) : Array.Empty<UpgradeDrawRecord>();
    internal static string LocalPayload => DrawHistoryStore.Serialize(Local);
    internal static string? CurrentPayload => IsHost ? LocalPayload : RoleSyncStatus.Property(PropertyKey);

    internal static bool IsDisplayPreview
    {
        get
        {
            if (_previewRecords != null && (!ReferenceEquals(_previewRoom, PhotonNetwork.CurrentRoom) ||
                !ReferenceEquals(_previewStats, StatsManager.instance?.runStats))) ClearDisplayPreview();
            return _previewRecords != null;
        }
    }
    // Only the menu reads these properties. Reports and network publication use
    // the actual history even while a local display test is active.
    internal static string? DisplayPayload => IsDisplayPreview ? _previewPayload : CurrentPayload;
    internal static IReadOnlyList<UpgradeDrawRecord> ReadForDisplay() => IsDisplayPreview ? _previewRecords! : Read();

    internal static void ClearDisplayPreview()
    {
        _previewRecords = null; _previewPayload = null; _previewRoom = null; _previewStats = null;
    }

    internal static bool TrySetDisplayPreview(string[] args, out string response)
    {
        response = $"Usage: /drawhistory [0-{DrawHistoryStore.Capacity}|reset]";
        if (args.Length > 1) return false;
        if (args.Length == 1 && string.Equals(args[0], "reset", StringComparison.OrdinalIgnoreCase))
        {
            ClearDisplayPreview();
            response = "Draw history preview reset. DRAW HISTORY now shows the current run's history.";
            return true;
        }
        int count = DrawHistoryStore.Capacity;
        if (args.Length == 1 && (!int.TryParse(args[0], NumberStyles.None, CultureInfo.InvariantCulture, out count) ||
            count < 0 || count > DrawHistoryStore.Capacity)) return false;

        // Exercise the real record format without touching StatsManager or upgrades.
        Dictionary<string, int> sampleStats = new();
        for (int index = 0; index < count; index++)
        {
            int target = index % (DrawHistoryStore.UpgradeNames.Length + 1) - 1;
            int delta = (index % 4) switch { 0 => 2, 1 => -1, 2 => 0, _ => 1 };
            var record = new UpgradeDrawRecord { Level = index + 1, Upgrade = target, Delta = delta };
            for (int upgrade = 0; upgrade < record.Before.Length; upgrade++)
            {
                int cap = DrawHistoryStore.UpgradeNames[upgrade] == "MapPlayerCount" ? 1 : 200;
                bool selected = target < 0 || target == upgrade;
                int before = index % 4 == 3 && selected ? cap : Math.Min(cap, 2 + upgrade % 4);
                record.Before[upgrade] = before;
                record.After[upgrade] = selected ? Math.Max(0, Math.Min(cap, before + delta)) : before;
            }
            DrawHistoryStore.Append(sampleStats, record);
        }
        _previewRecords = DrawHistoryStore.Read(sampleStats);
        _previewPayload = DrawHistoryStore.Serialize(_previewRecords);
        _previewRoom = PhotonNetwork.CurrentRoom;
        _previewStats = StatsManager.instance?.runStats;
        response = $"Local draw history preview: {count} sample draws. Open ROLES > DRAW HISTORY. Use /drawhistory reset to restore actual history.";
        return true;
    }

    internal static IReadOnlyList<UpgradeDrawRecord> Read()
    {
        if (IsHost) return Local;
        string? payload = CurrentPayload;
        if (payload != _remotePayload)
        {
            _remotePayload = payload;
            _remoteRecords = payload != null && DrawHistoryStore.TryParse(payload, out var parsed)
                ? parsed : Array.Empty<UpgradeDrawRecord>();
        }
        return _remoteRecords;
    }

    internal static int[] Levels(StageRolesConfig config)
    {
        int[] levels = new int[DrawHistoryStore.UpgradeNames.Length];
        foreach (UpgradeGrant grant in RoleCatalog.BaseUpgrades(config))
        {
            int index = Array.IndexOf(DrawHistoryStore.UpgradeNames, grant.CommandName);
            if (index >= 0) levels[index] = grant.Level;
        }
        return levels;
    }

    internal static void Record(string upgrade, int delta, int[] before, StageRolesConfig config)
    {
        if (!IsHost || StatsManager.instance?.runStats == null) return;
        DrawHistoryStore.Append(StatsManager.instance.runStats, new UpgradeDrawRecord
        {
            Level = RunManager.instance != null ? RunManager.instance.levelsCompleted + 1 : 0,
            Upgrade = Array.IndexOf(DrawHistoryStore.UpgradeNames, upgrade),
            Delta = delta, Before = before, After = Levels(config)
        });
        RoleSyncStatus.Instance?.PublishNow();
    }

    internal static string Describe(UpgradeDrawRecord record, bool japanese) =>
        Describe(record, japanese ? RoleGuideLanguage.Japanese : RoleGuideLanguage.English);

    internal static string Describe(UpgradeDrawRecord record, RoleGuideLanguage language)
    {
        string target = record.Upgrade < 0 ? RoleText.Get("All Upgrades", language, "全アップグレード")
            : DrawHistoryStore.UpgradeNames[record.Upgrade];
        StringBuilder text = new($"#{record.Number}  {RoleText.Get("Level", language, "ステージ")} {record.Level}  {target}  {record.Delta:+0;-0;0}\n");
        bool changed = false;
        for (int i = 0; i < record.Before.Length; i++)
        {
            if (record.Upgrade >= 0 && record.Upgrade != i) continue;
            int delta = record.After[i] - record.Before[i];
            changed |= delta != 0;
            text.AppendLine($"  {DrawHistoryStore.UpgradeNames[i]}: {record.Before[i]} → {record.After[i]} ({delta:+0;-0;0})");
        }
        if (!changed) text.AppendLine("  " + RoleText.Get("No change (zero draw or level limit)", language, "変化なし（抽選値0、または上限・下限）"));
        return text.ToString().TrimEnd();
    }
}

internal sealed class RoleSyncStatus : MonoBehaviour
{
    private const string StampKey = "RS.Sync.Status";
    private const string RequestKey = "RS.Sync.Request";
    private static readonly string[] Keys =
        { "RoleShuffleGuideV2", "RoleShuffleGuideEnabledV1", "RS.BaseUpgrades", "RS.RoleMap", BaseUpgradeHistory.PropertyKey };
    internal static RoleSyncStatus? Instance { get; private set; }
    private StageRolesConfig _config = null!;
    private object? _room;
    private int _master;
    private float _nextPublish;
    private float _nextPoll;
    private float _lastPublish = -10f;
    private bool _force;
    private readonly Dictionary<int, int> _requests = new();
    private int? _requestedAt;
    private float _requestTime;
    private float _joinedAt;
    private string? _cachedStamp;
    private string?[] _cachedPayloads = new string?[0];
    private bool _cachedMatch;

    internal void Initialize(StageRolesConfig config) { _config = config; Instance = this; }
    internal static string? Property(string key) => PhotonNetwork.CurrentRoom != null &&
        PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out object value) ? value as string : null;

    private void Update()
    {
        object? room = PhotonNetwork.CurrentRoom;
        int master = PhotonNetwork.MasterClient?.ActorNumber ?? 0;
        if (!ReferenceEquals(room, _room) || master != _master)
        {
            _room = room; _master = master; _nextPublish = 0; _lastPublish = -10;
            _force = true; _requests.Clear(); _requestedAt = null; _cachedStamp = null;
            _joinedAt = Time.unscaledTime;
        }
        if (room == null || !PhotonNetwork.IsMasterClient) return;
        if (Time.unscaledTime >= _nextPoll)
        {
            _nextPoll = Time.unscaledTime + 1f;
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties.TryGetValue(RequestKey, out object raw) && raw is int request &&
                    (!_requests.TryGetValue(player.ActorNumber, out int previous) || previous != request))
                { _requests[player.ActorNumber] = request; _force = true; }
            }
        }
        if (Time.unscaledTime >= _nextPublish || (_force && Time.unscaledTime - _lastPublish >= 3f)) PublishNow();
    }

    internal void PublishNow()
    {
        if (PhotonNetwork.CurrentRoom == null || !PhotonNetwork.IsMasterClient || GameManager.instance == null) return;
        string[] payloads =
        {
            RoleGuideSync.CurrentSignature(_config), RoleGuideSync.VisibilitySignature(_config),
            BaseUpgradeSync.LocalSignature(_config), RoleAssignmentSync.LocalPayload, BaseUpgradeHistory.LocalPayload
        };
        Hashtable props = new();
        for (int i = 0; i < Keys.Length; i++)
            if (_force || Property(Keys[i]) != payloads[i]) props[Keys[i]] = payloads[i];
        props[StampKey] = SyncStamp.Create(PhotonNetwork.LocalPlayer.ActorNumber,
            StageRolesPlugin.PluginVersion, PhotonNetwork.ServerTimestamp, payloads);
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        _force = false; _lastPublish = Time.unscaledTime; _nextPublish = Time.unscaledTime + 5f;
    }

    internal void RequestRefresh()
    {
        if (Time.unscaledTime - _requestTime < 3f && _requestedAt.HasValue) return;
        RoleAssignmentSync.ResetCache(); RoleGuideSync.Invalidate();
        if (PhotonNetwork.CurrentRoom == null || PhotonNetwork.IsMasterClient)
        { _force = true; PublishNow(); return; }
        _requestedAt = PhotonNetwork.ServerTimestamp;
        _requestTime = Time.unscaledTime;
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { [RequestKey] = _requestedAt.Value });
    }

    internal string Describe(bool ja) => Describe(ja ? RoleGuideLanguage.Japanese : RoleGuideLanguage.English);

    internal string Describe(RoleGuideLanguage language)
    {
        string Pick(string en, string jp) => RoleText.Get(en, language, jp);
        if (PhotonNetwork.CurrentRoom == null) return Pick("Local / no room", "ローカル / ルーム未参加");
        if (PhotonNetwork.IsMasterClient) return Pick("Host — publishing display data", "ホスト — 表示データを配信中");
        string? stamp = Property(StampKey);
        if (stamp == null)
            return Property(Keys[0]) != null || Property("RoleShuffleGuideV1") != null
                ? Pick("Host does not support sync status (older version)", "ホストは同期状態表示に未対応（旧バージョン）")
                : Time.unscaledTime - _joinedAt < 20f
                    ? Pick("Waiting for host data", "ホストのデータを待機中")
                    : Pick("Host data unavailable / host may not have RoleShuffle", "ホストのデータ未受信 / 未導入の可能性あり");
        if (!SyncStamp.TryRead(stamp, out int actor, out string version, out int sent, out string[] hashes))
            return Pick("Unsupported or invalid sync status", "未対応または不正な同期情報");
        if (actor != (PhotonNetwork.MasterClient?.ActorNumber ?? 0))
            return Time.unscaledTime - _joinedAt < 20f
                ? Pick("Waiting for new host", "新しいホストからの同期を待機中")
                : Pick("New host data unavailable / status may be unsupported", "新しいホストのデータ未受信 / 状態表示に未対応の可能性あり");
        int age = SyncStamp.AgeMilliseconds(PhotonNetwork.ServerTimestamp, sent);
        string suffix = "\n" + RoleText.Format("Host v{0} / Local v{1}", language, version, StageRolesPlugin.PluginVersion);
        if (age < -1000 || age > 20000) return Pick("Host updates delayed", "ホストの更新が遅延しています") + suffix;
        string?[] payloads = Keys.Select(Property).ToArray();
        if (_cachedStamp != stamp || !_cachedPayloads.SequenceEqual(payloads))
        {
            _cachedStamp = stamp; _cachedPayloads = payloads;
            _cachedMatch = SyncStamp.Matches(hashes, payloads);
        }
        if (!_cachedMatch) return Pick("Receiving display data", "表示データを受信中") + suffix;
        if (_requestedAt.HasValue)
        {
            if (SyncStamp.AgeMilliseconds(sent, _requestedAt.Value) > 0) _requestedAt = null;
            else return Pick("Refresh requested — waiting for host", "再取得を要求済み — ホストの応答待ち") + suffix;
        }
        return Pick("Synchronized", "同期済み") + suffix +
            (version == StageRolesPlugin.PluginVersion ? "" : Pick("\nVersion differs", "\nバージョンが異なります"));
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }
}

internal sealed class RoleBugReport : ILogListener
{
    internal const string IssuesUrl = "https://github.com/CapacityDown/RoleShuffle/issues/new";
    private const int MaximumLogEntries = 500;
    private const int MaximumLogEntryCharacters = 2048;
    private readonly object _gate = new();
    private readonly Queue<string> _logs = new();
    private readonly HashSet<string> _privateValues = new(StringComparer.Ordinal);
    internal string LatestText { get; private set; } = string.Empty;
    internal string LatestPath { get; private set; } = string.Empty;
    internal void RememberPlayers()
    {
        lock (_gate)
        {
            if (PhotonNetwork.CurrentRoom != null) _privateValues.Add(PhotonNetwork.CurrentRoom.Name);
            foreach (var player in PhotonNetwork.PlayerList)
            { _privateValues.Add(player.NickName); if (player.UserId != null) _privateValues.Add(player.UserId); }
            if (GameDirector.instance != null)
                foreach (var player in GameDirector.instance.PlayerList)
                    if (player != null)
                    { _privateValues.Add(PlayerIdentity.Name(player)); _privateValues.Add(PlayerIdentity.SteamId(player)); }
            // Keep the privacy set bounded without leaving older log entries unprotected.
            if (_privateValues.Count > 1000) { _logs.Clear(); _privateValues.Clear(); }
        }
    }
    public void LogEvent(object sender, LogEventArgs args)
    {
        string body = args.Data?.ToString() ?? string.Empty;
        bool fromMod = args.Source.SourceName == StageRolesPlugin.PluginName || args.Source.SourceName == StageRolesPlugin.PluginGuid;
        bool relatedException = (args.Level & (LogLevel.Error | LogLevel.Fatal)) != 0 &&
            body.Contains("REPOJP.StageRoles.");
        if (!fromMod && !relatedException) return;
        string value = $"[{DateTime.UtcNow:O}] [{args.Level}] {body}";
        lock (_gate)
        {
            _logs.Enqueue(value.Substring(0, Math.Min(value.Length, MaximumLogEntryCharacters)));
            while (_logs.Count > MaximumLogEntries) _logs.Dequeue();
        }
    }

    internal void Create(ConfigFile config)
    {
        RememberPlayers();
        // Protect known version fields from the IP-address redactor. The values come
        // from structured version metadata, never from diagnostic log messages.
        Dictionary<string, string> versions = new();
        string VersionField(string value)
        {
            string marker = "RSVERSION" + Guid.NewGuid().ToString("N");
            versions[marker] = value;
            return marker;
        }
        StringBuilder report = new("# RoleShuffle bug report\n\n## What happened?\nPlease describe the problem.\n\n## Reproduction steps\n1. \n\n## Expected / actual result\n\n## Environment\n");
        report.AppendLine($"RoleShuffle: {StageRolesPlugin.PluginVersion} (build-{RoleMenu.UiBuildNumber})");
        report.AppendLine($"Game: {VersionField(Application.version)}\nUnity: {VersionField(Application.unityVersion)}\nOS: {SystemInfo.operatingSystem}");
        report.AppendLine($"Scene: {SceneManager.GetActiveScene().name}\nSync: {RoleSyncStatus.Instance?.Describe(false)}");
        report.AppendLine("\n## Installed mods");
        int sectionStart = report.Length;
        foreach (var info in Chainloader.PluginInfos.Values.OrderBy(p => p.Metadata.GUID))
        {
            report.AppendLine($"- {info.Metadata.GUID} {VersionField(info.Metadata.Version.ToString())}");
            if (report.Length - sectionStart > 10000) { report.AppendLine("[TRUNCATED]"); break; }
        }
        report.AppendLine("\n## RoleShuffle configuration (local, size-limited)");
        sectionStart = report.Length;
        foreach (var entry in config.OrderBy(e => e.Key.Section).ThenBy(e => e.Key.Key))
        {
            string value = entry.Value.GetSerializedValue();
            if (value.Length > 512) value = value.Substring(0, 512) + "[TRUNCATED]";
            report.AppendLine($"{entry.Key.Section}.{entry.Key.Key} = {value}");
            if (report.Length - sectionStart > 30000) { report.AppendLine("[TRUNCATED]"); break; }
        }
        report.AppendLine("\n## Recent Base Upgrade draws");
        foreach (var record in BaseUpgradeHistory.Read().Take(10)) report.AppendLine(BaseUpgradeHistory.Describe(record, false));
        string[] privateValues, recentLogs;
        lock (_gate)
        {
            privateValues = _privateValues.ToArray();
            recentLogs = _logs.Reverse().ToArray();
        }
        string cleaned = ReportRedactor.Clean(report.ToString(), privateValues);
        foreach (var version in versions) cleaned = cleaned.Replace(version.Key, version.Value);
        // Redact bounded log entries individually, so the metadata's total-size
        // limit cannot discard older entries from the retained 500-log window.
        StringBuilder complete = new(cleaned);
        complete.AppendLine($"\n## Recent RoleShuffle logs (up to {MaximumLogEntries} entries, newest first; each entry limited to {MaximumLogEntryCharacters} characters)\n");
        foreach (string log in recentLogs)
            complete.AppendLine(ReportRedactor.Clean(log, privateValues, MaximumLogEntryCharacters));
        string text = complete.ToString();
        string directory = Path.Combine(Paths.BepInExRootPath, "RoleShuffleReports");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, $"RoleShuffle-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.md");
        File.WriteAllText(path, text, new UTF8Encoding(false));
        LatestText = text;
        LatestPath = path;
    }
    public void Dispose() { lock (_gate) { _logs.Clear(); _privateValues.Clear(); } }
}
