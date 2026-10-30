using System.Reflection;
using REPOJP.StageRoles;
using UnityEngine;
using HarmonyLib;
int checks=0;
void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
void Near(float actual,float expected,string label)=>Check(Math.Abs(actual-expected)<0.0001f,label);
object? Hook(string name,params object?[] arguments)=>typeof(HunterBatteryPatches).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,arguments);
(ItemBattery battery,ItemEquippable equipped,PhysGrabObject grabbed) Setup(Component weapon,PlayerAvatar owner){
 var battery=new ItemBattery();var equipped=new ItemEquippable{Owner=owner};var grabbed=new PhysGrabObject{lastPlayerGrabbing=owner};
 var family=new List<Component>{weapon,battery,equipped,grabbed};foreach(var part in family)part.Family=family;
 return(battery,equipped,grabbed);
}
var hunter=new PlayerAvatar{Hunter=true};
// All continuous-drain weapon families apply the configured rate once per tick.
foreach(var weapon in new Component[]{new ItemLeafBlower(),new ItemCartLaser(),new ItemStaffVoid(),new ItemStaffTorque(),new ItemStaffZeroGravity(),new ItemCartCannon(),new ItemGun(),new ItemMelee()}){
 var x=Setup(weapon,hunter);
 foreach(float factor in new[]{0f,0.25f,0.75f,1f}){
  StageRolesPlugin.Instance.Settings.ClampedHunterBatteryConsumption=factor;
  object?[] arguments={x.battery,4f};Hook("ItemBatteryDrainContinuousPrefix",arguments);
  Near((float)arguments[1]!,4f*factor,"Continuous weapon rate is scaled exactly once");
  // Native v0.5.0 applies rate * 2 * dt; the duration must remain unchanged.
  Near(100f-(float)arguments[1]!*2f*0.5f,100f-4f*factor,"Per-tick battery loss retains elapsed time");
 }
 x.equipped.Owner=null;float fallback=4;HunterBatteryRuntime.TryScaleDrain(x.battery,ref fallback);Near(fallback,4,"Last holder fallback remains valid");
}
StageRolesPlugin.Instance.Settings.ClampedHunterBatteryConsumption=0.75f;
var other=Setup(new ItemLeafBlower(),new PlayerAvatar());float rate=4;Check(!HunterBatteryRuntime.TryScaleDrain(other.battery,ref rate),"Other role rejected");Near(rate,4,"Other role keeps native rate");
var nonweapon=Setup(new Component(),hunter);rate=4;Check(!HunterBatteryRuntime.TryScaleDrain(nonweapon.battery,ref rate),"Nonweapon rejected");Near(rate,4,"Nonweapon unchanged");
var gun=new ItemGun();var weaponState=Setup(gun,hunter);
SemiFunc.Authority=false;rate=4;Check(!HunterBatteryRuntime.TryScaleDrain(weaponState.battery,ref rate),"Client does not apply host bonus");Near(rate,4,"Client rate unchanged");SemiFunc.Authority=true;
HunterBatteryRuntime.Clear();int consumed=0;for(int i=0;i<4;i++){int bars=1;HunterBatteryRuntime.ScaleFullBarDrain(weaponState.battery,ref bars);consumed+=bars;}Check(consumed==3,"Four full-bar shots consume three bars at 75 percent");
weaponState.battery.batteryLife=100;var snapshot=HunterBatteryRuntime.Capture(gun);weaponState.battery.batteryLife=80;HunterBatteryRuntime.Refund(snapshot);Near(weaponState.battery.batteryLife,85,"Direct gun/melee loss consumes 75 percent");
weaponState.battery.batteryLife=90;HunterBatteryRuntime.Refund(snapshot);Near(weaponState.battery.batteryLife,92.5f,"Only actual consumed amount is refunded");
weaponState.battery.batteryLife=100;HunterBatteryRuntime.Refund(snapshot);Near(weaponState.battery.batteryLife,100,"No consumption adds no battery");
gun.batteryDrainFullBar=true;object?[] capture={gun,null};Hook("ItemGunShootRpcPrefix",capture);Check(!((HunterBatteryState)capture[1]!).Active,"Full-bar gun skips direct-consumption refund");
var patches=typeof(HunterBatteryPatches).GetMethods(BindingFlags.Static|BindingFlags.NonPublic).SelectMany(m=>m.GetCustomAttributes<HarmonyPatch>()).ToArray();
Check(patches.Count(p=>p.Type==typeof(ItemBattery)&&p.Name=="DrainContinuous")==1,"Exactly one central continuous-drain hook");
Check(!patches.Any(p=>p.Type==typeof(ItemLeafBlower)||p.Name=="Drain"),"No duplicate leaf-blower or scheduled-drain discount");
Console.WriteLine($"PASS: {checks} Hunter battery checks using production handlers and isolated native adapters.");
