using System.Reflection;
using Mono.Cecil;
using REPOJP.StageRoles;
using UnityEngine;
using Instrument = ValuableMusicInstrument.Instrument;
using InstrumentState = ValuableMusicInstrument.State;

int checks=0;
void Check(bool pass, string label) { checks++; if (!pass) throw new Exception(label); }
void Clock(float t) { Time.time=t; Time.unscaledTime=t; }
PlayerAvatar Player(int hp=50, float distance=0) => new() { playerHealth=new() { Health=hp }, transform=new() { position=new(distance,0,0) } };
var runtime=new MusicianRoleRuntime();
var musician=Player(); var friend=Player(50,10); var far=Player(50,10.01f); var dead=Player(0); var inactive=Player(); inactive.Active=false;
PlayerAvatar[] party={musician,friend,far,dead,inactive,musician};
void Play(string id="a", int limit=1000, float cooldown=1) => runtime.Play(musician,id,party,5,10,cooldown,limit);
Clock(0); Play();
Check(musician.playerHealth.Health==55 && friend.playerHealth.Health==55, "Self and boundary ally heal");
Check(runtime.HealingUsed("a")==10 && far.playerHealth.Health==50 && dead.playerHealth.Health==0 && inactive.playerHealth.Health==50, "Radius, death, inactive and duplicate filtering");
for (int i=0;i<100;i++) { Clock(i/100f); Play(); }
Check(runtime.HealingUsed("a")==10,"Rapid notes and continuous frames share cooldown");
Clock(1); Play(); Check(runtime.HealingUsed("a")==20,"Exact one-second boundary");
Play("b"); Check(runtime.HealingUsed("b")==10,"Performers have independent clocks and budgets");
Clock(100); Play(); Check(runtime.HealingUsed("a")==30,"A long pause produces one pulse, no catch-up burst");
Play(limit:32); Check(runtime.HealingUsed("a")==30,"Settings changes do not bypass active cooldown");
Clock(101); Play(limit:32); Check(runtime.HealingUsed("a")==32,"Final partial heal cannot exceed shared cap");
Clock(102); Play(limit:32); Check(runtime.HealingUsed("a")==32,"Exhausted budget stops healing");
Clock(103); Play(limit:40); Check(runtime.HealingUsed("a")==40,"Raised cap retains used budget");
runtime.Clear(); RoleHealingRuntime.Clear(); Clock(0); Play(); Check(runtime.HealingUsed("a")==10,"New stage resets budget and clock");
// Same identity after revival or role reacquisition keeps its ledger.
musician=Player(); party=new[]{musician}; Clock(0.5f); Play(); Check(musician.playerHealth.Health==50,"Avatar replacement cannot bypass cooldown");
Clock(1); Play(); Check(runtime.HealingUsed("a")==15,"Stable identity retains budget after avatar replacement");
Clock(2); musician.playerHealth.Health=99; Play(); Check(runtime.HealingUsed("a")==16,"Missing HP alone is charged locally");
Clock(3); Play(); Check(runtime.HealingUsed("a")==16,"Full HP costs nothing");
Clock(4); musician.playerHealth.Health=50; musician.playerHealth.OnHeal=_=>{}; Play(); Check(runtime.HealingUsed("a")==16,"Rejected local heal refunded");
Clock(5); musician.playerHealth.OnHeal=_=>{musician.playerHealth.Health+=2;throw new Exception("partial");}; Play(); Check(runtime.HealingUsed("a")==18,"Partial local exception result charged accurately");
// Uncertain remote healing retains reservations to enforce the cap.
runtime.Clear(); RoleHealingRuntime.Clear(); SemiFunc.Multiplayer=true; Clock(0);
musician=Player(); musician.photonView.IsMine=false; musician.playerHealth.OnHeal=_=>{}; party=new[]{musician}; Play(limit:12);
Check(runtime.HealingUsed("a")==5,"Remote send reserves before RPC");
Clock(1); Play(limit:12); Check(runtime.HealingUsed("a")==5,"Busy recipient does not consume another reservation");
RoleHealingRuntime.Observe(musician,50,51); Check(runtime.HealingUsed("a")==5,"Ambiguous remote increase does not refund in-flight healing");
Clock(2); Play(limit:12); Check(runtime.HealingUsed("a")==10,"Acknowledgement releases recipient");
Clock(8); Play(limit:12); Check(runtime.HealingUsed("a")==12,"Timeout cannot refund potentially delivered healing");
Clock(20); Play(limit:12); Check(runtime.HealingUsed("a")==12,"Remote healing cannot exceed cap");
RoleHealingRuntime.Clear(); Clock(21); Play(limit:12); Check(runtime.HealingUsed("a")==12,"Role-change pending cleanup preserves performer budget");
runtime.Clear(); Clock(22); Play(); runtime.Clear(); RoleHealingRuntime.Observe(musician,50,55);
Check(runtime.HealingUsed("a")==0,"Late callback cannot alter next stage ledger");
RoleHealingRuntime.Clear(); Clock(23); musician.playerHealth.OnHeal=_=>throw new Exception("uncertain send"); Play();
Check(runtime.HealingUsed("a")==5,"Uncertain remote exception retains reservation");
// The budget is combined across recipients, not 1000 HP each.
runtime.Clear(); RoleHealingRuntime.Clear(); SemiFunc.Multiplayer=false; musician=Player(); party=Enumerable.Range(0,4).Select(_=>Player()).ToArray();
for(int i=0;i<60;i++){Clock(i);foreach(var p in party)p.playerHealth.Health=50;Play();}
Check(runtime.HealingUsed("a")==1000 && party.Sum(p=>p.playerHealth.Calls)==200,"Four recipients exhaust 1000 HP in 50 pulses");
foreach(int fps in new[]{30,60,144})
{
    runtime.Clear(); RoleHealingRuntime.Clear(); musician=Player(); party=new[]{musician};
    for(int i=0;i<fps*10;i++){Clock(i/(float)fps); musician.playerHealth.Health=50; Play();}
    Check(runtime.HealingUsed("a")==50,$"Continuous play at {fps} FPS has ten one-second pulses");
}
var hook=typeof(MusicianPatches).GetMethod("ContinuousPlayed",BindingFlags.Static|BindingFlags.NonPublic)!;
var controller=StageRolesPlugin.Instance.Controller;
foreach(var type in Enum.GetValues<Instrument>())
foreach(var state in Enum.GetValues<InstrumentState>())
foreach(bool active in new[]{false,true})
foreach(bool enabled in new[]{false,true})
foreach(float loudness in new[]{0f,0.1f,0.2f})
{
    var instrument=new ValuableMusicInstrument { instrument=type, isActiveAndEnabled=enabled };
    int before=controller.Continuous;
    hook.Invoke(null,new object?[]{instrument,state,musician,active,loudness});
    bool expected=type!=Instrument.Ocarina && state==InstrumentState.Attached && active && enabled && loudness>0.1f;
    Check(controller.Continuous-before==(expected?1:0),"Supported + attached + active + audible gates");
    if(expected)Check(ReferenceEquals(controller.Performer,musician),"Attached player credited, not grabber");
}
var one=new ValuableMusicInstrument(); int count=controller.Continuous;
hook.Invoke(null,new object?[]{one,InstrumentState.Attached,null,true,1f}); Check(controller.Continuous==count,"No attached player cannot heal");
UnityEngine.Object.Objects=Array.Empty<object>();Check(!MusicianPatches.HasInstrumentContext(),"Empty scene excludes Musician");
UnityEngine.Object.Objects=new object[]{new MusicalValuableLogic()};Check(MusicianPatches.HasInstrumentContext(),"Legacy context");
foreach(var type in Enum.GetValues<Instrument>())
{
    UnityEngine.Object.Objects=new object[]{new ValuableMusicInstrument{instrument=type}};
    Check(MusicianPatches.HasInstrumentContext()==(type!=Instrument.Ocarina),"Playable new instrument context");
}
using var game=AssemblyDefinition.ReadAssembly("E:/SteamLibrary/steamapps/common/REPO/REPO_Data/Managed/Assembly-CSharp.dll");
var legacy=game.MainModule.Types.Single(t=>t.Name=="MusicalValuableLogic");
var accepted=legacy.Methods.Single(m=>m.Name=="MusicKeyPressedRPC");
Check(accepted.Body.Instructions.Any(i=>i.Operand is MethodReference m && m.Name=="MasterOnlyRPC"),"Native note RPC checks authority");
Check(accepted.Body.Instructions.Count(i=>i.Operand is MethodReference m && m.Name=="PlayKey")==1,"Accepted RPC reaches playback once");
Check(legacy.Methods.Single(m=>m.Name=="PlayKey").Parameters.Any(p=>p.Name=="grabberID" && p.ParameterType.FullName=="System.Int32"),"Native performer argument matches");
var continuous=game.MainModule.Types.Single(t=>t.Name=="ValuableMusicInstrument");
foreach(var (name,type) in new[]{("currentState","ValuableMusicInstrument/State"),("attachedPlayer","PlayerAvatar"),("active","System.Boolean"),("currentLoudness","System.Single")})
    Check(continuous.Fields.Any(f=>f.Name==name && f.FieldType.FullName==type),"Native injected field: "+name);
var sound=continuous.Methods.Single(m=>m.Name=="SoundUpdate");
foreach(string name in new[]{"LoudnessUpdate","SoundHarmonica","SoundPanFlute","SoundMouthHarp"})
    Check(sound.Body.Instructions.Any(i=>i.Operand is MethodReference m && m.Name==name),"Native audio path: "+name);
using var plugin=AssemblyDefinition.ReadAssembly("bin/Release/netstandard2.1/RoleShuffle.dll");
var startup=plugin.MainModule.Types.Single(t=>t.Name=="StageRolesPlugin").Methods.Single(m=>m.Name=="Awake");
var instructions=startup.Body.Instructions;
int registration=-1;
for(int i=0;i<instructions.Count;i++)
    if(instructions[i].Operand is TypeReference t && t.Name=="MusicianPatches")registration=i;
Check(registration>=0 && instructions.Skip(registration+1).Take(3).Any(i=>i.Operand is MethodReference m && m.Name=="PatchAll"),
    "Plugin startup installs the Musician instrument hooks");
Console.WriteLine($"PASS: {checks} Musician cooldown, budget, native hook and context checks.");
Console.WriteLine("Production sources with deterministic stand-ins and installed-game metadata; live Unity/voice transport is untested.");
