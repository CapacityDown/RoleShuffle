// Minimal stand-ins for Photon/Unity. Tests compile the production sync sources.
using System.Text;

namespace ExitGames.Client.Photon
{
    public sealed class Hashtable : Dictionary<object, object?> { }
}

namespace Photon.Pun
{
    public static class PhotonNetwork
    {
        public static bool IsMasterClient { get; set; }
        public static TestRoom? CurrentRoom { get; set; }
    }
    public sealed class TestRoom
    {
        public TestProperties CustomProperties { get; } = new();
        public int Writes { get; private set; }
        public void SetCustomProperties(ExitGames.Client.Photon.Hashtable values)
        {
            Writes++;
            foreach (var pair in values)
            {
                string key = (string)pair.Key;
                if (pair.Value == null) CustomProperties.Remove(key);
                else CustomProperties[key] = pair.Value;
            }
        }
    }
    public sealed class TestProperties : Dictionary<string, object>
    {
        public new bool TryGetValue(string key, out object value) => base.TryGetValue(key, out value!);
    }
}

public sealed class PlayerAvatar
{
    public string Id { get; set; } = "local";
    public string Name { get; set; } = "Local Player";
}
public static class SemiFunc
{
    public static bool Multiplayer { get; set; }
    public static PlayerAvatar? Local { get; set; }
    public static bool IsMultiplayer() => Multiplayer;
    public static PlayerAvatar? PlayerGetLocal() => Local;
}

namespace REPOJP.StageRoles
{
    internal enum StageRole { Tank = 1, Runner = 2, Imitator = 38, Superbot = 1001, Disaster = 1002 }
    internal sealed class StageRolesConfig
    {
        internal const int MaximumSupportedPlayers = 30;
        internal int Value { get; set; } = 21;
        internal HashSet<StageRole> DisabledRoles { get; } = new();
        internal bool RoleIsEnabled(StageRole role) => !DisabledRoles.Contains(role);
    }
    internal sealed class RoleAssignment
    {
        internal string SteamId { get; set; } = "local";
        internal PlayerAvatar Player { get; set; } = new();
        internal StageRole AssignedRole { get; set; }
        internal StageRole Role { get; set; }
    }
    internal static class PlayerIdentity
    {
        internal static string SteamId(PlayerAvatar? player) => player?.Id ?? "";
        internal static string Name(PlayerAvatar? player) => player?.Name ?? "";
    }
    internal static class RoleCatalog
    {
        internal static IReadOnlyList<StageRole> AllRoles { get; } = Enum.GetValues<StageRole>();
    }
    internal static class RoleGuideCatalog
    {
        internal static int Calls;
        internal static string Description(StageRole role, StageRolesConfig config, RoleGuideLanguage language)
        {
            Calls++;
            return Expected(role, config.Value, language);
        }
        internal static string Expected(StageRole role, int value, RoleGuideLanguage language) =>
            $"{role}:{value}:{language}:説明; text";
        internal static string GenericDescription(StageRole role, RoleGuideLanguage language) =>
            $"generic:{role}:{language}";
    }
    internal static class StageRolesPlugin
    {
        internal static TestLogger ModLogger { get; } = new();
    }
    internal sealed class TestLogger
    {
        internal int Warnings;
        internal void LogWarning(string message) => Warnings++;
    }
}
