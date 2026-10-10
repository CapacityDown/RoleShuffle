using System.Reflection;
using UnityEngine;

namespace UnityEngine
{
    public class Object
    {
        public static readonly List<Object> World = new();
        public static T[] FindObjectsOfType<T>() => World.OfType<T>().ToArray();
    }
    public class Component : Object
    {
        static int next;
        readonly int id = ++next;
        public readonly Transform transform = new();
        public readonly Dictionary<Type, Component> Components = new();
        public T? GetComponent<T>() where T : Component => this as T ?? Components.Values.OfType<T>().FirstOrDefault();
        public T[] GetComponentsInChildren<T>(bool inactive) => Components.Values.OfType<T>().ToArray();
        public int GetInstanceID() => id;
    }
    public class Behaviour : Component { public bool enabled = true; }
    public class MonoBehaviour : Behaviour { }
    public class Transform { public Vector3 position; public Quaternion rotation; }
    public struct Quaternion { public static Quaternion identity => new(); }
    public struct Vector3(float x, float y, float z)
    {
        public float x=x, y=y, z=z;
        public static Vector3 up => new(0,1,0);
        public static Vector3 down => new(0,-1,0);
        public static Vector3 zero => new(0,0,0);
        public float magnitude => MathF.Sqrt(sqrMagnitude);
        public float sqrMagnitude => x*x+y*y+z*z;
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
        public Vector3 normalized => magnitude > 0 ? this * (1/magnitude) : zero;
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator *(Vector3 a,float b)=>new(a.x*b,a.y*b,a.z*b);
    }
    public class Rigidbody { public bool isKinematic; public bool detectCollisions=true; public Vector3 velocity,angularVelocity; }
    public enum QueryTriggerInteraction { Ignore }
    public struct RaycastHit { public float distance; }
    public static class LayerMask { public static int GetMask(params string[] names)=>1; }
    public static class Physics
    {
        public static bool Raycast(Vector3 a,Vector3 b,float c,int d,QueryTriggerInteraction e)=>true;
        public static bool Raycast(Vector3 a,Vector3 b,out RaycastHit hit,float c,int d,QueryTriggerInteraction e){hit=new();return false;}
    }
    public static class Time { public static float time; }
    public static class Mathf { public static float Sin(float x)=>MathF.Sin(x);public static float Cos(float x)=>MathF.Cos(x); }
}
namespace HarmonyLib
{
    public static class AccessTools { public static FieldInfo? Field(Type t,string name)=>t.GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic); }
}
namespace Photon.Pun { public class PhotonView : Component { public int ViewID=42; } }
public class ValuableVolume { public enum Type { Tiny,Small,Medium,Big,Wide,Tall,VeryTall,Unknown } }
public class ValuableObject : MonoBehaviour { public float dollarValueCurrent=100; public ValuableVolume.Type volumeType; }
public class ValuableTestEffect : MonoBehaviour { }
public class ItemAttributes : Component { }
public class Trap : MonoBehaviour { }
public class HurtCollider : MonoBehaviour { }
public class View { public int ViewID=42; }
public class PhysGrabObject : Component
{
    public readonly Rigidbody rb=new();
    public readonly List<PhysGrabber> playerGrabbing=new();
    public readonly View photonView=new();
    public float massOriginal=2;
    public bool FailStore,FailRestore,Stored;
    public float timerAlterDeactivate=-123;
    public bool isActive=true;
    public int Teleports,Restores,ParkingTeleports,DeactivateCalls;
    public void OverrideIndestructible(float x) { }
    public void OverrideGrabDisable(float x) { }
    public void DisableDeathPitEffect(float x) { }
    public void OverrideBreakEffects(float x) { }
    public void OverrideDeactivate(float x){if(FailStore)throw new Exception("store failure");DeactivateCalls++;Stored=true;timerAlterDeactivate=x;rb.isKinematic=true;transform.position=new(0,3000,0);}
    public void NativeTimersTick(float elapsed)
    {
        if(timerAlterDeactivate>0)
        {
            if(isActive)transform.position=new(0,3000,0);
            isActive=false;rb.isKinematic=true;rb.detectCollisions=false;timerAlterDeactivate-=elapsed;
        }
        else if(timerAlterDeactivate!=-123)OverrideDeactivateReset();
    }
    public void OverrideDeactivateReset(){if(FailRestore)throw new Exception("restore failure");Restores++;Stored=false;isActive=true;timerAlterDeactivate=-123;rb.detectCollisions=true;rb.isKinematic=false;}
    public void Teleport(Vector3 p,Quaternion r){transform.position=p;transform.rotation=r;if(p.y>2000)ParkingTeleports++;else Teleports++;}
}
public class PhysGrabber
{
    public int Releases;
    public void OverrideGrabRelease(int view,float seconds)
    {
        Releases++;
        foreach(var item in UnityEngine.Object.FindObjectsOfType<PhysGrabObject>()) item.playerGrabbing.Remove(this);
    }
}
public class RoomVolume : Component { public bool Truck,Extraction; }
public class RoomVolumeCheck : Component { public readonly List<RoomVolume> CurrentRooms=new(); public void CheckSet() { } }
public class PlayerAvatar : Component
{
    public string Id=Guid.NewGuid().ToString();
    public bool Alive=true;
    public readonly PhysGrabber physGrabber=new();
    public readonly RoomVolumeCheck RoomVolumeCheck=new();
}
namespace REPOJP.StageRoles
{
    internal sealed class Setting<T>(T value) { public T Value=value; }
    internal sealed class StageRolesConfig
    {
        public Setting<float> PorterCapacity=new(15),PorterHoldSeconds=new(3),PorterUnloadSeconds=new(10);
        public Setting<string> PorterAllowedSizes=new("Tiny,Small,Medium");
        public Setting<bool> Enabled=new(true),PorterDropOnEnemyHit=new(true);
    }
    internal sealed class RoleAssignment(PlayerAvatar p)
    { public PlayerAvatar Player=p; public StageRole Role=StageRole.Porter; public string SteamId=>Player.Id; }
    internal sealed class RoleNotifier { public readonly List<string> Messages=new();public void NotifyResponse(PlayerAvatar p,string text)=>Messages.Add(text); }
    internal static class PlayerState { public static bool IsLiving(PlayerAvatar p)=>p!=null&&p.Alive; }
    internal static class PlayerIdentity { public static string SteamId(PlayerAvatar p)=>p.Id; }
    internal sealed class TestLog { public readonly List<string> Messages=new();public void LogWarning(string x)=>Messages.Add(x); }
    internal static class StageRolesPlugin { public static readonly TestLog ModLogger=new(); }
    internal sealed partial class StageRoleController
    { private bool _stageReady=true,_assignmentsInitialized=true;private readonly PorterRuntime _porter=new(new());private readonly RoleNotifier _notifier=new();private bool IsAuthority()=>true; }
}

namespace REPOJP.StageRoles
{
    internal static class UpgradeService
    {
        internal static readonly Dictionary<string,int> Speeds = new();
        internal static bool TryGetLevels(string id,out Dictionary<string,int> levels)
        { levels=new(){{"playerUpgradeSpeed",Speeds.GetValueOrDefault(id,10)}};return true; }
        internal static bool AddLevelsHostAuthoritativeRaw(string id,string command,int delta)
        { Speeds[id]=Math.Max(0,Speeds.GetValueOrDefault(id,10)+delta);return true; }
    }
}
