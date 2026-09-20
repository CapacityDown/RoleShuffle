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
Check(LifterStrengthRuntime.FieldsAvailable, "Runtime override fields found");
Near(RoleOverhaulRules.LifterEffectiveStrength(true), 7.087792207792208, "Light target uses upgrade level ONE, not zero");
Near(RoleOverhaulRules.LifterEffectiveStrength(false), 7.160727272727273, "Heavy target uses upgrade level ONE");

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
int[] sites = installed.Select((code, index) => (code, index)).Where(x => x.code.Calls(blendMethod)).Select(x => x.index).ToArray();
Check(installed[sites[0]-1].opcode == OpCodes.Ldc_I4_0 && installed[sites[1]-1].opcode == OpCodes.Ldc_I4_1, "Grip and rotation site order");
var unsupported = InstalledInstructions(); unsupported.Add(new CodeInstruction(OpCodes.Call, lerp));
var rejected = LifterStrengthPatch.Transpiler(unsupported, proxyMethod).ToList();
Check(!RoleOverhaulRules.LifterPhysicsAvailable && rejected.All(i => !i.Calls(blendMethod)), "Unknown game shape fails without partial patch");

// Decode with real Harmony, transform with production code, then emit/run it.
var fixture = typeof(PhysGrabObject).GetMethod(nameof(PhysGrabObject.PhysicsGrabbingManipulation))!;
var executable = new DynamicMethod("LifterPatchedFixture", typeof(void), new[] { typeof(PhysGrabObject) }, typeof(PhysGrabObject).Module, true);
var generator = executable.GetILGenerator();
var original = PatchProcessor.GetOriginalInstructions(fixture, generator);
var patched = LifterStrengthPatch.Transpiler(original, fixture).ToList();
if (!RoleOverhaulRules.LifterPhysicsAvailable)
{
    Console.WriteLine(string.Join("\n", fixture.GetMethodBody()!.LocalVariables.Select(v => $"local {v.LocalIndex}: {v.LocalType}")));
    Console.WriteLine(string.Join("\n", original.Select((code, index) => $"{index}: {code}")));
}
Check(RoleOverhaulRules.LifterPhysicsAvailable && patched.Count(i => i.Calls(blendMethod)) == 2, "Executable fixture patched at both sites");
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
var run = executable.CreateDelegate<Action<PhysGrabObject>>();
var lifter = new PhysGrabber();
var other = new PhysGrabber { playerAvatar = new PlayerAvatar { Role = StageRole.Tank } };
var owner = new PhysGrabObject { playerGrabbing = new[] { lifter, other } };
foreach (float mass in new[] { 0.5f, 1.999f, 2f, 4f, 8f, 100f })
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
    Check(!RoleOverhaulRules.LifterBaseReachesTarget(level), "Every supported Base level remains eligible");
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
foreach (var field in typeof(PhysGrabObject).GetFields().Where(f => f.Name.StartsWith("override")))
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

Console.WriteLine($"PASS: {checks} fixed Lifter math, installed-game IL, emitted IL and host runtime checks.");
Console.WriteLine("Unity gameplay and vanilla guest networking still require in-game verification.");
