using System.Reflection;
namespace UnityEngine {
 public class Component {
  private static int next; private readonly int id=++next;
  public List<Component> Family = new();
  public T? GetComponent<T>() where T:Component => Family.OfType<T>().FirstOrDefault();
  public T? GetComponentInParent<T>() where T:Component => GetComponent<T>();
  public T? GetComponentInChildren<T>() where T:Component => GetComponent<T>();
  public int GetInstanceID() => id;
 }
 public static class Mathf { public static int FloorToInt(float x)=>(int)Math.Floor(x); public static int Max(int a,int b)=>Math.Max(a,b); public static float Max(float a,float b)=>Math.Max(a,b); public static float Clamp(float x,float a,float b)=>Math.Clamp(x,a,b); }
}
namespace HarmonyLib {
 [AttributeUsage(AttributeTargets.Class|AttributeTargets.Method,AllowMultiple=true)] public class HarmonyPatch:Attribute { public Type? Type; public string? Name; public HarmonyPatch(){} public HarmonyPatch(Type type,string name){Type=type;Name=name;} }
 [AttributeUsage(AttributeTargets.Method)] public class HarmonyPrefix:Attribute{}
 [AttributeUsage(AttributeTargets.Method)] public class HarmonyPostfix:Attribute{}
 public static class AccessTools { public static FieldInfo? Field(Type t,string n)=>t.GetField(n); }
}
public class PlayerAvatar { public bool Hunter; }
public class PhysGrabObject:UnityEngine.Component { public PlayerAvatar? lastPlayerGrabbing; }
public class ItemEquippable:UnityEngine.Component { public PlayerAvatar? Owner; public PlayerAvatar? GetOwnerPlayerAvatar()=>Owner; }
public class ItemBattery:UnityEngine.Component { public float batteryLife=100; public void RemoveFullBar(){} }
public class ItemGun:UnityEngine.Component { public bool batteryDrainFullBar; public void ShootRPC(){} }
public class ItemMelee:UnityEngine.Component { public void EnemyOrPVPSwingHitRPC(){} public void SwingHitRPC(){} }
public class ItemLeafBlower:UnityEngine.Component{}
public class ItemCartCannon:UnityEngine.Component{}
public class ItemCartLaser:UnityEngine.Component{}
public class ItemStaffTorque:UnityEngine.Component{}
public class ItemStaffVoid:UnityEngine.Component{}
public class ItemStaffZeroGravity:UnityEngine.Component{}
public static class SemiFunc { public static bool Authority=true; public static bool IsMasterClientOrSingleplayer()=>Authority; }
namespace REPOJP.StageRoles {
 internal class StageRoleController { internal bool PlayerHasRole(PlayerAvatar p,StageRole role)=>p.Hunter; }
 internal class StageRolesConfig { internal float ClampedHunterBatteryConsumption=0.75f; }
 internal class StageRolesPlugin { internal static StageRolesPlugin Instance=new(); internal StageRoleController Controller=new(); internal StageRolesConfig Settings=new(); internal static Logger ModLogger=new(); }
 internal class Logger { internal void LogWarning(string text)=>throw new Exception(text); }
}
