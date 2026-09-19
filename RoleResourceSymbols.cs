using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

// Smooth monochrome silhouettes, in vanilla's 25x25 icon box.
internal static class RoleResourceSymbols
{
    internal readonly struct Point(float x, float y, float alpha = 1)
    {
        public float X { get; } = x;
        public float Y { get; } = y;
        public float Alpha { get; } = alpha;
    }
    private const float Fringe = 0.28f;
    private static readonly Dictionary<AbilityMetric, IReadOnlyList<Point>> Cache = new();

    internal static IReadOnlyList<Point> Get(AbilityMetric metric)
    {
        if (Cache.TryGetValue(metric, out var cached)) return cached;
        List<Point> mesh = new();
        void Fill(params float[] xy)
        {
            List<Point> shape = new();
            for (int i = 0; i < xy.Length; i += 2) shape.Add(new Point(xy[i],xy[i+1]));
            FillShape(mesh,shape);
        }
        void Circle(float x, float y, float r)
        {
            List<Point> shape = new();
            for (int i = 0; i < 40; i++)
            {
                double a = i*Math.PI/20;
                shape.Add(new Point(x+(float)Math.Cos(a)*r,y+(float)Math.Sin(a)*r));
            }
            FillShape(mesh,shape);
        }
        void Line(float ax, float ay, float bx, float by, float w)
        {
            float length = (float)Math.Sqrt((bx-ax)*(bx-ax)+(by-ay)*(by-ay));
            float dx = -(by-ay)/length*w/2, dy = (bx-ax)/length*w/2;
            Fill(ax+dx,ay+dy,bx+dx,by+dy,bx-dx,by-dy,ax-dx,ay-dy);
            Circle(ax,ay,w/2); Circle(bx,by,w/2);
        }
        void Cross(float x, float y, float size, float w)
        {
            Line(x+size/2,y+w/2,x+size/2,y+size-w/2,w);
            Line(x+w/2,y+size/2,x+size-w/2,y+size/2,w);
        }
        void RoundedFrame(float x, float y, float w, float h, float r)
        {
            var outer = RoundedRect(x,y,w,h,r);
            var inner = RoundedRect(x+2.5f,y+2.5f,w-5,h-5,Math.Max(0.4f,r-2.5f));
            for (int i = 0; i < outer.Count; i++)
            {
                int next=(i+1)%outer.Count;
                Triangle(mesh,outer[i],outer[next],inner[next]);
                Triangle(mesh,outer[i],inner[next],inner[i]);
            }
            Feather(mesh,outer);
            inner.Reverse(); Feather(mesh,inner);
        }
        switch (metric)
        {
            case AbilityMetric.Medic: Cross(3,3,19,5.5f); break; // Prefer the native Plus sprite.
            case AbilityMetric.MageRecovery:
                Cross(2.5f,9,13.5f,4);
                Fill(18,2,19.5f,5.5f,23,7,19.5f,8.5f,18,12,16.5f,8.5f,13,7,16.5f,5.5f);
                break;
            case AbilityMetric.Rescuer:
                Circle(7.5f,5,2.8f); Line(7.5f,11,7.5f,20,5);
                Line(3,12,12,12,3); Line(19,11,19,21,3.5f);
                Fill(13.5f,10,19,3,24,10);
                break;
            case AbilityMetric.Phoenix:
                List<Point> bird = new() { new(12.5f,22) };
                Curve(bird,8,19.5f,3.5f,16.5f,2.5f,7);
                Curve(bird,6,10,8,11,10.5f,11.5f);
                Curve(bird,10,8,11,5,13,2.5f);
                bird.Add(new Point(16.5f,6.3f)); bird.Add(new Point(14,6.7f));
                Curve(bird,14,8.5f,13.8f,10,14.5f,11.5f);
                Curve(bird,17,11,20,9,22.5f,6);
                Curve(bird,22,16,17,20,12.5f,22);
                bird.RemoveAt(bird.Count-1);
                FillShape(mesh,bird);
                break;
            case AbilityMetric.Repair:
                // One curved open jaw; the shank joins its solid back, not the opening.
                Line(4.8f,20.2f,13.2f,11.8f,4.5f);
                List<Point> head = new();
                for (int i=0;i<=36;i++)
                {
                    double a=(5+i*260d/36)*Math.PI/180;
                    head.Add(new Point(16.5f+(float)Math.Cos(a)*5.8f,8.5f+(float)Math.Sin(a)*5.8f));
                }
                head.Add(new Point(14.1f,6.7f)); head.Add(new Point(18.3f,10.9f));
                FillShape(mesh,head);
                break;
            case AbilityMetric.Charge:
                Fill(14,2,4,14,11,14,9,23,21,10,14,10); break;
            case AbilityMetric.Wager:
                RoundedFrame(3,3,19,19,3.5f);
                Circle(8.5f,8.5f,1.5f); Circle(12.5f,12.5f,1.5f); Circle(16.5f,16.5f,1.5f);
                break;
            case AbilityMetric.Contracts:
                RoundedFrame(4,4,17,19,3);
                FillShape(mesh,RoundedRect(8.5f,1.5f,8,5,1.5f));
                Line(9,11,16,11,2); Line(9,16,16,16,2);
                break;
        }
        return Cache[metric] = mesh.ToArray();
    }

    private static List<Point> RoundedRect(float x,float y,float w,float h,float r)
    {
        List<Point> points=new();
        foreach (var corner in new[] { (x+w-r,y+r,-90), (x+w-r,y+h-r,0), (x+r,y+h-r,90), (x+r,y+r,180) })
            for (int i=0;i<=8;i++)
            {
                double a=(corner.Item3+i*90d/8)*Math.PI/180;
                points.Add(new Point(corner.Item1+(float)Math.Cos(a)*r,corner.Item2+(float)Math.Sin(a)*r));
            }
        return points;
    }

    private static void Curve(List<Point> points,float ax,float ay,float bx,float by,float x,float y)
    {
        Point start=points[points.Count-1];
        for(int i=1;i<=12;i++)
        {
            float t=i/12f,u=1-t;
            points.Add(new Point(u*u*u*start.X+3*u*u*t*ax+3*u*t*t*bx+t*t*t*x,
                u*u*u*start.Y+3*u*u*t*ay+3*u*t*t*by+t*t*t*y));
        }
    }

    private static float Cross(Point a,Point b,Point c) => (b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);
    private static void Triangle(List<Point> mesh,Point a,Point b,Point c) { mesh.Add(a);mesh.Add(b);mesh.Add(c); }

    private static void FillShape(List<Point> mesh,List<Point> shape)
    {
        float area=0;
        for(int i=0;i<shape.Count;i++)
        {
            Point a=shape[i],b=shape[(i+1)%shape.Count]; area+=a.X*b.Y-a.Y*b.X;
        }
        if(area<0) shape.Reverse();
        List<Point> pending=new(shape);
        // Ear clipping preserves the transparent opening of concave silhouettes.
        while(pending.Count>3)
        {
            bool clipped=false;
            for(int i=0;i<pending.Count;i++)
            {
                int before=(i+pending.Count-1)%pending.Count,after=(i+1)%pending.Count;
                Point a=pending[before],b=pending[i],c=pending[after];
                if(Cross(a,b,c)<=0.000001f) continue;
                bool contains=false;
                for(int j=0;j<pending.Count;j++)
                {
                    if(j==before||j==i||j==after) continue;
                    Point p=pending[j];
                    if(Cross(a,b,p)>=0 && Cross(b,c,p)>=0 && Cross(c,a,p)>=0) { contains=true;break; }
                }
                if(contains) continue;
                Triangle(mesh,a,b,c); pending.RemoveAt(i);clipped=true;break;
            }
            if(!clipped) throw new InvalidOperationException("Invalid resource icon contour");
        }
        if(pending.Count==3) Triangle(mesh,pending[0],pending[1],pending[2]);
        Feather(mesh,shape);
    }

    private static void Feather(List<Point> mesh,List<Point> contour)
    {
        // A narrow transparent rim softens the silhouette without a texture or custom shader.
        for(int i=0;i<contour.Count;i++)
        {
            Point a=contour[i],b=contour[(i+1)%contour.Count];
            float dx=b.X-a.X,dy=b.Y-a.Y,length=(float)Math.Sqrt(dx*dx+dy*dy);
            if(length<0.00001f) continue;
            float nx=dy/length*Fringe,ny=-dx/length*Fringe;
            Point outerA=new(a.X+nx,a.Y+ny,0),outerB=new(b.X+nx,b.Y+ny,0);
            Triangle(mesh,a,outerA,outerB); Triangle(mesh,a,outerB,b);
        }
    }
}
