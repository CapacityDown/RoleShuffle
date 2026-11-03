using REPOJP.StageRoles;
using UnityEngine;
int count=0;
void Check(bool value,string message){count++;if(!value)throw new Exception(message);}
(StageRoleController C,PlayerAvatar A,PlayerAvatar B) Pair()
{
    var c=new StageRoleController();var a=new PlayerAvatar{Id="a"};var b=new PlayerAvatar{Id="b"};
    a.playerHealth.health=100;b.playerHealth.health=60;c.Start(a,b);return(c,a,b);
}
var (c,a,b)=Pair();
Check(a.playerHealth.health==160 && b.playerHealth.health==160 && a.playerHealth.maxHealth==240,"Pool starts with sum of current HP, no free healing");
c.Hit(a,30);c.Tick(.1f);Check(a.playerHealth.health==130 && b.playerHealth.health==130,"Damage mirrored once, writes do not echo");
c.Hit(b,-20);c.Tick(.2f);Check(c.Pool==150 && a.playerHealth.health==150,"Healing applied once");
c.Hit(a,20);c.Hit(b,30);c.Tick(.3f);Check(c.Pool==100,"Two owner updates before publication are both counted");
c.End();Check(a.playerHealth.health==50 && b.playerHealth.health==50 && a.playerHealth.maxHealth==120,"Unlink restores individual max and remaining proportion");
(c,a,b)=Pair();c.Hit(a,100);c.Hit(b,100);c.Tick(.1f);
Check(a.Deaths==1&&b.Deaths==1&&c.Pool==0,"Two individually nonfatal hits that exhaust the shared pool kill both");c.End();
(c,a,b)=Pair();c.Hit(a,999);c.Tick(.1f);Check(a.Deaths==1&&b.Deaths==1&&c.Pool==0,"Shared pool death invokes each native death once");
c.TwinDied(a);c.Tick(.2f);Check(a.Deaths==1&&b.Deaths==1,"Death notification recursion is idempotent");
a.Revive();a.playerHealth.UpdateHealthRPC(25,240,false,false);c.Tick(.7f);
Check(a.Revivals==1&&b.Revivals==1&&a.playerHealth.health==25&&b.playerHealth.health==25,"One rescue revives both with one healing amount");
c.End();
(c,a,b)=Pair();a.PlayerDeathRPC(-1);c.Tick(.1f);Check(b.Deaths==1&&c.Pool==0,"Non-HP death is linked");c.End();
(c,a,b)=Pair();c.Depart(b);Check(!c.IsTwin(a)&&a.playerHealth.health==80&&b.playerHealth.health==80,"Departure breaks link with proportional restoration");
(c,a,b)=Pair();var partner=c.Unlink(a);Check(partner?.Player==b&&!c.IsTwin(b),"Infection captures partner before ending sharing");
(c,a,b)=Pair();a.isCrouching=b.isCrouching=true;
c.Tick(.1f);c.Tick(2);c.Hit(a,1);c.Tick(3.2f);Check(c.Cooldown==0,"Damage interrupts rest without spending cooldown");
c.Tick(6.3f);Check(c.Pool==183 && c.Cooldown>60,"Completed rest restores ten percent once");
Check(UpgradeService.Calls.Count(x=>x.Command=="Stamina")==4&&UpgradeService.Levels["a"]["playerUpgradeStamina"]==0,"Both stamina refills restore original upgrade levels");
c.Tick(7);Check(c.Pool==183,"Continuous crouching cannot bypass cooldown");c.End();
(c,a,b)=Pair();a.isCrouching=b.isCrouching=true;c.Tick(.1f);a.transform.position=new(0,2,0);c.Tick(3.2f);
Check(c.Cooldown==0,"Vertical motion also interrupts stationary rest");c.End();
(c,a,b)=Pair();c.TwinDied(a);Check(c.Pool==160&&b.Deaths==0,"Rejected native death calls cannot trigger a linked death");c.End();
(c,a,b)=Pair();a.isSprinting=true;b.transform.position=new(20,0,0);c.Tick(.1f);a.transform.position=new(1,0,0);c.Tick(.3f);
Check(UpgradeService.Levels["a"].GetValueOrDefault("playerUpgradeSpeed")>0,"Approaching distant partner grants speed");
a.transform.position=new(0,0,0);c.Tick(.5f);Check(UpgradeService.Levels["a"]["playerUpgradeSpeed"]==0,"Moving away removes only temporary speed");
a.transform.position=new(1,0,0);c.Tick(.7f);c.End();Check(UpgradeService.Levels["a"]["playerUpgradeSpeed"]==0,"Stage end removes rendezvous boost");
(c,a,b)=Pair();var item=new PhysGrabObject();item.Holders.UnionWith(new[]{"a","b"});
c.Tick(.01f);
Check(c.TwinsStrengthUpgradeMultiplier(a)==1.5f&&c.TwinsCollisionMultiplier(item)==.5f,"Both holders receive carry strength and collision protection");
item.Components[typeof(ItemAttributes)]=new ItemAttributes();Check(c.TwinsStrengthUpgradeMultiplier(a)==1,"Shop equipment excluded");item.Components.Remove(typeof(ItemAttributes));
c.Tick(.1f);c.Tick(3.2f);item.GetComponent<RoomVolumeCheck>()!.CurrentRooms.Add(new(){Truck=true});c.TwinsCargoReleased(item);
Check(item.GetComponent<ValuableObject>()!.dollarValueCurrent==11000,"Qualified cargo gets ten percent on release into truck");
c.Tick(4);c.TwinsCargoReleased(item);Check(item.GetComponent<ValuableObject>()!.dollarValueCurrent==11000,"Delivery cannot pay repeatedly");
item.Holders.Remove("b");Check(c.TwinsStrengthUpgradeMultiplier(a)==1,"Releasing either holder removes the buff");c.End();
// The host observes a vanilla remote upgrade before any owner health packet arrives.
(c,a,b)=Pair();PunManager.instance.UpgradePlayerHealth("a");c.Tick(.1f);
Check(a.playerHealth.maxHealth==260 && b.playerHealth.maxHealth==260,"A remote Health upgrade publishes the new shared maximum without waiting for healing");
Check(c.Pool==160,"Maximum-only synchronization does not fabricate healing");
a.playerHealth.UpdateHealthRPC(180,260,false,false);c.Tick(.2f);
Check(c.Pool==180 && b.playerHealth.health==180,"Native owner upgrade healing is shared once");
c.End();
Check(a.playerHealth.maxHealth==140 && b.playerHealth.maxHealth==120,"Unlink retains the upgrade only on its original recipient");

// A host-owned upgrade heals through the ordinary owner callback, exactly once.
(c,a,b)=Pair();a.Local=true;PunManager.instance.UpgradePlayerHealth("a");c.Tick(.1f);
Check(c.Pool==180 && a.playerHealth.maxHealth==260 && b.playerHealth.maxHealth==260,"Host-owned upgrade shares maximum and its one native heal");
b.playerHealth.UpdateHealthRPC(180,240,false,false);c.Tick(.2f);
Check(c.Pool==180 && b.playerHealth.maxHealth==260,"A partner's stale maximum cannot undo an upgrade");
a.playerHealth.UpdateHealthRPC(180,280,false,false);c.Tick(.3f);
Check(a.playerHealth.maxHealth==260 && c.Pool==180,"A late client maximum cannot count the same upgrade twice");
c.End();
Check(a.playerHealth.maxHealth==140 && b.playerHealth.maxHealth==120 && a.playerHealth.health==96 && b.playerHealth.health==83,"Unlink keeps original recipient levels and shared remaining-HP proportions");

// The owner can publish its healed HP before the host processes the item event.
(c,a,b)=Pair();a.playerHealth.UpdateHealthRPC(180,260,false,false);
PunManager.instance.UpgradePlayerHealth("a");c.Tick(.1f);
Check(c.Pool==180 && a.playerHealth.maxHealth==260 && b.playerHealth.maxHealth==260,"Owner health packet before native event does not double the maximum");
c.End();

// Full-health owner packets must not lose their native upgrade healing while waiting.
(c,a,b)=Pair();c.Hit(a,-80);c.Tick(.1f);
a.playerHealth.UpdateHealthRPC(260,260,false,false);c.Tick(.2f);
Check(c.Pool==240 && b.playerHealth.maxHealth==240,"Unconfirmed larger owner maximum cannot raise the authoritative pool");
a.playerHealth.UpdateHealthRPC(260,260,false,false);
PunManager.instance.UpgradePlayerHealth("a");c.Tick(.3f);
Check(c.Pool==260 && a.playerHealth.health==260 && b.playerHealth.maxHealth==260,"An early full-health owner packet preserves its one upgrade heal, even if repeated");
c.End();
(c,a,b)=Pair();c.Hit(a,-80);c.Tick(.1f);
a.playerHealth.UpdateHealthRPC(260,260,false,false);c.Hit(b,999);c.Tick(.2f);
PunManager.instance.UpgradePlayerHealth("a");c.Tick(.3f);
Check(c.Pool==0 && a.Revivals==0 && b.Revivals==0,"Death discards deferred upgrade healing");
c.End();

// Both personal uses, or one retained AllPlayers use, have two real recipients.
(c,a,b)=Pair();PunManager.instance.UpgradePlayerHealth("a");PunManager.instance.UpgradePlayerHealth("b");c.Tick(.1f);
Check(a.playerHealth.maxHealth==280 && b.playerHealth.maxHealth==280,"Two real player increments contribute once each to shared maximum");
a.playerHealth.UpdateHealthRPC(180,260,false,false);c.Tick(.2f);
b.playerHealth.UpdateHealthRPC(180,260,false,false);c.Tick(.3f);
Check(a.playerHealth.maxHealth==280 && b.playerHealth.maxHealth==280,"Delayed packets from both upgraded owners preserve the combined maximum");
c.End();Check(a.playerHealth.maxHealth==140 && b.playerHealth.maxHealth==140,"Both recipients keep their own increment after unlink");

(c,a,b)=Pair();a.Local=true;c.Hit(a,-80);c.Tick(.1f);
PunManager.instance.UpgradePlayerHealth("a");c.Tick(.2f);
Check(c.Pool==260 && b.playerHealth.health==260 && b.playerHealth.maxHealth==260,"A full pool gains exactly 20 current and maximum HP");
PunManager.instance.UpgradePlayerHealth("a",-100);c.Tick(.3f);
Check(c.Pool==220 && a.playerHealth.maxHealth==220 && b.playerHealth.maxHealth==220,"Native clamped level reduction is applied once, without double damage");
PunManager.instance.UpgradePlayerHealth("a",-1);c.Tick(.4f);
Check(c.Pool==220 && b.playerHealth.maxHealth==220,"No-op upgrade below level zero cannot change shared health");
c.End();Check(a.playerHealth.maxHealth==100 && b.playerHealth.maxHealth==120,"Clamped reduction restores the correct individual maximum");

(c,a,b)=Pair();PunManager.instance.UpgradePlayerHealth("b",3);c.Tick(.1f);
Check(a.playerHealth.maxHealth==300 && b.playerHealth.maxHealth==300,"Multi-level Health increases use the actual level delta");
c.End();

(c,a,b)=Pair();c.Hit(a,999);c.Tick(.1f);PunManager.instance.UpgradePlayerHealth("a");c.Tick(.2f);
Check(c.Pool==0 && a.playerHealth.maxHealth==260 && b.playerHealth.maxHealth==260 && a.Revivals==0 && b.Revivals==0,"Maximum increase while dead never heals or revives the pair");
a.Revive();a.playerHealth.UpdateHealthRPC(25,240,false,false);c.Tick(.7f);
Check(c.Pool==25 && a.playerHealth.maxHealth==260 && b.playerHealth.maxHealth==260,"Paired revival retains the upgraded shared maximum");
c.End();

(c,a,b)=Pair();var outsider=new PlayerAvatar{Id="outsider"};PunManager.instance.UpgradePlayerHealth("outsider");c.Tick(.1f);
Check(a.playerHealth.maxHealth==240 && b.playerHealth.maxHealth==240 && outsider.playerHealth.maxHealth==140,"Other players' upgrades do not alter the pair");
c.Authority=false;TwinsHealthUpgradePatch.Prefix("a",1);c.Authority=true;c.Tick(.2f);
Check(a.playerHealth.maxHealth==240 && b.playerHealth.maxHealth==240,"Only the authority changes the shared maximum");
c.RoleAssignmentsReady=false;TwinsHealthUpgradePatch.Prefix("a",1);c.RoleAssignmentsReady=true;c.Tick(.3f);
Check(a.playerHealth.maxHealth==240 && b.playerHealth.maxHealth==240,"Inactive assignments do not consume upgrade deltas");
c.End();TwinsHealthUpgradePatch.Prefix("a",1);
Check(a.playerHealth.maxHealth==120 && b.playerHealth.maxHealth==120,"Upgrades after unlink leave the former pair alone");

var state=new TwinsCooperation();state.ObserveCarry(1,true,false,0,3);state.ObserveCarry(1,true,false,3,3);
Check(state.Deliver(1,true,false,10000,10,5000)==0,"Both players must be near delivery");
Check(state.Deliver(1,true,true,100000,10,5000)==5000&&state.DeliveryBonusUsed==5000,"Bonus capped before dispatch");
Check(state.Deliver(1,true,true,100000,10,5000)==0,"Same item never pays twice");
state=new();state.ObserveCarry(2,true,true,0,3);state.ObserveCarry(2,true,true,9,3);Check(state.Deliver(2,true,true,10000,10,5000)==0,"Holding in delivery area cannot qualify");
state=new();state.ObserveCarry(3,true,false,0,3);state.ObserveCarry(0,false,false,2,3);state.ObserveCarry(3,true,false,3,3);Check(state.Deliver(3,true,true,10000,10,5000)==0,"Carry duration resets on release before qualification");
Check(state.Rendezvous(0,16,true,15,5)&&state.Rendezvous(0,9,true,15,5)&&!state.Rendezvous(0,5,true,15,5)&&!state.Rendezvous(0,8,true,15,5),"Rendezvous distance hysteresis");
Console.WriteLine($"Twins: {count} production runtime and cooperation checks passed. Native networking is simulated, not a live host-only test.");
