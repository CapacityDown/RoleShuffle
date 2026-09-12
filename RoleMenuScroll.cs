using System;
using HarmonyLib;
using MenuLib.MonoBehaviors;
using UnityEngine;

namespace REPOJP.StageRoles;

[HarmonyPatch(typeof(MenuScrollBox), "Update")]
internal static class RoleGuideScrollPatch
{
    internal readonly struct ScrollState(REPOScrollView view, float? speed)
    {
        internal REPOScrollView? View { get; } = view;
        internal float? Speed { get; } = speed;
    }

    [HarmonyPrefix]
    internal static void Prefix(MenuScrollBox __instance, out ScrollState __state)
    {
        __state = default;
        if (!RoleMenu.TryGetScrollSettings(__instance, out REPOScrollView view, out float multiplier) ||
            Mathf.Approximately(SemiFunc.InputScrollY(), 0f)) return;

        __state = new ScrollState(view, view.scrollSpeed);
        // MenuLib uses Sign(InputScrollY), losing extra wheel detents batched
        // into one frame on a busy stage. Legacy mouseScrollDelta is already in
        // wheel units, so one detent keeps the existing lobby distance while
        // batched/fractional input preserves its full amount at any frame rate.
        // Apply this before MenuLib updates the handle and content, not afterward.
        view.scrollSpeed = WheelSpeed(view.scrollSpeed ?? 3f, multiplier, Input.mouseScrollDelta.y);
    }

    internal static float WheelSpeed(float baseSpeed, float multiplier, float wheelDelta) =>
        float.IsNaN(wheelDelta) || float.IsInfinity(wheelDelta) ? 0f : baseSpeed * multiplier * Math.Abs(wheelDelta);

    [HarmonyFinalizer]
    internal static Exception? Finalizer(
        MenuScrollBox __instance,
        ref float ___scrollHandleTargetPosition,
        ScrollState __state,
        Exception? __exception)
    {
        ScrollState state = __state;
        if (state.View == null) return __exception;
        // Also restore after an exception: keyboard-only navigation and later
        // frames must never inherit a wheel-specific speed (or compound it).
        state.View.scrollSpeed = state.Speed;
        if (__exception == null)
        {
            float halfHandleHeight = __instance.scrollHandle.sizeDelta.y / 2f;
            float maximum = __instance.scrollBarBackground.rect.height - halfHandleHeight;
            ___scrollHandleTargetPosition = Mathf.Clamp(___scrollHandleTargetPosition, halfHandleHeight, maximum);
        }
        return __exception;
    }
}
