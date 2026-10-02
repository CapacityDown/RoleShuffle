"""Prepare deterministic audit probes from unmodified production method bodies.

Run this script, then: dotnet run --project tmp/role-safety-probe/Probe.csproj -c Release
These are diagnostic scenarios, not a live Unity/Photon test or pass/fail regression suite.
"""

from pathlib import Path
import hashlib
import json

repo = Path(__file__).resolve().parents[1]
out = repo / 'tmp' / 'role-safety-probe'
out.mkdir(parents=True, exist_ok=True)

def slice_source(file, start, end):
    text = (repo / file).read_text(encoding='utf-8-sig')
    result = text[text.index(start):text.index(end, text.index(start))]
    slices.append({'source': file, 'method': start.strip(), 'sha256': hashlib.sha256(result.encode()).hexdigest()})
    return result

slices = []
tuna = slice_source('StageRoleController.cs', '    private void TickTuna(', '    private void TickPhoenix(')
cast = slice_source('MageRoleRuntime.cs', '    internal bool TryCast(', '    internal void Stop(')
cost = slice_source('MageRoleRuntime.cs', '    private int HealthCost(', '    private delegate bool TryResolveProjectile')
transfer = slice_source('EventRoleRuntime.cs', '    private void ApplyBodyguardTransfer(', '    private void TickPendingRevivals(')
send_damage = slice_source('EventRoleRuntime.cs', '    private bool SendInternalDamage(', '    private static IEnumerable<string> DynamicDictionaryNames(')
config = '''
public class Entry<T>(T value) { public T Value = value; }
public class Config {
 public Entry<float> TunaStationaryDelaySeconds = new(3), BodyguardDamageSharePercent = new(50);
 public Entry<int> TunaDamage = new(1), MageStarHealthCost = new(10), MageGravityHealthCost = new(10), MageRollHealthCost = new(15), MageVoidHealthCost = new(30), MageLaserHealthCost = new(50);
 public float ClampedTunaDamageInterval => 0.1f;
}
'''
stubs = '''
using System;
using System.Collections.Generic;
using System.Text.Json;
using REPOJP.StageRoles;

public record struct Vector3(float x, float y, float z) {
 public static Vector3 zero => new(0,0,0);
 public float sqrMagnitude => x*x+y*y+z*z;
 public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x-b.x,a.y-b.y,a.z-b.z);
}
public static class Time { public static float time, deltaTime; }
public static class Mathf {
 public static int Clamp(int x,int a,int b)=>Math.Clamp(x,a,b);
 public static float Clamp(float x,float a,float b)=>Math.Clamp(x,a,b);
 public static int Min(int a,int b)=>Math.Min(a,b);
 public static int CeilToInt(float x)=>(int)Math.Ceiling(x);
}
public class Transform { public Vector3 position; }
public class View { public bool IsMine=true; }
public class PlayerAvatar {
 public string Id=Guid.NewGuid().ToString();
 public Transform transform=new(); public View photonView=new();
 public PlayerHealth playerHealth=new();
}
public class PlayerHealth {
 public int Health=120, Actual=120;
 public bool Defer;
 public int DamageCalls;
 public List<int> Queue=new();
 public void HurtOther(int amount,Vector3 position,bool grace) {
  DamageCalls++; if(Defer) Queue.Add(amount); else Health=Actual=Math.Max(0,Actual-amount);
 }
 public void HealOther(int amount,bool effect) { Actual+=amount; if(!Defer) Health=Actual; }
 public void Flush() { foreach(var amount in Queue) Actual=Math.Max(0,Actual-amount); Queue.Clear(); Health=Actual; }
}
public static class PlayerState {
 public static bool IsLiving(PlayerAvatar p)=>p.playerHealth.Health>0;
 public static bool IsInTruck(PlayerAvatar p)=>false;
 public static bool TryGetCurrentHealth(PlayerAvatar p,out int h) { h=p.playerHealth.Health; return true; }
}
public static class PlayerIdentity { public static string SteamId(PlayerAvatar p)=>p.Id; }
public static class SemiFunc { public static bool Multiplayer; public static bool IsMultiplayer()=>Multiplayer; }
public class RoleAssignment {
 public PlayerAvatar Player=new(); public string SteamId=>Player.Id;
 public Vector3 TunaPreviousPosition;
 public float TunaStationaryTimer,TunaDamageTimer,MageNextCastAt;
 public bool TunaWasAlive=true;
 public float TunaGraceUntil;
}
public class Log { public void LogDebug(string s){} public void LogInfo(string s){} public void LogWarning(string s){} }
public static class StageRolesPlugin { public static Log ModLogger=new(); }
public static class MageSpellAim { public static Vector3 GetDirection(PlayerAvatar p)=>Vector3.zero; }
public class ResolvedRolePrefab { }
public class Resolver {
 public bool TryGetStarProjectile(out ResolvedRolePrefab r) { r=new(); return true; }
 public bool TryGetRollProjectile(out ResolvedRolePrefab r) { r=new(); return true; }
 public bool TryGetZeroGravityProjectile(out ResolvedRolePrefab r) { r=new(); return true; }
 public bool TryGetVoidProjectile(out ResolvedRolePrefab r) { r=new(); return true; }
}
'''
classes = '''
class TunaProbe {
 private const float TunaMovementThresholdSquared=0.000025f;
 private const float TunaStageStartGraceSeconds=5f;
 private float _tunaStageGraceUntil=5;
 private Config _config=new();
 public void Tick(RoleAssignment a)=>TickTuna(a);
''' + tuna + '''
}
class MageProbe {
 private Config _config=new(); private Resolver _resolver=new();
 public float Interval=3;
 private float ClampedInterval=>Interval;
 private delegate bool TryResolveProjectile(out ResolvedRolePrefab resolved);
 private bool TryCastProjectile(PlayerAvatar p,Vector3 d,string spell,TryResolveProjectile resolver)=>true;
 private bool TryCastBeam(PlayerAvatar p)=>true;
''' + cast + cost + '''
}
class GuardProbe {
 private Config _config=new(); private PendingDamageBudget _internalDamage=new();
 public RoleAssignment Guard=new();
 private RoleAssignment? FindNearestBodyguard(PlayerAvatar p)=>Guard;
 private RoleAssignment? FindAssignment(string id)=>null;
 private class PendingRevival { public RoleAssignment Assignment=null!; public int Health; public float ExpiresAt; }
 private Dictionary<string,PendingRevival> _pendingRevivals=new();
 public void Transfer(PlayerAvatar p,int before,int after,int damage)=>ApplyBodyguardTransfer(p,before,after,damage);
''' + transfer + send_damage + '''
}
'''
main = '''
class Program {
 static void Main() {
  var results=new List<object>();
  foreach(int fps in new[]{60,120,240}) {
   var p=new TunaProbe(); var a=new RoleAssignment();
   Time.deltaTime=1f/fps;
   for(int frame=0;frame<fps*4;frame++) {
    Time.time=10+(frame+1f)/fps;
    a.Player.transform.position=new((frame+1f)*0.5f/fps,0,0);
    p.Tick(a);
   }
   results.Add(new {caseName="Tuna moving at 0.5 m/s for 4 s",fps,hp=a.Player.playerHealth.Health,damageCalls=a.Player.playerHealth.DamageCalls});
  }
  {
   var p=new TunaProbe(); var a=new RoleAssignment(); a.Player.playerHealth.Health=a.Player.playerHealth.Actual=0;
   Time.time=100; Time.deltaTime=1f/60; p.Tick(a);
   a.Player.playerHealth.Health=a.Player.playerHealth.Actual=1;
   float deadAt=0;
   for(int frame=1;frame<=600;frame++) {
    Time.time=100+frame/60f; p.Tick(a);
    if(a.Player.playerHealth.Health==0) { deadAt=frame/60f; break; }
   }
   results.Add(new{caseName="Tuna stationary 1 HP revival after stage grace",deathAfterSeconds=deadAt});
  }
  foreach(bool delay in new[]{false,true}) {
   var p=new MageProbe{Interval=0.1f}; var a=new RoleAssignment();
   a.Player.playerHealth.Health=a.Player.playerHealth.Actual=15; a.Player.playerHealth.Defer=delay;
   Time.time=10; bool first=p.TryCast(a,"star"); Time.time=10.2f; bool second=p.TryCast(a,"star");
   int queued=a.Player.playerHealth.Queue.Count; a.Player.playerHealth.Flush();
   results.Add(new {caseName="Mage 15 HP, two 10 HP casts 0.2 s apart",delay,first,second,queued,hp=a.Player.playerHealth.Health});
  }
  {
   var p=new GuardProbe(); var victim=new PlayerAvatar(); SemiFunc.Multiplayer=true;
   p.Guard.Player.photonView.IsMine=false; p.Guard.Player.playerHealth.Defer=true;
   p.Guard.Player.playerHealth.Health=30; p.Guard.Player.playerHealth.Actual=5;
   victim.playerHealth.Health=victim.playerHealth.Actual=60;
   p.Transfer(victim,100,60,40);
   int transferred=p.Guard.Player.playerHealth.Queue[0]; p.Guard.Player.playerHealth.Flush();
   results.Add(new {caseName="Bodyguard host HP30, owner HP5 after unsynchronized hit, receives transfer",transferred,guardHp=p.Guard.Player.playerHealth.Health});
  }
  Console.WriteLine(JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
 }
}
'''
(out/'Probe.cs').write_text(stubs+config+classes+main, encoding='utf-8')
(out/'PendingDamageBudget.cs').write_text((repo/'PendingDamageBudget.cs').read_text(encoding='utf-8-sig'),encoding='utf-8')
(out/'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup></Project>',encoding='utf-8')
(out/'sources.json').write_text(json.dumps(slices,indent=2),encoding='utf-8')
print('Prepared probes from current production method bodies; Unity and delayed health transport are stand-ins.')
