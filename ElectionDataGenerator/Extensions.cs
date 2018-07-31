using System;
using System.Drawing;

namespace ElectionDataGenerator
{
    static class Extensions
    {
        public static float DistanceTo(this PointF p1, PointF p2)
        {
            return (float)Math.Sqrt(p1.DistanceSqTo(p2));
        }

        public static float DistanceSqTo(this PointF p1, PointF p2)
        {
            return (p1.X - p2.X) * (p1.X - p2.X)
                 + (p1.Y - p2.Y) * (p1.Y - p2.Y);
        }
    }
}