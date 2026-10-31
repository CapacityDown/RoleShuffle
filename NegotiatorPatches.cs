using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace REPOJP.StageRoles;

internal static class NegotiatorPatches
{
    private static readonly FieldInfo ItemValue = AccessTools.Field(typeof(ItemAttributes), "value");
    private static int Value(PhysGrabObject? item) => item != null && item.GetComponent<ItemAttributes>() is ItemAttributes attributes
        ? (int)(ItemValue.GetValue(attributes) ?? 0) : 0;
    private static NegotiatorRuntime? Runtime => StageRolesPlugin.Instance?.Negotiator;
    // Only the limit comparison is bypassed. The native counter and its RPC
    // payload retain the ordinary players' three attempts, without a fake reset.
    internal static int AttemptCount(int attempts, PlayerAvatar player) => Runtime?.CanUse(player) == true ? 0 : attempts;
    internal static int SpendAttempt(int incremented, PlayerAvatar player) => Runtime?.CanUse(player) == true ? incremented - 1 : incremented;
    internal static bool Positive(bool original, ShopKeeper keeper, PhysGrabObject item) =>
        Runtime?.Guaranteed(keeper, item) == true || original;

    [HarmonyPatch(typeof(ShopKeeper), "ItemPresentationTargetValid"), HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> TargetTranspiler(IEnumerable<CodeInstruction> code) => Attempts(code, false);
    [HarmonyPatch(typeof(ShopKeeper), "ItemNoticeStart"), HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> NoticeTranspiler(IEnumerable<CodeInstruction> code) => Attempts(code, false);
    [HarmonyPatch(typeof(ShopKeeper), "TryStartItemDiscount"), HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> StartTranspiler(IEnumerable<CodeInstruction> code) => Attempts(code, true);

    private static IEnumerable<CodeInstruction> Attempts(IEnumerable<CodeInstruction> instructions, bool spending)
    {
        List<CodeInstruction> code = instructions.ToList();
        FieldInfo counter = AccessTools.Field(typeof(ShopKeeper), "itemDiscountAttempts");
        int comparisons = 0, writes = 0;
        for (int i = 0; i < code.Count; i++)
        {
            if (code[i].LoadsField(counter) && i + 1 < code.Count && code[i + 1].LoadsConstant(3))
            {
                code.InsertRange(i + 1, new[] { new CodeInstruction(OpCodes.Ldarg_2),
                    CodeInstruction.Call(typeof(NegotiatorPatches), nameof(AttemptCount)) });
                comparisons++; i += 2;
            }
            else if (spending && code[i].StoresField(counter))
            {
                CodeInstruction argument = new(OpCodes.Ldarg_2);
                argument.MoveLabelsFrom(code[i]); argument.MoveBlocksFrom(code[i]);
                code.InsertRange(i, new[] { argument, CodeInstruction.Call(typeof(NegotiatorPatches), nameof(SpendAttempt)) });
                writes++; i += 2;
            }
        }
        if (comparisons != 1 || writes != (spending ? 1 : 0))
            throw new InvalidOperationException($"Shopkeeper attempt pattern changed ({comparisons}/{writes}).");
        return code;
    }

    [HarmonyPatch(typeof(ShopKeeper), "TryStartItemDiscount"), HarmonyPostfix]
    private static void Started(ShopKeeper __instance, bool __result, PhysGrabObject _physGrab,
        PlayerAvatar _player, ShopKeeper.State ___currentState, PhysGrabObject ___itemReactObject)
    {
        if (__result && ___currentState == ShopKeeper.State.ScanObject && ___itemReactObject == _physGrab)
            Runtime?.Reserve(__instance, _physGrab, _player);
    }

    [HarmonyPatch(typeof(ShopKeeper), "ItemVerdictPositiveGet"), HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> VerdictTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> code = instructions.ToList();
        MethodInfo random = AccessTools.Method(typeof(UnityEngine.Random), "Range", new[] { typeof(int), typeof(int) });
        int matches = 0;
        for (int i = 0; i + 2 < code.Count; i++)
        {
            if (!code[i].Calls(random) || !code[i + 1].LoadsConstant(0) || code[i + 2].opcode != OpCodes.Ceq) continue;
            // Change the decision before native ItemShownSet records dislike.
            code.InsertRange(i + 3, new[] { new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ShopKeeper), "itemReactObject")),
                CodeInstruction.Call(typeof(NegotiatorPatches), nameof(Positive)) });
            matches++; i += 6;
        }
        if (matches != 1) throw new InvalidOperationException($"Shopkeeper verdict pattern changed ({matches}).");
        return code;
    }

    [HarmonyPatch(typeof(ShopKeeper), "ItemDiscountApply"), HarmonyPrefix]
    private static void BeforeDiscount(PhysGrabObject ___itemReactObject, out int __state) =>
        __state = Value(___itemReactObject);
    [HarmonyPatch(typeof(ShopKeeper), "ItemDiscountApply"), HarmonyPostfix]
    private static void AfterDiscount(ShopKeeper __instance, PhysGrabObject ___itemReactObject, int __state)
    {
        ItemAttributes? item = ___itemReactObject != null ? ___itemReactObject.GetComponent<ItemAttributes>() : null;
        if (item != null && __state > Value(___itemReactObject)) Runtime?.Complete(__instance, ___itemReactObject);
    }
    [HarmonyPatch(typeof(ShopKeeper), "ItemReactionClearRefs"), HarmonyPostfix]
    private static void Release(ShopKeeper __instance) => Runtime?.Release(__instance);
    [HarmonyPatch(typeof(StatsManager), nameof(StatsManager.ResetAllStats)), HarmonyPostfix]
    private static void ResetRun() => Runtime?.Reset();
}
