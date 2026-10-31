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
void Grant(string id, StageRole role = StageRole.Negotiator) => runtime.Capture(new[] { new RoleAssignment(id, role) });
void Hook(string name, params object?[] args) => typeof(NegotiatorPatches).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, args);
runtime.BeginStage(); Grant(one.Id); Grant(one.Id); Grant(other.Id, StageRole.Runner);
Check(runtime.Remaining(one.Id) == 1 && runtime.Remaining(other.Id) == 0, "One entitlement without stacking, no grant for unrelated role");
Check(!runtime.CanUse(one), "Cannot spend before shop");
Grant(one.Id, StageRole.Runner); runtime.SceneChanged(); Tick();
Check(runtime.Remaining(one.Id) == 1, "Role change and transition retain earned right");
SemiFunc.Shop = true; var keeper = new ShopKeeper(); ShopKeeper.instance = keeper;
SemiFunc.Generated = false; Tick(); Check(!runtime.CanUse(one), "Do not enter shop during generation");
SemiFunc.Generated = true; RoleNotifier.Ready = false; Tick();
Check(runtime.HasShopRole(one) && !runtime.HasShopRole(other), "Only entitled players receive shop roles");
Check(RoleAssignmentSync.Last.Count == 1 && RoleAssignmentSync.Last[0].SteamId == one.Id, "Shop role list identifies holder");
Check(RoleAbilitySync.Last.Single().Values.Single().Remaining == 1, "Shop budget 1/1");
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
runtime.Reserve(keeper, item, one); one.Alive = false;
Check(runtime.Guaranteed(keeper, item), "Already accepted negotiation stays safe if presenter dies"); one.Alive = true;
item.Item.value = 18; Hook("AfterDiscount", keeper, item, 30);
Check(runtime.Remaining(one.Id) == 0 && !runtime.CanUse(one), "Actual discount consumes exactly once");
Hook("AfterDiscount", keeper, item, 30); Hook("Release", keeper); Tick();
Check(StageRolesPlugin.Instance.Controller._notifier.Sent.Count(n => n.Message == "Discounted") == 1, "No duplicate completion notification");
Check(runtime.HasShopRole(one) && RoleAbilitySync.Last.Single().Values.Single().Remaining == 0, "Used holder stays identifiable with 0/1");
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
runtime.BeginStage(); Grant(one.Id); Hook("ResetRun"); Check(runtime.Remaining(one.Id) == 0, "New run clears pending rights");
foreach (string context in new[] { "menu", "lobby", "arena", "client", "disabled" })
{
    runtime.BeginStage(); Grant(one.Id);
    SemiFunc.MainMenu = context == "menu"; SemiFunc.LobbyMenu = context == "lobby"; SemiFunc.Arena = context == "arena";
    SemiFunc.Authority = context != "client"; config.Enabled.Value = context != "disabled";
    Tick(); Check(runtime.Remaining(one.Id) == 0, "Clear stale rights: " + context);
    SemiFunc.MainMenu = SemiFunc.LobbyMenu = SemiFunc.Arena = false; SemiFunc.Authority = config.Enabled.Value = true;
}
Console.WriteLine($"PASS: {checks} Negotiator runtime, entitlement, shop display and installed-game patch checks.");
