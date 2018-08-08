using System;
using System.Collections.Generic;

namespace ElectionDataGenerator
{
    public class PointF
    {
        public PointF(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; }
        public float Y { get; }

        public float DistanceTo(PointF other)
        {
            return (float)Math.Sqrt(DistanceSqTo(other));
        }

        public float DistanceSqTo(PointF other)
        {
            return (X - other.X) * (X - other.X)
                 + (Y - other.Y) * (Y - other.Y);
        }

        internal PointF GetClosest(IEnumerable<PointF> points)
        {
            float bestDistance = float.MaxValue;
            PointF bestPoint = null;

            foreach (var point in points)
            {
                var distance = DistanceSqTo(point);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPoint = point;
                }
            }

            return bestPoint;
        }
    }
}