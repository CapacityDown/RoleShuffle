using REPOJP.StageRoles;
using Photon.Pun;
using UnityEngine;
using System.Reflection;

int count=0;
void Check(bool condition,string message) { count++;if(!condition)throw new Exception(message); }
double Cos(double degrees)=>Math.Cos(degrees*Math.PI/180);
foreach (bool sneeze in new[]{false,true})
{
    double edge=sneeze?20:30, range=sneeze?25:9;
    Check(InfluenzaRules.InRange(range,Cos(edge),sneeze),"Fan/range boundary included");
    Check(!InfluenzaRules.InRange(range+0.001,1,sneeze),"Outside radius excluded");
    Check(!InfluenzaRules.InRange(1,Cos(edge+0.01),sneeze),"Outside fan excluded");
    Check(!InfluenzaRules.InRange(1,-1,sneeze),"Behind excluded");
    Check(!InfluenzaRules.Infects(sneeze?0.6:0.3,sneeze),"Exact probability boundary excluded");
    Check(Enumerable.Range(0,10000).Count(i=>InfluenzaRules.Infects(i/10000d,sneeze))==(sneeze?6000:3000),"60%/30% independent trials");
}
Check(InfluenzaRules.SneezeInterval(0)==30 && InfluenzaRules.SneezeInterval(1)==90 && InfluenzaRules.SneezeInterval(0.5)==60,"Irregular sneeze bounds and mean");
var state=new InfluenzaState(10);
Check(!state.Symptomatic(39.999f)&&state.Symptomatic(40),"Incubation exactly 30 seconds");
Check(!state.ObserveVoice(39.9f,true)&&!state.ObserveVoice(40,true),"Already ongoing speech at onset does not gain a trial");
Check(state.ObserveVoice(41,true)&&!state.ObserveVoice(41.5f,true),"One trial per utterance");
Check(!state.ObserveVoice(41.7f,true)&&state.ObserveVoice(42.5f,true),"Short speech pauses are debounced");
state.CaptureHealth(500,20);state.CaptureHealth(75,20);
Check(state.RestoredMaximum(20)==500&&state.RestoredMaximum(22)==540,"Capture once; include later upgrade changes");

Time.time=0;
var source=new RoleAssignment("source",StageRole.Influenza,0,0);
source.Player.photonView.Owner=PhotonNetwork.LocalPlayer;
var front=new RoleAssignment("front",StageRole.Lifter,0,2);
var behind=new RoleAssignment("behind",StageRole.Runner,0,-2);
var far=new RoleAssignment("far",StageRole.Tank,0,6);
var side=new RoleAssignment("side",StageRole.Medic,2,1);
var dead=new RoleAssignment("dead",StageRole.Phoenix,0,1);dead.Player.Living=false;
var controller=new StageRoleController();controller.Add(source,front,behind,far,side,dead);
controller.Tick(29.99f);
Check(source.Player.Maximum==500,"No HP cap during incubation");
controller.Tick(30);
Check(source.Player.Maximum==75&&source.Player.Health==75,"Onset clamps HP and maximum");
Check(front.Role==StageRole.Lifter,"Onset alone does not infect");
int syncs=PlayerState.SyncCount;controller.Tick(30.1f);
Check(syncs==PlayerState.SyncCount,"Stable cap does not send repeated health RPCs");
controller.OnInfluenzaChat(source.Player,"Influenza",new(){Sender=PhotonNetwork.LocalPlayer});
Check(front.Role==StageRole.Lifter,"Generated host announcements do not spread infection");
source.Player.voiceChat.clipLoudnessNoTTS=0.2f;controller.Tick(31);
Check(front.Role==StageRole.Influenza&&front.AssignedRole==StageRole.Influenza,"Infection replaces original and effective roles");
Check(controller.Onset("front")==61,"Secondary infection gets its own 30 seconds");
Check(behind.Role==StageRole.Runner&&far.Role==StageRole.Tank&&side.Role==StageRole.Medic&&dead.Role==StageRole.Phoenix,"Fan, distance, death and self exclusions");
Check(controller.OnlyCleaned("front")&&UpgradeService.Resets.SequenceEqual(new[]{"front"}),"Only infected player's old effects/upgrades cleared");
front.Player.Living=false;controller.Tick(50);front.Player.Living=true;controller.Tick(60.9f);
Check(front.Player.Maximum==500&&controller.Onset("front")==61,"Death/revival preserve incubation deadline");
controller.Tick(61);Check(front.Player.Maximum==75,"Secondary infection becomes symptomatic");
controller.Depart(front);controller.Tick(62);controller.Rejoin(front);
Check(controller.Onset("front")==61,"Reconnect cannot reset infection");
source.Player.Maximum=95;UpgradeService.HealthLevels["source"]=21;controller.Tick(62.2f);
Check(source.Player.Maximum==75,"Upgrade changes cannot lift the health cap");
source.Player.Health=30;controller.Change(source,StageRole.Runner);
Check(source.Player.Maximum==520&&source.Player.Health==30&&!controller.HasInfection("source"),"Role change restores current normal maximum without healing");
front.Player.Health=0;front.Player.Living=false;controller.Stop();
Check(front.Player.Maximum==500&&front.Player.Health==0&&!controller.HasInfection("front"),"Stage end clears illness and does not revive dead players");

Time.time=100;
var chatSource=new RoleAssignment("chat",StageRole.Influenza,0,0);
var chatTarget=new RoleAssignment("chat-target",StageRole.Runner,0,2);
var c=new StageRoleController();c.Add(chatSource,chatTarget);c.Tick(130);
c.OnInfluenzaChat(chatSource.Player,"hello",new(){Sender=new NetworkPlayer()});
Check(chatTarget.Role==StageRole.Runner,"Reject spoofed chat sender");
c.OnInfluenzaChat(chatSource.Player,"/roles",new(){Sender=chatSource.Player.photonView.Owner});
Check(chatTarget.Role==StageRole.Runner,"Role command does not infect");
c.OnInfluenzaChat(chatSource.Player,"hello",new(){Sender=chatSource.Player.photonView.Owner});
Check(chatTarget.Role==StageRole.Influenza,"Vanilla remote player's ordinary chat infects");
c.Stop();

Time.time=200;
var host=new RoleAssignment("host",StageRole.Influenza,0,0);host.Player.photonView.Owner=PhotonNetwork.LocalPlayer;
var hostTarget=new RoleAssignment("host-target",StageRole.Runner,0,2);
var h=new StageRoleController();h.Add(host,hostTarget);h.Tick(230);
var property=typeof(InfluenzaChatPatches).GetProperty("LocalSubmission",BindingFlags.Static|BindingFlags.NonPublic)!;
property.SetValue(null,true);h.OnInfluenzaChat(host.Player,"hello",new(){Sender=PhotonNetwork.LocalPlayer});property.SetValue(null,false);
Check(hostTarget.Role==StageRole.Influenza,"Local user chat accepted inside submission scope");
h.Stop();

Time.time=300;
var sneezer=new RoleAssignment("sneezer",StageRole.Influenza,0,0);
var sneezeTarget=new RoleAssignment("sneeze-target",StageRole.Runner,0,4.9f);
var s=new StageRoleController();s.Add(sneezer,sneezeTarget);s.Tick(330);s.Tick(365.9f);
Check(sneezeTarget.Role==StageRole.Runner,"No early sneeze");
s.Tick(366);
Check(sneezeTarget.Role==StageRole.Influenza&&sneezer.Player.Spoken.Contains("Achoo!"),"Scheduled sneeze uses 5m range and native voice cue");
s.Stop();
Console.WriteLine($"Influenza: {count} checks passed (production rules and runtime).");
