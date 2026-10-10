using System;

namespace REPOJP.StageRoles;

// Rasterize the union of the solid vector triangles. Supersampling smooths both
// inner and outer edges without the seams from overlapping transparent fringes.
internal static class RoleResourceRaster
{
    internal const int Size = 256;
    private const int Samples = 4;
    internal static byte[] Alpha(AbilityMetric metric)
    {
        const int extent = Size * Samples;
        bool[] covered = new bool[extent * extent];
        var mesh = RoleResourceSymbols.Get(metric);
        const float scale = extent / 25f;
        for (int i = 0; i < mesh.Count; i += 3)
        {
            var a = mesh[i]; var b = mesh[i + 1]; var c = mesh[i + 2];
            if (a.Alpha != 1 || b.Alpha != 1 || c.Alpha != 1) continue;
            float ax = a.X * scale, ay = a.Y * scale, bx = b.X * scale, by = b.Y * scale, cx = c.X * scale, cy = c.Y * scale;
            float area = Cross(ax, ay, bx, by, cx, cy);
            if (Math.Abs(area) < 0.000001f) continue;
            float sign = area > 0 ? 1 : -1;
            int left = Math.Max(0, (int)Math.Floor(Math.Min(ax, Math.Min(bx, cx))));
            int right = Math.Min(extent - 1, (int)Math.Ceiling(Math.Max(ax, Math.Max(bx, cx))));
            int top = Math.Max(0, (int)Math.Floor(Math.Min(ay, Math.Min(by, cy))));
            int bottom = Math.Min(extent - 1, (int)Math.Ceiling(Math.Max(ay, Math.Max(by, cy))));
            for (int y = top; y <= bottom; y++)
            for (int x = left; x <= right; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                if (Cross(ax, ay, bx, by, px, py) * sign >= 0 &&
                    Cross(bx, by, cx, cy, px, py) * sign >= 0 &&
                    Cross(cx, cy, ax, ay, px, py) * sign >= 0) covered[y * extent + x] = true;
            }
        }
        byte[] alpha = new byte[Size * Size];
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            int count = 0;
            for (int sy = 0; sy < Samples; sy++)
            for (int sx = 0; sx < Samples; sx++)
                if (covered[(y * Samples + sy) * extent + x * Samples + sx]) count++;
            alpha[y * Size + x] = (byte)((count * 255 + Samples * Samples / 2) / (Samples * Samples));
        }
        return alpha;
    }

    private static float Cross(float ax, float ay, float bx, float by, float x, float y) =>
        (bx - ax) * (y - ay) - (by - ay) * (x - ax);
}
