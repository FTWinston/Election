using System.Drawing;

namespace ElectionDataGenerator
{
    struct TriangleF
    {
        public TriangleF(PointF v1, PointF v2, PointF v3)
        {
            Vertices = new PointF[] { v1, v2, v3 };

            CircumCenter = GetCircumCenter(v1, v2, v3);
            CircumRadiusSq = CircumCenter.DistanceSqTo(v1);
        }

        public PointF[] Vertices { get; }
        public PointF CircumCenter { get; }
        public float CircumRadiusSq { get; }

        public static bool operator ==(TriangleF left, TriangleF right)
        {
            return left.Vertices[0] == right.Vertices[0]
                && left.Vertices[1] == right.Vertices[1]
                && left.Vertices[2] == right.Vertices[2];
        }

        public static bool operator !=(TriangleF left, TriangleF right)
        {
            return left.Vertices[0] != right.Vertices[0]
                || left.Vertices[1] != right.Vertices[1]
                || left.Vertices[2] != right.Vertices[2];
        }

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
    }
}