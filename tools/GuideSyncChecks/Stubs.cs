using System.Reflection;
using ExitGames.Client.Photon;

internal static class NativeProtocol
{
    private static readonly object Protocol = Activator.CreateInstance(typeof(Hashtable).Assembly.GetType("ExitGames.Client.Photon.Protocol18")!)!;
    private static readonly MethodInfo Serialize = Protocol.GetType().GetMethod("Serialize", new[] { typeof(object) })!;
    private static readonly MethodInfo Deserialize = Protocol.GetType().GetMethod("Deserialize", new[] { typeof(byte[]) })!;
    internal static object RoundTrip(object value)
    {
        byte[] bytes = (byte[])Serialize.Invoke(Protocol, new[] { value })!;
        return Deserialize.Invoke(Protocol, new object[] { bytes })!;
    }
}
internal static class SemiFunc { internal static bool Multiplayer = true; internal static bool IsMultiplayer() => Multiplayer; }
namespace Photon.Pun
{
    internal static class PhotonNetwork
    {
        internal static bool IsMasterClient = true;
        internal static Room? CurrentRoom;
    }
    internal sealed class Room
    {
        internal Hashtable CustomProperties = new();
        internal Hashtable LastSent = new();
        internal int Sends;
        internal bool FailSend;
        internal void SetCustomProperties(Hashtable values)
        {
            Sends++;
            if (FailSend) throw new IOException("Injected connection failure");
            LastSent = (Hashtable)NativeProtocol.RoundTrip(values);
            foreach (var pair in LastSent) CustomProperties[pair.Key] = pair.Value;
        }
    }
}
namespace REPOJP.StageRoles
{
    internal static class RoleCatalog { internal static IReadOnlyList<StageRole> AllRoles { get; } = Enum.GetValues<StageRole>(); }
    internal sealed class TestLogger { internal readonly List<string> Warnings = new(); internal void LogWarning(object value) => Warnings.Add(value.ToString()!); }
    internal static class StageRolesPlugin { internal static TestLogger ModLogger = new(); }
}
