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
for file in ('RoleNotifier.cs', 'PlayerMessageActivity.cs', 'StageRole.cs', 'SignalmanRules.cs', 'SignalmanRuntime.cs'):
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
namespace Photon.Realtime { public sealed class Player(int actor) { public int ActorNumber=actor; } }
namespace Photon.Pun {
 public static class PhotonNetwork { public static Photon.Realtime.Player? LocalPlayer => Clock.Players.FirstOrDefault(p=>p.isLocal)?.photonView.Owner; }
 public struct PhotonMessageInfo(Photon.Realtime.Player? sender) { public Photon.Realtime.Player? Sender=sender; }
 public sealed class PhotonView {
  public int ViewID; public Photon.Realtime.Player? Owner;
  public void RPC(string name,Photon.Realtime.Player receiver,object[] args) {
   if(Clock.RpcFailure) throw new Exception("RPC unavailable");
   Clock.Rpcs.Add((ViewID,receiver.ActorNumber,(string)args[0]));
   var listener=Clock.Players.Single(p=>p.photonView.Owner?.ActorNumber==receiver.ActorNumber);
   if(listener.isLocal) {
    var speaker=Clock.Players.Single(p=>p.photonView.ViewID==ViewID);
    if((string)args[0]=="") {speaker.StartAt=float.MaxValue;speaker.UpdateAudio();}
    else speaker.ChatMessageSend((string)args[0]);
   }
  }
 }
}
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
 public string Id; public string playerName; public bool isLocal;
 public UnityEngine.GameObject gameObject = new();
 public UnityEngine.Transform transform = new();
 public Photon.Pun.PhotonView photonView = new();
 public PlayerVoiceChat voiceChat = new();
 public float Duration = 1, Delay, StartAt = float.MaxValue, EndAt;
 public bool Fail, Mute;
 public List<(string Message,float At)> Sent = new();
 public PlayerAvatar(string id) { Id=playerName=id;photonView.ViewID=Clock.Players.Count+1;photonView.Owner=new(photonView.ViewID);Clock.Players.Add(this); }
 public void ChatMessageSendRPC() { }
 public void ChatMessageSend(string message) {
  if (Fail) throw new Exception("Speech unavailable");
  // Production hook on ChatMessageSpeak observes all valid native chat, including other mods.
  PlayerMessageActivity.ObserveNative(this);
  if (voiceChat.ttsAudioSource.isPlaying) Clock.Overlaps++;
  Sent.Add((message,UnityEngine.Time.time));
  StartAt = UnityEngine.Time.time + Delay; EndAt = StartAt + Duration;
  UpdateAudio();
 }
 public void UpdateAudio() => voiceChat.ttsAudioSource.isPlaying = gameObject.activeInHierarchy &&
  !Mute && UnityEngine.Time.time >= StartAt && UnityEngine.Time.time < EndAt;
}
public static class Clock {
 public static List<(int View,int Actor,string Message)> Rpcs=new(); public static bool RpcFailure;
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
  Rpcs.Clear();RpcFailure=false;InfluenzaChatPatches.LocalSubmission=false;
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
  internal Entry<bool> AnnouncementsEnabled=new(true), Enabled=new(true), SignalmanDeathAlerts=new(true);
  internal Entry<float> SignalmanCooldownSeconds=new(20),SignalmanMaximumDelaySeconds=new(5);
  internal Entry<string> SignalmanExcludedCommands=new("star,roll,gravity,void,laser,decoy"),SignalmanExcludedPrefixes=new("!");
  internal Entry<float> AnnouncementDelaySeconds=new(0), StageFluxAnnouncementDelaySeconds=new(0);
  internal Entry<float> InfluencerTtsQuietPeriodSeconds=new(0), InfluencerTtsInvestigateRadius=new(20);
 }
 internal sealed class RoleAssignment(PlayerAvatar player,StageRole role=StageRole.Tank) {
  internal PlayerAvatar Player=player; internal string SteamId=>Player.Id;
  internal StageRole AssignedRole=role,Role=role; internal float InfluencerNextTtsAt,SignalmanNextTransmitAt;
 }
 internal static class RoleCatalog {
  internal static string AssignmentName(StageRole assigned,StageRole effective)=>assigned.ToString();
  internal static bool HasCapability(StageRole assigned,StageRole role)=>assigned==role||assigned==StageRole.Superbot;
 }
 internal static class PlayerIdentity { internal static string Name(PlayerAvatar p)=>p.playerName; }
 internal static class PlayerState { internal static bool IsLiving(PlayerAvatar p)=>p.gameObject.activeInHierarchy; }
 internal sealed partial class StageRoleController(RoleNotifier notifier) {
  private readonly RoleNotifier _notifier=notifier;
  internal readonly StageRolesConfig _config=new();
  private bool _stageReady=true,_assignmentsInitialized=true;
  private static bool IsAuthority()=>SemiFunc.Authority;
  internal readonly List<RoleAssignment> _assignments=new();
  internal void Death(PlayerAvatar player)=>NotifySignalmanDeath(player);
  internal void TickRadio()=>TickSignalman();
  internal void StopRadio(){_stageReady=false;_assignmentsInitialized=false;_signalmanGeneration++;_notifier.End();}
  internal bool HasPendingAutomaticNotifications(PlayerAvatar p)=>_notifier.HasPendingNotificationsFor(p);
  internal bool TrySendPlayerMessage(PlayerAvatar p,string message)=>_notifier.TrySendPlayerMessage(p,message);
 }
 internal static class InfluenzaChatPatches { internal static bool LocalSubmission; }
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
foreach(var text in new[]{"", "  ", "/roles", " /unknown arg", "STAR", " roll ", "gravity", "void", "laser", "decoy", "!mod command"})
 Check(!SignalmanRules.IsOrdinaryChat(text,"", "!"),"Commands/empty input excluded: "+text);
foreach(var text in new[]{"出口に 戻って！", "a laser is here", "star power", "hello /roles", "Hi!"})
 Check(SignalmanRules.IsOrdinaryChat(text,"", "!"),"Ordinary sentence retained: "+text);
Check(!SignalmanRules.IsOrdinaryChat("CUSTOM", " custom ", "")&&SignalmanRules.IsOrdinaryChat("custom text","custom",""),"Extra exclusions match full input only");
Check(!SignalmanRules.IsOrdinaryChat("#command args", "", "#"),"Extra command prefixes supported");
Clock.Reset();
{
 var n=Create();var c=new StageRoleController(n);var a=new PlayerAvatar("sender");var b=new PlayerAvatar("remote");var h=new PlayerAvatar("host"){isLocal=true};
 var assignment=new RoleAssignment(a,StageRole.Signalman);c._assignments.AddRange(new[]{assignment,new RoleAssignment(b),new RoleAssignment(h)});
 string raw="  出口に 戻って！  ";a.ChatMessageSend(raw);c.OnSignalmanChat(a,raw,new(a.photonView.Owner));
 Check(Clock.Rpcs.Count(r=>r.Message==raw)==2&&h.Sent.Single().Message==raw,"Original Unicode, spaces and punctuation reach each living teammate unchanged");
 Check(Clock.Rpcs.Where(r=>r.Message==raw).All(r=>r.View==r.Actor)&&Clock.Rpcs.All(r=>r.Actor!=a.photonView.Owner!.ActorNumber),"Each recipient receives only its own private native voice, with no sender echo");
 Check(Clock.Rpcs.Count(r=>r.Message=="")==2,"Original proximity voice is stopped only for the receiving listeners");
 Check(assignment.SignalmanNextTransmitAt==20,"Accepted transmission starts the 20-second cooldown");
 int count=Clock.Rpcs.Count;c.OnSignalmanChat(a,"/roles",new(a.photonView.Owner));Clock.Run(19.8f);c.OnSignalmanChat(a,"NotYet",new(a.photonView.Owner));
 Check(Clock.Rpcs.Count==count&&assignment.SignalmanNextTransmitAt==20,"Commands and cooldown posts neither relay nor extend cooldown");
 Clock.Run(0.3f);c.OnSignalmanChat(a,"ReadyNow",new(a.photonView.Owner));Check(Clock.Rpcs.Count(r=>r.Message=="ReadyNow")==2,"Next post after 20 seconds relays normally");
 Clock.Run(22);Check(Clock.Rpcs.All(r=>r.Message!="NotYet"),"Cooldown posts are never replayed later");
}
Clock.Reset();
{
 var n=Create();var c=new StageRoleController(n);var a=new PlayerAvatar("sender"){isLocal=true};var b=new PlayerAvatar("other");
 var assignment=new RoleAssignment(a,StageRole.Signalman);c._assignments.AddRange(new[]{assignment,new RoleAssignment(b)});
 c.OnSignalmanChat(a,"AutoNotice",new(a.photonView.Owner));Check(Clock.Rpcs.Count==0,"Host automatic notifications are never relayed");
 InfluenzaChatPatches.LocalSubmission=true;c.OnSignalmanChat(a,"Manual",new(a.photonView.Owner));Check(Clock.Rpcs.Any(r=>r.Message=="Manual"),"Host manual chat is accepted");
 Check(!PrivatePlayerSpeech.IsDispatching,"Private transport restores its reentrancy guard");
}
Clock.Reset();
{
 var n=Create();var c=new StageRoleController(n);var a=new PlayerAvatar("sender");var b=new PlayerAvatar("receiver");
 var assignment=new RoleAssignment(a,StageRole.Signalman);c._assignments.AddRange(new[]{assignment,new RoleAssignment(b)});
 c.OnSignalmanChat(a,"Forged",new(b.photonView.Owner));c.OnSignalmanChat(a,"MissingSender",new(null));
 Check(Clock.Rpcs.Count==0&&assignment.SignalmanNextTransmitAt==0,"Only the avatar owner can submit radio chat");
 assignment.Role=StageRole.Runner;c.OnSignalmanChat(a,"WrongRole",new(a.photonView.Owner));
 Check(Clock.Rpcs.Count==0,"Non-Signalman chat stays ordinary");
 assignment.Role=StageRole.Signalman;b.gameObject.activeInHierarchy=false;c.OnSignalmanChat(a,"NoAudience",new(a.photonView.Owner));
 Check(Clock.Rpcs.Count==0&&assignment.SignalmanNextTransmitAt==0,"No living audience consumes no cooldown");
 b.gameObject.activeInHierarchy=true;SemiFunc.Authority=false;c.OnSignalmanChat(a,"Client",new(a.photonView.Owner));
 Check(Clock.Rpcs.Count==0,"Non-host instances never relay");
}
Clock.Reset();
{
 var n=Create();var c=new StageRoleController(n);var a=new PlayerAvatar("sender");var b=new PlayerAvatar("receiver"){Duration=1};
 var assignment=new RoleAssignment(a,StageRole.Signalman);c._assignments.AddRange(new[]{assignment,new RoleAssignment(b)});
 b.ChatMessageSend("Busy");c.OnSignalmanChat(a,"Queued",new(a.photonView.Owner));
 Check(Clock.Rpcs.Count==0,"Private radio waits for the receiver's native speech");
 a.ChatMessageSend("LaterChat");Clock.Run(2);
 Check(Clock.Rpcs.Count==1&&Clock.Rpcs[0].Message=="Queued","A later ordinary chat from the sender is never stopped by delayed radio");
 Check(PlayerMessageActivity.IsBusy(b),"Private remote playback reserves that player's notification channel");
 n.NotifyCountdown(b,"5",()=>true);Clock.Run(2);
 Check(!PlayerMessageActivity.IsBusy(b),"A priority count interrupts and clears the private playback reservation");
}
Clock.Reset();
{
 var n=Create();var c=new StageRoleController(n);var a=new PlayerAvatar("sender");var b=new PlayerAvatar("receiver"){Duration=10};
 c._assignments.AddRange(new[]{new RoleAssignment(a,StageRole.Signalman),new RoleAssignment(b)});
 b.ChatMessageSend("Busy");c.OnSignalmanChat(a,"Stale",new(a.photonView.Owner));Clock.Run(6);
 Check(Clock.Rpcs.Count==0&&!n.HasPendingNotificationsFor(b),"Stale radio expires promptly without holding the queue for 15 seconds");
 Clock.Run(5);Check(Clock.Rpcs.Count==0,"Stale radio is never sent after the listener becomes free");
}
Clock.Reset();
{
 var settings=new StageRolesConfig();settings.SignalmanMaximumDelaySeconds.Value=30;
 var n=Create(settings);var c=new StageRoleController(n);c._config.SignalmanMaximumDelaySeconds.Value=30;
 var a=new PlayerAvatar("sender");var b=new PlayerAvatar("receiver"){Duration=20};
 c._assignments.AddRange(new[]{new RoleAssignment(a,StageRole.Signalman),new RoleAssignment(b)});
 b.ChatMessageSend("LongSpeech");c.OnSignalmanChat(a,"LongWait",new(a.photonView.Owner));Clock.Run(21);
 Check(Clock.Rpcs.Any(r=>r.Message=="LongWait"),"Configured 30-second radio wait exceeds the ordinary 15-second queue limit");
}
foreach(string change in new[]{"role","stage","depart","dead"}) {
 Clock.Reset();var n=Create();var c=new StageRoleController(n);var a=new PlayerAvatar("sender");var b=new PlayerAvatar("receiver");
 var assignment=new RoleAssignment(a,StageRole.Signalman);c._assignments.AddRange(new[]{assignment,new RoleAssignment(b)});
 b.ChatMessageSend("Busy");c.OnSignalmanChat(a,"Cancel",new(a.photonView.Owner));
 if(change=="role")assignment.Role=StageRole.Runner;else if(change=="stage")c.StopRadio();else if(change=="depart")c._assignments.Remove(assignment);else a.gameObject.activeInHierarchy=false;
 Clock.Run(2);Check(Clock.Rpcs.Count==0,"Pending radio cancelled after "+change);
}
Clock.Reset();
{
 var n=Create();var c=new StageRoleController(n);var a=new PlayerAvatar("radio");var b=new PlayerAvatar("Fallen Ally");
 c._assignments.AddRange(new[]{new RoleAssignment(a,StageRole.Signalman),new RoleAssignment(b)});
 b.gameObject.activeInHierarchy=false;c.Death(b);c.Death(b);
 Check(Clock.Rpcs.Count==1&&Clock.Rpcs[0].Message=="Down:FallenAlly"&&Clock.Rpcs[0].Actor==a.photonView.Owner!.ActorNumber,"Death alert is short, private, and once per death");
 Clock.Run(10);b.gameObject.activeInHierarchy=true;c.TickRadio();b.gameObject.activeInHierarchy=false;c.Death(b);
 Check(Clock.Rpcs.Count==2,"Revival rearms death alerts");
}
Clock.Reset();
{
 var n=Create();var c=new StageRoleController(n);var a=new PlayerAvatar("sender");var b=new PlayerAvatar("receiver");
 c._assignments.AddRange(new[]{new RoleAssignment(a,StageRole.Signalman),new RoleAssignment(b)});
 Clock.RpcFailure=true;c.OnSignalmanChat(a,"Failed",new(a.photonView.Owner));
 Check(!PrivatePlayerSpeech.IsDispatching&&!n.HasPendingNotificationsFor(b),"Transport failure releases dispatch and queue state");
}
Console.WriteLine($"Notification and Signalman checks passed: {checks}");
''', encoding='utf-8')
(out/'sources.json').write_text(json.dumps(sources, indent=2)+'\n', encoding='utf-8')
print(f'Generated {out}; {len(sources)} production source sections recorded.')
