using System;
using System.Reflection;

namespace REPOJP.StageRoles;

internal static class GameSaveState
{
    // GameLibs exposes these fields for compilation, but the installed game
    // keeps them internal. Read cached metadata instead of emitting ldfld.
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly FieldInfo? CurrentFile = typeof(StatsManager).GetField("saveFileCurrent", Fields);
    private static readonly FieldInfo? FileReady = typeof(StatsManager).GetField("saveFileReady", Fields);
    private static readonly FieldInfo? LobbyType = typeof(GameManager).GetField("lobbyType", Fields);
    private static StatsManager? _resetStats;
    private static string _resetIdentity = string.Empty;

    internal static string CurrentName => StatsManager.instance != null
        ? CurrentFile?.GetValue(StatsManager.instance) as string ?? string.Empty : string.Empty;

    private static bool CanWrite => StatsManager.instance != null &&
        GameManager.instance != null && GameDirector.instance != null &&
        LobbyType?.GetValue(GameManager.instance) is GameManager.LobbyTypes lobby &&
        StatsManager.instance.savedLobbyTypes.Contains(lobby);

    internal static bool CanSave => CanWrite && FileReady?.GetValue(StatsManager.instance) is true;

    // A reset leaves the old filename behind with saveFileReady=false. A fresh
    // token prevents a callback from the defeated run from editing its successor.
    internal static void RecordRunReset(StatsManager stats)
    {
        _resetStats = stats;
        _resetIdentity = "new-run:" + Guid.NewGuid().ToString("N");
    }

    internal static void RecordSaveReady(StatsManager stats)
    {
        if (ReferenceEquals(_resetStats, stats) && FileReady?.GetValue(stats) is true)
        {
            _resetStats = null;
            _resetIdentity = string.Empty;
        }
    }

    internal static string PendingNewRunIdentity => CanWrite && !CanSave &&
        ReferenceEquals(_resetStats, StatsManager.instance) &&
        RunManager.instance != null && RunManager.instance.levelCurrent != null &&
        RunManager.instance.levelsCompleted == 0 && SemiFunc.RunIsLobbyMenu()
            ? _resetIdentity : string.Empty;

    internal static bool PrepareNewRun(string identity)
    {
        if (!RoleSelectionSettings.CanEdit || string.IsNullOrEmpty(identity) ||
            identity != PendingNewRunIdentity) return false;
        // Use the native initializer so the deleted run's filename/metadata is
        // not reused. No disk write occurs until the validated adjustment saves.
        StatsManager.instance!.SaveFileCreate();
        return CanSave && CurrentName.Length > 0;
    }
}
