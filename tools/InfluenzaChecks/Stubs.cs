#pragma warning disable CS0649
using System.Reflection;
using REPOJP.StageRoles;
using UnityEngine;

namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)] public sealed class HarmonyPatch : Attribute
    { public HarmonyPatch() {} public HarmonyPatch(Type type, string method) {} }
    public sealed class HarmonyPrefix : Attribute {}
    public sealed class HarmonyPostfix : Attribute {}
    public sealed class HarmonyFinalizer : Attribute {}
    public static class AccessTools
    { public static FieldInfo? Field(Type type, string name) => type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); }
}
namespace UnityEngine
{
    public static class Time { public static float time; }
    public static class Random { public static float value = 0.1f; }
    public sealed class Transform { public Vector3 position; public Vector3 forward = new(0, 0, 1); }
    public struct Vector3(float x, float y, float z)
    {
        public float x = x, y = y, z = z;
        public float sqrMagnitude => x*x+y*y+z*z;
        public static Vector3 up => new(0,1,0);
        public Vector3 normalized { get { var v=this;v.Normalize();return v; } }
        public void Normalize() { float n=MathF.Sqrt(sqrMagnitude);x/=n;y/=n;z/=n; }
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x+b.x,a.y+b.y,a.z+b.z);
        public static float Dot(Vector3 a, Vector3 b) => a.x*b.x+a.y*b.y+a.z*b.z;
    }
}
namespace Photon.Pun
{
    public sealed class NetworkPlayer {}
    public sealed class PhotonView { public NetworkPlayer Owner = new(); }
    public struct PhotonMessageInfo { public NetworkPlayer? Sender; }
    public static class PhotonNetwork { public static NetworkPlayer LocalPlayer = new(); }
}
public static class SemiFunc { public static bool Multiplayer = true; public static bool IsMultiplayer() => Multiplayer; }
public sealed class PlayerVoiceChat { public float clipLoudnessNoTTS; }
public sealed class Vision { public Transform VisionTransform = new(); }
public sealed class PlayerAvatar
{
    public string Id = "";
    public bool Living = true;
    public int Health=500, Maximum=500;
    public Transform transform = new();
    public Transform? playerTransform;
    public Vision? PlayerVisionTarget;
    public Photon.Pun.PhotonView photonView = new();
    public PlayerVoiceChat voiceChat = new();
    public List<string> Spoken = new();
    public void ChatMessageSend(string text) => Spoken.Add(text);
    public void ChatMessageSendRPC() {}
}
public sealed class ChatManager {}
namespace REPOJP.StageRoles
{
    internal sealed class Entry<T>(T value) { internal T Value = value; }
    internal sealed class Config { internal Entry<bool> Enabled = new(true); }
    internal sealed class RoleAssignment(string id, StageRole role, float x, float z)
    {
        internal string SteamId = id;
        internal PlayerAvatar Player = new() { Id=id, transform = new Transform { position=new(x,0,z) } };
        internal StageRole Role=role, AssignedRole=role;
    }
    internal static class PlayerState
    {
        internal static int SyncCount;
        internal static bool IsLiving(PlayerAvatar p) => p.Living;
        internal static bool TryGetMaximumHealth(PlayerAvatar p, out int maximum) { maximum=p.Maximum;return true; }
        internal static bool TryGetCurrentHealth(PlayerAvatar p, out int health) { health=p.Health;return true; }
        internal static bool SetMaximumHealthSynchronized(PlayerAvatar p, int maximum)
        { SyncCount++;p.Maximum=maximum;p.Health=Math.Clamp(p.Health,0,maximum);return true; }
    }
    internal static class UpgradeService
    {
        internal static readonly Dictionary<string, int> HealthLevels = new();
        internal static readonly List<string> Resets = new();
        internal static bool TryGetLevels(string id, out Dictionary<string,int> levels)
        { levels=new(){{"playerUpgradeHealth",HealthLevels.GetValueOrDefault(id,20)}};return true; }
        internal static void SetLevels(string id, int ignored) => Resets.Add(id);
    }
    internal static class RoleCatalog
    {
        // Capability membership is separately checked against production RoleModels
        // by Test-SecretRoles.ps1; this harness exercises the runtime consumers.
        internal static bool HasCapability(StageRole role, StageRole capability) =>
            role == capability || (role == StageRole.Disaster && capability == StageRole.Influenza);
        internal static int TargetUpgrades(StageRole role, Config config) => 0;
    }
    internal sealed class Runtime
    {
        internal readonly List<string> Removed = new();
        internal void RemovePlayer(string id) => Removed.Add(id);
    }
    internal static class KingUpgradeAura { internal static void Tick(Config config, IReadOnlyList<RoleAssignment> assignments) {} }
    internal static class RoleAssignmentSync { internal static int Publishes;internal static void Publish(object assignments) => Publishes++; }
    internal sealed class Notifier { internal void NotifyConditional(PlayerAvatar player, string text, Func<bool> valid) { if(valid()) player.Spoken.Add(text); } }
    internal sealed class StageRolesPlugin
    {
        internal static StageRolesPlugin? Instance;
        internal StageRoleController? Controller;
        internal static Logger ModLogger = new();
    }
    internal sealed class Logger { internal void LogInfo(string text) {} internal void LogDebug(string text) {} }
    internal sealed partial class StageRoleController
    {
        private readonly List<RoleAssignment> _assignments = new();
        private readonly Dictionary<string, RoleAssignment> _departedAssignments = new();
        private bool _stageReady=true, _assignmentsInitialized=true;
        private readonly Config _config = new();
        private readonly Runtime _bomber=new(), _medic=new(), _stinker=new(), _trickster=new(), _eventRoles=new(), _diver=new();
        private float _nextAbilityPublishAt;
        private readonly Notifier _notifier=new();
        private static bool IsAuthority() => true;
        private RoleAssignment? FindAssignment(PlayerAvatar player) => _assignments.Find(a=>a.Player==player);
        private void ResetAssignmentForRoleChange(RoleAssignment a) {}
        private void RestoreKingCrown() {}
        internal void Add(params RoleAssignment[] players) { _assignments.AddRange(players);foreach(var a in players) StartInfluenza(a); }
        internal void Tick(float now) { Time.time=now;TickInfluenza(); }
        internal void Change(RoleAssignment a, StageRole role) { EndInfluenza(a);a.Role=a.AssignedRole=role; }
        internal void Stop() { _stageReady=false;StopInfluenza(); }
        internal bool HasInfection(string id) => _influenza.ContainsKey(id);
        internal float Onset(string id) => _influenza[id].OnsetAt;
        internal void Depart(RoleAssignment a) { _assignments.Remove(a);_departedAssignments[a.SteamId]=a; }
        internal void Rejoin(RoleAssignment a) { _departedAssignments.Remove(a.SteamId);Add(a); }
        internal bool OnlyCleaned(string id) => new[]{_bomber,_medic,_stinker,_trickster,_eventRoles,_diver}.All(r=>r.Removed.SequenceEqual(new[]{id})) && _nextAbilityPublishAt==0;
    }
}
