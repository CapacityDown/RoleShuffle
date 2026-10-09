using UnityEngine;
using Photon.Pun;
using REPOJP.StageRoles;
namespace UnityEngine
{
    public class Object
    {
        public static List<Object> World = new();
        private static int _next;
        private readonly int _id = ++_next;
        public Object() { World.Add(this); }
        public int GetInstanceID() => _id;
        public static T[] FindObjectsOfType<T>() => World.OfType<T>().ToArray();
    }
    public class Component : Object
    {
        public Transform transform = new();
        public readonly Dictionary<Type, Component> Components = new();
        public T? GetComponent<T>() where T : class => this as T ?? Components.GetValueOrDefault(typeof(T)) as T;
        public T? GetComponentInChildren<T>() where T : class => GetComponent<T>();
        public T? GetComponentInParent<T>() where T : class => GetComponent<T>();
    }
    public class MonoBehaviour : Component { }
    public class Transform { public Vector3 position; }
    public struct Vector3(float x, float y, float z)
    {
        public float x=x,y=y,z=z;
        public float magnitude => (float)Math.Sqrt(x*x+y*y+z*z);
        public Vector3 normalized => magnitude>0 ? new(x/magnitude,y/magnitude,z/magnitude) : new();
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x-b.x,a.y-b.y,a.z-b.z);
        public static float Distance(Vector3 a,Vector3 b) => (a-b).magnitude;
        public static float Dot(Vector3 a,Vector3 b) => a.x*b.x+a.y*b.y+a.z*b.z;
    }
    public static class Time { public static float time; }
    public static class Mathf { public static int CeilToInt(float value)=>(int)Math.Ceiling(value); }
}
namespace HarmonyLib
{
    public static class AccessTools
    {
        public static System.Reflection.FieldInfo? Field(Type t,string name)=>t.GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
    }
}
namespace Photon.Pun
{
    public enum RpcTarget { All }
    public class PhotonView : Component
    {
        public int ViewID=1;
        public Action<string,object[]>? Dispatch;
        public void RPC(string method,RpcTarget target,params object[] args)=>Dispatch?.Invoke(method,args);
    }
}
public class PlayerAvatar : Component
{
    public string Id="";
    public PlayerHealth playerHealth=new();
    public PhotonView photonView=new();
    public bool isCrouching=false,isSprinting=false,deadSet=false;
    public int Deaths,Revivals;
    public PlayerAvatar()
    {
        playerHealth.Player=this;
        photonView.Dispatch=(m,a)=>
        {
            if(m==nameof(PlayerHealth.UpdateHealthRPC))playerHealth.UpdateHealthRPC((int)a[0],(int)a[1],false,false);
            else if(m==nameof(PlayerDeathRPC))PlayerDeathRPC(-1);
            else throw new Exception(m);
        };
    }
    public void PlayerDeathRPC(int ignored) { Deaths++;deadSet=true;playerHealth.health=0;StageRoleController.Current?.TwinDied(this); }
    public void Revive() { Revivals++;deadSet=false;playerHealth.health=1;StageRoleController.Current?.TwinRevived(this); }
}
public class PlayerHealth : Component
{
    public PlayerAvatar Player=null!;
    public int health=100,maxHealth=120;
    public void UpdateHealthRPC(int current,int maximum,bool effect,bool hurtByHeal)
    {
        int before=health;health=current;maxHealth=maximum;
        StageRoleController.Current?.ObserveTwinsHealth(Player,before,current,maximum);
    }
}
public class ItemAttributes : Component { }
public class ValuableObject : Component
{
    public float dollarValueCurrent=10000;
    public void DollarValueSetRPC(float value)=>dollarValueCurrent=value;
}
public class PhysGrabObject : Component
{
    public Vector3 centerPoint;
    internal HashSet<string> Holders=new();
    public PhysGrabObject()
    {
        var v=new ValuableObject();var p=new PhotonView();p.Dispatch=(m,a)=>v.DollarValueSetRPC((float)a[0]);
        Components[typeof(ValuableObject)]=v;v.Components[typeof(PhotonView)]=p;
        Components[typeof(RoomVolumeCheck)]=new RoomVolumeCheck();
    }
}
public class RoomVolumeCheck : Component
{
    public List<RoomVolume> CurrentRooms=new();
    public void CheckSet(){}
}
public class RoomVolume { public bool Truck=false,Extraction=false; }
public static class SemiFunc { public static bool IsMultiplayer()=>true; }
public class PunManager
{
    public static PunManager instance=new();
    public Dictionary<string,int> Saved=new();
    public void UpdateStat(string key,string id,int health)=>Saved[id]=health;
}
namespace REPOJP.StageRoles
{
    internal class Setting<T>(T value) { public T Value=value; }
    internal sealed partial class StageRolesConfig { }
    internal class RoleAssignment(string id,PlayerAvatar player)
    { public string SteamId=id;public PlayerAvatar Player=player;public StageRole Role=StageRole.Twins; }
    internal static class PlayerIdentity { public static string Name(PlayerAvatar p)=>p.Id; }
    internal static class PlayerState
    {
        public static bool IsLiving(PlayerAvatar p)=>!p.deadSet && p.playerHealth.health>0;
        public static bool TryGetCurrentHealth(PlayerAvatar p,out int h){h=p.playerHealth.health;return true;}
        public static bool TryGetMaximumHealth(PlayerAvatar p,out int h){h=p.playerHealth.maxHealth;return true;}
        public static bool TryRequestDeathHeadRevival(PlayerAvatar p){if(!p.deadSet)return false;p.Revive();return true;}
    }
    internal static class HealthUpgradeReset
    {
        public static int ScaleRemaining(int h,int max,int target)=>h<=0?0:Math.Max(1,(int)((long)Math.Min(h,max)*target/max));
    }
    internal static class UtilityRoleRuntime
    { public static bool IsHeldBy(PhysGrabObject item,RoleAssignment a)=>item.Holders.Contains(a.SteamId); }
    internal static class UpgradeService
    {
        public static bool Ready=true;
        public static readonly Dictionary<string,Dictionary<string,int>> Levels=new();
        public static readonly List<(string Id,string Command,int Amount)> Calls=new();
        public static bool TryGetLevels(string id,out Dictionary<string,int> levels)
        { if(!Levels.TryGetValue(id,out var l))Levels[id]=l=new();levels=new(l);return true; }
        public static bool AddLevelsHostAuthoritative(string id,string command,int amount)
        { TryGetLevels(id,out _);string key=command=="Speed"?"playerUpgradeSpeed":"playerUpgradeStamina";
            Levels[id][key]=Math.Max(0,Levels[id].GetValueOrDefault(key)+amount);Calls.Add((id,command,amount));return true; }
    }
    internal class RoleNotifier
    { public void NotifyConditional(PlayerAvatar p,string text,Func<bool> condition){} public void NotifyResponse(PlayerAvatar p,string text){} }
    internal sealed partial class StageRoleController
    {
        internal static StageRoleController? Current;
        private readonly List<RoleAssignment> _assignments=new();
        private readonly StageRolesConfig _config=new();
        private readonly RoleNotifier _notifier=new();
        internal bool RoleAssignmentsReady=true;
        private static bool IsAuthority()=>true;
        internal StageRoleController() { Current=this;Time.time=0;UnityEngine.Object.World.Clear();UpgradeService.Levels.Clear();UpgradeService.Calls.Clear(); }
        internal void Start(PlayerAvatar a,PlayerAvatar b){_assignments.Add(new(a.Id,a));_assignments.Add(new(b.Id,b));BeginTwins();}
        internal void Tick(float t){Time.time=t;TickTwins();}
        internal void End()=>StopTwins();
        internal void Depart(PlayerAvatar p){_assignments.RemoveAll(a=>a.Player==p);TickTwins();}
        internal void Hit(PlayerAvatar p,int amount)
        { int previous=p.playerHealth.health;p.playerHealth.health=Math.Max(0,previous-amount);ObserveTwinsHealth(p,previous,p.playerHealth.health,p.playerHealth.maxHealth); }
        internal RoleAssignment? Unlink(PlayerAvatar p)=>UnlinkTwinForInfection(_assignments.First(a=>a.Player==p));
        internal int Pool=>_twins?.Health??-1;
        internal float Cooldown=>_twins?.Cooperation.RestReadyAt??-1;
    }
}
