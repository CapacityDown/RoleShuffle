using System;

namespace REPOJP.StageRoles;

internal static class RoleResourceLayout
{
    // Native Game Hud units: do not apply a second screen/Canvas scale.
    internal static (float Scale, float Left, float Top) Fit(
        float width, float height, int count, float rowWidth, float rowHeight, float rowPitch,
        float nativeLeft, float nativeTop, int percent, int offsetX, int offsetY)
    {
        int rows = Math.Max(1, count);
        float totalHeight = rowHeight + (rows - 1) * rowPitch;
        // Reserve the lower part of the HUD for the role list.
        float bottom = Math.Max(nativeTop + 1, height * 0.6f);
        float scale = Math.Min(Math.Clamp(percent, 50, 200) / 100f,
            Math.Min(Math.Max(1, width - nativeLeft - 2) / rowWidth,
                Math.Max(1, bottom - nativeTop - 2) / totalHeight));
        float left = Math.Clamp(nativeLeft + offsetX, 0, Math.Max(0, width - rowWidth * scale));
        float top = Math.Clamp(nativeTop + offsetY, nativeTop, Math.Max(nativeTop, bottom - totalHeight * scale));
        return (scale, left, top);
    }

    internal static float MaximumOffset(int remaining, float nativeOffset) =>
        nativeOffset + Math.Max(0, remaining.ToString(System.Globalization.CultureInfo.InvariantCulture).Length - 3) * 20;
}
