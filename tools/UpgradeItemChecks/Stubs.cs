using System.Runtime.CompilerServices;
using REPOJP.StageRoles;

public sealed class PlayerAvatar(string id, int photonId)
{
    public string SteamId = id;
    public int PhotonId = photonId;
}
public sealed class GameDirector
{
    public static GameDirector instance = new();
    public List<PlayerAvatar> PlayerList = new();
}
public sealed class StatsManager
{
    public static StatsManager instance = new();
    public Dictionary<string, int> runStats = new();
    public Dictionary<string, Dictionary<string, int>> Players = new();
    public int Saves;
    public bool FailSave;
    public void SaveFileSave()
    {
        if (FailSave) throw new IOException("Simulated disk failure");
        Saves++;
    }
}
public sealed class ItemToggle
{
    public bool toggleState = true;
    public int playerTogglePhotonID;
}
public sealed class ItemUpgrade(int photonId, string upgrade, int amount = 1)
{
    private bool upgradeDone;
    public bool isPlayerUpgrade = true;
    public ItemToggle Toggle = new() { playerTogglePhotonID = photonId };
    public bool Fail;
    public Action? AfterEffect;
    public T GetComponent<T>() where T : class => (Toggle as T)!;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void PlayerUpgrade()
    {
        UpgradeItemRetentionPatch.Prefix(this, out var state);
        PlayerUpgradeOriginal();
        UpgradeItemRetentionPatch.Postfix(this, state);
    }
    private void PlayerUpgradeOriginal()
    {
        if (upgradeDone || !isPlayerUpgrade || !Toggle.toggleState) return;
        if (Fail) throw new InvalidOperationException("Item use refused");
        var player = SemiFunc.PlayerAvatarGetFromPhotonID(Toggle.playerTogglePhotonID)!;
        UpgradeService.AddLevelsHostAuthoritative(player.SteamId, upgrade, amount);
        AfterEffect?.Invoke();
        upgradeDone = true;
    }
}
public static class SemiFunc
{
    public static bool Authority = true;
    public static bool IsMasterClientOrSingleplayer() => Authority;
    public static PlayerAvatar? PlayerAvatarGetFromPhotonID(int id) =>
        GameDirector.instance.PlayerList.FirstOrDefault(p => p.PhotonId == id);
}
namespace REPOJP.StageRoles
{
    internal enum UpgradeItemScope { Player, AllPlayers }
    internal sealed class Entry<T>(T value) { internal T Value = value; }
    internal sealed class StageRolesConfig
    {
        internal Entry<bool> Enabled = new(true);
        internal Entry<bool> KeepUpgradeItems = new(false);
        internal Entry<UpgradeItemScope> UpgradeItemScope = new(REPOJP.StageRoles.UpgradeItemScope.Player);
    }
    internal sealed class StageRolesPlugin
    {
        internal static StageRolesPlugin Instance = new();
        internal static Logger ModLogger = new();
        internal StageRolesConfig Settings = new();
        internal StageRoleController Controller = new();
    }
    internal sealed class Logger { internal void LogWarning(string value) { } }
    internal sealed class StageRoleController
    {
        internal List<(string, IReadOnlyList<UpgradeGrant>)> Received = new();
        internal void RetainedUpgradeApplied(string steamId, IReadOnlyList<UpgradeGrant> changes) => Received.Add((steamId, changes));
    }
    internal static class PlayerIdentity { internal static string SteamId(PlayerAvatar player) => player.SteamId; }
    internal static class GameSaveState
    {
        internal static string CurrentName = "save-a";
        internal static bool CanSave = true;
    }
    internal readonly record struct UpgradeGrant(string CommandName, string DictionaryName, int Level);
    internal sealed record DynamicUpgradeDefinition(string Name, string CommandName, string DictionaryName, int MaximumLevel);
    internal static class RoleUpgradeScaling
    {
        internal static readonly IReadOnlyList<DynamicUpgradeDefinition> Definitions =
            new[] { "Health", "Stamina", "ExtraJump", "Speed", "Strength", "Range", "Launch", "TumbleClimb", "TumbleWings", "CrouchRest", "MapPlayerCount", "DeathHeadBattery" }
            .Select(n => new DynamicUpgradeDefinition(n, n, "playerUpgrade" + n, n == "MapPlayerCount" ? 1 : 200)).ToArray();
    }
    internal static class UpgradeService
    {
        internal static bool TryGetLevels(string id, out Dictionary<string, int> levels)
        {
            levels = StatsManager.instance.Players.GetValueOrDefault(id, new());
            return !string.IsNullOrEmpty(id);
        }
        internal static bool AddLevelsHostAuthoritative(string id, string command, int delta)
        {
            if (!StatsManager.instance.Players.TryGetValue(id, out var levels))
                StatsManager.instance.Players[id] = levels = new();
            string key = "playerUpgrade" + command;
            long next = (long)levels.GetValueOrDefault(key) + delta;
            levels[key] = (int)Math.Clamp(next, 0, int.MaxValue);
            return true;
        }
    }
}
