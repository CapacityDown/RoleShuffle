using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Mono.Cecil;
using REPOJP.StageRoles;
using UnityEngine;

int checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
void Near(double actual, double expected, string message) => Check(Math.Abs(actual - expected) < 0.00002, message);
var opcodes = typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public)
    .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)!).ToDictionary(op => op.Name!);
var lerp = AccessTools.Method(typeof(Mathf), nameof(Mathf.Lerp));
var blendMethod = AccessTools.Method(typeof(LifterStrengthRuntime), nameof(LifterStrengthRuntime.Blend));
var inputMethod = AccessTools.Method(typeof(LifterStrengthRuntime), nameof(LifterStrengthRuntime.NativeStrengthInput));
Check(LifterStrengthRuntime.FieldsAvailable, "Runtime override fields found");
Near(RoleOverhaulRules.LifterEffectiveStrength(true), 1.1812987012987013, "Light handling uses vanilla level ONE without sixfold boost");
Near(RoleOverhaulRules.LifterEffectiveStrength(true, true), 1.1812987012987013, "Light rotation uses vanilla level ONE without sixfold boost");
foreach (bool rotation in new[] { false, true })
{
    double target = RoleOverhaulRules.LifterEffectiveStrength(false, rotation);
    Near(target, 143d / 24d, "Heavy target is the exact level-50 peak, without rounding");
    Check(target > RoleOverhaulRules.EffectiveGrabStrength(200, false, rotation), "Peak exceeds the level-200 endpoint");
    for (int level = 0; level <= 200; level++)
        Check(RoleOverhaulRules.EffectiveGrabStrength(level, false, rotation) <= target + 1e-9,
            "Heavy target covers every level, including the penalty dip");
}

// Inspect installed-game IL without loading Unity; pass all its instructions
// to the real transpiler, mapping only the locals/members it examines.
using var game = AssemblyDefinition.ReadAssembly(@"E:\SteamLibrary\steamapps\common\REPO\REPO_Data\Managed\Assembly-CSharp.dll");
var physics = game.MainModule.Types.Single(t => t.Name == "PhysGrabObject");
var method = physics.Methods.Single(m => m.Name == "PhysicsGrabbingManipulation");
Check(method.Body.Instructions.Count(i => i.Operand is MethodReference m && m.DeclaringType.FullName == "UnityEngine.Mathf" && m.Name == "Lerp") == 2,
    "Installed game has exactly two scalar penalty blends");
foreach (var field in typeof(PhysGrabObject).GetFields().Where(f => f.Name.StartsWith("override")))
    Check(physics.Fields.Any(f => f.Name == field.Name && f.FieldType.FullName == field.FieldType.FullName), "Installed override field " + field.Name);
Check(game.MainModule.Types.Single(t => t.Name == "PlayerAvatar").Fields.Any(f => f.Name == "isTumbling" && f.FieldType.FullName == "System.Boolean"), "Installed tumbling field");
Check(game.MainModule.Types.Single(t => t.Name == "PhysGrabber").Fields.Any(f => f.Name == "overrideGrabStrength" && f.FieldType.FullName == "System.Single"), "Installed player override field");
var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("LifterILLocals"), AssemblyBuilderAccess.Run);
var type = assembly.DefineDynamicModule("main").DefineType("Locals");
var proxy = type.DefineMethod("Original", System.Reflection.MethodAttributes.Public, typeof(void), Type.EmptyTypes);
var proxyIL = proxy.GetILGenerator();
foreach (var variable in method.Body.Variables)
    proxyIL.DeclareLocal(variable.VariableType.Name == "PhysGrabber" ? typeof(PhysGrabber) : typeof(object));
proxyIL.Emit(OpCodes.Ret);
var proxyMethod = type.CreateType()!.GetMethod("Original")!;
List<CodeInstruction> InstalledInstructions() => method.Body.Instructions.Select(i => {
    object? operand = i.Operand;
    if (operand is MethodReference m && m.DeclaringType.FullName == "UnityEngine.Mathf" && m.Name == "Lerp") operand = lerp;
    else if (operand is FieldReference f && f.DeclaringType.Name == "PhysGrabber" && f.Name == "grabStrength") operand = AccessTools.Field(typeof(PhysGrabber), "grabStrength");
    else if (operand is Mono.Cecil.Cil.VariableDefinition v) operand = v.Index;
    return new CodeInstruction(opcodes[i.OpCode.Name], operand);
}).ToList();
var installed = LifterStrengthPatch.Transpiler(InstalledInstructions(), proxyMethod).ToList();
Check(RoleOverhaulRules.LifterPhysicsAvailable && installed.Count(i => i.Calls(blendMethod)) == 2, "Production transpiler matches installed game");
Check(installed.Count(i => i.Calls(inputMethod)) == 2, "Both installed-game Strength reads receive the gun input correction");
Check(physics.Fields.Any(f => f.Name == "isGun" && f.FieldType.FullName == "System.Boolean"), "Installed native gun classification field");
var gunUpdate = game.MainModule.Types.Single(t => t.Name == "ItemGun").Methods.Single(m => m.Name == "UpdateMaster");
Check(gunUpdate.Body.Instructions.Any(i => i.Operand is MethodReference m && m.Name == "OverrideTorqueStrength") &&
    gunUpdate.Body.Instructions.Any(i => i.OpCode.Code == Mono.Cecil.Cil.Code.Ldc_R4 && (float)i.Operand == 12f),
    "Installed guns refresh the aiming torque override even when not firing");
int[] sites = installed.Select((code, index) => (code, index)).Where(x => x.code.Calls(blendMethod)).Select(x => x.index).ToArray();
Check(installed[sites[0]-1].opcode == OpCodes.Ldc_I4_0 && installed[sites[1]-1].opcode == OpCodes.Ldc_I4_1, "Grip and rotation site order");
var unsupported = InstalledInstructions(); unsupported.Add(new CodeInstruction(OpCodes.Call, lerp));
var rejected = LifterStrengthPatch.Transpiler(unsupported, proxyMethod).ToList();
Check(!RoleOverhaulRules.LifterPhysicsAvailable && rejected.All(i => !i.Calls(blendMethod) && !i.Calls(inputMethod)), "Unknown game shape fails without partial patch");

// Decode with real Harmony, transform with production code, then emit/run it.
Action<PhysGrabObject> CompileFixture(string methodName)
{
    var fixture = typeof(PhysGrabObject).GetMethod(methodName)!;
    var executable = new DynamicMethod("LifterPatchedFixture", typeof(void), new[] { typeof(PhysGrabObject) }, typeof(PhysGrabObject).Module, true);
    var generator = executable.GetILGenerator();
    var original = PatchProcessor.GetOriginalInstructions(fixture, generator);
    var patched = LifterStrengthPatch.Transpiler(original, fixture).ToList();
    if (!RoleOverhaulRules.LifterPhysicsAvailable)
    {
        Console.WriteLine(string.Join("\n", fixture.GetMethodBody()!.LocalVariables.Select(v => $"local {v.LocalIndex}: {v.LocalType}")));
        Console.WriteLine(string.Join("\n", original.Select((code, index) => $"{index}: {code}")));
    }
    Check(RoleOverhaulRules.LifterPhysicsAvailable && patched.Count(i => i.Calls(blendMethod)) == 2 && patched.Count(i => i.Calls(inputMethod)) == 2, "Executable fixture patched at both sites");
    foreach (var code in patched)
    {
        foreach (var label in code.labels) generator.MarkLabel(label);
        Check(code.blocks.Count == 0, "Fixture has no exception blocks");
        OpCode opcode = code.opcode;
        if (code.operand is Label && opcode.OperandType == OperandType.ShortInlineBrTarget) opcode = opcodes[opcode.Name![..^2]];
        switch (code.operand)
        {
            case null: generator.Emit(opcode); break;
            case LocalBuilder local: generator.Emit(opcode, local); break;
            case Label label: generator.Emit(opcode, label); break;
            case MethodInfo target: generator.Emit(opcode, target); break;
            case FieldInfo field: generator.Emit(opcode, field); break;
            case Type target: generator.Emit(opcode, target); break;
            case float value: generator.Emit(opcode, value); break;
            case int value when opcode.OperandType == OperandType.InlineVar: generator.Emit(opcode, (short)value); break;
            case int value: generator.Emit(opcode, value); break;
            default: throw new Exception("Unhandled fixture operand " + code.operand.GetType());
        }
    }
    return executable.CreateDelegate<Action<PhysGrabObject>>();
}
var run = CompileFixture(nameof(PhysGrabObject.PhysicsGrabbingManipulation));
var runGun = CompileFixture(nameof(PhysGrabObject.PhysicsGrabbingGunFixture));
var lifter = new PhysGrabber();
var other = new PhysGrabber { playerAvatar = new PlayerAvatar { Role = StageRole.Tank } };
var owner = new PhysGrabObject { playerGrabbing = new[] { lifter, other } };
foreach (float mass in new[] { 0.02f, 0.05f, 0.1f, 0.5f, 1.999f, 2f, 2.001f, 4f, 8f, 100f })
for (int level = 0; level <= 200; level++)
{
    owner.rb.mass = mass;
    lifter.grabStrength = other.grabStrength = 1f + 0.2f * level;
    run(owner); run(owner);
    Near(lifter.Grip, RoleOverhaulRules.LifterEffectiveStrength(mass < 2), "Lifter absolute grip across 0-200");
    Near(lifter.Torque, RoleOverhaulRules.LifterEffectiveStrength(mass < 2, true), "Lifter absolute rotation coefficient");
    Near(other.Grip, RoleOverhaulRules.EffectiveGrabStrength(level, mass < 2), "Mixed grabber vanilla grip");
    Near(other.Torque, RoleOverhaulRules.EffectiveGrabStrength(level, mass < 2, true), "Mixed grabber vanilla rotation");
    Near(lifter.grabStrength, 1f + 0.2f * level, "Never mutate the shared grabber Strength");
    Check(RoleOverhaulRules.LifterBaseReachesTarget(level) == (level == 50), "Only Base at the actual peak is excluded");
}
owner.rb.mass = 10; lifter.grabStrength = 41;
void Vanilla(string reason) { run(owner); Near(lifter.Grip, RoleOverhaulRules.EffectiveGrabStrength(200, false), reason); Near(lifter.Torque, RoleOverhaulRules.EffectiveGrabStrength(200, false, true), reason); }
var controller = StageRolesPlugin.Instance.Controller;
controller.Authority = false; Vanilla("Guests do not apply physics"); controller.Authority = true;
controller.Ready = false; Vanilla("Stage cleanup stops correction"); controller.Ready = true;
controller._config.Enabled.Value = false; Vanilla("Disabled mod"); controller._config.Enabled.Value = true;
lifter.playerAvatar.Living = false; Vanilla("Dead player"); lifter.playerAvatar.Living = true;
lifter.playerAvatar.isTumbling = true; Vanilla("Tumbling override"); lifter.playerAvatar.isTumbling = false;
lifter.overrideGrabStrength = 0; Vanilla("Player override"); lifter.overrideGrabStrength = -1;
foreach (var field in typeof(PhysGrabObject).GetFields().Where(f => f.Name.StartsWith("override") &&
    (f.Name.EndsWith("Timer") || f.Name.EndsWith("Disable"))))
{
    field.SetValue(owner, field.FieldType == typeof(bool) ? (object)true : 1f);
    run(owner);
    bool rotation = field.Name.Contains("Torque");
    Near(rotation ? lifter.Torque : lifter.Grip, RoleOverhaulRules.EffectiveGrabStrength(200, false, rotation), "Explicit object override: " + field.Name);
    field.SetValue(owner, field.FieldType == typeof(bool) ? (object)false : 0f);
}
lifter.playerAvatar.Role = StageRole.Superbot; run(owner); Near(lifter.Grip, RoleOverhaulRules.LifterEffectiveStrength(false), "Superbot capability");
lifter.playerAvatar.Role = StageRole.Tank; Vanilla("Role change stops correction immediately");
lifter.playerAvatar.Role = StageRole.Lifter; run(owner); Near(lifter.Grip, RoleOverhaulRules.LifterEffectiveStrength(false), "Copied effective Lifter role");
Near(LifterStrengthRuntime.Blend(2f, 1f, 0.5f, owner, lifter, false), 1.5, "Changed raw value from another override remains untouched");
RoleOverhaulRules.LifterPhysicsAvailable = false; Vanilla("Failed or disabled patch");

// Exercise the real absolute upgrade setter together with the patched physics.
// The old role-only test above intentionally kept Lv200, so it could not catch
// a cached vanilla grabStrength left over after the stored level was restored.
RoleOverhaulRules.LifterPhysicsAvailable = true;
SemiFunc.Players["changed"] = lifter.playerAvatar;
void SetStrength(int level) => UpgradeService.SetLevels("changed",
    new[] { new UpgradeGrant("Strength", "playerUpgradeStrength", level) });
foreach (bool multiplayer in new[] { false, true })
foreach (StageRole previous in new[] { StageRole.Lifter, StageRole.Superbot })
for (int baseline = 0; baseline <= 200; baseline++)
{
    SemiFunc.Multiplayer = multiplayer;
    StatsManager.instance.Strength["changed"] = baseline;
    lifter.grabStrength = 1f + 0.2f * baseline;
    Photon.Pun.PhotonView.GuestLevels["changed"] = baseline;
    Photon.Pun.PhotonView.GuestStrength["changed"] = lifter.grabStrength;
    for (int repeat = 0; repeat < 3; repeat++)
    {
        lifter.playerAvatar.Role = previous;
        SetStrength(200);
        run(owner);
        Near(lifter.Grip, RoleOverhaulRules.LifterEffectiveStrength(false), "Assigned Lifter/Superbot has fixed grip");
        // Model the observed failure class: the additive cache contains an old
        // Lv200 contribution (e.g. native delayed initialization), independently
        // of StatsManager. A level delta alone cannot remove this discrepancy.
        if (repeat == 1) lifter.grabStrength += 40f;
        lifter.playerAvatar.Role = StageRole.Runner;
        SetStrength(baseline);
        Check(StatsManager.instance.Strength["changed"] == baseline, "Runner restores configured Base Strength");
        Near(lifter.grabStrength, 1f + 0.2f * baseline, "Lifter to Runner reconciles cached Strength");
        foreach (float mass in new[] { 0.5f, 10f })
        {
            owner.rb.mass = mass;
            run(owner);
            Near(lifter.Grip, RoleOverhaulRules.EffectiveGrabStrength(baseline, mass < 2), "Runner has Base grip");
            Near(lifter.Torque, RoleOverhaulRules.EffectiveGrabStrength(baseline, mass < 2, true), "Runner has Base rotation");
        }
        owner.rb.mass = 10;
        if (multiplayer)
        {
            Check(Photon.Pun.PhotonView.GuestLevels["changed"] == baseline, "Native RPC restores unmodded guest level");
            Near(Photon.Pun.PhotonView.GuestStrength["changed"], 1f + 0.2f * baseline, "Native guest delta is not doubled");
        }
    }
}
StatsManager.instance.Strength["changed"] = 3;
lifter.grabStrength = 41;
lifter.overrideGrabStrength = 0.25f;
int beforeNoOp = Photon.Pun.PhotonView.Sends;
SetStrength(3);
Near(lifter.grabStrength, 1.6f, "Stored level already matches but stale force is still restored");
Near(lifter.overrideGrabStrength, 0.25f, "Absolute reset preserves temporary player override");
Check(Photon.Pun.PhotonView.Sends == beforeNoOp, "Cache-only repair sends no extra upgrade delta");
Near(other.grabStrength, 41f, "Reset does not change another player's force");
lifter.overrideGrabStrength = -1;
controller.Ready = false;
lifter.grabStrength = 41;
SetStrength(0); run(owner);
Near(lifter.Grip, 1f, "Stage cleanup restores Base with fixed Lifter physics inactive");
controller.Ready = true;
SemiFunc.Authority = false;
lifter.grabStrength = 9;
SetStrength(0);
Near(lifter.grabStrength, 9f, "Guest cannot reconcile host-owned grab cache");
SemiFunc.Authority = true;
StatsManager.instance.Strength["changed"] = 20;
lifter.grabStrength = 5;
UpgradeService.EnsureAtLeastLevels("changed", new[] { new UpgradeGrant("Strength", "playerUpgradeStrength", 3) });
Near(lifter.grabStrength, 5f, "Minimum grant leaves higher existing upgrade intact");
SemiFunc.Players.Remove("changed");
SetStrength(0);
Check(StatsManager.instance.Strength["changed"] == 0, "Disconnected avatar does not prevent stored Base restoration");

var pun = game.MainModule.Types.Single(t => t.Name == "PunManager");
var nativeCacheUpdate = pun.Methods.Single(m => m.Name == "UpdateGrabStrengthRightAway").Body.Instructions;
Check(nativeCacheUpdate.Any(i => i.OpCode.Code == Mono.Cecil.Cil.Code.Ldc_R4 && (float)i.Operand == 0.2f)
    && nativeCacheUpdate.Any(i => i.OpCode.Code == Mono.Cecil.Cil.Code.Add)
    && nativeCacheUpdate.Any(i => i.OpCode.Code == Mono.Cecil.Cil.Code.Stfld &&
        i.Operand is FieldReference f && f.Name == "grabStrength"), "Installed game uses the modeled additive Strength cache");

// Numerical regression using the installed game's native small-object force
// equations, 50 Hz integration and cached-force interpolation. This deliberately
// models a free, centered hold (not Unity contacts/networking). The old sixfold
// light coefficients must reproduce sustained motion; the new coefficients
// obtained from the emitted production patch must settle without extra damping.
(double Span, double Mean) HoldingMotion(double coefficient, bool rotation)
{
    double position = rotation ? Math.PI / 4 : 0.3;
    double velocity = 0, previousForce = 0, low = double.MaxValue, high = double.MinValue, sum = 0;
    const double dt = 0.02;
    int samples = 0;
    for (int tick = 0; tick < 3000; tick++)
    {
        double force;
        if (rotation)
        {
            const double mass = 0.5, extent = 0.2;
            double degrees = Math.Abs(position) * 180 / Math.PI;
            force = (degrees / 90) * 15 * mass * dt * mass * (coefficient + 10) * Math.Min(degrees / 30, 1);
            double divisor = Math.Max(mass * 30, 1) / 7 * mass * 6 / (1 + coefficient);
            force *= (1 + coefficient) / (1 + extent * extent * 12) * 6500 / divisor;
            force = Math.CopySign(force, -position);
            velocity = velocity * 0.8 + (previousForce * 0.1 + force * 0.9) * dt;
        }
        else
        {
            const double mass = 0.05;
            double displacement = Math.Clamp(-position * 10, -4, 4);
            force = Math.Clamp(displacement * 0.9 - velocity * 0.5, -4, 4) *
                2 / mass * coefficient * 4 * Math.Min(Math.Abs(position) * 10, 1);
            velocity = velocity * 0.98 + (previousForce * 0.2 + force * 0.8 - 9.81) * dt;
        }
        previousForce = force;
        position += velocity * dt;
        if (rotation) position = Math.Atan2(Math.Sin(position), Math.Cos(position));
        if (tick >= 2500) { low = Math.Min(low, position); high = Math.Max(high, position); sum += position; samples++; }
    }
    return (high - low, sum / samples);
}
RoleOverhaulRules.LifterPhysicsAvailable = true;
controller.Authority = controller.Ready = true;
lifter.playerAvatar.Role = StageRole.Lifter;
owner.rb.mass = 0.5f;
lifter.grabStrength = 41;
run(owner);
var oldTranslation = HoldingMotion(RoleOverhaulRules.EffectiveGrabStrength(1, true) * 6, false);
var newTranslation = HoldingMotion(lifter.Grip, false);
var oldRotation = HoldingMotion(RoleOverhaulRules.EffectiveGrabStrength(1, true, true) * 6, true);
var newRotation = HoldingMotion(lifter.Torque, true);
Check(oldTranslation.Span > 1 && oldRotation.Span > 0.1, "Prior light coefficients reproduce unsettled native holding motion");
Check(newTranslation.Span < 0.001 && Math.Abs(newTranslation.Mean) < 0.05, "Reduced light grip settles within 5 cm of its hold target");
Check(newRotation.Span < 0.001 && Math.Abs(newRotation.Mean) < 0.001, "Reduced light rotation settles without custom force damping");


// Compare patched guns against the same unpatched native override pipeline at
// Strength level 1. Include idle aim, manual rotation, firing and custom overrides.
var gunLifter = new PhysGrabber();
var gunOther = new PhysGrabber { playerAvatar = new PlayerAvatar { Role = StageRole.Tank } };
var gun = new PhysGrabObject { isGun = true, playerGrabbing = new[] { gunLifter, gunOther } };
var referenceHolder = new PhysGrabber();
var reference = new PhysGrabObject { isGun = true, playerGrabbing = new[] { referenceHolder } };
void GunState(PhysGrabObject obj, PhysGrabber holder, int state)
{
    obj.overrideTorqueStrength = state == 1 ? 2f : state == 2 ? 0.01f : 12f;
    obj.overrideTorqueStrengthTimer = state == 3 ? 0f : 0.1f;
    obj.overrideExtraGrabStrengthDisable = obj.overrideExtraTorqueStrengthDisable = state == 2;
    obj.overrideGrabStrengthTimer = state == 4 ? 0.1f : 0;
    obj.overrideGrabStrength = 0.5f;
    obj.overrideMinGrabStrengthTimer = obj.overrideMinTorqueStrengthTimer = state == 5 ? 0.1f : 0;
    obj.overrideMinGrabStrength = 3; obj.overrideMinTorqueStrength = 15;
    holder.overrideGrabStrength = state == 6 ? 0.25f : -1;
    holder.playerAvatar.isTumbling = state == 7;
}
foreach (float mass in new[] { 0.5f, 1.999f, 2f, 2.001f, 8f })
for (int state = 0; state < 8; state++)
foreach (StageRole role in new[] { StageRole.Lifter, StageRole.Superbot })
{
    gun.rb.mass = reference.rb.mass = mass;
    gunLifter.playerAvatar.Role = role;
    GunState(gun, gunLifter, state); GunState(reference, referenceHolder, state);
    referenceHolder.grabStrength = 1.2f;
    reference.PhysicsGrabbingGunFixture();
    float expectedGrip = referenceHolder.Grip, expectedTorque = referenceHolder.Torque;
    GunState(gun, gunOther, state);
    for (int level = 0; level <= 200; level++)
    {
        gunLifter.grabStrength = gunOther.grabStrength = 1f + 0.2f * level;
        runGun(gun);
        Near(gunLifter.Grip, expectedGrip, "Gun grip equals native level-1 handling with overrides");
        Near(gunLifter.Torque, expectedTorque, "Gun rotation retains native aiming/firing overrides at level 1");
        Near(gunLifter.grabStrength, 1f + 0.2f * level, "Gun handling never mutates the shared Strength cache");
        referenceHolder.grabStrength = gunOther.grabStrength;reference.PhysicsGrabbingGunFixture();
        Near(gunOther.Grip, referenceHolder.Grip, "Another gun holder retains native grip");
        Near(gunOther.Torque, referenceHolder.Torque, "Another gun holder retains native rotation");
    }
}
gun.rb.mass = reference.rb.mass = 2f;
GunState(gun, gunLifter, 0);GunState(reference, referenceHolder, 0);
gunLifter.playerAvatar.Role = StageRole.Lifter;gunLifter.grabStrength = 41;
referenceHolder.grabStrength = 41;reference.PhysicsGrabbingGunFixture();
float oldGunGrip = referenceHolder.Grip;
runGun(gun);
Check(gunLifter.Grip < oldGunGrip / 3, "Idle gun no longer gets level-200 or heavy-object grip");
void GunUsesVanilla(string reason)
{
    runGun(gun);
    Near(gunLifter.Grip, referenceHolder.Grip, reason + " grip");
    Near(gunLifter.Torque, referenceHolder.Torque, reason + " torque");
}
gunLifter.playerAvatar.Role = StageRole.Runner;GunUsesVanilla("Leaving Lifter ends gun correction");
gunLifter.playerAvatar.Role = StageRole.Lifter;
controller.Authority = false;GunUsesVanilla("Non-host does not correct guns");controller.Authority = true;
controller.Ready = false;GunUsesVanilla("Stage cleanup ends gun correction");controller.Ready = true;
controller._config.Enabled.Value = false;GunUsesVanilla("Disabled mod leaves guns native");controller._config.Enabled.Value = true;
gunLifter.playerAvatar.Living = false;GunUsesVanilla("Dead holder receives no gun correction");gunLifter.playerAvatar.Living = true;
RoleOverhaulRules.LifterPhysicsAvailable = false;GunUsesVanilla("Unavailable patch leaves guns native");RoleOverhaulRules.LifterPhysicsAvailable = true;
// Returning to an ordinary heavy object restores the existing role peak immediately.
gun.isGun = false;GunState(gun, gunLifter, 3);gun.overrideTorqueStrength = 1;
runGun(gun);
Near(gunLifter.Grip, RoleOverhaulRules.LifterEffectiveStrength(false), "Switching gun to heavy cargo restores peak grip");
Near(gunLifter.Torque, RoleOverhaulRules.LifterEffectiveStrength(false, true), "Switching gun to heavy cargo restores peak rotation");


// Shop equipment of any mass uses the native level-1 pipeline, including
// weapons and other purchase items. Plain valuables keep the heavy target.
Check(physics.Fields.Any(f => f.Name == "isMelee" && f.FieldType.FullName == "System.Boolean"), "Installed melee classification field");
var meleeType = game.MainModule.Types.Single(t => t.Name == "ItemMelee");
Check(meleeType.Fields.Any(f => f.Name == "physGrabObject" && f.FieldType.Name == "PhysGrabObject"), "Installed melee object reference");
Check(meleeType.Methods.Single(m => m.Name == "MeleeStrengthBonus").Body.Instructions.Count(i =>
    i.Operand is FieldReference f && f.Name == "grabStrength") == 1, "Melee independently reads first-holder Strength");
var itemHolder = new PhysGrabber();
var itemOther = new PhysGrabber { playerAvatar = new PlayerAvatar { Role = StageRole.Runner } };
var shopItem = new PhysGrabObject { playerGrabbing = new[] { itemHolder, itemOther } };
foreach (string kind in new[] { "shop", "melee", "gun", "shop-melee" })
foreach (float mass in new[] { 0.1f, 1.999f, 2f, 8f, 100f })
foreach (StageRole role in new[] { StageRole.Lifter, StageRole.Superbot })
{
    shopItem.isGun = kind == "gun";
    shopItem.isMelee = kind.Contains("melee");
    shopItem.itemAttributes = kind.StartsWith("shop") ? new ItemAttributes() : null;
    shopItem.rb.mass = reference.rb.mass = mass;
    itemHolder.playerAvatar.Role = role;
    for (int state = 0; state < 8; state++)
    {
        GunState(shopItem, itemHolder, state); GunState(shopItem, itemOther, state);
        GunState(reference, referenceHolder, state);
        referenceHolder.grabStrength = 1.2f;reference.PhysicsGrabbingGunFixture();
        float expectedGrip = referenceHolder.Grip, expectedTorque = referenceHolder.Torque;
        foreach (int level in new[] { 0, 1, 25, 50, 100, 200 })
        {
            itemHolder.grabStrength = itemOther.grabStrength = 1f + level * 0.2f;
            runGun(shopItem);
            Near(itemHolder.Grip, expectedGrip, kind + " native level-1 grip");
            Near(itemHolder.Torque, expectedTorque, kind + " native level-1 torque with item overrides");
            Near(itemHolder.grabStrength, 1f + level * 0.2f, "Equipment does not mutate shared Strength");
            referenceHolder.grabStrength = itemOther.grabStrength;reference.PhysicsGrabbingGunFixture();
            Near(itemOther.Grip, referenceHolder.Grip, "Other equipment holder keeps their own Strength");
            Near(itemOther.Torque, referenceHolder.Torque, "Other equipment holder keeps native torque");
        }
    }
}

// Run the production prefix/finalizer through the fixture dispatcher: the
// game's legacy Harmony detour runtime cannot patch the net9 test process.
// Swing bonus calls outside the holding scope must stay native.
ItemMelee.HoldingPatchesEnabled = true;
try
{
    var meleeHolder = new PhysGrabber();
    var meleeObject = new PhysGrabObject { isMelee = true, itemAttributes = new(), playerGrabbing = new[] { meleeHolder } };
    var melee = new ItemMelee(meleeObject);
    var nativeHolder = new PhysGrabber { playerAvatar = new PlayerAvatar { Role = StageRole.Runner }, grabStrength = 1.2f };
    var nativeObject = new PhysGrabObject { isMelee = true, itemAttributes = new(), playerGrabbing = new[] { nativeHolder } };
    var nativeMelee = new ItemMelee(nativeObject);
    foreach (StageRole role in new[] { StageRole.Lifter, StageRole.Superbot })
    foreach (float massRatio in new[] { 0.1f, 1f })
    foreach (float torque in new[] { 0.4f, 2f, 12f })
    foreach (bool rotating in new[] { false, true })
    foreach (bool attacking in new[] { false, true })
    {
        meleeHolder.playerAvatar.Role = role;
        melee.MassRatio = nativeMelee.MassRatio = massRatio;
        melee.CustomTorque = nativeMelee.CustomTorque = torque;
        melee.Rotate = nativeMelee.Rotate = rotating;
        melee.Attack = nativeMelee.Attack = attacking;
        nativeMelee.GrabOverridesLogic();
        for (int level = 0; level <= 200; level++)
        {
            meleeHolder.grabStrength = 1f + 0.2f * level;
            float swingBefore = melee.MeleeStrengthScale(0.3f);
            melee.GrabOverridesLogic();
            Near(meleeObject.overrideMinGrabStrength, nativeObject.overrideMinGrabStrength, "Melee hold force matches native Lv1");
            Near(meleeObject.overrideTorqueStrength, nativeObject.overrideTorqueStrength, "Melee custom torque matches native Lv1 holding");
            Near(meleeObject.overrideMinTorqueStrength, nativeObject.overrideMinTorqueStrength, "Melee manual rotation matches native Lv1 holding");
            runGun(meleeObject);nativeObject.PhysicsGrabbingGunFixture();
            Near(meleeHolder.Grip, nativeHolder.Grip, "Combined melee override and physics path matches Lv1 grip");
            Near(meleeHolder.Torque, nativeHolder.Torque, "Combined melee override and physics path matches Lv1 rotation");
            Near(melee.MeleeStrengthScale(0.3f), swingBefore, "Swing bonus unchanged outside holding scope");
            Near(meleeHolder.grabStrength, 1f + 0.2f * level, "Melee overrides do not change stored grabber Strength");
        }
    }
    meleeHolder.playerAvatar.Role = StageRole.Lifter;meleeHolder.grabStrength = 41;
    melee.Rotate = false;melee.Attack = false;melee.MassRatio = 1;melee.CustomTorque = 0.4f;
    float expectedNativeBonus = melee.MeleeStrengthBonus(90f);
    melee.ThrowWhileHolding = true;
    try { melee.GrabOverridesLogic();Check(false, "Fixture must throw"); }
    catch (InvalidOperationException error) { Check(error.Message == "holding fixture failure", "Original exception preserved"); }
    melee.ThrowWhileHolding = false;
    Near(melee.MeleeStrengthBonus(90f), expectedNativeBonus, "Finalizer clears context after a failed hold update");
    nativeMelee.ThrowWhileHolding = true;
    melee.DuringHolding = () =>
    {
        Near(nativeMelee.MeleeStrengthBonus(90f), 1f / 91f, "Other weapon unaffected by active holding scope");
        try { nativeMelee.GrabOverridesLogic(); } catch (InvalidOperationException) { }
        Near(melee.MeleeStrengthBonus(90f), 1f / 91f, "Nested failure restores outer weapon context");
    };
    melee.GrabOverridesLogic();melee.DuringHolding = null;nativeMelee.ThrowWhileHolding = false;
    Near(melee.MeleeStrengthBonus(90f), expectedNativeBonus, "Nested context fully cleared after update");
    void NativeMeleeHold(string reason)
    {
        melee.GrabOverridesLogic();
        Near(meleeObject.overrideMinGrabStrength, 17f + 42f * expectedNativeBonus, reason);
    }
    controller.Authority = false;NativeMeleeHold("Guests do not modify melee overrides");controller.Authority = true;
    controller.Ready = false;NativeMeleeHold("Stage end stops melee overrides");controller.Ready = true;
    controller._config.Enabled.Value = false;NativeMeleeHold("Disabled role mod leaves melee native");controller._config.Enabled.Value = true;
    meleeHolder.playerAvatar.Living = false;NativeMeleeHold("Dead holder leaves melee native");meleeHolder.playerAvatar.Living = true;
    RoleOverhaulRules.LifterPhysicsAvailable = false;NativeMeleeHold("Unavailable Lifter patch leaves melee native");RoleOverhaulRules.LifterPhysicsAvailable = true;
    meleeHolder.playerAvatar.Role = StageRole.Runner;NativeMeleeHold("Role change ends melee holding correction");
    meleeHolder.playerAvatar.Role = StageRole.Lifter;
    nativeHolder.grabStrength = 41;meleeObject.playerGrabbing = new[] { nativeHolder, meleeHolder };
    NativeMeleeHold("Non-Lifter first holder retains native shared override selection");
    meleeObject.playerGrabbing = Array.Empty<PhysGrabber>();melee.GrabOverridesLogic();
    Near(melee.MeleeStrengthBonus(90f), 0, "Unheld melee retains zero bonus");
}
finally { ItemMelee.HoldingPatchesEnabled = false; }

Console.WriteLine($"PASS: {checks} fixed Lifter math, installed-game IL, emitted IL and host runtime checks.");
Console.WriteLine("Unity gameplay and vanilla guest networking still require in-game verification.");
