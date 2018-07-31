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

        public AdjacencyInfo GetAdjacencyInfo(PolygonF other)
        {
            // if any two consecutive points from this match any two consecutive points on that, either way round, they're adjacent
            for (var i = Vertices.Count - 1; i >= 0; i--)
            {
                var firstVertex = Vertices[i];

                var otherIndex = other.Vertices.IndexOf(firstVertex);
                if (otherIndex == -1)
                    continue;

                var sharedVertices = new List<PointF>();
                sharedVertices.Add(firstVertex);

                // The two polygons share a vertex. See if they share 2 or more adjacent vertices!
                int testIndex1 = WrapIndex(otherIndex, other.Vertices.Count, false);
                int testIndex2 = WrapIndex(otherIndex, other.Vertices.Count, true);
                
                bool localGoingForwards = false, otherGoingForwards = false;
                int localIndex = WrapIndex(i, Vertices.Count, localGoingForwards);
                var secondVertex = Vertices[localIndex];
                if (other.Vertices[testIndex1] == secondVertex)
                {
                    sharedVertices.Add(secondVertex);
                    otherIndex = testIndex1;
                    otherGoingForwards = false;
                }
                else if (other.Vertices[testIndex2] == secondVertex)
                {
                    sharedVertices.Add(secondVertex);
                    otherIndex = testIndex2;
                    otherGoingForwards = true;
                }
                else
                {
                    localGoingForwards = true;
                    localIndex = WrapIndex(i, Vertices.Count, localGoingForwards);
                    secondVertex = Vertices[localIndex];
                    if (other.Vertices[testIndex1] == secondVertex)
                    {
                        sharedVertices.Add(secondVertex);
                        otherIndex = testIndex2;
                        otherGoingForwards = false;
                    }
                    else if (other.Vertices[testIndex2] == secondVertex)
                    {
                        sharedVertices.Add(secondVertex);
                        otherIndex = testIndex2;
                        otherGoingForwards = true;
                    }
                    else
                        continue;
                }

                // These may share more than just two vertices, so keep going as long as they keep being shared.
                while (true)
                {
                    localIndex = WrapIndex(localIndex, Vertices.Count, localGoingForwards);
                    otherIndex = WrapIndex(otherIndex, other.Vertices.Count, otherGoingForwards);

                    var localVertex = Vertices[localIndex];
                    if (localVertex != other.Vertices[otherIndex])
                        break;

                    sharedVertices.Add(localVertex);
                }

                return new AdjacencyInfo(sharedVertices);
            }

            return null;
        }

        private static void CompareAdjacency(PolygonF polygon, AdjacencyInfo adjacencyInfo, out int startIndex, out int endIndex, out bool forward)
        {
            startIndex = polygon.Vertices.IndexOf(adjacencyInfo.Vertices[0]);
            var index2 = polygon.Vertices.IndexOf(adjacencyInfo.Vertices[1]);

            endIndex = adjacencyInfo.Vertices.Length > 2
                ? polygon.Vertices.IndexOf(adjacencyInfo.Vertices[adjacencyInfo.Vertices.Length - 1])
                : index2;

            forward = Math.Abs(index2 - startIndex) == 1 ? startIndex < index2 : startIndex > index2;
        }

        private static int RemoveMidPoints(PolygonF polygon, int startIndexExclusive, int endIndexExclusive, bool forward)
        {
            if (!forward)
            {
                if (startIndexExclusive > endIndexExclusive)
                {
                    // where we need to wrap around the bounds in reverse, we need to make the section being retained contiguous,
                    // or the vertexes aren't added in the right order
                    polygon.Vertices.AddRange(polygon.Vertices.Take(endIndexExclusive + 1));
                    polygon.Vertices.RemoveRange(0, endIndexExclusive + 1);

                    startIndexExclusive -= endIndexExclusive + 1;
                    endIndexExclusive = polygon.Vertices.Count - 1;
                }

                var tmp = endIndexExclusive;
                endIndexExclusive = startIndexExclusive;
                startIndexExclusive = tmp;
            }

            if (startIndexExclusive < endIndexExclusive)
            {
                polygon.Vertices.RemoveRange(startIndexExclusive + 1, endIndexExclusive - startIndexExclusive - 1);
                return startIndexExclusive + 1;
            }
            else
            {
                polygon.Vertices.RemoveRange(startIndexExclusive + 1, polygon.Vertices.Count - startIndexExclusive - 1);
                polygon.Vertices.RemoveRange(0, endIndexExclusive);
                return 0;
            }
        }

        public void MergeWith(PolygonF otherPolygon, AdjacencyInfo adjacencyInfo)
        {
            Area += otherPolygon.Area;

            if (adjacencyInfo.Length == otherPolygon.Vertices.Count)
            {
                // other polygon is entirely wrapped, remove its vertices from this one
                foreach (var vertex in otherPolygon.Vertices)
                    Vertices.Remove(vertex);
                return;
            }
            else if (adjacencyInfo.Length == otherPolygon.Vertices.Count)
            {
                // this polygon is entirely wrapped, remove this polygon's vertices from it and then use those on this one
                foreach (var vertex in Vertices)
                    otherPolygon.Vertices.Remove(vertex);

                Vertices.Clear();
                Vertices.AddRange(otherPolygon.Vertices);
                return;
            }

            CompareAdjacency(this, adjacencyInfo, out int localStartIndex, out int localEndIndex, out bool localForward);
            CompareAdjacency(otherPolygon, adjacencyInfo, out int otherStartIndex, out int otherEndIndex, out bool otherForward);

            // remove any "mid" points in the adjacent edge array from this polygon
            int insertIndex = RemoveMidPoints(this, localStartIndex, localEndIndex, localForward);

            // remove ALL points in the adjacent edge array from the other polygon
            otherStartIndex = WrapIndex(otherStartIndex, otherPolygon.Vertices.Count, !otherForward);
            otherEndIndex = WrapIndex(otherEndIndex, otherPolygon.Vertices.Count, otherForward);
            RemoveMidPoints(otherPolygon, otherStartIndex, otherEndIndex, otherForward);

            // add all remaining vertices from the other polgyon, reversing them if necessary
            if (localForward == otherForward)
                otherPolygon.Vertices.Reverse();

            Vertices.InsertRange(insertIndex, otherPolygon.Vertices);
        }
    }
}