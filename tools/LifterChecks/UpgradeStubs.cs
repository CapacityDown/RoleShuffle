// Vanilla stores the level and cached grab force separately. Upgrade RPCs add
// only the accepted level delta to the cache; they do not assign an absolute force.
public static class SemiFunc
{
    public static bool Multiplayer, Authority = true;
    public static Dictionary<string, PlayerAvatar> Players = new();
    public static bool IsMultiplayer() => Multiplayer;
    public static bool IsMasterClientOrSingleplayer() => Authority;
    public static PlayerAvatar? PlayerAvatarGetFromSteamID(string id) => Players.GetValueOrDefault(id);
}
public sealed class StatsManager
{
    public static StatsManager instance = new();
    public Dictionary<string, int> Strength = new();
    public Dictionary<string, int> FetchPlayerUpgrades(string id) =>
        new() { ["playerUpgradeStrength"] = Strength.GetValueOrDefault(id) };
}
public sealed class PunManager
{
    public static PunManager instance = new();
    public T GetComponent<T>() where T : new() => new();
    public void TesterUpgradeCommandRPC(string id, string command, int delta)
    {
        if (command != "Strength") throw new InvalidOperationException(command);
        UpgradePlayerGrabStrength(id, delta);
    }
    public int UpgradePlayerGrabStrength(string id, int value)
    {
        int old = StatsManager.instance.Strength.GetValueOrDefault(id);
        int delta = Math.Max(0, old + value) - old;
        if (delta == 0) return old;
        StatsManager.instance.Strength[id] = old + delta;
        var player = SemiFunc.PlayerAvatarGetFromSteamID(id);
        if (player != null) player.physGrabber.grabStrength += 0.2f * delta;
        return old + delta;
    }
    public int UpgradePlayerCrouchRest(string id, int delta) => throw new NotSupportedException();
    public int UpgradePlayerExtraJump(string id, int delta) => throw new NotSupportedException();
    public int UpgradePlayerHealth(string id, int delta) => throw new NotSupportedException();
    public int UpgradePlayerTumbleLaunch(string id, int delta) => throw new NotSupportedException();
    public int UpgradeMapPlayerCount(string id, int delta) => throw new NotSupportedException();
    public int UpgradePlayerGrabRange(string id, int delta) => throw new NotSupportedException();
    public int UpgradePlayerSprintSpeed(string id, int delta) => throw new NotSupportedException();
    public int UpgradePlayerEnergy(string id, int delta) => throw new NotSupportedException();
    public int UpgradePlayerThrowStrength(string id, int delta) => throw new NotSupportedException();
    public int UpgradePlayerTumbleWings(string id, int delta) => throw new NotSupportedException();
    public int UpgradePlayerTumbleClimb(string id, int delta) => throw new NotSupportedException();
    public int UpgradeDeathHeadBattery(string id, int delta) => throw new NotSupportedException();
}
namespace Photon.Pun
{
    public enum RpcTarget { All, Others }
    public sealed class PhotonView
    {
        public static Dictionary<string, int> GuestLevels = new();
        public static Dictionary<string, float> GuestStrength = new();
        public static int Sends;
        public void RPC(string method, RpcTarget target, params object[] args)
        {
            if (method != nameof(PunManager.TesterUpgradeCommandRPC)) throw new InvalidOperationException(method);
            Sends++;
            string id = (string)args[0], command = (string)args[1];
            int delta = (int)args[2];
            if (target == RpcTarget.All) PunManager.instance.TesterUpgradeCommandRPC(id, command, delta);
            int old = GuestLevels.GetValueOrDefault(id);
            int accepted = Math.Max(0, old + delta) - old;
            GuestLevels[id] = old + accepted;
            GuestStrength[id] = GuestStrength.GetValueOrDefault(id, 1f) + 0.2f * accepted;
        }
    }
}
namespace REPOJP.StageRoles
{
    internal readonly record struct UpgradeGrant(string CommandName, string DictionaryName, int Level);
    internal static class KingUpgradeAura
    {
        internal static int WithoutBonus(string id, string key, int level) => level;
        internal static void Forget(string id, string key) { }
    }
}

namespace REPOJP.StageRoles
{
    internal static class UpgradeItemRetentionPatch { internal static void ApplyPendingThrow(string steamId) { } }
}
