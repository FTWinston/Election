using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ElectionDataGenerator
{
    public class AdjacencyInfo
    {
        public AdjacencyInfo(IEnumerable<PointF> vertices)
        {
            Vertices = vertices.ToArray();
            Length = CalculateLength(Vertices);
        }

        public PointF[] Vertices { get; }
        public float Length { get; }

        private static float CalculateLength(PointF[] vertices)
        {
            float total = 0;
            for (int i = vertices.Length - 2; i >= 0; i--)
            {
                var point1 = vertices[i];
                var point2 = vertices[i + 1];
                total += point1.DistanceTo(point2);
            }

            return total;
        }
    }
}