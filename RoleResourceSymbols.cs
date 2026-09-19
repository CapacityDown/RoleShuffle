using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

// Solid, single-color HUD pictograms. Coordinates share vanilla's 25x25 icon box.
internal static class RoleResourceSymbols
{
    internal readonly struct Point(float x, float y)
    {
        public float X { get; } = x;
        public float Y { get; } = y;
    }
    private static readonly Dictionary<AbilityMetric, IReadOnlyList<Point>> Cache = new();

    internal static IReadOnlyList<Point> Get(AbilityMetric metric)
    {
        if (Cache.TryGetValue(metric, out var cached)) return cached;
        List<Point> points = new();
        void Polygon(params float[] xy)
        {
            for (int i = 2; i < xy.Length - 2; i += 2)
            {
                points.Add(new Point(xy[0], xy[1]));
                points.Add(new Point(xy[i], xy[i + 1]));
                points.Add(new Point(xy[i + 2], xy[i + 3]));
            }
        }
        void Box(float x, float y, float w, float h) => Polygon(x,y,x+w,y,x+w,y+h,x,y+h);
        void Line(float ax, float ay, float bx, float by, float width)
        {
            float length = (float)Math.Sqrt((bx-ax)*(bx-ax)+(by-ay)*(by-ay));
            float dx = -(by-ay)/length*width/2, dy = (bx-ax)/length*width/2;
            Polygon(ax+dx,ay+dy,bx+dx,by+dy,bx-dx,by-dy,ax-dx,ay-dy);
        }
        void Circle(float x, float y, float radius)
        {
            for (int i = 0; i < 20; i++)
            {
                float a = i * (float)Math.PI / 10, b = (i+1) * (float)Math.PI / 10;
                Polygon(x,y,x+(float)Math.Cos(a)*radius,y+(float)Math.Sin(a)*radius,
                    x+(float)Math.Cos(b)*radius,y+(float)Math.Sin(b)*radius);
            }
        }
        void Cross(float x, float y, float size)
        {
            Box(x+size/3,y,size/3,size); Box(x,y+size/3,size,size/3);
        }
        switch (metric)
        {
            case AbilityMetric.Medic: Cross(3,3,19); break; // Native Plus sprite is preferred at runtime.
            case AbilityMetric.MageRecovery:
                Cross(2,8,15);
                Polygon(18,1,20,6,18,11,16,6); Polygon(13,6,18,4,23,6,18,8);
                break;
            case AbilityMetric.Rescuer:
                Circle(8,5,3); Box(5,10,6,10); Box(3,11,10,3);
                Box(17,9,4,13); Polygon(12,10,19,2,24,10);
                break;
            case AbilityMetric.Phoenix:
                Polygon(12,3,16,10,13,22,9,13); // rising bird, spread wings
                Polygon(11,12,2,5,4,15,12,20); Polygon(14,12,23,5,21,15,13,20);
                Polygon(11,4,17,7,12,8);
                break;
            case AbilityMetric.Repair:
                // Open wrench head, diagonal shank and rounded handle.
                Line(5,20,16,9,5); Circle(5,20,3);
                Line(13,3,12,8,4); Line(12,8,17,13,5);
                Line(17,13,22,12,4); Line(22,12,23,9,3);
                break;
            case AbilityMetric.Charge:
                Polygon(14,1,4,14,12,14); Polygon(12,11,21,11,9,24);
                break;
            case AbilityMetric.Wager:
                Box(3,3,19,3); Box(3,19,19,3); Box(3,6,3,13); Box(19,6,3,13);
                Circle(9,9,1.8f); Circle(16,16,1.8f); Circle(12.5f,12.5f,1.8f);
                break;
            case AbilityMetric.Contracts:
                Box(4,4,3,19); Box(18,4,3,19); Box(7,20,11,3); Box(4,4,17,3);
                Box(9,1,7,5); Box(9,10,7,2.5f); Box(9,15,7,2.5f);
                break;
        }
        return Cache[metric] = points.ToArray();
    }
}
