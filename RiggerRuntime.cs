using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed partial class StageRoleController
{
    internal float RiggerMultiplier(PlayerAvatar? player, bool duration)
    {
        if (!RiggerActive(player)) return 1f;
        return duration ? _config.RiggerEnemyHoldMultiplier.Value : _config.RiggerDurabilityMultiplier.Value;
    }

    internal float RiggerDamageMultiplier(PlayerAvatar? player) =>
        RiggerActive(player) ? _config.RiggerEnemyDamageMultiplier.Value : 1f;

    private bool RiggerActive(PlayerAvatar? player) =>
        _config.Enabled.Value && IsAuthority() && PlayerState.IsLiving(player) &&
        PlayerHasRole(player, StageRole.Rigger);
}

internal static class RiggerPatches
{
    // Awake reparents the cable's strike collider under a separate cast point,
    // so hierarchy lookup cannot identify it. Weak keys release destroyed cables.
    private static readonly ConditionalWeakTable<HurtCollider, ItemMeleeWhip> StrikeOwners = new();
    private static readonly FieldInfo StrikeAttacker =
        AccessTools.Field(typeof(HurtCollider), "playerCausingHurtOverride");

    [HarmonyPatch(typeof(ItemMeleeWhip), "Awake"), HarmonyPostfix]
    internal static void RegisterStrike(ItemMeleeWhip __instance)
    {
        HurtCollider collider = __instance.crackHurtCollider;
        if (collider == null) return;
        StrikeOwners.Remove(collider);
        StrikeOwners.Add(collider, __instance);
    }

    internal static int EnemyDamage(int damage, HurtCollider? collider)
    {
        if (damage <= 0 || !SemiFunc.IsMasterClientOrSingleplayer() || collider == null ||
            !StrikeOwners.TryGetValue(collider, out ItemMeleeWhip? whip) || whip == null ||
            !whip.isActiveAndEnabled || !ReferenceEquals(whip.crackHurtCollider, collider)) return damage;
        // Native CrackImpactPlay captures the original swing's attacker here.
        // Never attribute an in-flight hit to a new holder of the same cable.
        PlayerAvatar? attacker = StrikeAttacker.GetValue(collider) as PlayerAvatar;
        float multiplier = StageRolesPlugin.Instance?.Controller?.RiggerDamageMultiplier(attacker) ?? 1f;
        if (float.IsNaN(multiplier) || float.IsInfinity(multiplier)) return damage;
        multiplier = Mathf.Clamp(multiplier, 1f, 10f);
        return multiplier == 1f ? damage : Mathf.RoundToInt(Mathf.Clamp(damage * multiplier, 0f, 100000f));
    }

    [HarmonyPatch(typeof(HurtCollider), "EnemyHurt"), HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> DamageTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> code = instructions.ToList();
        FieldInfo damage = AccessTools.Field(typeof(HurtCollider), "enemyDamage");
        FieldInfo force = AccessTools.Field(typeof(HurtCollider), "applyForce");
        int matches = 0;
        for (int i = 0; i + 2 < code.Count; i++)
        {
            // Scale only Hurt's damage argument, leaving the > 0 gate, stun,
            // death-pit/instant-kill paths and the collider's saved stats intact.
            if (!code[i].LoadsField(damage) || code[i + 1].opcode != OpCodes.Ldarg_0 ||
                code[i + 2].opcode != OpCodes.Ldflda || !Equals(code[i + 2].operand, force)) continue;
            code.InsertRange(i + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(RiggerPatches), nameof(EnemyDamage))
            });
            matches++;
            i += 2;
        }
        RequireOne(matches, nameof(EnemyDamage));
        return code;
    }

    // Work only at native spending points, never rewrite a weapon's saved stats.
    // Read the role on every call, so release, death and role changes cannot
    // leave a permanent bonus on a cable passed to another player.
    internal static float HoldElapsed(float elapsed, PlayerAvatar? holder) =>
        elapsed / Multiplier(holder, true);

    internal static float DurabilityCost(float cost, PlayerAvatar? holder) =>
        cost * Multiplier(holder, false);

    private static float Multiplier(PlayerAvatar? holder, bool duration)
    {
        if (!SemiFunc.IsMasterClientOrSingleplayer()) return 1f;
        float value = StageRolesPlugin.Instance?.Controller?.RiggerMultiplier(holder, duration) ?? 1f;
        if (float.IsNaN(value) || float.IsInfinity(value)) return 1f;
        return duration ? Mathf.Clamp(value, 1f, 3f) : Mathf.Clamp(value, 0.1f, 1f);
    }

    [HarmonyPatch(typeof(ItemMeleeWhip), "EnemyHoldTick"), HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> HoldTranspiler(IEnumerable<CodeInstruction> instructions) =>
        ScaleForHolder(instructions, AccessTools.PropertyGetter(typeof(Time), "deltaTime"), null, nameof(HoldElapsed));

    [HarmonyPatch(typeof(ItemMeleeWhip), "ReleasePhysics"), HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> ThrowTranspiler(IEnumerable<CodeInstruction> instructions) =>
        ScaleForHolder(instructions, null, AccessTools.Field(typeof(ItemMeleeWhip), "durabilityPerFling"), nameof(DurabilityCost));

    private static IEnumerable<CodeInstruction> ScaleForHolder(IEnumerable<CodeInstruction> instructions,
        MethodInfo? method, FieldInfo? field, string helper)
    {
        List<CodeInstruction> code = instructions.ToList();
        int matches = 0;
        for (int i = 0; i < code.Count; i++)
        {
            // ReleasePhysics also reads durabilityPerFling in its > 0 gate.
            // Only scale the value used in the multiplication, not that gate.
            bool match = method != null ? code[i].Calls(method) : field != null && code[i].LoadsField(field) &&
                i + 2 < code.Count && code[i + 2].opcode == OpCodes.Mul;
            if (!match) continue;
            code.InsertRange(i + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemMeleeWhip), "holder")),
                CodeInstruction.Call(typeof(RiggerPatches), helper)
            });
            matches++;
            i += 3;
        }
        RequireOne(matches, helper);
        return code;
    }

    [HarmonyPatch(typeof(ItemMeleeWhip), "CrackDurabilityUse"), HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> HitTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> code = instructions.ToList();
        Type window = AccessTools.Inner(typeof(ItemMeleeWhip), "CrackHitWindow");
        FieldInfo durability = AccessTools.Field(window, "durability");
        int matches = 0;
        for (int i = 1; i + 2 < code.Count; i++)
        {
            if (!code[i].LoadsField(durability) || code[i + 1].opcode != OpCodes.Ldarg_3 ||
                code[i + 2].opcode != OpCodes.Mul || !code[i - 1].IsLdloc()) continue;
            // Delayed hit reports belong to the original swing's holder, even
            // if the cable has since changed hands. Native once-per-swing and
            // self-hit checks remain intact. The original holder must still
            // have Rigger when the durability is spent.
            code.InsertRange(i + 1, new[]
            {
                new CodeInstruction(code[i - 1].opcode, code[i - 1].operand),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(window, "holder")),
                CodeInstruction.Call(typeof(RiggerPatches), nameof(DurabilityCost))
            });
            matches++;
            i += 3;
        }
        RequireOne(matches, "CrackDurabilityUse");
        return code;
    }

    private static void RequireOne(int matches, string operation)
    {
        if (matches != 1) throw new InvalidOperationException($"Expected one cable {operation} calculation, found {matches}.");
    }
}
