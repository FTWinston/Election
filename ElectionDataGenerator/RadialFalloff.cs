using System;

namespace ElectionDataGenerator
{
    public class RadialFalloff : IPositionalEffect
    {
        public PointF Center { get; }
        public float Magnitude { get; }
        public float FalloffDistance { get; }

        public RadialFalloff(PointF center, float magnitude, float falloffDist)
        {
            Center = center;
            Magnitude = magnitude;
            FalloffDistance = falloffDist;
        }

        public float GetValue(PointF point)
        {
            var distance = Center.DistanceTo(point);
            return Magnitude * Math.Max(1f - distance / FalloffDistance, 0);
        }
    }
}
