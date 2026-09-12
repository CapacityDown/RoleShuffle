using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

[HarmonyPatch(typeof(MenuScrollBox), "Update")]
internal static class RoleGuideScrollPatch
{
    // The game's Windows Input System reports 120 units per wheel detent.
    private const float WheelUnitsPerNotch = 120f;
    private const float BodyLinesPerNotch = 3f;

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> code = new(instructions);
        var input = AccessTools.Method(typeof(SemiFunc), nameof(SemiFunc.InputScrollY));
        var height = AccessTools.Field(typeof(MenuScrollBox), "scrollHeight");
        int matches = 0;
        for (int i = 0; i + 5 < code.Count; i++)
        {
            // Replace only the wheel delta after the native hover/input gates.
            // MenuLib 2.5.4 retains an old scrollSpeed IL hook but does not
            // register it. Changing REPOScrollView.scrollSpeed has no effect.
            if (!code[i].Calls(input) || code[i + 1].opcode != OpCodes.Ldarg_0 ||
                !code[i + 2].LoadsField(height) || code[i + 3].opcode != OpCodes.Ldc_R4 ||
                !Equals(code[i + 3].operand, 0.01f) || code[i + 4].opcode != OpCodes.Mul ||
                code[i + 5].opcode != OpCodes.Div) continue;

            code.InsertRange(i + 6, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MenuScrollBox), "scrollerStartPosition")),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MenuScrollBox), "scrollerEndPosition")),
                CodeInstruction.Call(typeof(RoleGuideScrollPatch), nameof(WheelStep))
            });
            matches++;
            i += 11;
        }
        if (matches != 1)
            throw new InvalidOperationException($"Expected one MenuScrollBox wheel calculation, found {matches}.");
        return code;
    }

    internal static float WheelStep(float nativeStep, MenuScrollBox box, float start, float end)
    {
        if (!RoleMenu.TryGetScrollSettings(box, out float lineHeight)) return nativeStep;
        float wheel = SemiFunc.InputScrollY();
        float contentTravel = Mathf.Abs(start - end);
        float handleTravel = box.scrollBarBackground.rect.height - box.scrollHandle.sizeDelta.y;
        if (!IsFinite(wheel) || wheel == 0f || !IsFinite(contentTravel) || contentTravel <= 0f ||
            !IsFinite(handleTravel) || handleTravel <= 0f) return 0f;

        // Convert a fixed content distance to handle distance using the current
        // layout. Native scrollHeight can still describe the previous page.
        // Keep native clamping, animation, keyboard input and dragging intact.
        // Preserve magnitude: multiple detents can arrive in one input frame,
        // especially at lower FPS. Fractional input keeps its proportional distance.
        return wheel / WheelUnitsPerNotch * BodyLinesPerNotch * lineHeight * handleTravel / contentTravel;
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
