using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Mono.Cecil;
using REPOJP.StageRoles;
int checks = 0;
void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
string path = args.Length > 0 ? args[0] : @"E:\SteamLibrary\steamapps\common\REPO\REPO_Data\Managed\Assembly-CSharp.dll";
using var game = AssemblyDefinition.ReadAssembly(path);
var nativeType = game.MainModule.Types.Single(t => t.Name == "ShopKeeper");
var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType == typeof(OpCode))
    .Select(f => (OpCode)f.GetValue(null)!).GroupBy(o => o.Value).ToDictionary(g => g.Key, g => g.First());
object? Operand(object? value)
{
    if (value is FieldReference f && f.DeclaringType.Name == "ShopKeeper" && f.Name is "itemDiscountAttempts" or "itemReactObject")
        return typeof(ShopKeeper).GetField(f.Name)!;
    if (value is MethodReference m && m.DeclaringType.Name == "Random" && m.Name == "Range" && m.ReturnType.Name == "Int32")
        return typeof(UnityEngine.Random).GetMethod("Range")!;
    return value;
}
var cases = new (string Native, Func<IEnumerable<CodeInstruction>, IEnumerable<CodeInstruction>> Patch, int Added)[]
{
    ("ItemPresentationTargetValid", NegotiatorPatches.TargetTranspiler, 2),
    ("ItemNoticeStart", NegotiatorPatches.NoticeTranspiler, 2),
    ("TryStartItemDiscount", NegotiatorPatches.StartTranspiler, 4),
    ("ItemVerdictPositiveGet", NegotiatorPatches.VerdictTranspiler, 4)
};
foreach (var test in cases)
{
    var native = nativeType.Methods.Single(m => m.Name == test.Native);
    var code = native.Body.Instructions.Select(i => new CodeInstruction(opcodes[i.OpCode.Value], Operand(i.Operand))).ToList();
    var patched = test.Patch(code).ToList();
    Check(patched.Count == code.Count + test.Added, test.Native + ": exact patch shape against installed game");
    Check(patched.Where(code.Contains).SequenceEqual(code), test.Native + ": retain all native validation, bookkeeping and RPCs");
    bool rejected = false;
    try { test.Patch(new[] { new CodeInstruction(OpCodes.Ret) }).ToList(); } catch (InvalidOperationException) { rejected = true; }
    Check(rejected, test.Native + ": fail on incompatible game code");
    if (test.Native == "ItemVerdictPositiveGet")
    {
        int force = patched.FindIndex(i => i.Calls(AccessTools.Method(typeof(NegotiatorPatches), "Positive")));
        int shown = patched.FindIndex(i => i.operand is MethodReference m && m.Name == "ItemShownSet");
        Check(force > 0 && force < shown, "Guarantee applied before rejection bookkeeping");
    }
}
// Exercise the real runtime with lightweight Unity/game adapters, not a rewritten model.
var runtime = StageRolesPlugin.Instance.Negotiator;
var config = new StageRolesConfig(); runtime.Initialize(config);
var one = new PlayerAvatar(); var other = new PlayerAvatar { Id = "b", Name = "Other" };
SemiFunc.Players.AddRange(new[] { one, other });
void Tick() { UnityEngine.Time.unscaledTime += 1; typeof(NegotiatorRuntime).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(runtime, null); }
void Grant(string id, StageRole role = StageRole.Negotiator)
{
    var assignment = new RoleAssignment(id, role);
    runtime.Capture(new[] { assignment });
    runtime.EnemyDefeated(assignment, 3);
}
void Hook(string name, params object?[] args) => typeof(NegotiatorPatches).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, args);
runtime.BeginStage(); Grant(one.Id); Grant(one.Id); Grant(other.Id, StageRole.Runner);
Check(runtime.Remaining(one.Id) == 1 && runtime.Remaining(other.Id) == 0, "One Tier3 reward per stage; no reward for unrelated role");
Check(!runtime.CanUse(one), "Cannot spend before shop");
StageRolesPlugin.Instance.Controller._notifier.Sent.Clear();
runtime.SceneChanged(); Tick();
Check(runtime.Remaining(one.Id) == 1, "Normal stage transition retains earned right");
SemiFunc.Shop = true; var keeper = new ShopKeeper(); ShopKeeper.instance = keeper;
SemiFunc.Generated = false; Tick(); Check(!runtime.CanUse(one), "Do not enter shop during generation");
SemiFunc.Generated = true; RoleNotifier.Ready = false; Tick();
Check(runtime.HasShopRole(one) && !runtime.HasShopRole(other), "Only entitled players receive shop roles");
Check(RoleAssignmentSync.Last.Count == 1 && RoleAssignmentSync.Last[0].SteamId == one.Id, "Shop role list identifies holder");
Check(RoleAbilitySync.Last.Single().Values.Single().Remaining == 1, "Shop budget 1/2");
Check(StageRolesPlugin.Instance.Controller._notifier.Sent.Count == 0, "Entry notice waits for native game readiness");
RoleNotifier.Ready = true; Tick(); Tick();
Check(StageRolesPlugin.Instance.Controller._notifier.Sent.Count == 1, "Exactly one entry announcement per avatar");
Check(StageRolesPlugin.Instance.Controller._notifier.Sent[0] == (one, "Negotiator"), "Vanilla holder speaks short role identifier");
for (int attempts = 0; attempts <= 3; attempts++)
{
    Check(NegotiatorPatches.AttemptCount(attempts, one) == 0 && NegotiatorPatches.SpendAttempt(attempts + 1, one) == attempts, "Guaranteed use remains available and preserves native shared attempts");
    Check(NegotiatorPatches.AttemptCount(attempts, other) == attempts && NegotiatorPatches.SpendAttempt(attempts + 1, other) == attempts + 1, "Ordinary negotiation limits and spending unchanged");
}
one.Alive = false; Check(!runtime.CanUse(one), "Dead players cannot initiate negotiation"); one.Alive = true;
var item = new PhysGrabObject();
Hook("Started", keeper, false, item, one, ShopKeeper.State.NoticeItem, item);
Check(!runtime.Guaranteed(keeper, item), "Rejected presentation does not reserve a token");
Hook("Started", keeper, true, item, one, ShopKeeper.State.ScanObject, item);
Check(runtime.Guaranteed(keeper, item), "Accepted native scan reserves token for its item");
Check(!runtime.Guaranteed(new ShopKeeper(), item) && !runtime.Guaranteed(keeper, new PhysGrabObject()), "No cross-keeper or cross-item guarantee");
Check(NegotiatorPatches.Positive(false, keeper, item), "Failure becomes success before dislike recording");
Hook("AfterDiscount", keeper, item, item.Item.value); Check(runtime.Remaining(one.Id) == 1, "No price reduction means no consumption");
Hook("Release", keeper); Check(runtime.CanUse(one) && !runtime.Guaranteed(keeper, item), "Interrupted scan can retry without consuming");
runtime.Reserve(keeper, item, one);
Check(runtime.Guaranteed(keeper, item), "Living presenter's accepted negotiation is guaranteed");
item.Item.value = 18; Hook("AfterDiscount", keeper, item, 30);
Check(runtime.Remaining(one.Id) == 0 && !runtime.CanUse(one), "Actual discount consumes exactly once");
Hook("AfterDiscount", keeper, item, 30); Hook("Release", keeper); Tick();
Check(StageRolesPlugin.Instance.Controller._notifier.Sent.Count(n => n.Message == "Discounted") == 1, "No duplicate completion notification");
Check(runtime.HasShopRole(one) && RoleAbilitySync.Last.Single().Values.Single().Remaining == 0, "Used holder stays identifiable with 0/2");
Check(!NegotiatorPatches.Positive(false, keeper, item) && NegotiatorPatches.Positive(true, keeper, item), "Later attempts retain native chance");
Grant(one.Id); Check(runtime.Remaining(one.Id) == 0, "Cannot reacquire in same shop");
runtime.SceneChanged(); SemiFunc.Shop = false; Check(!runtime.HasShopRole(one) && runtime.Remaining(one.Id) == 0, "Leaving shop expires displayed right");
runtime.BeginStage(); Grant(one.Id); Grant(other.Id, StageRole.Superbot); runtime.SceneChanged(); SemiFunc.Shop = true; Tick();
Check(runtime.Remaining(other.Id) == 1, "Inherited capability uses same entitlement");
runtime.Reserve(keeper, item, one); Check(!runtime.CanUse(other), "Prevent overlapping reservations"); runtime.Release(keeper);
SemiFunc.Players.Remove(one); Tick(); Check(RoleAssignmentSync.Last.Count == 1, "Disconnected holder is not shown");
var rejoined = new PlayerAvatar { Id = one.Id, Name = "Rejoined" }; SemiFunc.Players.Add(rejoined); Tick();
Check(runtime.CanUse(rejoined) && RoleAssignmentSync.Last.Any(r => r.PlayerName == "Rejoined"), "Same player rejoins with entitlement and new avatar identity");
runtime.SceneChanged(); SemiFunc.Shop = false; Check(runtime.Remaining(one.Id) == 0 && runtime.Remaining(other.Id) == 0, "Unused rights expire too");
SemiFunc.Players.Remove(rejoined); SemiFunc.Players.Add(one);
runtime.BeginStage(); Grant(one.Id); Hook("ResetRun"); Check(runtime.Remaining(one.Id) == 0, "New run clears pending rights");
foreach (string context in new[] { "menu", "lobby", "arena", "client", "disabled" })
{
    runtime.BeginStage(); Grant(one.Id);
    SemiFunc.MainMenu = context == "menu"; SemiFunc.LobbyMenu = context == "lobby"; SemiFunc.Arena = context == "arena";
    SemiFunc.Authority = context != "client"; config.Enabled.Value = context != "disabled";
    Tick(); Check(runtime.Remaining(one.Id) == 0, "Clear stale rights: " + context);
    SemiFunc.MainMenu = SemiFunc.LobbyMenu = SemiFunc.Arena = false; SemiFunc.Authority = config.Enabled.Value = true;
}
// A different effective role revokes the entitlement, not just its stage HUD.
runtime.Reset(); runtime.BeginStage(); Grant(one.Id); Grant(other.Id);
Grant(one.Id, StageRole.Runner);
Check(runtime.Remaining(one.Id) == 0 && runtime.Remaining(other.Id) == 1,
    "Assignment capture revokes only the changed player's right");
Grant(one.Id);
Check(runtime.Remaining(one.Id) == 0, "Changing back cannot reacquire within the same stage");
runtime.Capture(Array.Empty<RoleAssignment>());
Check(runtime.Remaining(other.Id) == 1, "An empty assignment list does not revoke shop carryover");
runtime.RoleChanged(new RoleAssignment(other.Id, StageRole.Superbot));
Check(runtime.Remaining(other.Id) == 1, "Inherited capability retains its right");
SemiFunc.Authority = false;
runtime.RoleChanged(new RoleAssignment(other.Id, StageRole.Runner));
Check(runtime.Remaining(other.Id) == 1, "Non-host cannot revoke rights");
SemiFunc.Authority = true;
runtime.RoleChanged(new RoleAssignment(other.Id, StageRole.Influenza));
Check(runtime.Remaining(other.Id) == 0, "Infection's common role-change callback revokes the right");

runtime.BeginStage(); Grant(one.Id); Grant(other.Id);
Check(runtime.Remaining(one.Id) == 1 && runtime.Remaining(other.Id) == 1, "A new stage can grant fresh rights");
runtime.RoleChanged(new RoleAssignment(one.Id, StageRole.Runner));
runtime.SceneChanged(); SemiFunc.Shop = true; Tick();
Check(!runtime.HasShopRole(one) && !runtime.CanUse(one), "Revoked holder cannot negotiate in the next shop");
Check(RoleAssignmentSync.Last.All(r => r.SteamId != one.Id) &&
    RoleAbilitySync.Last.All(s => s.SteamId != one.Id), "Revoked holder has no shop role or resource HUD");
Check(runtime.HasShopRole(other) && runtime.CanUse(other), "Unchanged holder carries the right into the shop");
runtime.Reserve(keeper, item, other);
Check(runtime.Guaranteed(keeper, item), "Other holder can reserve a discount");
runtime.RoleChanged(new RoleAssignment(other.Id, StageRole.Runner));
Check(!runtime.Guaranteed(keeper, item) && !runtime.HasShopRole(other),
    "Revocation invalidates a reserved negotiation and shop role");
Tick();
Check(RoleAssignmentSync.Last.Count == 0 && RoleAbilitySync.Last.Count == 0,
    "Shop publication removes revoked holders entirely");
runtime.SceneChanged(); SemiFunc.Shop = false;
// Confirmed native death hooks apply in stages and shops, with no revival refund.
var avatarType = game.MainModule.Types.Single(t => t.Name == "PlayerAvatar");
Check(avatarType.Fields.Any(f => f.Name == "deadSet" && f.FieldType.FullName == "System.Boolean") &&
    avatarType.Methods.Any(m => m.Name == "PlayerDeathRPC"), "Installed death callback and confirmation field exist");
runtime.Reset(); runtime.BeginStage(); Grant(one.Id); Grant(other.Id);
Hook("Died", one, false);
Check(runtime.Remaining(one.Id) == 1, "Rejected death RPC does not revoke");
SemiFunc.Authority = false; Hook("Died", one, true); SemiFunc.Authority = true;
Check(runtime.Remaining(one.Id) == 1, "Client death callback cannot mutate host rights");
one.Alive = false; Hook("Died", one, true);
Check(runtime.Remaining(one.Id) == 0 && runtime.Remaining(other.Id) == 1, "Stage death revokes only the victim's right");
Hook("Died", one, true); one.Alive = true; Grant(one.Id);
Check(runtime.Remaining(one.Id) == 0, "Duplicate death and revival cannot restore the grant");
runtime.SceneChanged(); SemiFunc.Shop = true; Tick();
Check(!runtime.HasShopRole(one) && runtime.HasShopRole(other), "Dead former holder carries no right into the shop");
runtime.Reserve(keeper, item, other);
Check(runtime.Guaranteed(keeper, item), "Surviving holder can still negotiate");
other.Alive = false; Hook("Died", other, true);
Check(!runtime.Guaranteed(keeper, item) && runtime.Remaining(other.Id) == 0, "Shop death cancels the reserved guarantee");
other.Alive = true; Grant(other.Id); Tick();
Check(!runtime.CanUse(other) && !runtime.HasShopRole(other), "Shop revival does not restore the right");
Check(RoleAssignmentSync.Last.Count == 0 && RoleAbilitySync.Last.Count == 0, "Death removes shop roles and resource HUD rows");
runtime.SceneChanged(); SemiFunc.Shop = false; runtime.BeginStage(); Grant(other.Id);
Check(runtime.Remaining(other.Id) == 1, "A later stage may grant a new right after death");

// Real earning adapter: no free rights, exact size/tier limits and personal carried distance.
RoleAssignment Fresh(StageRole role = StageRole.Negotiator)
{
    runtime.Reset(); SemiFunc.Shop = false; one.Alive = true; one.InTruck = false;
    one.transform.position = new(0,0,0); UnityEngine.Time.time += 1;
    runtime.BeginStage(); var a = new RoleAssignment(one.Id, role); runtime.Capture(new[] { a }); return a;
}
PhysGrabObject Cargo(RoleAssignment a, ValuableVolume.Type size = ValuableVolume.Type.Big)
{
    var c = new PhysGrabObject { Valuable = new() { volumeType = size }, Holder = a.Player };
    UnityEngine.Object.Items = new[] { c }; return c;
}
void Move(RoleAssignment a, float x)
{
    a.Player.transform.position = new(x,0,0); UnityEngine.Time.time += .26f; runtime.TickStage(new[] { a });
}
void Carry20(RoleAssignment a) { float x = a.Player.transform.position.x; for (int i=0;i<=10;i++) Move(a,x+2*i); }
void Deliver(RoleAssignment a, PhysGrabObject c, bool truck = false)
{
    c.Holder = null; c.Rooms.CurrentRooms.Clear(); c.Rooms.CurrentRooms.Add(new() { Extraction = !truck, Truck = truck });
    runtime.CargoReleased(a,c);
}
var active = Fresh();
Check(runtime.Remaining(one.Id) == 0, "Receiving role grants no free right");
for (int tier=0;tier<3;tier++) { runtime.EnemyDefeated(active,tier); Check(runtime.Remaining(one.Id)==0,"Only native Tier3 qualifies: "+tier); }
runtime.EnemyDefeated(active,4); Check(runtime.Remaining(one.Id)==0,"Unknown tier rejected");
runtime.EnemyDefeated(active,3); runtime.EnemyDefeated(active,3);
Check(runtime.Remaining(one.Id)==1,"Tier3 repeat kills earn only one");
var cargo = Cargo(active); Carry20(active); Deliver(active,cargo);
Check(runtime.Remaining(one.Id)==2,"Tier3 then delivery reaches two");
cargo.Holder=one; Carry20(active); Deliver(active,cargo); runtime.EnemyDefeated(active,3);
Check(runtime.Remaining(one.Id)==2,"Repeated sources cannot exceed two");
foreach (ValuableVolume.Type size in Enum.GetValues<ValuableVolume.Type>())
{
    active=Fresh(); cargo=Cargo(active,size); Carry20(active); Deliver(active,cargo);
    bool large=size is ValuableVolume.Type.Big or ValuableVolume.Type.Wide or ValuableVolume.Type.Tall or ValuableVolume.Type.VeryTall;
    Check(runtime.Remaining(one.Id)==(large?1:0), "Native size eligibility: "+size);
}
active=Fresh(); cargo=Cargo(active); Carry20(active); Deliver(active,cargo,true);
Check(runtime.Remaining(one.Id)==0,"Truck delivery never earns a right");
cargo.Rooms.CurrentRooms.Add(new() { Extraction=true }); runtime.CargoReleased(active,cargo);
Check(runtime.Remaining(one.Id)==0,"Overlapping truck/extraction remains excluded");
cargo.Rooms.CurrentRooms.Clear(); cargo.Holder=one; Move(active,20); Deliver(active,cargo);
Check(runtime.Remaining(one.Id)==1,"Valid extraction after prior truck delivery");
runtime.EnemyDefeated(active,3); Check(runtime.Remaining(one.Id)==2,"Delivery then Tier3 independently earns second right");
active=Fresh(); cargo=Cargo(active); Move(active,0); for(int i=1;i<=9;i++) Move(active,i*2); Move(active,19.9f); Deliver(active,cargo);
Check(runtime.Remaining(one.Id)==0,"Below 20 metres rejected");
cargo.Holder=one; Move(active,19.9f); one.transform.position=new(20,0,0);
cargo.Rooms.CurrentRooms.Clear(); cargo.Rooms.Refresh=()=>cargo.Rooms.CurrentRooms.Add(new(){Extraction=true});
runtime.CargoReleased(active,cargo);
Check(runtime.Remaining(one.Id)==1,"Exact 20 metres and refreshed release between polls qualifies");
active=Fresh(); cargo=Cargo(active); one.InTruck=true; Carry20(active); one.InTruck=false; Deliver(active,cargo);
Check(runtime.Remaining(one.Id)==0,"Player movement inside truck never counts");
active=Fresh(); cargo=Cargo(active); cargo.Rooms.CurrentRooms.Add(new(){Truck=true}); Carry20(active); Deliver(active,cargo);
Check(runtime.Remaining(one.Id)==0,"Cargo in truck never counts");
active=Fresh(); cargo=Cargo(active); Move(active,0); Move(active,2); cargo.Holder=null; Move(active,20); cargo.Holder=one; Move(active,40); Deliver(active,cargo);
Check(runtime.Remaining(one.Id)==0,"Unheld movement and re-grab gaps never count");
active=Fresh(); cargo=Cargo(active); for(int i=0;i<30;i++) Move(active,0); Deliver(active,cargo);
Check(runtime.Remaining(one.Id)==0,"Standing still with object movement earns nothing");
active=Fresh(); cargo=Cargo(active); Carry20(active); Move(active,100); Deliver(active,cargo);
Check(runtime.Remaining(one.Id)==0,"Teleport discards carry progress");
active=Fresh(); cargo=Cargo(active); Move(active,0); for(int i=1;i<=5;i++) Move(active,2*i); cargo.Holder=null; runtime.CargoReleased(active,cargo); Move(active,30);
cargo.Holder=one; for(int i=0;i<=5;i++) Move(active,30+2*i); Deliver(active,cargo);
Check(runtime.Remaining(one.Id)==1,"Separate personal carries of same item accumulate without unheld distance");
active=Fresh(); cargo=Cargo(active); Carry20(active); var different=Cargo(active); Deliver(active,different);
Check(runtime.Remaining(one.Id)==0,"Distance is not transferred to another valuable");
var otherAssignment=new RoleAssignment(other.Id,StageRole.Negotiator); runtime.Capture(new[]{otherAssignment}); Deliver(otherAssignment,cargo);
Check(runtime.Remaining(other.Id)==0,"Distance is not transferred to another player");
foreach (float value in new[]{0f,-1f,float.NaN,float.PositiveInfinity})
{
    active=Fresh(); cargo=Cargo(active); Carry20(active); cargo.Valuable!.dollarValueCurrent=value; Deliver(active,cargo);
    Check(runtime.Remaining(one.Id)==0,"Worthless/destroyed/invalid valuable cannot earn: "+value);
}
foreach (string reason in new[]{"death","role"})
{
    active=Fresh();
    if(reason=="death") runtime.PlayerDied(one); else { active.Role=StageRole.Runner; runtime.RoleChanged(active); active.Role=StageRole.Negotiator; }
    runtime.Capture(new[]{active}); runtime.EnemyDefeated(active,3); cargo=Cargo(active); Carry20(active); Deliver(active,cargo);
    Check(runtime.Remaining(one.Id)==0,"Revocation before first reward blocks later earnings: "+reason);
    runtime.BeginStage(); runtime.Capture(new[]{active}); runtime.EnemyDefeated(active,3);
    Check(runtime.Remaining(one.Id)==1,"Next stage clears revocation: "+reason);
}
foreach (string reason in new[]{"client","disabled","dead","wrong-role","stage-end","shop"})
{
    active=Fresh(); cargo=Cargo(active); Carry20(active);
    switch(reason) { case "client": SemiFunc.Authority=false; break; case "disabled": config.Enabled.Value=false; break; case "dead":one.Alive=false;break;
    case "wrong-role": active.Role=StageRole.Runner;break;case "stage-end":runtime.EndStage();break;case "shop":SemiFunc.Shop=true;break; }
    runtime.EnemyDefeated(active,3); Deliver(active,cargo); Check(runtime.Remaining(one.Id)==0,"Both earning paths reject "+reason);
    SemiFunc.Authority=true;config.Enabled.Value=true;one.Alive=true;SemiFunc.Shop=false;
}
active=Fresh(); active.Role=StageRole.Runner; runtime.RoleChanged(active); active.Role=StageRole.Superbot; runtime.Capture(new[]{active});
runtime.EnemyDefeated(active,3);Check(runtime.Remaining(one.Id)==0,"Inherited role cannot bypass revocation");
active=Fresh(StageRole.Runner); runtime.RoleChanged(active); active.Role=StageRole.Negotiator;runtime.Capture(new[]{active});runtime.EnemyDefeated(active,3);
Check(runtime.Remaining(one.Id)==1,"First acquisition after ordinary role can earn");
active=Fresh(StageRole.Superbot);runtime.EnemyDefeated(active,3);cargo=Cargo(active);Carry20(active);Deliver(active,cargo);
runtime.EndStage();runtime.SceneChanged();SemiFunc.Shop=true;Tick();
Check(runtime.Remaining(one.Id)==2 && RoleAbilitySync.Last.Single().Values.Single().Limit==2,"Two rights survive stage completion and publish 2/2");
runtime.Reserve(keeper,item,one);runtime.Complete(keeper,item);runtime.Complete(keeper,item);
Check(runtime.Remaining(one.Id)==1,"Duplicate completion does not consume second right");
runtime.Reserve(keeper,item,one);runtime.Release(keeper);Check(runtime.Remaining(one.Id)==1,"Interrupted second use is free");
runtime.Reserve(keeper,item,one);runtime.Complete(keeper,item);Tick();
Check(runtime.Remaining(one.Id)==0 && runtime.HasShopRole(one) && RoleAbilitySync.Last.Single().Values.Single().Limit==2,"Both uses consumed, retain 0/2 identity");
active=Fresh();runtime.EnemyDefeated(active,3);cargo=Cargo(active);Carry20(active);Deliver(active,cargo);runtime.PlayerDied(one);
Check(runtime.Remaining(one.Id)==0,"Death revokes both rights");
active=Fresh();cargo=Cargo(active);Move(active,0);Move(active,50);Deliver(active,cargo);
Check(runtime.Remaining(one.Id)==1,"High native Speed levels still accumulate legitimate carried distance");
Console.WriteLine($"PASS: {checks} Negotiator earning, revocation, shop and installed-game patch checks.");
