using REPOJP.StageRoles;
using UnityEngine;
int checks=0;
void Check(bool ok,string label) {checks++;if(!ok)throw new Exception(label);}
void At(float time,int frame) {Time.time=Time.unscaledTime=time;Time.frameCount=frame;}
var c=new StageRoleController();var p=new PlayerAvatar();var a=new RoleAssignment(p,StageRole.Brawler);c._assignments.Add(a);
var enemy=new Enemy();var weapon=new HurtCollider();weapon.Parts[typeof(ItemMelee)]=new ItemMelee();
foreach(var (index,damage) in new[]{125,150,175,200,200}.Select((d,i)=>(i,d)))
{
 At(index,index);weapon.enemyDamage=100;c.ApplyBrawlerEnemyDamage(weapon,enemy,p);Check(weapon.enemyDamage==damage,"combo ramp");
 var hp=enemy.GetComponent<EnemyHealth>()!;hp.healthCurrent=100;c.ConfirmEnemyDamage(weapon,enemy,p,100);
 weapon.enemyDamage=100;c.ApplyBrawlerEnemyDamage(weapon,enemy,p);Check(weapon.enemyDamage==damage,"blocked hit does not advance");
 hp.healthCurrent=90;c.ConfirmEnemyDamage(weapon,enemy,p,100);
 weapon.enemyDamage=100;c.ApplyBrawlerEnemyDamage(weapon,enemy,p);Check(weapon.enemyDamage==damage,"same-frame multiplier reused");
 c.ConfirmEnemyDamage(weapon,enemy,p,100);
}
At(8,8);weapon.enemyDamage=100;c.ApplyBrawlerEnemyDamage(weapon,enemy,p);Check(weapon.enemyDamage==125,"timeout exact boundary");
At(5,9);weapon.enemyDamage=100;c.ApplyBrawlerEnemyDamage(weapon,new Enemy(),p);Check(weapon.enemyDamage==125,"different target");
a.BrawlerCombo.Reset();weapon.enemyDamage=100;c.ApplyBrawlerEnemyDamage(weapon,enemy,p);Check(weapon.enemyDamage==125,"death/role reset");
Check(a.BrawlerCombo.Multiplier(1,0,0,3,.25f,2,4)==3,"cap never nerfs configured base");
var smallStep=new BrawlerCombo();for(int i=0;i<150;i++)smallStep.Confirm(1,i,i,4);
Check(Math.Abs(smallStep.Multiplier(1,150,150,1.25f,.001f,2,4)-1.4f)<.00001f,"small custom steps continue beyond 100 hits");
var gun=new HurtCollider();gun.Parts[typeof(ItemGun)]=new ItemGun();c.ApplyBrawlerEnemyDamage(gun,enemy,p);Check(gun.enemyDamage==75,"ranged penalty retained");
var objectHit=new HurtCollider();c.ApplyBrawlerEnemyDamage(objectHit,enemy,p);Check(objectHit.enemyDamage==100,"ordinary object excluded");
c.TryGetBrawlerMultiplier(weapon,out float friendly);Check(friendly==1.25f,"friendly fire stays at base multiplier");
int credit=c.Credits;enemy.GetComponent<EnemyHealth>()!.healthCurrent=0;c.ConfirmEnemyDamage(weapon,enemy,p,0);Check(c.Credits==credit,"corpse hit not credited");
SemiFunc.Authority=false;c.ConfirmEnemyDamage(weapon,enemy,p,100);Check(c.Credits==credit,"guest cannot own combo");SemiFunc.Authority=true;

a.Role=StageRole.Sniper;
foreach(var (distance,expected) in new[]{(0f,50),(3f,75),(6f,100),(12f,150),(18f,200),(50f,200)})
{enemy.transform.position=new(distance);weapon.enemyDamage=100;c.ApplySniperDamage(weapon,enemy,p);Check(weapon.enemyDamage==expected,"Sniper curve "+distance);}
enemy.transform.position=new(float.NaN);weapon.enemyDamage=100;c.ApplySniperDamage(weapon,enemy,p);Check(weapon.enemyDamage==100,"invalid position unchanged");
enemy.transform.position=new(18);weapon.Parts[typeof(ItemVehicle)]=new ItemVehicle();c.ApplySniperDamage(weapon,enemy,p);Check(weapon.enemyDamage==100,"vehicle excluded");weapon.Parts.Remove(typeof(ItemVehicle));

a.Role=StageRole.Avenger;var ally=new PlayerAvatar();At(0,0);c.NotifyAvengerAllyHit(ally,14);Check(a.AvengerHitUntil==0,"below hit threshold");
ally.transform.position=new(16);c.NotifyAvengerAllyHit(ally,20);Check(a.AvengerHitUntil==0,"outside hit radius");
ally.transform.position=new(15);c.NotifyAvengerAllyHit(ally,15);Check(a.AvengerHitUntil==10 && a.AvengerNextHitAt==20,"hit trigger boundary");
At(5,1);c.NotifyAvengerAllyHit(ally,50);Check(a.AvengerHitUntil==10,"hit cooldown");
weapon.enemyDamage=100;c.ApplyAvengerDamage(weapon,p);Check(weapon.enemyDamage==125,"hit damage bonus");
a.AvengerEmpoweredUntil=30;weapon.enemyDamage=100;c.ApplyAvengerDamage(weapon,p);Check(weapon.enemyDamage==150,"death bonus wins, never multiplies");
At(20,2);c.NotifyAvengerAllyHit(p,50);Check(a.AvengerHitUntil==10,"self cannot activate");c.NotifyAvengerAllyHit(ally,50);Check(a.AvengerHitUntil==30,"cooldown boundary");
Check(CombatRoleRules.AvengerMultiplier(30,30,30,1.5f,1.25f)==1,"exact expiration");
Check(CombatRoleRules.AvengerMultiplier(15,10,20,1.5f,2)==2,"independent timers and custom stronger hit bonus");
p.Alive=false;At(21,3);weapon.enemyDamage=100;c.ApplyAvengerDamage(weapon,p);Check(weapon.enemyDamage==100,"dead Avenger cannot attack with bonus");

for(int kills=1;kills<=8;kills++)Check(CombatRoleRules.HunterExtraOrbs(1,kills,2,false,10,false)==(kills%2==0?1:0),"Hunter guarantee "+kills);
Check(CombatRoleRules.HunterExtraOrbs(1,2,0,false,10,false)==0,"guarantee disabled");
Check(CombatRoleRules.HunterExtraOrbs(2,2,2,false,10,true)==2,"double satisfies guarantee");
Check(CombatRoleRules.HunterExtraOrbs(1,2,2,true,10,true)==9,"jackpot replaces double and guarantee");
Check(CombatRoleRules.HunterExtraOrbs(12,2,2,true,10,false)==1,"guarantee when normal exceeds jackpot");

c=new();var ghost=new PlayerAvatar {Alive=false};var g=new RoleAssignment(ghost,StageRole.Ghost);var t=new PlayerAvatar {Health=98};var t2=new PlayerAvatar {Health=99};
c._assignments.AddRange(new[]{g,new RoleAssignment(t,StageRole.Tank),new RoleAssignment(t2,StageRole.Runner)});
At(0,0);c.GhostTick();Check(t.Health==99&&t2.Health==100&&g.GhostHealingUsed==2,"Ghost heals each injured ally");
At(1,1);c.GhostTick();Check(g.GhostHealingUsed==2,"Ghost pulse interval");
At(2,2);c.GhostTick();Check(t.Health==100&&g.GhostHealingUsed==3,"no budget spent on full ally");
g.GhostHealingUsed=49;t.Health=t2.Health=80;At(4,3);c.GhostTick();Check(t.Health+t2.Health==161&&g.GhostHealingUsed==50,"shared cap never exceeded");
ghost.Alive=true;At(5,4);c.GhostTick();ghost.Alive=false;At(7,5);c.GhostTick();Check(g.GhostHealingUsed==50&&t.Health+t2.Health==161,"revive and redeath preserve budget");
g.GhostHealingUsed=0;ghost.Head=null;At(9,6);c.GhostTick();Check(g.GhostHealingUsed==0,"no missing-head aura");
ghost.Head=new(100);At(11,7);c.GhostTick();Check(g.GhostHealingUsed==0,"head position controls radius");
ghost.Head=new(0);t.Health=99;t2.Health=100;t.playerHealth.IgnoreHeal=true;At(13,8);c.GhostTick();Check(g.GhostHealingUsed==0,"local rejected heal refunds reservation");
t.playerHealth.IgnoreHeal=false;SemiFunc.Multi=true;t.photonView.IsMine=false;At(15,9);c.GhostTick();Check(g.GhostHealingUsed==1&&t.Health==99,"remote heal reserves budget before confirmation");
At(17,10);c.GhostTick();Check(g.GhostHealingUsed==1,"pending remote heal cannot duplicate");
RoleHealingRuntime.Observe(t,99,100);Check(g.GhostHealingUsed==1,"remote acknowledgment retains budget");SemiFunc.Multi=false;

UpgradeService.Levels[t.Id]=new(){{"playerUpgradeSpeed",10}};UpgradeService.Levels[t2.Id]=new(){{"playerUpgradeSpeed",200}};UpgradeService.Levels[ghost.Id]=new();
t.HeldHeads.Add(ghost);t2.HeldHeads.Add(ghost);KingUpgradeAura.Tick(c._config,c._assignments);
Check(UpgradeService.Levels[t.Id]["playerUpgradeSpeed"]==11,"carrier gets speed");Check(UpgradeService.Levels[t2.Id]["playerUpgradeSpeed"]==200,"speed cap");
KingUpgradeAura.Tick(c._config,c._assignments);Check(UpgradeService.Levels[t.Id]["playerUpgradeSpeed"]==11,"repeated tick no stacking");
UpgradeService.Levels[t.Id]["playerUpgradeSpeed"]+=3;t.HeldHeads.Clear();KingUpgradeAura.Tick(c._config,c._assignments);Check(UpgradeService.Levels[t.Id]["playerUpgradeSpeed"]==13,"release preserves acquired upgrades");
t.HeldHeads.Add(ghost);KingUpgradeAura.Tick(c._config,c._assignments);ghost.Alive=true;KingUpgradeAura.Tick(c._config,c._assignments);Check(UpgradeService.Levels[t.Id]["playerUpgradeSpeed"]==13,"revival removes carry aura");
ghost.Alive=false;KingUpgradeAura.Tick(c._config,c._assignments);KingUpgradeAura.Stop();Check(UpgradeService.Levels[t.Id]["playerUpgradeSpeed"]==13,"stage cleanup removes temporary speed");
Console.WriteLine($"PASS: {checks} combat, healing budget and carrier aura checks using production code.");
