using System;

namespace REPOJP.StageRoles;

internal static class RoleResourceLayout
{
    internal const int RowsPerColumn = 4;

    // Native Game Hud units: do not apply a second screen/Canvas scale.
    internal static (float Scale, float Left, float Top, int Rows, float ColumnPitch) Fit(
        float width, float height, int count, float rowWidth, float rowHeight, float rowPitch,
        float nativeLeft, float nativeTop, int percent, int offsetX, int offsetY)
    {
        int rows = Math.Min(RowsPerColumn, Math.Max(1, count));
        int columns = (Math.Max(1, count) + rows - 1) / rows;
        float columnPitch = rowWidth + 12;
        float totalWidth = rowWidth + (columns - 1) * columnPitch;
        float totalHeight = rowHeight + (rows - 1) * rowPitch;
        // Reserve the lower part of the HUD for the role list.
        float bottom = Math.Max(nativeTop + 1, height * 0.6f);
        float scale = Math.Min(Math.Clamp(percent, 50, 200) / 100f,
            Math.Min(Math.Max(1, width - nativeLeft - 2) / totalWidth,
                Math.Max(1, bottom - nativeTop - 2) / totalHeight));
        float left = Math.Clamp(nativeLeft + offsetX, 0, Math.Max(0, width - totalWidth * scale));
        float top = Math.Clamp(nativeTop + offsetY, nativeTop, Math.Max(nativeTop, bottom - totalHeight * scale));
        return (scale, left, top, rows, columnPitch);
    }

    internal static float MaximumOffset(int remaining, float nativeOffset) =>
        nativeOffset + Math.Max(0, remaining.ToString(System.Globalization.CultureInfo.InvariantCulture).Length - 3) * 20;
}
