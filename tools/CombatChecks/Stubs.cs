using System.Reflection;
using UnityEngine;

namespace UnityEngine
{
    public class Component
    {
        private static int _id;
        private readonly int _instance = ++_id;
        public Transform transform = new();
        public Dictionary<Type, object> Parts = new();
        public T? GetComponent<T>() where T : class => Parts.GetValueOrDefault(typeof(T)) as T;
        public T? GetComponentInParent<T>() where T : class => GetComponent<T>();
        public int GetInstanceID() => _instance;
    }
    public class Transform { public Vector3 position; }
    public struct Vector3(float x, float y = 0, float z = 0)
    {
        public float x=x,y=y,z=z;
        public float sqrMagnitude => x*x+y*y+z*z;
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x-b.x,a.y-b.y,a.z-b.z);
        public static float Distance(Vector3 a, Vector3 b) => MathF.Sqrt((a-b).sqrMagnitude);
    }
    public static class Time { public static float time, unscaledTime; public static int frameCount; }
    public static class Mathf
    {
        public static float Clamp(float x,float min,float max)=>Math.Clamp(x,min,max);
        public static int Clamp(int x,int min,int max)=>Math.Clamp(x,min,max);
        public static float Clamp01(float x)=>Clamp(x,0,1);
        public static int RoundToInt(float x)=>(int)MathF.Round(x);
        public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Lerp(float a,float b,float t)=>a+(b-a)*t;
    }
}
namespace Photon.Pun { public class PhotonView { public bool IsMine=true; } }
namespace HarmonyLib { public static class AccessTools { public static FieldInfo? Field(Type t,string name)=>t.GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic); } }
public class PlayerAvatar : Component
{
    public string Id=Guid.NewGuid().ToString(); public bool Alive=true; public int Health=50, Maximum=100;
    public Vector3? Head=new Vector3(0); public PlayerHealth playerHealth; public Photon.Pun.PhotonView photonView=new();
    public HashSet<PlayerAvatar> HeldHeads=new();
    public PlayerAvatar() { playerHealth=new(this); }
}
public class PlayerHealth(PlayerAvatar p)
{
    public bool IgnoreHeal;
    public void HealOther(int amount,bool effect) { if(!IgnoreHeal && (!SemiFunc.Multi || p.photonView.IsMine))p.Health=Math.Min(p.Maximum,p.Health+amount); }
}
public class Enemy : Component { public Enemy(int health=100) { Parts[typeof(EnemyHealth)]=new EnemyHealth { healthCurrent=health }; } }
public class EnemyHealth { public int healthCurrent; }
public class HurtCollider : Component { public int enemyDamage=100,playerDamage=100; }
public class ItemMelee; public class ItemGunBullet; public class ItemGun; public class SlowProjectile; public class SemiLaser; public class PlayerTumble; public class ItemVehicle;
public static class SemiFunc { public static bool Multi, Authority=true; public static bool IsMultiplayer()=>Multi; public static bool IsMasterClientOrSingleplayer()=>Authority; }
namespace REPOJP.StageRoles
{
    internal sealed class Entry<T>(T value) { internal T Value=value; }
    internal static class StageRolesPlugin { internal static Logger ModLogger=new(); }
    internal class Logger { internal void LogDebug(string text) {} }
    internal static class PlayerState
    {
        internal static bool IsLiving(PlayerAvatar? p)=>p?.Alive==true;
        internal static bool TryGetCurrentHealth(PlayerAvatar p,out int hp) { hp=p.Health;return true; }
        internal static bool TryGetMaximumHealth(PlayerAvatar p,out int hp) { hp=p.Maximum;return true; }
        internal static bool TryGetActiveDeathHeadPosition(PlayerAvatar p,out Vector3 pos) {pos=p.Head??default;return !p.Alive && p.Head.HasValue;}
        internal static bool IsGrabbingDeathHead(PlayerAvatar p,PlayerAvatar ghost)=>p.HeldHeads.Contains(ghost);
    }
    internal static class PlayerIdentity { internal static string SteamId(PlayerAvatar? p)=>p?.Id??""; }
    internal static class RoleCatalog { internal static bool HasCapability(StageRole actual,StageRole role)=>actual==role; }
    internal sealed class RoleAssignment(PlayerAvatar player,StageRole role)
    {
        internal PlayerAvatar Player=player; internal StageRole Role=role; internal string SteamId=player.Id;
        internal BrawlerCombo BrawlerCombo=new(); internal float AvengerEmpoweredUntil,AvengerHitUntil,AvengerNextHitAt,GhostNextHealAt;
        internal int GhostHealingUsed; internal AuraState Overhaul=new();
    }
    internal sealed class AuraState { internal int KingSupportedAllies; }
    internal sealed class RoleNotifier { internal int Notices; internal void NotifyConditional(PlayerAvatar p,string msg,Func<bool> valid) {if(valid())Notices++;} }
    internal static class RoleOverhaulRules { internal static double EffectiveGrabStrength(int level,bool light,bool rotation)=>level; }
    internal static class UpgradeService
    {
        internal static bool Ready=true; internal static Dictionary<string,Dictionary<string,int>> Levels=new();
        internal static bool TryGetLevels(string id,out Dictionary<string,int> levels)=>Levels.TryGetValue(id,out levels!);
        internal static void AddLevelsHostAuthoritative(string id,string command,int delta) {var d=Levels[id];string k="playerUpgrade"+command;d[k]=d.GetValueOrDefault(k)+delta;}
    }
    internal sealed partial class StageRoleController
    {
        internal bool _stageReady=true; internal StageRolesConfig _config=new(); internal List<RoleAssignment> _assignments=new();
        internal RoleNotifier _notifier=new(); internal int Credits;
        private bool IsAuthority()=>SemiFunc.Authority;
        internal RoleAssignment? FindAssignment(PlayerAvatar player)=>_assignments.Find(a=>ReferenceEquals(a.Player,player));
        private bool PlayerHasRole(PlayerAvatar player,StageRole role)=>FindAssignment(player)?.Role==role;
        private void RecordEnemyAttacker(Enemy enemy,PlayerAvatar? player) {Credits++;}
        internal void GhostTick()=>TickGhostSupport();
    }
}
