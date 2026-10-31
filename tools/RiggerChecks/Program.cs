using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Mono.Cecil;
using REPOJP.StageRoles;

int checks = 0;
void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
bool Near(float a, float b) => Math.Abs(a - b) < .0001f;
string gamePath = args.Length > 0 ? args[0] : @"E:\SteamLibrary\steamapps\common\REPO\REPO_Data\Managed\Assembly-CSharp.dll";
using var game = AssemblyDefinition.ReadAssembly(gamePath);
var whipType = game.MainModule.Types.Single(t => t.Name == "ItemMeleeWhip");
var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType == typeof(OpCode))
    .Select(f => (OpCode)f.GetValue(null)!).GroupBy(o => o.Value).ToDictionary(g => g.Key, g => g.First());
object? Operand(object? value)
{
    if (value is FieldReference f && f.DeclaringType.Name is "ItemMeleeWhip" or "CrackHitWindow" or "HurtCollider")
    {
        var type = f.DeclaringType.Name == "ItemMeleeWhip" ? typeof(ItemMeleeWhip) : f.DeclaringType.Name == "HurtCollider" ? typeof(HurtCollider) : typeof(ItemMeleeWhip.CrackHitWindow);
        return (object?)type.GetField(f.Name) ?? value;
    }
    if (value is MethodReference m && m.DeclaringType.Name == "Time" && m.Name == "get_deltaTime")
        return typeof(UnityEngine.Time).GetProperty("deltaTime")!.GetMethod!;
    return value;
}
var patchCases = new (string Native, Func<IEnumerable<CodeInstruction>, IEnumerable<CodeInstruction>> Patch, string Helper)[]
{
    ("EnemyHoldTick", RiggerPatches.HoldTranspiler, "HoldElapsed"),
    ("ReleasePhysics", RiggerPatches.ThrowTranspiler, "DurabilityCost"),
    ("CrackDurabilityUse", RiggerPatches.HitTranspiler, "DurabilityCost")
};
foreach (var test in patchCases)
{
    var native = whipType.Methods.Single(m => m.Name == test.Native);
    var code = native.Body.Instructions.Select(i => new CodeInstruction(opcodes[i.OpCode.Value], Operand(i.Operand))).ToList();
    var patched = test.Patch(code).ToList();
    var helper = AccessTools.Method(typeof(RiggerPatches), test.Helper);
    Check(patched.Count == code.Count + 3, test.Native + ": one scaling operation");
    Check(patched.Count(i => i.Calls(helper)) == 1, test.Native + ": one helper call");
    Check(patched.Where(code.Contains).SequenceEqual(code), test.Native + ": native gates, lifetime checks, spending and effects retained");
    int callIndex = patched.FindIndex(i => i.Calls(helper));
    Check(patched[callIndex - 1].operand is FieldInfo holderField && holderField.Name == "holder" &&
        holderField.DeclaringType == (test.Native == "CrackDurabilityUse" ? typeof(ItemMeleeWhip.CrackHitWindow) : typeof(ItemMeleeWhip)),
        test.Native + ": original swing holder for delayed hit, current holder otherwise");
    bool rejected = false;
    try { test.Patch(new[] { new CodeInstruction(OpCodes.Ret) }).ToList(); } catch (InvalidOperationException) { rejected = true; }
    Check(rejected, test.Native + ": reject incompatible native IL");
}
// The damage hook must alter only the native Hurt argument, not its gate or
// persistent field. It also must work after the strike collider is reparented.
var hurtType = game.MainModule.Types.Single(t => t.Name == "HurtCollider");
var hurt = hurtType.Methods.Single(m => m.Name == "EnemyHurt");
var hurtCode = hurt.Body.Instructions.Select(i => new CodeInstruction(opcodes[i.OpCode.Value], Operand(i.Operand))).ToList();
var scaled = RiggerPatches.DamageTranspiler(hurtCode).ToList();
var damageHelper = AccessTools.Method(typeof(RiggerPatches), nameof(RiggerPatches.EnemyDamage));
Check(scaled.Count == hurtCode.Count + 2 && scaled.Count(i => i.Calls(damageHelper)) == 1, "Exactly one direct enemy damage argument is scaled");
Check(scaled.Where(hurtCode.Contains).SequenceEqual(hurtCode), "All native damage guards, stun and kill paths retained");
int scaledIndex = scaled.FindIndex(i => i.Calls(damageHelper));
Check(scaled[scaledIndex - 1].opcode == OpCodes.Ldarg_0 && scaled[scaledIndex - 2].LoadsField(typeof(HurtCollider).GetField("enemyDamage")), "Damage and exact originating collider passed to helper");
Check(scaled[scaledIndex + 2].opcode == OpCodes.Ldflda && ((FieldInfo)scaled[scaledIndex + 2].operand).Name == "applyForce", "Scaling is adjacent to the Hurt call, after the positive-damage gate");
foreach (var malformed in new[] { new List<CodeInstruction> { new(OpCodes.Ret) }, hurtCode.Concat(hurtCode).ToList() })
{
    bool rejected = false;
    try { RiggerPatches.DamageTranspiler(malformed).ToList(); } catch (InvalidOperationException) { rejected = true; }
    Check(rejected, "Damage patch rejects missing or ambiguous calculations");
}
Check(whipType.Methods.Single(m => m.Name == "Awake").Body.Instructions.Any(i => i.Operand is MethodReference m && m.Name == "SetParent"), "Installed game detaches the strike collider; registry is required");
Check(whipType.Methods.Single(m => m.Name == "CrackImpactPlay").Body.Instructions.Any(i => i.OpCode.Code == Mono.Cecil.Cil.Code.Stfld && i.Operand is FieldReference f && f.Name == "playerCausingHurtOverride"), "Native strike captures its original attacker");
var controller = StageRolesPlugin.Instance!.Controller!;
var player = new PlayerAvatar();
var cable = new ItemMeleeWhip { holder = player };
var strike = cable.crackHurtCollider;
strike.playerCausingHurtOverride = player;
RiggerPatches.RegisterStrike(cable);
RiggerPatches.RegisterStrike(cable); // Re-registration is idempotent.
Check(RiggerPatches.EnemyDamage(20, strike) == 40, "Default cable hit is doubled");
Check(strike.enemyDamage == 20 && strike.playerDamage == 10, "Enemy and player collider fields are never mutated");
for (int hit = 0; hit < 3; hit++) Check(RiggerPatches.EnemyDamage(20, strike) == 40, "Repeated hits do not compound");
foreach (float multiplier in new[] { 1f, 1.5f, 2f, 10f })
foreach (int damage in new[] { 1, 3, 20, 100 })
{
    controller._config.RiggerEnemyDamageMultiplier.Value = multiplier;
    Check(RiggerPatches.EnemyDamage(damage, strike) == (int)Math.Round(damage * multiplier), "Configured damage and native integer rounding");
}
controller._config.RiggerEnemyDamageMultiplier.Value = 2;
Check(RiggerPatches.EnemyDamage(0, strike) == 0 && RiggerPatches.EnemyDamage(-1, strike) == -1, "No damage created from non-damaging hit");
Check(RiggerPatches.EnemyDamage(int.MaxValue, strike) == 100000, "Damage cannot overflow the supported cap");
Check(RiggerPatches.EnemyDamage(20, null) == 20, "Missing collider stays native");
var ordinary = new HurtCollider { playerCausingHurtOverride = player };
Check(RiggerPatches.EnemyDamage(20, ordinary) == 20, "Other melee, ranged and environmental colliders stay native");
cable.isActiveAndEnabled = false; Check(RiggerPatches.EnemyDamage(20, strike) == 20, "Inactive weapon has no damage bonus"); cable.isActiveAndEnabled = true;
cable.crackHurtCollider = ordinary; Check(RiggerPatches.EnemyDamage(20, strike) == 20, "Replaced weapon collider loses stale mapping"); cable.crackHurtCollider = strike;
strike.playerCausingHurtOverride = null; Check(RiggerPatches.EnemyDamage(20, strike) == 20, "Missing original attacker stays native"); strike.playerCausingHurtOverride = player;
foreach (float duration in new[] { 3f, 4f, 7f, 9f, 10f, 12f })
foreach (float multiplier in new[] { 1f, 1.5f, 2f, 3f })
foreach (int fps in new[] { 30, 60, 144 })
{
    controller._config.RiggerEnemyHoldMultiplier.Value = multiplier;
    float remaining = duration; int ticks = 0;
    while (remaining > 0 && ticks < 10000) { remaining = Math.Max(0, remaining - RiggerPatches.HoldElapsed(1f / fps, player)); ticks++; }
    Check(Math.Abs(ticks / (float)fps - duration * multiplier) <= 2f / fps, "Enemy restraint duration at supported difficulty, rate and multiplier");
}
controller._config.RiggerEnemyHoldMultiplier.Value = 1.5f;
foreach (float cost in new[] { .01f, .65f, 1f, 5f, 10f })
foreach (float multiplier in new[] { .1f, .5f, 1f })
{
    controller._config.RiggerDurabilityMultiplier.Value = multiplier;
    Check(Near(RiggerPatches.DurabilityCost(cost, player), cost * multiplier), "Fractional hits and throws preserve configured spending");
}
controller._config.RiggerDurabilityMultiplier.Value = .5f;
void Normal(string label) { Check(Near(RiggerPatches.HoldElapsed(.3f, player), .3f) && Near(RiggerPatches.DurabilityCost(5, player), 5) && RiggerPatches.EnemyDamage(20, strike) == 20, label); }
player.Role = StageRole.Runner; Normal("Role change removes all bonuses immediately, including an ongoing hold");
player.Role = StageRole.Imitator; Normal("Uncopied Imitator has no bonus");
player.Role = StageRole.Rigger; player.Alive = false; Normal("Dead holder gets no bonus"); player.Alive = true;
controller.Ready = false; Normal("Stage cleanup removes bonus"); controller.Ready = true;
controller._config.Enabled.Value = false; Normal("Disabling mod removes bonus"); controller._config.Enabled.Value = true;
SemiFunc.Authority = false; Normal("Non-host does not alter native results"); SemiFunc.Authority = true;
Check(Near(RiggerPatches.HoldElapsed(.3f, null), .3f) && RiggerPatches.DurabilityCost(5, null) == 5, "No holder is unmodified");
var other = new PlayerAvatar { Role = StageRole.Runner };
cable.holder = other;
Check(RiggerPatches.EnemyDamage(20, strike) == 40, "In-flight Rigger hit keeps original attacker after handoff");
strike.playerCausingHurtOverride = other; cable.holder = player;
Check(RiggerPatches.EnemyDamage(20, strike) == 20, "Previous non-Rigger swing does not inherit new holder bonus");
strike.playerCausingHurtOverride = player;
Check(Near(RiggerPatches.DurabilityCost(5, player), 2.5f) && RiggerPatches.DurabilityCost(5, other) == 5, "Shared cable does not confer owner bonus to another holder");
player.Role = StageRole.Superbot; Check(Near(RiggerPatches.HoldElapsed(.3f, player), .2f) && RiggerPatches.EnemyDamage(20, strike) == 40, "Capability holder uses the same path"); player.Role = StageRole.Rigger;
foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
{
    controller._config.RiggerEnemyDamageMultiplier.Value = invalid;
    controller._config.RiggerEnemyHoldMultiplier.Value = invalid;
    controller._config.RiggerDurabilityMultiplier.Value = invalid;
    Normal("Invalid settings fall back to native values");
}
controller._config.RiggerEnemyHoldMultiplier.Value = 0; controller._config.RiggerDurabilityMultiplier.Value = -1;
Check(RiggerPatches.HoldElapsed(1, player) == 1 && Near(RiggerPatches.DurabilityCost(5, player), .5f), "Clamp unsafe lower bounds");
controller._config.RiggerEnemyHoldMultiplier.Value = 100; controller._config.RiggerDurabilityMultiplier.Value = 100;
Check(Near(RiggerPatches.HoldElapsed(1, player), 1f / 3) && RiggerPatches.DurabilityCost(5, player) == 5, "Clamp unsafe upper bounds");
controller._config.RiggerEnemyDamageMultiplier.Value = -10;
Check(RiggerPatches.EnemyDamage(20, strike) == 20, "Damage clamps to 1x minimum");
controller._config.RiggerEnemyDamageMultiplier.Value = 100;
Check(RiggerPatches.EnemyDamage(20, strike) == 200, "Damage clamps to 10x maximum");
controller._config.RiggerEnemyDamageMultiplier.Value = 2;
StageRolesPlugin.Instance = null; Normal("No initialized plugin leaves native behavior intact");
Console.WriteLine($"PASS: {checks} Rigger runtime and installed-game IL checks. Live cable/multiplayer untested.");
