"""Generate Tuna/Trickster regression tests from production method bodies.
Run Python, then dotnet run --project tmp/role-safety-checks/Checks.csproj -c Release.
Unity objects, coroutine scheduling and transport are stand-ins, not a live game.
"""
from pathlib import Path
import hashlib
import json

root = Path(__file__).resolve().parents[1]
out = root / 'tmp/role-safety-checks'
out.mkdir(parents=True, exist_ok=True)
sources = []

def extract(file, start, end):
    text = (root / file).read_text(encoding='utf-8-sig')
    begin = text.index(start)
    body = text[begin:text.index(end, begin)]
    sources.append(dict(file=file, method=start.strip(), sha256=hashlib.sha256(body.encode()).hexdigest()))
    return body

tuna = extract('StageRoleController.cs', '    private void TickTuna(', '    private void TickPhoenix(')
place = extract('TricksterRoleRuntime.cs', '    internal bool TryPlace(', '    internal void Tick(')
remove = extract('TricksterRoleRuntime.cs', '    internal void RemovePlayer(', '    internal bool IsDecoy(')
initialize = extract('TricksterRoleRuntime.cs', '    internal void Stop()', '    private int Pulse(')
active = extract('TricksterRoleRuntime.cs', '    private sealed class ActiveDecoy', '\n}\n')

stubs = r'''
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
public record struct Vector3(float x, float y, float z) {
 public static Vector3 zero => new(0,0,0);
 public float sqrMagnitude => x*x+y*y+z*z;
 public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x-b.x,a.y-b.y,a.z-b.z);
}
public struct Quaternion { public static Quaternion identity => new(); }
public static class Time { public static float time, deltaTime; }
public static class Mathf {
 public static int Clamp(int x,int lo,int hi)=>Math.Clamp(x,lo,hi);
 public static float Clamp(float x,float lo,float hi)=>Math.Clamp(x,lo,hi);
}
public class Entry<T>(T value) { public T Value = value; }
public class Config {
 public Entry<float> TunaStationaryDelaySeconds=new(3), TricksterActiveSeconds=new(25),
  TricksterPulseIntervalSeconds=new(1), TricksterCooldownSeconds=new(45), TricksterNoTargetCooldownSeconds=new(10);
 public Entry<int> TunaDamage=new(1);
 public float ClampedTunaDamageInterval=>0.1f;
}
public enum StageRole { Tuna, Trickster, Influenza, Superbot }
public static class RoleCatalog {
 public static bool HasCapability(StageRole actual,StageRole wanted)=>actual==wanted || actual==StageRole.Superbot;
}
public class Transform { public Vector3 position; }
public class PlayerAvatar {
 public Transform transform=new(); public PlayerHealth playerHealth=new(); public bool Truck, Disabled;
}
public class PlayerHealth {
 public int Health=120, DamageCalls;
 public void HurtOther(int amount,Vector3 origin,bool grace) { Health=Math.Max(0,Health-amount); DamageCalls++; }
}
public static class PlayerState {
 public static bool IsLiving(PlayerAvatar p)=>!p.Disabled && p.playerHealth.Health>0;
 public static bool IsInTruck(PlayerAvatar p)=>p.Truck;
}
public class RoleAssignment {
 public string SteamId="test"; public PlayerAvatar Player=new(); public StageRole Role=StageRole.Trickster;
 public Vector3 TunaPreviousPosition; public float TunaStationaryTimer,TunaDamageTimer,TunaGraceUntil;
 public bool TunaWasAlive;
}
public class Log { public void LogDebug(string s){} public void LogWarning(string s){} public void LogInfo(string s){} }
public static class StageRolesPlugin { public static Log ModLogger=new(); }
public class ScreamDollValuable { public enum States { Idle, Active } public States State; }
public class PhysGrabObject {}
public class ValuableObject {}
public class GameObject {
 public bool Destroyed, MissingComponents;
 public string name=""; public Transform transform=new();
 public ScreamDollValuable Scream=new(); public PhysGrabObject Phys=new(); public ValuableObject Valuable=new();
 public T? GetComponentInChildren<T>(bool includeInactive) where T:class =>
  MissingComponents ? null : typeof(T)==typeof(ScreamDollValuable) ? Scream as T :
  typeof(T)==typeof(PhysGrabObject) ? Phys as T : Valuable as T;
}
public class MonoBehaviour {
 public readonly List<IEnumerator> Pending=new();
 public void StartCoroutine(IEnumerator action) { if(action.MoveNext()) Pending.Add(action); }
 public void ResumeAll() { var batch=Pending.ToArray(); Pending.Clear(); foreach(var action in batch) if(action.MoveNext()) Pending.Add(action); }
}
public class ResolvedRolePrefab { public string ResourcePath="decoy"; public GameObject Prefab=new(); }
public class Resolver {
 public bool TryGetScreamDoll(out ResolvedRolePrefab result) { result=new(); return true; }
}
public static class SemiFunc { public static bool Multiplayer; public static bool IsMultiplayer()=>Multiplayer; }
public static class PhotonNetwork {
 public static GameObject Instantiate(string path,Vector3 position,Quaternion rotation,int group)=>new();
}
namespace UnityEngine { public static class Object {
 public static GameObject Instantiate(GameObject template,Vector3 position,Quaternion rotation)=>new();
}}
public static class StageFluxCompatibility { public static void MarkInternalCarrier(GameObject g){} }
'''
classes = r'''
public class TunaProbe {
 private const float TunaMovementThresholdSquared=0.000025f, TunaStageStartGraceSeconds=5;
 private float _tunaStageGraceUntil=5;
 private Config _config=new();
 public void Tick(RoleAssignment a)=>TickTuna(a);
 public void Revived(RoleAssignment a)=>ResetTunaActivity(a);
''' + tuna + r'''
}
public class TricksterProbe {
 private const float StateRefreshSeconds=0.5f;
 private readonly MonoBehaviour _coroutineOwner=new();
 private readonly Config _config=new(); private readonly Resolver _resolver=new();
 private readonly Dictionary<string,ActiveDecoy> _active=new(StringComparer.Ordinal);
 private readonly Dictionary<string,GameObject> _pendingObjects=new(StringComparer.Ordinal);
 private readonly Dictionary<string,float> _nextPlacementAt=new(StringComparer.Ordinal);
 private readonly List<GameObject> _spawnedObjects=new(); private int _generation;
 public int Pulses, Locks;
 public int Pending=>_pendingObjects.Count;
 public int Active=>_active.Count;
 public int Tracked=>_spawnedObjects.Count;
 public bool Cooldown=>_nextPlacementAt.Count>0;
 public GameObject Latest=>_spawnedObjects.Last();
 public void Resume()=>_coroutineOwner.ResumeAll();
 private static Vector3 PlacementPosition(PlayerAvatar p)=>p.transform.position;
 private void Lock(PhysGrabObject p)=>Locks++;
 private static void SetValueZero(ValuableObject? v){}
 private static void SetScreamState(ScreamDollValuable s,ScreamDollValuable.States state)=>s.State=state;
 private int Pulse(Vector3 p) { Pulses++; return 1; }
 private static void DestroyNetworkObject(GameObject? g) { if(g!=null) g.Destroyed=true; }
''' + place + remove + initialize + active + '\n}\n'

tests = r'''
public class Program {
 static int checks;
 static void Check(bool condition,string name) { if(!condition) throw new Exception(name); checks++; }
 static void Frames(TunaProbe p,RoleAssignment a,int fps,float seconds,float speed=0,bool network=false) {
  float start=Time.time, x=a.Player.transform.position.x;
  for(int i=1;i<=Math.Round(seconds*fps);i++) {
   Time.deltaTime=1f/fps; Time.time=start+(float)i/fps;
   float elapsed=network ? MathF.Floor(i/(float)fps*10)/10 : i/(float)fps;
   a.Player.transform.position=new(x+speed*elapsed,0,0); p.Tick(a);
  }
 }
 public static void Main() {
  foreach(int fps in new[]{30,60,120,240}) foreach(float speed in new[]{0.02f,0.1f,0.5f,2f}) foreach(bool network in new[]{false,true}) {
   var p=new TunaProbe(); var a=new RoleAssignment { TunaWasAlive=true }; Time.time=10;
   Frames(p,a,fps,10,speed,network);
   Check(a.Player.playerHealth.DamageCalls==0,$"Moving Tuna at {speed}m/s, {fps}fps, network={network}");
   Frames(p,a,fps,4);
   Check(a.Player.playerHealth.Health<120,"Stopping still causes the intended damage");
   int before=a.Player.playerHealth.DamageCalls;
   Frames(p,a,fps,1,1);
   Check(a.Player.playerHealth.DamageCalls==before,"Resuming movement stops damage immediately");
  }
  foreach(int fps in new[]{30,60,120,240}) {
   var p=new TunaProbe(); var a=new RoleAssignment(); Time.time=100;
   a.Player.playerHealth.Health=0; p.Tick(a); a.Player.playerHealth.Health=1;
   Frames(p,a,fps,5);
   Check(a.Player.playerHealth.Health==1,"1HP revival gets full five-second grace");
   Frames(p,a,fps,2.5f);
   Check(a.Player.playerHealth.Health==1,"Stationary countdown starts after revival grace");
   Frames(p,a,fps,1);
   Check(a.Player.playerHealth.Health==0,"Prolonged stationary play remains lethal after grace");
   a.Player.playerHealth.Health=1; p.Revived(a); Frames(p,a,fps,5);
   Check(a.Player.playerHealth.Health==1,"Repeated revival resets grace without a dead frame");
   a.Player.Truck=true; Frames(p,a,fps,60);
   Check(a.Player.playerHealth.Health==1,"Truck never causes inactivity death");
   a.Player.Truck=false; Frames(p,a,fps,2.5f);
   Check(a.Player.playerHealth.Health==1,"Leaving truck starts a fresh stationary countdown");
   Frames(p,a,fps,1); Check(a.Player.playerHealth.Health==0,"Truck protection ends on exit");
  }
  {
   var p=new TunaProbe(); var a=new RoleAssignment(); Time.time=100;
   a.Player.Disabled=true; a.Player.playerHealth.Health=1; Frames(p,a,60,20);
   a.Player.Disabled=false; Frames(p,a,60,5);
   Check(a.Player.playerHealth.Health==1,"Revival grace waits for a living usable avatar");
  }
  foreach(bool multiplayer in new[]{false,true}) {
   SemiFunc.Multiplayer=multiplayer;
   foreach(string reason in new[]{"remove","infection","death","truck","replace","stage","expired","missing"}) {
    Time.time=100; var p=new TricksterProbe(); var a=new RoleAssignment();
    Check(p.TryPlace(a),"Valid placement accepted"); var old=p.Latest;
    Check(!p.TryPlace(a),"Pending placement blocks duplicates");
    switch(reason) {
     case "remove": p.RemovePlayer(a.SteamId); break;
     case "infection": a.Role=StageRole.Influenza; break;
     case "death": a.Player.playerHealth.Health=0; break;
     case "truck": a.Player.Truck=true; break;
     case "replace": a.Player=new(); break;
     case "stage": p.Stop(); break;
     case "expired": Time.time=126; break;
     case "missing": old.MissingComponents=true; break;
    }
    if(reason is "remove" or "stage") Check(old.Destroyed,"Removed pending object is destroyed immediately");
    p.Resume();
    Check(p.Pulses==0 && p.Locks==0,$"Cancelled {reason} decoy never attracts enemies");
    Check(old.Destroyed && p.Pending==0 && p.Active==0 && p.Tracked==0,"Cancelled placement releases object and ownership");
    Check(!p.Cooldown,"Cancelled placement does not charge cooldown");
   }
   foreach(bool stopStage in new[]{false,true}) {
    Time.time=100; var p=new TricksterProbe(); var a=new RoleAssignment();
    Check(p.TryPlace(a),"Initial placement"); var old=p.Latest;
    if(stopStage) p.Stop(); else p.RemovePlayer(a.SteamId);
    Check(p.TryPlace(a),"Replacement placement"); var replacement=p.Latest;
    p.Resume();
    Check(old.Destroyed && !replacement.Destroyed,"Old coroutine cannot destroy replacement");
    Check(p.Active==1 && p.Pending==0 && p.Tracked==1 && p.Pulses==1,"Only replacement activates");
    p.RemovePlayer(a.SteamId);
    Check(replacement.Destroyed && p.Active==0 && p.Tracked==0,"Active decoy also removed on role loss");
   }
   foreach(StageRole role in new[]{StageRole.Trickster,StageRole.Superbot}) {
    Time.time=100; var p=new TricksterProbe(); var a=new RoleAssignment { Role=role };
    Check(p.TryPlace(a),"Capability permits placement"); p.Resume();
    Check(p.Active==1 && p.Pulses==1 && p.Pending==0,"Valid ability still activates");
    Check(!p.TryPlace(a),"Active decoy prevents duplicates"); p.Stop();
    Check(p.Active==0 && p.Tracked==0,"Stage end clears all decoys");
   }
  }
  Console.WriteLine(JsonSerializer.Serialize(new { passed=checks, liveGameTested=false }));
 }
}
'''
(out / 'Checks.cs').write_text(stubs + classes + tests, encoding='utf-8')
(out / 'Checks.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup></Project>', encoding='utf-8')
(out / 'sources.json').write_text(json.dumps(sources, indent=2) + '\n', encoding='utf-8')
print('Prepared Tuna and Trickster regressions from production method bodies.')

