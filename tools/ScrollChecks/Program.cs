using REPOJP.StageRoles;
using UnityEngine;

int checks = 0;
void Check(bool result, string label)
{
    if (!result) throw new Exception(label);
    checks++;
}
bool Close(float a, float b) => Math.Abs(a - b) < 0.1f;

// Reproduce the installed MenuLib scroll-speed calculation between
// the real production prefix/finalizer. This does not run Unity or Harmony.
float Scroll(IEnumerable<float> frames, float multiplier, float legacyDelta = 0, float range = 100000, float bar = 1000)
{
    var box = RoleMenu.Box;
    box.scrollBarBackground.rect = new Rect { height = bar };
    RoleMenu.Multiplier = multiplier;
    float target = bar / 2, first = target;
    foreach (float wheel in frames)
    {
        // Deliberately independent: the two input APIs need not report the same
        // units or even a nonzero value in the same frame (build-417 regression).
        Input.mouseScrollDelta = new Vector2 { y = legacyDelta };
        SemiFunc.Scroll = wheel;
        RoleGuideScrollPatch.Prefix(box, out var state);
        float speed = RoleMenu.View.scrollSpeed ?? 3;
        target += Math.Sign(SemiFunc.Scroll) * speed * 10 / range * bar;
        Check(RoleGuideScrollPatch.Finalizer(box, ref target, state, null) == null, "Normal update remains successful");
        Check(RoleMenu.View.scrollSpeed == 3, "Per-frame speed is restored");
    }
    return (target - first) * range / bar;
}

foreach (float multiplier in new[] { 0.5f, 1f, 4f, 30f })
{
    float step = 30 * multiplier; // Existing MenuLib base speed (3 * 10), then the page multiplier.
    foreach (float gameDelta in new[] { 0.01f, 1f, 120f, 240f })
        foreach (float legacyDelta in new[] { 0f, 0.001f, 0.1f, 1f, 120f })
            Check(Close(Scroll(new[] { gameDelta }, multiplier, legacyDelta), step), "Valid game wheel input always preserves the lobby step, even with absent/fractional legacy input");
    Check(Close(Scroll(Enumerable.Repeat(120f, 12), multiplier), 12 * step), "Repeated events do not compound the temporary multiplier");
    Check(Close(Scroll(new[] { -0.01f, -1f, -120f }, multiplier), -3 * step), "Reverse game input has the same step size");
    Check(Close(Scroll(new[] { 1f, 0f, 1f, 0f }, multiplier), 2 * step), "Idle frames add no movement");
    Check(Close(Scroll(new[] { 1f }, multiplier, range: 4000, bar: 320), step), "Shorter content and a different scrollbar keep the same content distance");
}

RoleMenu.View.scrollSpeed = 3;
SemiFunc.Scroll = 0;
RoleGuideScrollPatch.Prefix(RoleMenu.Box, out _);
Check(RoleMenu.View.scrollSpeed == 3, "Keyboard-only and drag frames retain the normal speed");
SemiFunc.Scroll = 120;
Input.mouseScrollDelta = new Vector2 { y = 1 };
RoleGuideScrollPatch.Prefix(new MenuScrollBox(), out _);
Check(RoleMenu.View.scrollSpeed == 3, "Other menus are untouched");
RoleMenu.Open = false;
RoleGuideScrollPatch.Prefix(RoleMenu.Box, out _);
Check(RoleMenu.View.scrollSpeed == 3, "Closed Roles menu is untouched");
RoleMenu.Open = true;
Input.mouseScrollDelta = new Vector2 { y = 0 };
RoleGuideScrollPatch.Prefix(RoleMenu.Box, out var mismatch);
Check(RoleMenu.View.scrollSpeed == 3 * RoleMenu.Multiplier, "Valid game input is not suppressed when the legacy API reports zero");
float position = 100;
RoleGuideScrollPatch.Finalizer(RoleMenu.Box, ref position, mismatch, null);

RoleMenu.View.scrollSpeed = null;
Input.mouseScrollDelta = new Vector2 { y = 2 };
RoleGuideScrollPatch.Prefix(RoleMenu.Box, out var failed);
var error = new InvalidOperationException("simulated update failure");
Check(ReferenceEquals(RoleGuideScrollPatch.Finalizer(RoleMenu.Box, ref position, failed, error), error), "Original exceptions propagate");
Check(RoleMenu.View.scrollSpeed == null, "Nullable speed is restored even after failure");
RoleMenu.View.scrollSpeed = 3;
RoleGuideScrollPatch.Prefix(RoleMenu.Box, out var bottom);
position = -1000;
RoleGuideScrollPatch.Finalizer(RoleMenu.Box, ref position, bottom, null);
Check(position == 20, "Large downward input stays at the bottom boundary");
RoleGuideScrollPatch.Prefix(RoleMenu.Box, out var top);
position = 10000;
RoleGuideScrollPatch.Finalizer(RoleMenu.Box, ref position, top, null);
Check(position == RoleMenu.Box.scrollBarBackground.rect.height - 20, "Large upward input stays at the top boundary");
foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
{
    SemiFunc.Scroll = invalid;
    RoleGuideScrollPatch.Prefix(RoleMenu.Box, out _);
    Check(RoleMenu.View.scrollSpeed == 3, "Invalid game input does not alter the speed setting");
}
Console.WriteLine($"PASS: {checks} scroll checks (input API mismatch, lobby baseline, geometry, scope, restoration and bounds).");
