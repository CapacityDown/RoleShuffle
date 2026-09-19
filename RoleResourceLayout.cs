using System;

namespace REPOJP.StageRoles;

internal static class RoleResourceLayout
{
    internal const float Width = 222;
    internal const float RowHeight = 50;

    internal static (float Scale, float Left, float Top) Fit(float width, float height, int rows,
        int percent, int offsetX, int offsetY)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        float reference = height / 540f;
        float margin = 8 * reference;
        // Leave space for vanilla HP/stamina at the bottom, even with every Superbot budget.
        float bottom = 110 * reference;
        float rowHeight = Math.Max(1, rows) * RowHeight;
        float scale = Math.Min(Math.Clamp(percent, 50, 200) / 100f * reference,
            Math.Min(Math.Max(1, width - 2 * margin) / Width, (height - bottom - 2 * margin) / rowHeight));
        float left = Math.Clamp(offsetX * reference, margin, Math.Max(margin, width - margin - Width * scale));
        float top = Math.Clamp(offsetY * reference, margin, Math.Max(margin, height - bottom - margin - rowHeight * scale));
        return (scale, left, top);
    }
}
