using REPOJP.StageRoles;
using UnityEngine;

int checks=0;
void Check(bool condition,string label){checks++;if(!condition)throw new Exception(label);}
foreach(string size in new[]{"Tiny","Small","Medium"})Check(PorterRules.AllowsSize("Tiny,Small,Medium",size),"Allowed size "+size);
foreach(string size in new[]{"Big","Wide","Tall","VeryTall","Unknown","", "medium"})Check(!PorterRules.AllowsSize("Tiny,Small,Medium",size),"Rejected size "+size);
Check(PorterRules.AllowsSize(" tiny, SMALL,Medium,Unknown ","Tiny"),"Whitespace and case normalized");
Check(!PorterRules.AllowsSize("Unknown","Unknown"),"Unknown setting cannot enable unknown native size");
foreach(float mass in new[]{0,-1,float.NaN,float.PositiveInfinity})Check(!PorterRules.Fits(0,mass,15),"Invalid weight rejected");
Check(PorterRules.Fits(7.5f,7.5f,15)&&!PorterRules.Fits(7.5f,7.51f,15),"Exact full load and overflow");
PorterLoad load=new();
for(int i=1;i<=300;i++)Check(load.Add(i,0.05f,15),"No count limit "+i);
Check(load.Count==300&&Math.Abs(load.Weight-15)<0.0001,"Small items use total mass");
Check(!load.Add(301,0.1f,15)&&!load.Add(1,0.01f,100),"Full and duplicate rejected");
load.Remove(1);load.Remove(1);Check(load.Count==299,"Removal idempotent");
Check(PorterRules.UnloadSeconds(7.5f,15,10)==5&&PorterRules.UnloadSeconds(15,15,10)==10,"Proportional unloading");
PorterHold hold=new();Check(!hold.Ready(1,0,3)&&!hold.Ready(1,2.99f,3)&&hold.Ready(1,3,3),"Continuous hold boundary");
Check(!hold.Ready(2,3,3),"Switching target resets hold");hold.Reset();Check(!hold.Ready(2,10,3),"Release resets hold");
PorterUnload unloading=new();Check(!unloading.Tick(true,15,15,10,0)&&!unloading.Tick(true,15,15,10,9.99f)&&unloading.Tick(true,15,15,10,10),"Full wait exact boundary");
unloading.Reset();unloading.Tick(true,7.5f,15,10,20);unloading.Tick(false,7.5f,15,10,24);
Check(!unloading.Tick(true,7.5f,15,10,25)&&unloading.ReadyAt==30,"Leaving restarts wait");

var config=new StageRolesConfig();var runtime=new PorterRuntime(config);var notices=new RoleNotifier();
var player=new PlayerAvatar();var assignment=new RoleAssignment(player);var roster=new[]{assignment};
void Tick(float t){Time.time=t;runtime.Tick(roster,notices);}
PhysGrabObject Item(float mass,ValuableVolume.Type size=ValuableVolume.Type.Small)
{
    var item=new PhysGrabObject{massOriginal=mass};item.playerGrabbing.Add(player.physGrabber);
    item.Components.Add(typeof(ValuableObject),new ValuableObject{volumeType=size});
    item.Components.Add(typeof(ValuableTestEffect),new ValuableTestEffect());
    UnityEngine.Object.World.Add(item);return item;
}
void Grab(PhysGrabObject item){foreach(var other in UnityEngine.Object.FindObjectsOfType<PhysGrabObject>())other.playerGrabbing.Clear();item.playerGrabbing.Add(player.physGrabber);}
var first=Item(7.5f);Tick(0);Tick(3.01f);
Check(first.Stored&&runtime.Weight(player.Id)==7.5f&&player.physGrabber.Releases==1,"Authority stores original and releases owner grip");
Check(!first.GetComponent<ValuableTestEffect>()!.enabled&&first.GetComponent<ValuableObject>()!.dollarValueCurrent==100,"Pause special effect and preserve original value");
Check(UpgradeService.Speeds[player.Id]==5,"Half cargo halves Speed levels");
var second=Item(7.5f);Tick(4);Tick(7.01f);Check(runtime.Weight(player.Id)==15,"Multiple cargo reaches exact capacity");
Check(UpgradeService.Speeds[player.Id]==0,"Full cargo removes all Speed upgrades");
first.GetComponent<ValuableObject>()!.dollarValueCurrent=7000.5f;
second.GetComponent<ValuableObject>()!.dollarValueCurrent=5234.8f;
Check(runtime.Value(player.Id)==12235,"Cargo totals current native values above 10000 without per-item rounding");
second.GetComponent<ValuableObject>()!.dollarValueCurrent=float.PositiveInfinity;
Check(runtime.Value(player.Id)==7000,"Invalid cargo value cannot poison HUD");
second.GetComponent<ValuableObject>()!.dollarValueCurrent=float.MaxValue;
Check(runtime.Value(player.Id)==int.MaxValue,"Extreme total saturates without integer overflow");
second.GetComponent<ValuableObject>()!.dollarValueCurrent=100;
Check(notices.Messages.Count(x=>x=="CargoFull")==1,"Reaching weight limit notifies exactly once");
var excess=Item(.1f);Tick(8);Tick(12);Check(!excess.Stored&&notices.Messages.Count(x=>x=="TooHeavy")==1,"Overflow rejection does not spam");
excess.playerGrabbing.Clear();
Time.time=13;runtime.EnemyHit(player,notices);
Check(!first.Stored&&!second.Stored&&runtime.Weight(player.Id)==0,"One hit scatters every item");
Check(first.transform.position.x!=second.transform.position.x,"Spilled originals are spread around the carrier");
Check(UpgradeService.Speeds[player.Id]==10&&runtime.Value(player.Id)==0,"Spill restores speed and clears cargo value");
Check(first.GetComponent<ValuableTestEffect>()!.enabled&&first.Teleports==1,"Returned object resumes original effect once");
Grab(first);Tick(13.2f);Tick(15.5f);Check(!first.Stored,"Spilled item cannot be reabsorbed immediately");first.playerGrabbing.Clear();
Grab(second);Tick(16.1f);Tick(19.2f);Check(second.Stored,"Dropped cargo can be stored again after the block");
player.RoomVolumeCheck.CurrentRooms.Add(new RoomVolume{Extraction=true});Tick(19.4f);Tick(24.3f);Check(second.Stored,"Half-load waits five seconds");Tick(24.5f);
Check(!second.Stored&&runtime.Weight(player.Id)==0&&notices.Messages.Contains("Unloaded"),"Unload all in delivery area after wait");
Tick(24.7f);Check(notices.Messages.Count(x=>x=="Unloaded")==1,"Empty delivery area never repeats completion notice");
Grab(first);Tick(25);Tick(30);Check(!first.Stored,"Never store inside delivery area");
player.RoomVolumeCheck.CurrentRooms.Clear();Tick(31);Tick(34.01f);Check(first.Stored,"Storage available again after leaving");
config.PorterDropOnEnemyHit.Value=false;runtime.EnemyHit(player,notices);Check(first.Stored,"Spill can be disabled in config");config.PorterDropOnEnemyHit.Value=true;
assignment.Role=StageRole.Runner;Tick(35);Check(!first.Stored&&runtime.Weight(player.Id)==0,"Role change returns all cargo");assignment.Role=StageRole.Porter;
var big=Item(1,ValuableVolume.Type.Big);Tick(36);Tick(40);Check(!big.Stored&&notices.Messages.Contains("TooLarge"),"Size restriction independent of mass");
big.playerGrabbing.Clear();var shop=Item(1);shop.Components.Add(typeof(ItemAttributes),new ItemAttributes());Tick(41);Tick(45);Check(!shop.Stored,"Shop item excluded");shop.playerGrabbing.Clear();
var shared=Item(1);shared.playerGrabbing.Add(new PhysGrabber());Tick(46);Tick(50);Check(!shared.Stored,"No stealing shared-held cargo");shared.playerGrabbing.RemoveAt(1);Tick(51);Tick(54.01f);Check(shared.Stored,"Solo holding starts fresh after shared grip");
player.Alive=false;Tick(55);Check(!shared.Stored&&runtime.Weight(player.Id)==0,"Death bypasses unloading delay");player.Alive=true;
var rollback=Item(1);rollback.FailStore=true;Tick(56);Tick(59.01f);Check(!rollback.Stored&&runtime.Weight(player.Id)==0&&rollback.GetComponent<ValuableTestEffect>()!.enabled,"Store failure rolls back reservation and effect suspension");
rollback.playerGrabbing.Clear();var retry=Item(2);Tick(60);Tick(63.01f);retry.FailRestore=true;
runtime.ReleaseAll(player.Id);Check(runtime.Weight(player.Id)==2,"Failed return keeps custody and capacity accounted");
retry.FailRestore=false;runtime.ReleaseAll(player.Id);Check(runtime.Weight(player.Id)==0&&!retry.Stored,"Return retry restores the original exactly once");
var last=Item(3);Tick(65);Tick(68.01f);runtime.Stop();Check(!last.Stored&&runtime.Weight(player.Id)==0,"Stage cleanup returns all cargo");
runtime.Stop();Check(last.Teleports==1,"Repeated cleanup does not duplicate cargo");
var failedRollback=Item(1);failedRollback.FailStore=true;failedRollback.FailRestore=true;Tick(70);Tick(73.01f);
Check(runtime.Weight(player.Id)==1,"Failed rollback retains original cargo custody");
failedRollback.FailStore=false;failedRollback.FailRestore=false;Tick(74);
Check(runtime.Weight(player.Id)==0&&!failedRollback.Stored&&failedRollback.GetComponent<ValuableTestEffect>()!.enabled,"Failed rollback is retried before new storage");
var retiring=Item(2);Tick(75);Tick(78.01f);retiring.FailRestore=true;runtime.ReleaseAll(player.Id);
retiring.FailRestore=false;Tick(79);Check(runtime.Weight(player.Id)==0&&!retiring.Stored,"Retired carrier retries even when the role remains Porter");
Check(UpgradeService.Speeds[player.Id]==10,"Cleanup restores speed");
var spillRetry=Item(4);Tick(80);Tick(83.1f);
var spillOther=Item(5);Tick(84);Tick(87.1f);spillRetry.FailRestore=true;
runtime.EnemyHit(player,notices);
Check(spillRetry.Stored&&!spillOther.Stored&&runtime.Weight(player.Id)==4,"A failed return cannot block spilling the other cargo");
spillRetry.FailRestore=false;Tick(88);
Check(!spillRetry.Stored&&runtime.Weight(player.Id)==0&&UpgradeService.Speeds[player.Id]==10,"Failed enemy spill automatically retries and restores Speed");runtime.Stop();
for(int baseline=0;baseline<=200;baseline++)
{
    int previous=baseline;
    for(int step=0;step<=60;step++)
    {
        int level=PorterRules.SpeedLevel(baseline,step/4f,15);
        Check(level>=0&&level<=previous,"Speed falls monotonically with stored weight");
        previous=level;
    }
    Check(PorterRules.SpeedLevel(baseline,0,15)==baseline&&previous==0,"Empty restores and full zeros every baseline");
}
Console.WriteLine($"PASS: {checks} Porter accounting, native-adapter lifecycle and interruption checks.");
