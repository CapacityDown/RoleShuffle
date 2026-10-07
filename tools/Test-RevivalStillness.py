"""Exercise production revival methods with deterministic physics/transport stand-ins.

Run this generator, then dotnet run --project tmp/revival-stillness-checks/Checks.csproj -c Release.
This checks state transitions and head motion, not Unity or live multiplayer physics.
"""
from pathlib import Path
import hashlib
import json

root = Path(__file__).resolve().parents[1]
out = root / 'tmp/revival-stillness-checks'
out.mkdir(parents=True, exist_ok=True)
sources = []


def extract(file, start, end):
    text = (root / file).read_text(encoding='utf-8-sig')
    begin = text.index(start)
    body = text[begin:text.index(end, begin)]
    sources.append(dict(file=file, method=start.strip(), sha256=hashlib.sha256(body.encode()).hexdigest()))
    return body


controller_methods = ''.join(extract('StageRoleController.cs', start, end) for start, end in [
    ('    internal bool TryPreventPhoenixRetake()', '    internal void Shutdown()'),
    ('    private void TickPhoenix(', '    private void TickRescuer('),
    ('    private void TickRescuer(', '    private void RefreshRescueDeaths()'),
    ('    private void RefreshRescueDeaths()', '    internal bool PlayerHasRole('),
    ('    private void SchedulePhoenix(', '    private void TryRevivePhoenix('),
    ('    private void TryRevivePhoenix(', '    private void CompletePhoenixRevival('),
    ('    private void CompletePhoenixRevival(', '    private static void SetRevivalHealth('),
])
player_methods = ''.join(extract('PlayerState.cs', start, end) for start, end in [
    ('    internal static bool TryRequestDeathHeadRevival(', '    internal static bool TryGetDeathHeadPosition('),
    ('    internal static bool TryGetDeathHeadRuntime(', '    internal static bool TryMoveDeathHead('),
])
production = (root/'DeathHeadStillness.cs').read_text(encoding='utf-8-sig')
sources.append(dict(file='DeathHeadStillness.cs', sha256=hashlib.sha256(production.encode()).hexdigest()))
(out/'DeathHeadStillness.cs').write_text(production, encoding='utf-8')
(out/'Checks.csproj').write_text('''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings><TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup></Project>''', encoding='utf-8')

stubs = r'''
using System.Reflection;
using UnityEngine;
namespace UnityEngine {
 public struct Vector3(float x,float y,float z) {
  public float x=x,y=y,z=z;
  public float sqrMagnitude=>x*x+y*y+z*z;
  public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
 }
 public struct Quaternion(float degrees) {
  public float Degrees=degrees;
  public static float Angle(Quaternion a,Quaternion b)=>Math.Abs(a.Degrees-b.Degrees);
 }
 public static class Time { public static float time; }
 public static class Mathf {
  public static int Clamp(int value,int min,int max)=>Math.Clamp(value,min,max);
  public static float Clamp(float value,float min,float max)=>Math.Clamp(value,min,max);
 }
 public class GameObject { public bool activeInHierarchy=true; }
 public class Transform { public Vector3 position; public Quaternion rotation; }
 public class Rigidbody { public Vector3 velocity; public Vector3 angularVelocity; }
}
public class PhysGrabber {}
public class PhysGrabObject {
 public Rigidbody? rb=new();
 public List<PhysGrabber> playerGrabbing=new();
}
public class PlayerDeathHead {
 public bool setup=true,triggered=true,spectated=false;
 public float triggeredTimer=0;
 public PhysGrabObject? physGrabObject=new();
 public GameObject gameObject=new(); public Transform transform=new();
}
public class PlayerAvatar {
 public PhysGrabber? physGrabber=new();
 public bool Alive=false,Immediate=true,SecondChance=false;
 public int ReviveCalls=0,Health=0;
 public PlayerDeathHead? playerDeathHead=new();
 public Transform transform=new();
 public void Revive(bool unused) { ReviveCalls++; if(Immediate) Alive=true; }
}
namespace REPOJP.StageRoles {
internal static class PlayerState {
 private static readonly FieldInfo? DeathHeadField=typeof(PlayerAvatar).GetField("playerDeathHead");
 private static readonly FieldInfo? DeathHeadSetupField=typeof(PlayerDeathHead).GetField("setup");
 private static readonly FieldInfo? DeathHeadTriggeredField=typeof(PlayerDeathHead).GetField("triggered");
 private static readonly FieldInfo? DeathHeadTriggeredTimerField=typeof(PlayerDeathHead).GetField("triggeredTimer");
 private static readonly FieldInfo? DeathHeadPhysGrabObjectField=typeof(PlayerDeathHead).GetField("physGrabObject");
 private static readonly FieldInfo? DeathHeadSpectatedField=typeof(PlayerDeathHead).GetField("spectated");
 private static bool ReadBool(FieldInfo? field, object obj)=>field?.GetValue(obj) is true;
 internal static bool IsLiving(PlayerAvatar? player)=>player?.Alive==true;
 internal static bool TryGetDeathHeadPosition(PlayerAvatar player,out Vector3 position) {
  position=player.playerDeathHead?.transform.position??default; return player.playerDeathHead!=null;
 }
__PLAYER_METHODS__
}
internal enum StageRole { Phoenix,Rescuer,Tank,Superbot }
internal static class RoleCatalog {
 internal static bool HasCapability(StageRole actual,StageRole wanted)=>actual==wanted||actual==StageRole.Superbot;
}
internal class RoleAssignment(string id,StageRole role) {
 internal string SteamId=id;
 internal StageRole Role=role;
 internal PlayerAvatar Player=new();
 internal bool WasAlive=true,PhoenixUsed=false,PhoenixRevivePending=false;
 internal float PhoenixReadyAt=0,InfluencerNextTtsAt=0;
 internal int RescuerRevivesUsed=0;
 internal DeathHeadStillness RevivalHeadStillness=new();
}
internal class Entry<T>(T value) { internal T Value=value; }
internal class Config {
 internal Entry<int> RescuerMaximumRevives=new(2),RescuerRevivalHealth=new(25),PhoenixRevivalHealth=new(25);
 internal Entry<float> RescuerReviveDelaySeconds=new(2),PhoenixReviveDelaySeconds=new(2);
}
internal class EventRoles { internal bool HasCorrectiveRevivalPending(PlayerAvatar player)=>false; }
internal static class StageFluxCompatibility {
 internal static bool WillHandleSecondChance(PlayerAvatar player)=>player.SecondChance;
}
internal class Log {
 internal void LogInfo(string text){} internal void LogDebug(string text){} internal void LogWarning(string text){}
}
internal static class StageRolesPlugin { internal static Log ModLogger=new(); }
internal class Probe {
 internal bool _stageReady=true,Authority=true;
 internal readonly List<RoleAssignment> _assignments=new();
 internal readonly Dictionary<string,float> _rescueDeadSince=new();
 private readonly HashSet<string> _rescueRevivePending=new();
 private readonly Dictionary<string,string> _rescueReviverByTarget=new();
 internal readonly Config _config=new();
 private readonly EventRoles _eventRoles=new();
 private bool IsAuthority()=>Authority;
 private static void SetRevivalHealth(RoleAssignment assignment,string source,int health)=>assignment.Player.Health=health;
 internal void Tick(float now) {
  Time.time=now;
  foreach(var a in _assignments) a.RevivalHeadStillness.Observe(a.Player);
  RefreshRescueDeaths();
  foreach(var a in _assignments) {
   if(RoleCatalog.HasCapability(a.Role,StageRole.Phoenix)) TickPhoenix(a);
   if(RoleCatalog.HasCapability(a.Role,StageRole.Rescuer)) TickRescuer(a);
  }
 }
__CONTROLLER_METHODS__
}
}
'''
(out/'Stubs.cs').write_text(stubs.replace('__PLAYER_METHODS__', player_methods).replace('__CONTROLLER_METHODS__', controller_methods), encoding='utf-8')

tests = r'''
using REPOJP.StageRoles;
using UnityEngine;
int checks=0;
void Check(bool ok,string message) { checks++; if(!ok) throw new Exception(message); }
void Still(DeathHeadStillness state,PlayerAvatar p,float start,int frames=7) {
 for(int i=0;i<frames;i++) { Time.time=start+i*0.1f; state.Observe(p); }
}
PlayerAvatar p=new(); DeathHeadStillness state=new();
Time.time=0; Check(!state.Observe(p),"First observation cannot revive");
Time.time=0.2f; Check(!state.Observe(p),"A short pause is not enough");
Time.time=0.4f; Check(!state.Observe(p),"Stillness needs half a second");
Time.time=0.6f; Check(state.Observe(p),"Stationary head becomes ready");
p.playerDeathHead!.physGrabObject!.rb!.velocity=new(1,0,0);
Check(!state.Observe(p),"Translation invalidates readiness immediately");
p.playerDeathHead.physGrabObject.rb.velocity=default;
p.playerDeathHead.physGrabObject.rb.angularVelocity=new(0,1,0);
Still(state,p,1); Check(!state.Observe(p),"Rotation alone blocks revival");
p.playerDeathHead.physGrabObject.rb.angularVelocity=default;
Still(state,p,2); Check(state.Observe(p),"Ready after rotation stops");
p.playerDeathHead.transform.position=new(1,0,0);
Check(!state.Observe(p),"Network/carrier motion blocks even with zero rigidbody speed");
Still(state,p,3); p.playerDeathHead.transform.rotation=new(20);
Check(!state.Observe(p),"Transform rotation invalidates readiness");
Still(state,p,4); Time.time=10;
Check(!state.Observe(p),"No readiness carried across an unobserved gap");
Still(state,p,11); p.Alive=true;
Check(!state.Observe(p),"Revival clears observation");
p.Alive=false; Check(!state.Observe(p),"New death cannot reuse old readiness");
Still(state,p,12); p.playerDeathHead=new();
Check(!state.Observe(p),"Replaced head must settle again");
Still(state,p,13); p.playerDeathHead.gameObject.activeInHierarchy=false;
Check(!state.Observe(p),"Inactive head cannot revive");
p.playerDeathHead.gameObject.activeInHierarchy=true; p.playerDeathHead.physGrabObject!.rb=null;
Check(!state.Observe(p),"Missing rigidbody cannot revive");
p.playerDeathHead=null; Check(!state.Observe(p),"Missing head cannot revive");
p.playerDeathHead=new(); Still(state,p,14);
p.playerDeathHead.physGrabObject!.rb!.velocity=new(float.NaN,0,0);
Check(!state.Observe(p),"Invalid velocity cannot count as stationary");

foreach(int fps in new[]{30,60,144}) {
 p=new(); state=new();
 for(int i=0;i<fps;i++) {
  Time.time=20+(float)i/fps;
  p.playerDeathHead!.transform.position=new(0.2f*i/fps,0,0);
  Check(!state.Observe(p),"Slow network drift cannot build up stillness");
 }
 for(int i=0;i<=fps;i++) { Time.time=21+(float)i/fps; state.Observe(p); }
 Check(state.Observe(p),"Rest is detected at every tested frame rate");
}

// Rescue requires this rescuer's current grab, with no radius or stillness gate.
Probe rescue=new(); RoleAssignment rescuer=new("rescuer",StageRole.Rescuer);
rescuer.Player.Alive=true;
RoleAssignment nearby=new("nearby",StageRole.Tank),held=new("held",StageRole.Tank);
RoleAssignment bystander=new("bystander",StageRole.Tank); bystander.Player.Alive=true;
nearby.Player.playerDeathHead!.transform.position=new(1,0,0);
held.Player.playerDeathHead!.transform.position=new(10,0,0);
held.Player.playerDeathHead.physGrabObject!.rb!.velocity=new(5,0,0);
held.Player.playerDeathHead.physGrabObject.rb.angularVelocity=new(0,3,0);
rescue._assignments.AddRange(new[]{rescuer,nearby,held,bystander});
for(int i=0;i<40;i++) rescue.Tick(i*0.1f);
Check(nearby.Player.ReviveCalls==0&&held.Player.ReviveCalls==0&&rescuer.RescuerRevivesUsed==0,
 "Nearby stationary heads do not auto-revive");
held.Player.playerDeathHead.physGrabObject.playerGrabbing.Add(bystander.Player.physGrabber!);
rescue.Tick(4);
Check(held.Player.ReviveCalls==0,"Another player's grab cannot trigger Rescuer");
held.Player.playerDeathHead.physGrabObject.playerGrabbing.Add(rescuer.Player.physGrabber!);
rescue.Tick(4.1f); rescue.Tick(4.2f);
Check(held.Player.ReviveCalls==1&&nearby.Player.ReviveCalls==0,"Only the grabbed head revives, even moving beyond the old radius");
Check(rescuer.RescuerRevivesUsed==1&&held.Player.Health==25,"One successful grabbed-head rescue consumes one use and sets HP");
held.Player.playerDeathHead.physGrabObject.playerGrabbing.Clear();

// Release during the configured death delay leaves no queued revival.
rescue._rescueDeadSince[nearby.SteamId]=4.3f;
nearby.Player.playerDeathHead!.physGrabObject!.playerGrabbing.Add(rescuer.Player.physGrabber!);
rescue.Tick(4.3f);
Check(nearby.Player.ReviveCalls==0&&rescuer.RescuerRevivesUsed==1,"Grabbing early preserves the existing death delay and charge");
nearby.Player.playerDeathHead.physGrabObject.playerGrabbing.Clear();
for(int i=44;i<90;i++) rescue.Tick(i*0.1f);
Check(nearby.Player.ReviveCalls==0&&rescuer.RescuerRevivesUsed==1,"Released heads do not revive later");
nearby.Player.playerDeathHead.physGrabObject.playerGrabbing.Add(rescuer.Player.physGrabber!);
rescue.Tick(9); rescue.Tick(9.1f);
Check(nearby.Player.ReviveCalls==1&&rescuer.RescuerRevivesUsed==2&&nearby.Player.Health==25,
 "Grabbing again revives once and the final use still applies configured HP");
RoleAssignment exhaustedTarget=new("exhausted",StageRole.Tank);
exhaustedTarget.Player.playerDeathHead!.physGrabObject!.playerGrabbing.Add(rescuer.Player.physGrabber!);
rescue._assignments.Add(exhaustedTarget);
for(int i=92;i<125;i++) rescue.Tick(i*0.1f);
Check(exhaustedTarget.Player.ReviveCalls==0,"Grabbing with no uses left does not revive");

// Two rescuers holding the same remote head must send exactly one request.
Probe shared=new(); shared._config.RescuerMaximumRevives.Value=1; shared._config.RescuerReviveDelaySeconds.Value=0;
RoleAssignment r1=new("r1",StageRole.Rescuer),r2=new("r2",StageRole.Rescuer),remote=new("remote",StageRole.Tank);
r1.Player.Alive=r2.Player.Alive=true; remote.Player.Immediate=false;
remote.Player.playerDeathHead!.physGrabObject!.playerGrabbing.AddRange(new[]{r1.Player.physGrabber!,r2.Player.physGrabber!});
shared._assignments.AddRange(new[]{r1,r2,remote});
for(int i=0;i<100;i++) shared.Tick(20+i*0.1f);
Check(remote.Player.ReviveCalls==1&&r1.RescuerRevivesUsed+r2.RescuerRevivesUsed==1,
 "Multiple rescuers and delayed transport cannot duplicate a request or consume two uses");
remote.Player.Alive=true; shared.Tick(30);
Check(remote.Player.Health==25,"Delayed acknowledgement receives revival HP even after the rescuer's last use");

// Head setup and other revival systems keep their existing priority.
Probe priority=new(); priority._config.RescuerReviveDelaySeconds.Value=0;
RoleAssignment rp=new("rp",StageRole.Rescuer),tp=new("tp",StageRole.Phoenix);
rp.Player.Alive=true; priority._assignments.AddRange(new[]{rp,tp});
tp.Player.playerDeathHead!.physGrabObject!.playerGrabbing.Add(rp.Player.physGrabber!);
tp.Player.playerDeathHead.physGrabObject.rb!.velocity=new(5,0,0);
priority.Tick(40);
Check(tp.Player.ReviveCalls==0&&rp.RescuerRevivesUsed==0,"Grab rescue does not bypass an unused Phoenix");
tp.Role=StageRole.Tank; tp.Player.SecondChance=true; priority.Tick(40.1f);
Check(tp.Player.ReviveCalls==0&&rp.RescuerRevivesUsed==0,"Grab rescue preserves Stage Flux priority");
tp.Player.SecondChance=false; tp.Player.playerDeathHead.setup=false; priority.Tick(40.2f);
Check(tp.Player.ReviveCalls==0&&rp.RescuerRevivesUsed==0,"Uninitialized head does not consume a rescue");
rp.Player.Alive=false; tp.Player.playerDeathHead.setup=true; priority.Tick(40.3f);
Check(tp.Player.ReviveCalls==0,"A dead rescuer cannot revive through a stale grab");
rp.Player.Alive=true; priority.Tick(40.4f);
Check(tp.Player.ReviveCalls==1&&rp.RescuerRevivesUsed==1,"An initialized moving head revives while held");
Check(!PlayerState.IsGrabbingDeathHead(null,tp.Player),"Missing rescuer cannot be a grabber");
rp.Player.physGrabber=null;
Check(!PlayerState.IsGrabbingDeathHead(rp.Player,tp.Player),"Missing grabber cannot be treated as holding");
Check(!PlayerState.IsGrabbingDeathHead(r1.Player,null),"Missing head target is rejected");

// Long moving/setup/transport waits must keep an unused Phoenix alive in the run.
foreach(var role in new[]{StageRole.Phoenix,StageRole.Superbot}) {
 Probe phoenix=new(); RoleAssignment a=new("phoenix",role);
 phoenix._assignments.Add(a); a.Player.Immediate=false;
 a.Player.playerDeathHead!.physGrabObject!.rb!.velocity=new(0,0,-2);
 for(int i=0;i<=3000;i++) {
  phoenix.Tick(i*0.1f);
  Check(phoenix.TryPreventPhoenixRetake(),"No game over during a five-minute moving-head wait");
 }
 Check(!a.PhoenixUsed&&!a.PhoenixRevivePending&&a.Player.ReviveCalls==0,"Moving Phoenix retains charge without request");
 a.Player.playerDeathHead.physGrabObject.rb.velocity=default;
 a.Player.playerDeathHead.setup=false;
 for(int i=3001;i<3011;i++) phoenix.Tick(i*0.1f);
 Check(phoenix.TryPreventPhoenixRetake()&&!a.PhoenixUsed&&a.Player.ReviveCalls==0,"Uninitialized head preserves charge and stage");
 a.Player.playerDeathHead.setup=true;
 for(int i=3011;i<3111;i++) {
  phoenix.Tick(i*0.1f);
  Check(phoenix.TryPreventPhoenixRetake(),"No game over during delayed remote revival acknowledgement");
 }
 Check(a.PhoenixRevivePending&&!a.PhoenixUsed&&a.Player.ReviveCalls==1,"Exactly one revival request awaits acknowledgement");
 a.Player.Alive=true;
 Check(phoenix.TryPreventPhoenixRetake(),"Acknowledgement cancels a simultaneous stale game-over transition");
 Check(a.PhoenixUsed&&!a.PhoenixRevivePending&&a.Player.Health==25,"Charge consumed only when revival completes");
 Check(phoenix.TryPreventPhoenixRetake(),"A queued failure after confirmed revival cannot kill a living Phoenix");
 a.Player.Alive=false; phoenix.Tick(320);
 Check(!phoenix.TryPreventPhoenixRetake(),"A second death with no charge does not block game over");
}

Probe secondChance=new(); RoleAssignment sc=new("sc",StageRole.Phoenix);
secondChance._assignments.Add(sc); sc.Player.SecondChance=true;
for(int i=0;i<100;i++) { secondChance.Tick(i*0.1f); Check(secondChance.TryPreventPhoenixRetake(),"Second Chance waiting preserves stage"); }
Check(sc.Player.ReviveCalls==0&&!sc.PhoenixUsed,"Stage Flux retains priority and Phoenix charge");
sc.Player.SecondChance=false;
for(int i=100;i<140;i++) secondChance.Tick(i*0.1f);
Check(sc.Player.ReviveCalls==1&&sc.PhoenixUsed,"Phoenix resumes after Second Chance declines");

Probe missing=new(); RoleAssignment lost=new("lost",StageRole.Phoenix);
missing._assignments.Add(lost); lost.Player.playerDeathHead=null;
Time.time=10000; Check(missing.TryPreventPhoenixRetake(),"Head not spawned yet cannot time out unused Phoenix");
missing._stageReady=false; Check(!missing.TryPreventPhoenixRetake(),"Stage cleanup ends protection");
missing._stageReady=true; missing.Authority=false;
Check(!missing.TryPreventPhoenixRetake(),"Clients do not prevent transitions");
missing.Authority=true; missing._assignments.Clear();
Check(!missing.TryPreventPhoenixRetake(),"Departed players do not block game over forever");
Console.WriteLine($"PASS: {checks} revival stillness and Phoenix state-transition assertions.");
'''
(out/'Program.cs').write_text(tests, encoding='utf-8')
(out/'source-hashes.json').write_text(json.dumps(sources, indent=2)+'\n', encoding='utf-8')
print('Generated revival checks from production methods and DeathHeadStillness.cs.')
