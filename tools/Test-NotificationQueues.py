"""Exercise production notification coroutines and per-player native TTS tracking.

Generate, then run: dotnet run --project tmp/notification-checks/Checks.csproj -c Release
Virtual Unity time and native speech stand-ins are not live multiplayer validation.
"""
from pathlib import Path
import hashlib
import json

root = Path(__file__).resolve().parents[1]
out = root / 'tmp/notification-checks'
out.mkdir(parents=True, exist_ok=True)
sources = []
for file in ('RoleNotifier.cs', 'PlayerMessageActivity.cs', 'StageRole.cs'):
    body = (root/file).read_text(encoding='utf-8-sig')
    (out/file).write_text(body, encoding='utf-8')
    sources.append(dict(file=file, sha256=hashlib.sha256(body.encode()).hexdigest()))
event = (root/'EventRoleRuntime.cs').read_text(encoding='utf-8-sig')
start = event.index('    internal void LateTick(')
body = event[start:event.index('    internal bool HasCorrectiveRevivalPending(', start)]
sources.append(dict(file='EventRoleRuntime.cs', method='LateTick', sha256=hashlib.sha256(body.encode()).hexdigest()))
(out/'EventProbe.cs').write_text('''using System; using System.Collections.Generic; using UnityEngine;
namespace REPOJP.StageRoles;
internal sealed class EventProbe(StageRoleController controller, StageRolesConfig config) {
private readonly StageRoleController _controller = controller;
private readonly StageRolesConfig _config = config;
private readonly Dictionary<string,float> _lastTtsActivityAt = new();
private static readonly string[] InfluencerMessages = { "LookAtMe!" };
private float NextInfluencerInterval() => 20;
''' + body + '}\n', encoding='utf-8')
diver = (root/'DiverRoleRuntime.cs').read_text(encoding='utf-8-sig')
start = diver.index('    private static void TickCountdown(')
body = diver[start:diver.index('    internal void FixedTick()', start)]
sources.append(dict(file='DiverRoleRuntime.cs', method='TickCountdown', sha256=hashlib.sha256(body.encode()).hexdigest()))
(out/'DiverProbe.cs').write_text('''using System; using UnityEngine;
namespace REPOJP.StageRoles;
internal sealed class DiverProbe(PlayerAvatar player, RoleNotifier notifier, float duration) {
internal sealed class DiveState {
 internal bool Underfloor = true, PendingDeath, ZeroCountdownSent;
 internal float ExpiresAt;
 internal int LastCountdownSecond;
}
internal readonly DiveState State = new() {
 ExpiresAt = Time.time + duration, LastCountdownSecond = Mathf.CeilToInt(duration) + 1,
 PendingDeath = false, ZeroCountdownSent = false
};
internal void Tick() => TickCountdown(State, player, Time.time, notifier);
''' + body + '}\n', encoding='utf-8')
(out/'Checks.csproj').write_text('''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings><TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup></Project>''', encoding='utf-8')
(out/'Stubs.cs').write_text(r'''
using System.Collections;
using System.Reflection;
using REPOJP.StageRoles;
namespace UnityEngine {
 public static class Time { public static float time, realtimeSinceStartup; }
 public sealed class WaitForSeconds(float seconds) { public float Seconds = seconds; }
 public sealed class WaitForSecondsRealtime(float seconds) { public float Seconds = seconds; }
 public sealed class MonoBehaviour { public void StartCoroutine(IEnumerator routine) => Clock.Start(routine); }
 public sealed class GameObject { public bool activeInHierarchy = true; }
 public sealed class AudioSource { public bool isPlaying; }
 public sealed class Transform { public Vector3 position; }
 public struct Vector3 { }
 public static class Mathf {
  public static float Clamp(float v,float min,float max) => Math.Clamp(v,min,max);
  public static int Max(int a,int b) => Math.Max(a,b);
  public static int CeilToInt(float v) => (int)Math.Ceiling(v);
 }
 public static class Random { public static int Range(int min,int max) => min; }
}
namespace HarmonyLib { public static class AccessTools {
 public static FieldInfo? Field(Type type,string name) => type.GetField(name);
} }
namespace BepInEx.Bootstrap { public static class Chainloader { public static Dictionary<string,object> PluginInfos = new(); } }
namespace Photon.Pun { public sealed class PhotonView { public int ViewID; } }
public static class SemiFunc {
 public static bool Authority = true;
 public static bool IsMasterClientOrSingleplayer() => Authority;
 public static List<PlayerAvatar> PlayerGetList() => Clock.Players;
}
public sealed class GameDirector {
 public static GameDirector? instance = new(); public enum gameState { Main, Other }
 public gameState currentState = gameState.Main;
}
public sealed class EnemyDirector {
 public static EnemyDirector? instance = new(); public int Investigations;
 public void SetInvestigate(UnityEngine.Vector3 pos,float radius) => Investigations++;
}
public sealed class PlayerVoiceChat { public UnityEngine.AudioSource ttsAudioSource = new(); }
public sealed class PlayerAvatar {
 public string Id;
 public UnityEngine.GameObject gameObject = new();
 public UnityEngine.Transform transform = new();
 public Photon.Pun.PhotonView photonView = new();
 public PlayerVoiceChat voiceChat = new();
 public float Duration = 1, Delay, StartAt = float.MaxValue, EndAt;
 public bool Fail, Mute;
 public List<(string Message,float At)> Sent = new();
 public PlayerAvatar(string id) { Id=id; Clock.Players.Add(this); }
 public void ChatMessageSend(string message) {
  if (Fail) throw new Exception("Speech unavailable");
  // Production hook on ChatMessageSpeak observes all valid native chat, including other mods.
  PlayerMessageActivity.Observe(this);
  if (voiceChat.ttsAudioSource.isPlaying) Clock.Overlaps++;
  Sent.Add((message,UnityEngine.Time.time));
  StartAt = UnityEngine.Time.time + Delay; EndAt = StartAt + Duration;
  UpdateAudio();
 }
 public void UpdateAudio() => voiceChat.ttsAudioSource.isPlaying = gameObject.activeInHierarchy &&
  !Mute && UnityEngine.Time.time >= StartAt && UnityEngine.Time.time < EndAt;
}
public static class Clock {
 private sealed class Job(IEnumerator root) {
  public Stack<IEnumerator> Stack = new(new[] {root}); public float Wake;
 }
 private static List<Job> Jobs = new();
 public static List<PlayerAvatar> Players = new(); public static int Overlaps;
 public static void Start(IEnumerator routine) { var j=new Job(routine); Jobs.Add(j); Advance(j); }
 private static void Advance(Job job) {
  while(job.Stack.Count>0) {
   var top=job.Stack.Peek();
   if(!top.MoveNext()){ (top as IDisposable)?.Dispose(); job.Stack.Pop(); continue; }
   if(top.Current is IEnumerator nested){job.Stack.Push(nested);continue;}
   job.Wake=UnityEngine.Time.time+(top.Current switch {
    UnityEngine.WaitForSeconds w=>w.Seconds, UnityEngine.WaitForSecondsRealtime w=>w.Seconds, _=>0.01f});
   return;
  }
 }
 public static void Run(float seconds) {
  int frames=(int)Math.Ceiling(seconds/0.05f);
  for(int i=0;i<frames;i++) {
   UnityEngine.Time.time+=0.05f; UnityEngine.Time.realtimeSinceStartup=UnityEngine.Time.time;
   foreach(var p in Players) p.UpdateAudio();
   foreach(var j in Jobs.ToArray()) if(j.Stack.Count>0 && j.Wake<=UnityEngine.Time.time) Advance(j);
   Jobs.RemoveAll(j=>j.Stack.Count==0);
  }
 }
 public static void Reset() {
  foreach(var p in Players) p.gameObject.activeInHierarchy=false;
  Jobs.Clear(); Players.Clear(); PlayerMessageActivity.AnyBusy(); Overlaps=0;
  UnityEngine.Time.time=UnityEngine.Time.realtimeSinceStartup=0;
  StageFluxCompatibility.Reservations=0; StageFluxCompatibility.GlobalBusy=false;
  StageFluxCompatibility.Expectations.Clear(); StageFluxCompatibility.Cancellations=0;
  BepInEx.Bootstrap.Chainloader.PluginInfos.Clear(); SemiFunc.Authority=true;
  EnemyDirector.instance=new(); GameDirector.instance=new();
 }
}
namespace REPOJP.StageRoles {
 internal sealed class Entry<T>(T v) { internal T Value=v; }
 internal sealed class StageRolesConfig {
  internal Entry<bool> AnnouncementsEnabled=new(true);
  internal Entry<float> AnnouncementDelaySeconds=new(0), StageFluxAnnouncementDelaySeconds=new(0);
  internal Entry<float> InfluencerTtsQuietPeriodSeconds=new(0), InfluencerTtsInvestigateRadius=new(20);
 }
 internal sealed class RoleAssignment(PlayerAvatar player,StageRole role=StageRole.Tank) {
  internal PlayerAvatar Player=player; internal string SteamId=>Player.Id;
  internal StageRole AssignedRole=role,Role=role; internal float InfluencerNextTtsAt;
 }
 internal static class RoleCatalog { internal static string AssignmentName(StageRole assigned,StageRole effective)=>assigned.ToString(); }
 internal static class PlayerState { internal static bool IsLiving(PlayerAvatar p)=>p.gameObject.activeInHierarchy; }
 internal sealed class StageRoleController(RoleNotifier notifier) {
  internal bool HasPendingAutomaticNotifications(PlayerAvatar p)=>notifier.HasPendingNotificationsFor(p);
  internal bool TrySendPlayerMessage(PlayerAvatar p,string message)=>notifier.TrySendPlayerMessage(p,message);
 }
 internal static class StageRolesPlugin {
  internal const string StageFluxGuid="StageFlux"; internal static Logger ModLogger=new();
 }
 internal sealed class Logger {
  internal void LogDebug(string text){} internal void LogWarning(string text){} internal void LogInfo(string text){}
 }
 internal static class StageFluxCompatibility {
  internal static bool GlobalBusy; internal static int Reservations,Cancellations;
  internal static List<string> Expectations=new();
  internal static void ExpectNotification(PlayerAvatar p,string message)=>Expectations.Add(p.Id+":"+message);
  internal static void CancelExpectedNotification(PlayerAvatar p)=>Cancellations++;
  internal static void ClearNotifications(){}
  internal static bool TryGetNotificationBusy(out bool busy){busy=GlobalBusy;return true;}
  internal static bool TryReserveNotificationWindow(string owner,float seconds){Reservations++;return !GlobalBusy;}
 }
 internal static class NotificationEnemyReactionGuard { internal static bool HasActiveNotifications()=>false; }
}
''', encoding='utf-8')
(out/'Program.cs').write_text(r'''
using REPOJP.StageRoles;
using UnityEngine;
int checks=0;
void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
RoleNotifier Create(StageRolesConfig? config=null)=>new(new MonoBehaviour(),config??new());
Clock.Reset();
{
 var n=Create();var a=new PlayerAvatar("a"){Duration=5};var b=new PlayerAvatar("b");
 n.Notify(a,"First");n.Notify(a,"Second");n.NotifyResponse(b,"YourRole:Tank");
 Check(a.Sent.Count==1&&b.Sent.Count==1,"Different players send concurrently");
 Check(StageFluxCompatibility.Reservations==0,"Normal notices never reserve a global window");
 Clock.Run(4);Check(a.Sent.Count==1,"Same-player second notice waits for speech");
 Clock.Run(2);Check(a.Sent.Count==2&&Clock.Overlaps==0,"Same-player FIFO resumes without overlap");
}
Clock.Reset();
{
 var n=Create();var a=new PlayerAvatar("a");var b=new PlayerAvatar("b");
 a.ChatMessageSend("ManualChat"); n.Notify(a,"AfterChat");n.Notify(b,"OtherPlayer");
 Check(a.Sent.Count==1&&b.Sent.Count==1,"Vanilla text only blocks its own avatar");
 Clock.Run(2);Check(a.Sent.Count==2&&Clock.Overlaps==0,"Notice waits for ordinary text playback");
}
Clock.Reset();
{
 var n=Create();var a=new PlayerAvatar("a"){Delay=4};var b=new PlayerAvatar("b");
 a.ChatMessageSend("SlowSynthesis");n.Notify(a,"AfterSynthesis");n.Notify(b,"NoDelay");
 Clock.Run(3);Check(a.Sent.Count==1&&b.Sent.Count==1,"Delayed synthesis reserves only its player");
 Clock.Run(3);Check(a.Sent.Count==2,"Playback delay does not cause early overwrite");
}
Clock.Reset();
{
 var n=Create();var a=new PlayerAvatar("a");var b=new PlayerAvatar("b");
 n.Begin(new[]{new RoleAssignment(a),new RoleAssignment(b,StageRole.Runner)});
 n.NotifyResponse(a,"YourRole:Tank");n.Notify(b,"Ready");
 Clock.Run(0.1f);Check(a.Sent.Count==1&&b.Sent.Count==0,"Initial roles remain globally sequential");
 Clock.Run(1.6f);Check(b.Sent.Count==1&&b.Sent[0].Message=="Runner"&&a.Sent.Count==1,"Responses wait for assignment batch");
 Clock.Run(3);Check(a.Sent.Count==2&&b.Sent.Count==2&&Clock.Overlaps==0,"Per-player replies resume after initial roles");
 Check(StageFluxCompatibility.Reservations==2,"Only initial role messages reserve global windows");
}
Clock.Reset();
{
 var n=Create();var players=Enumerable.Range(0,20).Select(i=>new PlayerAvatar(i.ToString())).ToArray();
 n.Begin(players.Select(p=>new RoleAssignment(p)).ToArray());n.NotifyResponse(players[0],"AfterAll");
 Clock.Run(45);Check(players[0].Sent.Count==2&&players.Skip(1).All(p=>p.Sent.Count==1),"Long initial batch does not expire queued response");
 Check(Clock.Overlaps==0,"Large assignment batch has no same-player overlap");
}
Clock.Reset();
{
 var n=Create();var a=new PlayerAvatar("a");var b=new PlayerAvatar("b");
 StageFluxCompatibility.GlobalBusy=true;
 a.ChatMessageSend("StageFluxSpeech");n.Notify(a,"A");n.Notify(b,"B");
 Check(b.Sent.Count==1&&a.Sent.Count==1,"Stage Flux global reservations do not block unrelated players");
 Clock.Run(2);Check(a.Sent.Count==2,"Same-player Stage Flux speech is respected");
}
Clock.Reset();
{
 BepInEx.Bootstrap.Chainloader.PluginInfos[StageRolesPlugin.StageFluxGuid]=new();
 var n=Create();var a=new PlayerAvatar("a");StageFluxCompatibility.GlobalBusy=true;
 n.Begin(new[]{new RoleAssignment(a)});Clock.Run(2);Check(a.Sent.Count==0,"Initial roles wait for Stage Flux globally");
 StageFluxCompatibility.GlobalBusy=false;Clock.Run(1);Check(a.Sent.Count==0,"Initial Stage Flux quiet period retained");
 Clock.Run(1);Check(a.Sent.Count==1,"Initial role starts after quiet period");
}
Clock.Reset();
{
 var n=Create();var a=new PlayerAvatar("a");var b=new PlayerAvatar("b");
 a.ChatMessageSend("Busy");int sent=0;
 Check(n.NotifyCountdown(a,"0",()=>true,()=>sent++),"Countdown preempts the same player's current speech");
 Check(n.NotifyCountdown(b,"5",()=>true)&&n.NotifyCountdown(b,"4",()=>true),"Time-critical counts are never skipped due to previous speech");
 Check(sent==1&&StageFluxCompatibility.Reservations==0,"Countdown callback fires immediately without a global reservation");
}
Clock.Reset();
{
 var n=Create();var a=new PlayerAvatar("a"){Duration=10};var b=new PlayerAvatar("b");
 n.Notify(a,"LongNotice");n.Notify(a,"AfterCount");a.Duration=0.2f;
 n.NotifyCountdown(a,"2",()=>true);Clock.Run(0.9f);
 Check(a.Sent.Count==2,"Normal queue cannot enter the gap after a short count");
 n.NotifyCountdown(a,"1",()=>true);Clock.Run(0.9f);
 n.NotifyCountdown(a,"0",()=>true);n.Notify(b,"Independent");
 Check(a.Sent.Select(s=>s.Message).SequenceEqual(new[]{"LongNotice","2","1","0"})&&b.Sent.Count==1,
  "Priority count sequence keeps normal speech queued without blocking other players");
 Clock.Run(2);Check(a.Sent.Last().Message=="AfterCount", "Normal queue resumes after countdown priority ends");
}
Clock.Reset();
{
 var n=Create();var a=new PlayerAvatar("diver"){Duration=2.5f};var runtime=new DiverProbe(a,n,60);
 runtime.Tick();
 for(int i=0;i<1205;i++){Clock.Run(0.05f);runtime.Tick();runtime.Tick();}
 var expected=new[]{60,45,30,15,10,9,8,7,6,5,4,3,2,1,0};
 Check(a.Sent.Select(s=>int.Parse(s.Message)).SequenceEqual(expected),
  "Production Diver counts every 15 seconds above 10, then every second through zero, once each");
 Check(a.Sent.All(s=>Math.Abs(s.At-(60-int.Parse(s.Message)))<0.06f),
  "Counts follow their absolute deadlines within one frame, regardless of speech duration");
 var final=a.Sent.Where(s=>int.Parse(s.Message)<=10).ToArray();
 Check(final.Zip(final.Skip(1),(a,b)=>Math.Abs((b.At-a.At)-1)<0.06f).All(v=>v),
  "Final counts are one second apart without accumulating playback delays");
 Check(Clock.Overlaps==10&&runtime.State.ZeroCountdownSent,
  "Every final count interrupts the preceding 2.5-second utterance, including zero");
}
Clock.Reset();
{
 var n=Create();var a=new PlayerAvatar("diver"){Duration=0.1f};var b=new PlayerAvatar("other");
 var runtime=new DiverProbe(a,n,10);runtime.Tick();n.Notify(a,"AfterDive");n.Notify(b,"OtherPlayer");
 for(int i=0;i<203;i++){Clock.Run(0.05f);runtime.Tick();}
 Check(a.Sent.Select(s=>s.Message).SequenceEqual(Enumerable.Range(0,11).Reverse().Select(i=>i.ToString())),
  "Default 10-second dive has every count; ordinary notices stay queued between short utterances");
 Check(b.Sent.Count==1,"A full countdown does not block another player's notice");
 Clock.Run(2);Check(a.Sent.Last().Message=="AfterDive","Ordinary notice resumes after final count");
}
Clock.Reset();
{
 var n=Create();var a=new PlayerAvatar("diver");var runtime=new DiverProbe(a,n,10);
 runtime.Tick();Clock.Run(3.3f);runtime.Tick();
 Check(a.Sent.Select(s=>s.Message).SequenceEqual(new[]{"10","7"}),
  "A stalled frame sends only the actual remaining count without bursting stale numbers");
 runtime.State.Underfloor=false;Clock.Run(1);runtime.Tick();
 Check(a.Sent.Count==2,"Returning above the floor stops further countdowns");
}
Clock.Reset();
{
 var cfg=new StageRolesConfig();cfg.AnnouncementsEnabled.Value=false;var n=Create(cfg);
 var a=new PlayerAvatar("diver");var runtime=new DiverProbe(a,n,1);
 runtime.Tick();Clock.Run(1.1f);runtime.Tick();
 Check(a.Sent.Count==0&&runtime.State.ZeroCountdownSent,"Disabled countdown cannot stall timeout handling");
 cfg.AnnouncementsEnabled.Value=true;a.Fail=true;runtime=new DiverProbe(a,n,1);
 Clock.Run(1.1f);runtime.Tick();
 Check(runtime.State.ZeroCountdownSent,"Failed zero speech cannot stall timeout handling");
}
Clock.Reset();
{
 var cfg=new StageRolesConfig();cfg.AnnouncementsEnabled.Value=false;var n=Create(cfg);
 var a=new PlayerAvatar("a");var b=new PlayerAvatar("b");
 n.Notify(a,"Disabled");Check(a.Sent.Count==0,"Automatic notification setting retained");
 Check(n.TrySendPlayerMessage(a,"Achoo!")&&n.TrySendPlayerMessage(b,"LookAtMe!"),"Ability messages work with announcements disabled");
 Check(StageFluxCompatibility.Expectations.Count==0&&!n.TrySendPlayerMessage(a,"Again"),"Ability speech remains audible and cannot overwrite itself");
}
Clock.Reset();
{
 var n=Create();var a=new PlayerAvatar("a"){Duration=20};var b=new PlayerAvatar("b");
 a.ChatMessageSend("Busy");for(int i=0;i<64;i++)n.Notify(a,"Pending");
 Check(!n.CanQueueNotification(a)&&n.CanQueueNotification(b),"Queue capacity is per player");
 n.Notify(b,"Free");Check(b.Sent.Count==1,"Full queue cannot starve another player");
 n.End();Clock.Run(21);Check(a.Sent.Count==1,"Stage cleanup cancels all pending messages");
}
Clock.Reset();
{
 var n=Create();var a=new PlayerAvatar("a");bool valid=true;int sent=0,finished=0;
 a.ChatMessageSend("Busy");n.NotifyExhaustion(a,"Empty",()=>valid,()=>sent++,()=>finished++);
 valid=false;Clock.Run(2);Check(sent==0&&finished==1&&a.Sent.Count==1,"Invalidated notice is dropped with one completion callback");
 n.NotifyExhaustion(a,"Empty",()=>true,()=>sent++,()=>finished++);n.End();Clock.Run(2);
 Check(finished==2,"Completion callback runs once across send and cleanup");
}
Clock.Reset();
{
 var n=Create();var a=new PlayerAvatar("a");a.ChatMessageSend("Busy");n.Notify(a,"Old");
 n.ResetPendingLocally();n.Notify(a,"New");Clock.Run(2);
 Check(a.Sent.Select(s=>s.Message).SequenceEqual(new[]{"Busy","New"}),"Stale generation cannot steal new queue contents");
}
Clock.Reset();
{
 var n=Create();var a=new PlayerAvatar("a");a.ChatMessageSend("Busy");n.Notify(a,"AfterDisconnect");
 a.gameObject.activeInHierarchy=false;Clock.Run(2);Check(a.Sent.Count==1,"Disconnected avatars are rechecked before send");
 var b=new PlayerAvatar("b"){Fail=true};int sent=0,finished=0;
 n.NotifyExhaustion(b,"Fail",()=>true,()=>sent++,()=>finished++);Check(sent==0&&finished==1,"Transport failure finishes without marking sent");
 b.Fail=false;n.Notify(b,"Recovered");Check(b.Sent.Count==1,"Failure does not leave the player queue locked");
}
Clock.Reset();
{
 var n=Create();var cfg=new StageRolesConfig();var runtime=new EventProbe(new(n),cfg);
 var a=new PlayerAvatar("a");var b=new PlayerAvatar("b");
 var assignments=new[]{new RoleAssignment(a,StageRole.Influencer),new RoleAssignment(b,StageRole.Influencer)};
 a.ChatMessageSend("Busy");runtime.LateTick(assignments);
 Check(a.Sent.Count==1&&b.Sent.Count==1&&EnemyDirector.instance!.Investigations==1,"Influencer quiet check and enemy alert are per player");
 Clock.Run(2);runtime.LateTick(assignments);
 Check(a.Sent.Count==2&&EnemyDirector.instance!.Investigations==2,"Blocked Influencer speaks when its own channel clears");
 Check(StageFluxCompatibility.Reservations==0,"Influencer no longer reserves all players");
}
Console.WriteLine($"Notification queue checks passed: {checks}");
''', encoding='utf-8')
(out/'sources.json').write_text(json.dumps(sources, indent=2)+'\n', encoding='utf-8')
print(f'Generated {out}; {len(sources)} production source sections recorded.')
