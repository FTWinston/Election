using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ElectionDataGenerator
{
    public class PolygonF
    {
        public PolygonF(params PointF[] points)
        {
            Vertices = new List<PointF>(points);
            Area = CalculateArea(Vertices);
        }

        private static float CalculateArea(List<PointF> vertices)
        {
            // Add the first point as the last in order to close the figure and compute area properly.
            vertices.Add(vertices[0]);

            float area = Math.Abs(vertices.Take(vertices.Count - 1).Select((p, i) => p.X * vertices[i + 1].Y - p.Y * vertices[i + 1].X).Sum()) / 2;

            vertices.RemoveAt(vertices.Count - 1);
            return area;
        }

        public List<PointF> Vertices { get; }
        public float Area { get; private set; }

        private static int WrapIndex(int index, int numIndices, bool forward)
        {
            if (forward)
            {
                return index == numIndices - 1 ? 0 : index + 1;
            }
            else
            {
                return index == 0 ? numIndices - 1 : index - 1;
            }
        }

        public bool IsAdjacent(PolygonF other)
        {
            // if any two consecutive points from this match any two consecutive points on that, either way round, they're adjacent
            for (var i = Vertices.Count - 1; i >= 0; i--)
            {
                var firstVertex = Vertices[i];

                var otherIndex = other.Vertices.IndexOf(firstVertex);
                if (otherIndex == -1)
                    continue;

                int testIndex1 = WrapIndex(otherIndex, other.Vertices.Count, false);
                int testIndex2 = WrapIndex(otherIndex, other.Vertices.Count, true);

                var secondVertex = Vertices[WrapIndex(i, Vertices.Count, false)];
                if (other.Vertices[testIndex1] == secondVertex || other.Vertices[testIndex2] == secondVertex)
                    return true;

                secondVertex = Vertices[WrapIndex(i, Vertices.Count, true)];
                if (other.Vertices[testIndex1] == secondVertex || other.Vertices[testIndex2] == secondVertex)
                    return true;
            }

            return false;
        }

        public bool MergeIfAdjacent(PolygonF other)
        {
            // TODO: if THREE (or more?) vertices in a row are shared with this one, use the first and last of those,
            // and actually remove the intervening ones from this shape

            int localIndex1 = 0, localIndex2 = 0, otherIndex1 = 0, otherIndex2 = 0;
            bool reverseOther = false, foundAny = false;

            // if any two consecutive points from this match any two consecutive points on that, either way round, they're adjacent
            for (localIndex1 = Vertices.Count - 1; localIndex1 >= 0; localIndex1--)
            {
                var firstVertex = Vertices[localIndex1];

                otherIndex1 = other.Vertices.IndexOf(firstVertex);
                if (otherIndex1 == -1)
                    continue;

                localIndex2 = WrapIndex(localIndex1, Vertices.Count, false);
                var secondVertex = Vertices[localIndex2];

                otherIndex2 = WrapIndex(otherIndex1, other.Vertices.Count, false);
                if (other.Vertices[otherIndex2] == secondVertex)
                {
                    reverseOther = true;
                    foundAny = true;
                    break;
                }

                otherIndex2 = WrapIndex(otherIndex1, other.Vertices.Count, true);
                if (other.Vertices[otherIndex2] == secondVertex)
                {
                    reverseOther = false;
                    foundAny = true;
                    break;
                }


                localIndex2 = WrapIndex(localIndex1, Vertices.Count, true);
                secondVertex = Vertices[localIndex2];

                otherIndex2 = WrapIndex(otherIndex1, other.Vertices.Count, false);
                if (other.Vertices[otherIndex2] == secondVertex)
                {
                    reverseOther = false;
                    foundAny = true;
                    break;
                }

                otherIndex2 = WrapIndex(otherIndex1, other.Vertices.Count, true);
                if (other.Vertices[otherIndex2] == secondVertex)
                {
                    reverseOther = true;
                    foundAny = true;
                    break;
                }
            }

            if (!foundAny)
                return false;

            Area += other.Area;

            if (localIndex2 < localIndex1)
            {
                var tmp = localIndex1;
                localIndex1 = localIndex2;
                localIndex2 = tmp;
            }

            int insertIndex = localIndex2 == localIndex1 + 1 ? localIndex2 : localIndex1;

            // for triangles, doens't matter order or anything, there's only one vertex:
            // 0 & 2, want to insert 1
            // 0 & 1, want to insert 2
            // 1 & 2, want to insert 0

            // for quadrilaterals, given the following:
            // 0 & 1, insert 2 & 3
            // 1 & 2, insert 3 & 0
            // 2 & 3, insert 0 & 1
            // 3 & 0, insert 1 & 2

            // 1 & 0, insert 3 & 2
            // 2 & 1, insert 0 & 3
            // 3 & 2, insert 1 & 0
            // 0 & 3, insert 2 & 1

            // is it safe to just swap them? idk
            if (otherIndex1 > otherIndex2)
            {
                var tmp = otherIndex1;
                otherIndex1 = otherIndex2;
                otherIndex2 = tmp;
            }

            IEnumerable<PointF> otherPoints;

            if (otherIndex1 == otherIndex2 - 1)
            {
                otherPoints = other.Vertices.Skip(otherIndex2 + 1)
                    .Concat(other.Vertices.Take(otherIndex1));
            }
            else
            {
                // if the above swap is OK, they must be the first and last ones
                otherPoints = other.Vertices.Skip(1).Take(other.Vertices.Count - 2);
            }

            if (reverseOther)
            {
                otherPoints = otherPoints.Reverse();
            }

            Vertices.InsertRange(insertIndex, otherPoints);

            return true;
        }
    }
}