namespace UnityEngine
{
    public class MonoBehaviour { }
    public static class Time { public static float unscaledTime; }
    public static class Application { public static string version = "1.2.3.4", unityVersion = "2022.3.62f1"; }
    public static class SystemInfo { public static string operatingSystem = "Test OS"; }
}
namespace UnityEngine.SceneManagement
{
    public readonly record struct Scene(string name);
    public static class SceneManager { public static Scene GetActiveScene() => new("TestScene"); }
}
namespace ExitGames.Client.Photon
{
    public class Hashtable : Dictionary<string, object>
    {
        public new bool TryGetValue(string key, out object value) => base.TryGetValue(key, out value!);
    }
}
namespace Photon.Pun
{
    using ExitGames.Client.Photon;
    public class Player
    {
        public int ActorNumber;
        public string NickName = "", UserId = "";
        public Hashtable CustomProperties = new();
        public void SetCustomProperties(Hashtable data) { foreach (var item in data) CustomProperties[item.Key] = item.Value; }
    }
    public class Room
    {
        public string Name = "private-room-name";
        public Hashtable CustomProperties = new(), LastSent = new();
        public int Sends;
        public void SetCustomProperties(Hashtable data)
        { Sends++; LastSent = data; foreach (var item in data) CustomProperties[item.Key] = item.Value; }
    }
    public static class PhotonNetwork
    {
        public static Room? CurrentRoom;
        public static Player LocalPlayer = new() { ActorNumber = 1 };
        public static Player? MasterClient = LocalPlayer;
        public static Player[] PlayerList = { LocalPlayer };
        public static bool IsMasterClient = true;
        public static int ServerTimestamp = 1000;
    }
}
namespace BepInEx
{
    public static class Paths { public static string BepInExRootPath = Path.Combine(AppContext.BaseDirectory, "reports-test-" + Guid.NewGuid().ToString("N")); }
    public class PluginInfo { public Metadata Metadata = new(); }
    public class Metadata { public string GUID = "Test.Mod"; public Version Version = new(10, 2, 3, 4); }
}
namespace BepInEx.Bootstrap
{
    public static class Chainloader { public static Dictionary<string, BepInEx.PluginInfo> PluginInfos = new() { ["test"] = new() }; }
}
namespace BepInEx.Configuration
{
    public readonly record struct ConfigDefinition(string Section, string Key);
    public class ConfigEntryBase(string value) { public string GetSerializedValue() => value; }
    public class ConfigFile : Dictionary<ConfigDefinition, ConfigEntryBase> { }
}
namespace BepInEx.Logging
{
    [Flags] public enum LogLevel { Fatal = 1, Error = 2, Warning = 4, Info = 8 }
    public class LogSource { public string SourceName = "RoleShuffle"; }
    public class LogEventArgs : EventArgs { public LogSource Source = new(); public LogLevel Level = LogLevel.Error; public object Data = ""; }
    public interface ILogListener : IDisposable { void LogEvent(object sender, LogEventArgs args); }
}
public class StatsManager { public static StatsManager? instance = new(); public Dictionary<string, int> runStats = new(); }
public class RunManager { public static RunManager? instance = new(); public int levelsCompleted = 2; }
public class GameManager { public static GameManager? instance = new(); }
public class PlayerAvatar { public string Name = "Private Player", Id = "76561198000000001"; }
public class GameDirector { public static GameDirector? instance = new(); public List<PlayerAvatar> PlayerList = new() { new() }; }
namespace REPOJP.StageRoles
{
    internal class StageRolesConfig { }
    internal static class StageRolesPlugin
    { internal const string PluginName = "RoleShuffle", PluginGuid = "REPOJP.RoleShuffle", PluginVersion = "4.4.6"; }
    internal static class RoleMenu { internal const int UiBuildNumber = 404; }
    internal readonly record struct UpgradeGrant(string CommandName, int Level);
    internal static class RoleCatalog
    {
        internal static IReadOnlyList<UpgradeGrant> BaseUpgrades(StageRolesConfig config) =>
            DrawHistoryStore.UpgradeNames.Select((name, i) => new UpgradeGrant(name, i)).ToArray();
    }
    internal static class PlayerIdentity
    { internal static string Name(PlayerAvatar player) => player.Name; internal static string SteamId(PlayerAvatar player) => player.Id; }
    internal static class RoleGuideSync
    {
        internal static string CurrentSignature(StageRolesConfig config) => "guide";
        internal static string VisibilitySignature(StageRolesConfig config) => "visible";
        internal static void Invalidate() { }
    }
    internal static class BaseUpgradeSync { internal static string LocalSignature(StageRolesConfig config) => "base"; }
    internal static class RoleAssignmentSync { internal const string LocalPayload = ""; internal static void ResetCache() { } }
}
