using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed class MageStaffEffectMarker : MonoBehaviour { }

internal static class MageStaffRuntime
{
    private static readonly FieldInfo? LastPlayerGrabbingField = AccessTools.Field(typeof(PhysGrabObject), "lastPlayerGrabbing");
    internal const float DurationMultiplier = 1.3f;

    internal static bool IsMageStaff(GameObject? staff)
    {
        if (staff == null || !SemiFunc.IsMasterClientOrSingleplayer()) return false;
        if (staff.GetComponent<ItemStaffTorque>() == null &&
            staff.GetComponent<ItemStaffZeroGravity>() == null &&
            staff.GetComponent<ItemStaffVoid>() == null &&
            staff.GetComponent<ValuableWizardStaff>() == null) return false;
        PhysGrabObject? phys = staff.GetComponent<PhysGrabObject>();
        return phys != null && phys.playerGrabbing.Count > 0 &&
            StageRolesPlugin.Instance?.Controller?.PlayerHasRole(
                LastPlayerGrabbingField?.GetValue(phys) as PlayerAvatar, StageRole.Mage) == true;
    }

    internal static void PrepareProjectile(SlowProjectile projectile, GameObject staff)
    {
        if (!IsMageStaff(staff) || projectile.GetComponent<MageStaffEffectMarker>() != null)
            return;
        projectile.gameObject.AddComponent<MageStaffEffectMarker>();
        foreach (SemiAreaOfEffect area in projectile.GetComponentsInChildren<SemiAreaOfEffect>(true))
        {
            area.affectTimeMin *= DurationMultiplier;
            area.affectTimeMax *= DurationMultiplier;
            area.enemyHighestDifficultyTimeMin *= DurationMultiplier;
            area.enemyHighestDifficultyTimeMax *= DurationMultiplier;
            area.enemyLowestDifficultyTimeMin *= DurationMultiplier;
            area.enemyLowestDifficultyTimeMax *= DurationMultiplier;
        }
    }

    internal static GameObject SpawnNetworkImpact(string prefabName, Vector3 position,
        Quaternion rotation, byte group, object[] data, SlowProjectile projectile)
    {
        GameObject instance = PhotonNetwork.Instantiate(prefabName, position, rotation, group, data);
        MarkImpact(instance, projectile);
        return instance;
    }

    internal static GameObject SpawnLocalImpact(GameObject prefab, Vector3 position,
        Quaternion rotation, SlowProjectile projectile)
    {
        GameObject instance = UnityEngine.Object.Instantiate(prefab, position, rotation);
        MarkImpact(instance, projectile);
        return instance;
    }

    private static void MarkImpact(GameObject instance, SlowProjectile projectile)
    {
        if (projectile.GetComponent<MageStaffEffectMarker>() == null ||
            !SemiFunc.IsMasterClientOrSingleplayer()) return;
        foreach (SlowWalkerAttack attack in instance.GetComponentsInChildren<SlowWalkerAttack>(true))
        {
            if (attack.enemy == null && attack.autoStart &&
                attack.GetComponent<MageStaffEffectMarker>() == null)
                attack.gameObject.AddComponent<MageStaffEffectMarker>();
        }
    }

    internal static float BeamDuration(float duration, ValuableWizardStaff staff)
    {
        MageBeamCarrier? carrier = staff.GetComponent<MageBeamCarrier>();
        if (carrier != null) return carrier.BeamDurationSeconds;
        return IsMageStaff(staff.gameObject) ? duration * DurationMultiplier : duration;
    }
}

[HarmonyPatch(typeof(SlowProjectile), nameof(SlowProjectile.SetSpawner))]
internal static class MageStaffProjectilePatch
{
    [HarmonyPostfix]
    private static void Postfix(SlowProjectile __instance, GameObject spawnerObject)
    {
        try { MageStaffRuntime.PrepareProjectile(__instance, spawnerObject); }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug($"Mage staff duration skipped: {exception.Message}");
        }
    }
}

[HarmonyPatch]
internal static class MageStaffImpactPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(SlowProjectile), "DestroyRPC");
        yield return AccessTools.Method(typeof(SlowProjectile), "ExplosionRPC");
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            MethodInfo? replacement = null;
            if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo method &&
                method.Name == "Instantiate")
            {
                ParameterInfo[] args = method.GetParameters();
                if (method.DeclaringType == typeof(PhotonNetwork) && args.Length == 5 &&
                    args[0].ParameterType == typeof(string) && args[3].ParameterType == typeof(byte))
                    replacement = AccessTools.Method(typeof(MageStaffRuntime), nameof(MageStaffRuntime.SpawnNetworkImpact));
                else if (method.DeclaringType == typeof(UnityEngine.Object) &&
                    method.IsGenericMethod && method.GetGenericArguments()[0] == typeof(GameObject) &&
                    args.Length == 3 && args[1].ParameterType == typeof(Vector3) &&
                    args[2].ParameterType == typeof(Quaternion))
                    replacement = AccessTools.Method(typeof(MageStaffRuntime), nameof(MageStaffRuntime.SpawnLocalImpact));
            }
            if (replacement != null)
            {
                CodeInstruction source = new(OpCodes.Ldarg_0);
                source.MoveLabelsFrom(instruction);
                source.MoveBlocksFrom(instruction);
                yield return source;
                instruction.operand = replacement;
            }
            yield return instruction;
        }
    }
}

[HarmonyPatch(typeof(SlowWalkerAttack), "StateImplosion")]
internal static class MageStaffVoidDurationPatch
{
    [HarmonyPrefix]
    private static void Prefix(SlowWalkerAttack __instance, bool ___stateStart,
        bool ___stateFixed, out bool __state)
    {
        __state = ___stateStart && !___stateFixed &&
            SemiFunc.IsMasterClientOrSingleplayer() && __instance.enemy == null &&
            __instance.GetComponent<MageStaffEffectMarker>() != null;
    }

    [HarmonyPostfix]
    private static void Postfix(bool __state, ref float ___stateTimer)
    {
        if (__state) ___stateTimer *= MageStaffRuntime.DurationMultiplier;
    }
}

[HarmonyPatch(typeof(ValuableWizardStaff), nameof(ValuableWizardStaff.StaffLaser))]
internal static class MageStaffBeamDurationPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo range = AccessTools.Method(typeof(UnityEngine.Random), "Range", new[] { typeof(float), typeof(float) });
        foreach (CodeInstruction instruction in instructions)
        {
            yield return instruction;
            if (instruction.Calls(range))
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return CodeInstruction.Call(typeof(MageStaffRuntime), nameof(MageStaffRuntime.BeamDuration));
            }
        }
    }
}
