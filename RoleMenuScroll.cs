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
        if (!RoleMenu.TryGetScrollSettings(__instance, out REPOScrollView view, out float multiplier)) return;
        float wheel = SemiFunc.InputScrollY();
        if (wheel == 0f || float.IsNaN(wheel) || float.IsInfinity(wheel)) return;

        __state = new ScrollState(view, view.scrollSpeed);
        // Use the same input source and direction-based step as MenuLib. The
        // legacy mouseScrollDelta can be fractional or zero while the game's
        // Input System reports a wheel event; multiplying by it shrinks or drops
        // valid input. Keep the established lobby step regardless of raw units.
        view.scrollSpeed = (view.scrollSpeed ?? 3f) * multiplier;
    }

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
