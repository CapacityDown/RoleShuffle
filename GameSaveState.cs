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

    internal static string CurrentName => StatsManager.instance != null
        ? CurrentFile?.GetValue(StatsManager.instance) as string ?? string.Empty : string.Empty;

    internal static bool CanSave => StatsManager.instance != null &&
        GameManager.instance != null && GameDirector.instance != null &&
        FileReady?.GetValue(StatsManager.instance) is true &&
        LobbyType?.GetValue(GameManager.instance) is GameManager.LobbyTypes lobby &&
        StatsManager.instance.savedLobbyTypes.Contains(lobby);
}
