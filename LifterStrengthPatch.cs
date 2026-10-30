using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace REPOJP.StageRoles;

[HarmonyPatch]
internal static class LifterStrengthPatch
{
    private static bool _gripPatched;
    private static bool _torquePatched;

    internal static void Install(Harmony harmony)
    {
        RoleOverhaulRules.LifterPhysicsAvailable = _gripPatched = _torquePatched = false;
        try
        {
            harmony.PatchAll(typeof(LifterMeleeHoldingPatch));
            harmony.PatchAll(typeof(LifterStrengthPatch));
        }
        catch (Exception error)
        {
            RoleOverhaulRules.LifterPhysicsAvailable = false;
            StageRolesPlugin.ModLogger.LogWarning($"Lifter physics patch failed: {error}");
        }
        if (!RoleOverhaulRules.LifterPhysicsAvailable)
            StageRolesPlugin.ModLogger.LogWarning("Lifter small-item handling is unavailable and excluded from random assignment.");
    }

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(PhysGrabObject), "PhysicsGrabbingManipulation");
        yield return AccessTools.Method(typeof(PhysGrabObject), "GrabTorqueGet");
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var source = instructions.ToList();
        bool rotation = __originalMethod.Name == "GrabTorqueGet";
        if (rotation) _torquePatched = false; else _gripPatched = false;
        RoleOverhaulRules.LifterPhysicsAvailable = false;
        MethodInfo helper = AccessTools.Method(typeof(PhysGrabObject), "GrabStrengthGet");
        FieldInfo strength = AccessTools.Field(typeof(PhysGrabber), nameof(PhysGrabber.grabStrength));
        var sites = source.Select((code, index) => (code, index)).Where(x => x.code.Calls(helper)).ToArray();
        var loads = source.Select((code, index) => (code, index))
            .Where(x => x.code.opcode == OpCodes.Ldfld && Equals(x.code.operand, strength)).ToArray();
        var locals = __originalMethod.GetMethodBody()?.LocalVariables.Where(x => x.LocalType == typeof(PhysGrabber)).ToArray();
        if (!LifterStrengthRuntime.FieldsAvailable || sites.Length != 1 || loads.Length != 1 ||
            loads[0].index == 0 || sites[0].index < 3) return source;
        int grabberLocal = rotation ? -1 : locals?.Length == 1 ? locals[0].LocalIndex : -1;
        CodeInstruction grabber = rotation ? new CodeInstruction(OpCodes.Ldarg_1) : new CodeInstruction(OpCodes.Ldloc, grabberLocal);
        if (rotation ? __originalMethod.GetParameters().FirstOrDefault()?.ParameterType != typeof(PhysGrabber) ||
                source[loads[0].index - 1].opcode != OpCodes.Ldarg_1
            : grabberLocal < 0 || LoadIndex(source[loads[0].index - 1]) != grabberLocal) return source;
        int site = sites[0].index;
        // Both native call sites are this, local raw strength, constant divider.
        int rawLocal = LoadIndex(source[site - 2]);
        if (source[site - 3].opcode != OpCodes.Ldarg_0 || rawLocal < 0 ||
            source[site - 1].opcode != OpCodes.Ldc_R4 ||
            !Equals(source[site - 1].operand, rotation ? float.PositiveInfinity : 30f) ||
            loads[0].index >= site) return source;
        MethodInfo input = AccessTools.Method(typeof(LifterStrengthRuntime), nameof(LifterStrengthRuntime.NativeStrengthInput));
        var result = new List<CodeInstruction>(source.Count + 3);
        for (int i = 0; i < source.Count; i++)
        {
            result.Add(source[i]);
            if (i == loads[0].index)
            {
                result.Add(new CodeInstruction(OpCodes.Ldarg_0));
                result.Add(new CodeInstruction(grabber));
                result.Add(new CodeInstruction(OpCodes.Call, input));
            }
        }
        if (rotation) _torquePatched = true; else _gripPatched = true;
        RoleOverhaulRules.LifterPhysicsAvailable = _gripPatched && _torquePatched;
        return result;
    }

    private static int LoadIndex(CodeInstruction code)
    {
        if (code.opcode == OpCodes.Ldloc_0) return 0;
        if (code.opcode == OpCodes.Ldloc_1) return 1;
        if (code.opcode == OpCodes.Ldloc_2) return 2;
        if (code.opcode == OpCodes.Ldloc_3) return 3;
        if (code.opcode != OpCodes.Ldloc && code.opcode != OpCodes.Ldloc_S) return -1;
        return code.operand is LocalVariableInfo variable ? variable.LocalIndex : Convert.ToInt32(code.operand);
    }
}
