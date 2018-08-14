using System.Collections.Generic;
using System.Drawing;

namespace ElectionDataGenerator
{
    public class TriangleF
    {
        public TriangleF(PointF v1, PointF v2, PointF v3)
        {
            Vertices = new PointF[] { v1, v2, v3 };

            Centroid = new PointF((v1.X + v2.X + v3.X) / 3, (v1.Y + v2.Y + v3.Y) / 3);
            CircumCenter = GetCircumCenter(v1, v2, v3);
            CircumRadiusSq = CircumCenter.DistanceSqTo(v1);
        }

        public PointF[] Vertices { get; }
        public PointF CircumCenter { get; }
        public float CircumRadiusSq { get; }
        public PointF Centroid { get; }

        public HashSet<TriangleF> AdjacentTriangles { get; } = new HashSet<TriangleF>();

        private static PointF GetCircumCenter(PointF a, PointF b, PointF c)
        {
            var d = (a.X - c.X) * (b.Y - c.Y) - (b.X - c.X) * (a.Y - c.Y);

            var x = (
                ((a.X - c.X) * (a.X + c.X) + (a.Y - c.Y) * (a.Y + c.Y)) / 2 * (b.Y - c.Y)
              - ((b.X - c.X) * (b.X + c.X) + (b.Y - c.Y) * (b.Y + c.Y)) / 2 * (a.Y - c.Y)
            ) / d;

            var y = (
                ((b.X - c.X) * (b.X + c.X) + (b.Y - c.Y) * (b.Y + c.Y)) / 2 * (a.X - c.X)
              - ((a.X - c.X) * (a.X + c.X) + (a.Y - c.Y) * (a.Y + c.Y)) / 2 * (b.X - c.X)
            ) / d;

            return new PointF(x, y);
        }

        public bool IsAdjacent(TriangleF other)
        {
            PointF v1 = Vertices[0], v2 = Vertices[1], v3 = Vertices[2];
            PointF o1 = other.Vertices[0], o2 = other.Vertices[1], o3 = other.Vertices[2];

            bool alreadyMatchedOne = v1 == o1 || v1 == o2 || v1 == o3;

            if (v2 == o1 || v2 == o2 || v2 == o3)
            {
                if (alreadyMatchedOne)
                    return true;

                alreadyMatchedOne = true;
            }


            if (alreadyMatchedOne && (v3 == o1 || v3 == o2 || v3 == o3))
                return true;

            return false;
        }
    }
}