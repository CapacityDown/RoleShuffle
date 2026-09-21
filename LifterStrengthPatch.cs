using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

[HarmonyPatch(typeof(PhysGrabObject), "PhysicsGrabbingManipulation")]
internal static class LifterStrengthPatch
{
    internal static void Install(Harmony harmony)
    {
        RoleOverhaulRules.LifterPhysicsAvailable = false;
        try { harmony.PatchAll(typeof(LifterStrengthPatch)); }
        catch (Exception error)
        {
            RoleOverhaulRules.LifterPhysicsAvailable = false;
            StageRolesPlugin.ModLogger.LogWarning($"Lifter physics patch failed: {error}");
        }
        if (!RoleOverhaulRules.LifterPhysicsAvailable)
            StageRolesPlugin.ModLogger.LogWarning("Fixed-strength Lifter is unavailable and excluded from random assignment.");
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var source = instructions.ToList();
        RoleOverhaulRules.LifterPhysicsAvailable = false;
        MethodInfo lerp = AccessTools.Method(typeof(Mathf), nameof(Mathf.Lerp), new[] { typeof(float), typeof(float), typeof(float) });
        var sites = source.Select((code, index) => (code, index)).Where(item => item.code.Calls(lerp)).ToArray();
        var grabbers = __originalMethod.GetMethodBody()?.LocalVariables.Where(local => local.LocalType == typeof(PhysGrabber)).ToArray();
        FieldInfo strength = AccessTools.Field(typeof(PhysGrabber), nameof(PhysGrabber.grabStrength));
        // 0.4.4.3 has precisely two scalar blends and two raw strength loads
        // from the same foreach grabber: translation first, rotation second.
        if (!LifterStrengthRuntime.FieldsAvailable || sites.Length != 2 || grabbers?.Length != 1)
            return source;
        int local = grabbers[0].LocalIndex;
        var loads = source.Select((code, index) => (code, index))
            .Where(item => item.code.opcode == OpCodes.Ldfld && Equals(item.code.operand, strength)).ToArray();
        if (loads.Length != 2 || loads[0].index == 0 || loads[1].index == 0 ||
            LoadIndex(source[loads[0].index - 1]) != local || LoadIndex(source[loads[1].index - 1]) != local ||
            !(loads[0].index < sites[0].index && sites[0].index < loads[1].index && loads[1].index < sites[1].index))
            return source;

        MethodInfo replacement = AccessTools.Method(typeof(LifterStrengthRuntime), nameof(LifterStrengthRuntime.Blend));
        MethodInfo nativeInput = AccessTools.Method(typeof(LifterStrengthRuntime), nameof(LifterStrengthRuntime.NativeStrengthInput));
        var result = new List<CodeInstruction>(source.Count + 12);
        int site = 0;
        foreach (CodeInstruction instruction in source)
        {
            if (instruction.opcode == OpCodes.Ldfld && Equals(instruction.operand, strength))
            {
                result.Add(instruction);
                result.Add(new CodeInstruction(OpCodes.Ldarg_0));
                result.Add(new CodeInstruction(OpCodes.Ldloc, local));
                result.Add(new CodeInstruction(OpCodes.Call, nativeInput));
                continue;
            }
            if (!instruction.Calls(lerp)) { result.Add(instruction); continue; }
            var owner = new CodeInstruction(OpCodes.Ldarg_0);
            owner.MoveLabelsFrom(instruction);
            owner.MoveBlocksFrom(instruction);
            result.Add(owner);
            result.Add(new CodeInstruction(OpCodes.Ldloc, local));
            result.Add(new CodeInstruction(site++ == 0 ? OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1));
            instruction.operand = replacement;
            result.Add(instruction);
        }
        RoleOverhaulRules.LifterPhysicsAvailable = true;
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
