using System.Reflection;
using UnityEngine;
namespace UnityEngine
{
    public class GameObject { public bool activeInHierarchy=true; }
    public class Transform { public Vector3 position,forward=Vector3.forward; }
    public class Component
    {
        private static int next;
        private readonly int id=++next;
        public GameObject gameObject=new(); public Transform transform=new();
        public Dictionary<Type,object> Children=new();
        public int GetInstanceID()=>id;
        public T? GetComponentInChildren<T>() where T:class=>Children.GetValueOrDefault(typeof(T)) as T;
    }
    public struct Vector3(float x,float y,float z)
    {
        public float x=x,y=y,z=z;
        public float sqrMagnitude=>x*x+y*y+z*z;
        public float magnitude=>MathF.Sqrt(sqrMagnitude);
        public static Vector3 forward=>new(0,0,1);
        public static Vector3 up=>new(0,1,0);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
        public static float SignedAngle(Vector3 a,Vector3 b,Vector3 axis)=>MathF.Atan2(a.z*b.x-a.x*b.z,a.x*b.x+a.z*b.z)*180/MathF.PI;
    }
    public static class Time { public static float time; }
}
namespace HarmonyLib { public static class AccessTools { public static FieldInfo? Field(Type type,string name)=>type.GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic); } }
public enum EnemyState { Spawn,Despawn,Stunned,Roaming }
public class Enemy : Component { public EnemyState CurrentState=EnemyState.Roaming; public Transform? CenterTransform; }
public class EnemyHealth : Component { public bool dead=false; }
public class EnemyParent : Component { public bool Spawned=true; }
public class EnemyDirector { public static EnemyDirector instance=new();public List<EnemyParent> enemiesSpawned=new(); }
public class PhotonView { public object? Owner=new(); }
public class PlayerAvatar : Component { public string Name="Player"; public bool Living=true; public Transform? playerTransform;public PhotonView? photonView=new();public PlayerDeathHead? Head; }
public class PlayerDeathHead : Component { }
namespace REPOJP.StageRoles
{
    internal class Entry<T>(T value) { public T Value=value; }
    internal class StageRolesConfig
    {
        public Entry<bool> Enabled=new(true),AnnouncementsEnabled=new(true);
        public Entry<float> TrackerEnemyRange=new(25),TrackerDangerRange=new(8),TrackerNotificationInterval=new(20),TrackerDistanceChange=new(5);
    }
    internal class RoleAssignment(PlayerAvatar player,StageRole role=StageRole.Tracker) { public PlayerAvatar Player=player;public StageRole Role=role; }
    internal static class RoleCatalog { public static bool HasCapability(StageRole role,StageRole capability)=>role==capability||role==StageRole.Superbot; }
    internal static class PlayerState
    {
        public static bool IsLiving(PlayerAvatar p)=>p.Living&&p.gameObject.activeInHierarchy;
        public static bool TryGetDeathHeadRuntime(PlayerAvatar p,out PlayerDeathHead? head,out object? phys,out bool spectated)
        { head=p.Head;phys=null;spectated=false;return head!=null; }
    }
    internal static class PlayerIdentity { public static string Name(PlayerAvatar player)=>player.Name; }
    internal class RoleNotifier
    {
        internal readonly List<(Func<bool> Valid,Func<bool> Send,Action Finish)> Pending=new();
        internal bool Accept=true;
        public bool NotifyPrivate(PlayerAvatar player,string message,Func<bool> valid,Func<bool> dispatch,Action onFinished)
        { if(!Accept)return false;Pending.Add((valid,dispatch,onFinished));return true; }
        internal void Flush() { var pending=Pending.ToArray();Pending.Clear();foreach(var p in pending){if(p.Valid())p.Send();p.Finish();} }
        internal void Expire() { foreach(var p in Pending)p.Finish();Pending.Clear(); }
    }
    internal static class PrivatePlayerSpeech
    {
        internal static readonly List<(PlayerAvatar Receiver,string Text)> Sent=new();
        internal static bool Succeeds=true;
        public static bool Send(PlayerAvatar player,string message) { if(!Succeeds)return false;Sent.Add((player,message));return true; }
    }
}
