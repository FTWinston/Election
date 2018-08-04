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
            for (var startLocalIndex = Vertices.Count - 1; startLocalIndex >= 0; startLocalIndex--)
            {
                var firstVertex = Vertices[startLocalIndex];

                var testOtherIndex = other.Vertices.IndexOf(firstVertex);
                if (testOtherIndex == -1)
                    continue;

                var startOtherIndex = testOtherIndex;
                var sharedVertices = new List<PointF>();
                sharedVertices.Add(firstVertex);

                // The two polygons share a vertex. See if they share 2 or more adjacent vertices!
                int testIndex1 = WrapIndex(testOtherIndex, other.Vertices.Count, false);
                int testIndex2 = WrapIndex(testOtherIndex, other.Vertices.Count, true);
                
                bool localGoingForwards = false, otherGoingForwards = false;
                int testLocalIndex = WrapIndex(startLocalIndex, Vertices.Count, localGoingForwards);
                var secondVertex = Vertices[testLocalIndex];
                if (other.Vertices[testIndex1] == secondVertex)
                {
                    sharedVertices.Add(secondVertex);
                    testOtherIndex = testIndex1;
                    otherGoingForwards = false;
                }
                else if (other.Vertices[testIndex2] == secondVertex)
                {
                    sharedVertices.Add(secondVertex);
                    testOtherIndex = testIndex2;
                    otherGoingForwards = true;
                }
                else
                {
                    localGoingForwards = true;
                    testLocalIndex = WrapIndex(startLocalIndex, Vertices.Count, localGoingForwards);
                    secondVertex = Vertices[testLocalIndex];
                    if (other.Vertices[testIndex1] == secondVertex)
                    {
                        sharedVertices.Add(secondVertex);
                        testOtherIndex = testIndex2;
                        otherGoingForwards = false;
                    }
                    else if (other.Vertices[testIndex2] == secondVertex)
                    {
                        sharedVertices.Add(secondVertex);
                        testOtherIndex = testIndex2;
                        otherGoingForwards = true;
                    }
                    else
                        continue;
                }

                // These may share more than just two vertices, so keep going as long as they keep being shared.
                while (true)
                {
                    testLocalIndex = WrapIndex(testLocalIndex, Vertices.Count, localGoingForwards);
                    testOtherIndex = WrapIndex(testOtherIndex, other.Vertices.Count, otherGoingForwards);

                    var localVertex = Vertices[testLocalIndex];
                    if (localVertex != other.Vertices[testOtherIndex])
                        break;

                    sharedVertices.Add(localVertex);
                }

                var lastLocalIndex = testLocalIndex;
                var lastOtherIndex = testOtherIndex;

                // now try this same extension in the opposite direction too
                testLocalIndex = startLocalIndex;
                testOtherIndex = startOtherIndex;
                localGoingForwards = !localGoingForwards;
                otherGoingForwards = !otherGoingForwards;

                while (true)
                {
                    testLocalIndex = WrapIndex(testLocalIndex, Vertices.Count, localGoingForwards);
                    testOtherIndex = WrapIndex(testOtherIndex, other.Vertices.Count, otherGoingForwards);

                    if (testLocalIndex == lastLocalIndex || testOtherIndex == lastOtherIndex)
                        break;

                    var localVertex = Vertices[testLocalIndex];
                    if (localVertex != other.Vertices[testOtherIndex])
                        break;

                    sharedVertices.Insert(0, localVertex);
                }

                return new AdjacencyInfo(sharedVertices);
            }

            return null;
        }

        private static void CompareAdjacency(PolygonF polygon, AdjacencyInfo adjacencyInfo, out int startIndex, out int endIndex, out bool forward)
        {
            var index2 = polygon.Vertices.IndexOf(adjacencyInfo.Vertices[1]);
            startIndex = polygon.Vertices.LastIndexOf(adjacencyInfo.Vertices[0], index2);
            if (startIndex == -1)
            {
                startIndex = polygon.Vertices.IndexOf(adjacencyInfo.Vertices[0], index2);

                if (startIndex > 0)
                {
                    // if the one vertex is repeated multiple times, find the closest copy
                    var tmp1 = polygon.Vertices.LastIndexOf(adjacencyInfo.Vertices[1], startIndex - 1);
                    var tmp2 = polygon.Vertices.IndexOf(adjacencyInfo.Vertices[1], startIndex + 1);

                    if (tmp1 != -1)
                    {
                        if (tmp2 != -1)
                        {
                            index2 = Math.Abs(startIndex - tmp1) < Math.Abs(startIndex - tmp2)
                                ? tmp1
                                : tmp2;
                        }
                        else
                            index2 = tmp1;
                    }
                    else if (tmp2 != -1)
                        index2 = tmp2;
                }
            }

            endIndex = adjacencyInfo.Vertices.Length > 2
                ? polygon.Vertices.IndexOf(adjacencyInfo.Vertices[adjacencyInfo.Vertices.Length - 1])
                : index2;

            forward = Math.Abs(index2 - startIndex) == 1 ? startIndex < index2 : startIndex > index2;
        }

        private static int RemoveMidPoints(PolygonF polygon, int startIndexExclusive, int endIndexExclusive, bool forward)
        {
            if (forward)
            {
                // TODO: refactor this "almost" duplication
                if (startIndexExclusive < endIndexExclusive - 1)
                {
                    // where we need to wrap around the bounds in forwards, we need to make the section being retained contiguous,
                    // or the vertexes aren't added in the right order
                    polygon.Vertices.AddRange(polygon.Vertices.Take(startIndexExclusive + 1));
                    polygon.Vertices.RemoveRange(0, startIndexExclusive + 1);

                    endIndexExclusive -= startIndexExclusive + 1;
                    startIndexExclusive = polygon.Vertices.Count - 1;
                }
            }
            else
            {
                if (startIndexExclusive > endIndexExclusive + 1)
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
            if (Area == 0)
            {
                Vertices.Clear();
                Vertices.AddRange(otherPolygon.Vertices);
                Area = otherPolygon.Area;
                return;
            }

            if (otherPolygon.Area == 0)
                return;
            Area += otherPolygon.Area;

            if (adjacencyInfo.Vertices.Length >= otherPolygon.Vertices.Count)
            {
                // Other polygon is entirely contained in this one.
                // If it's fully wrapped, remove all its vertices, otherwise remove all but the first and last adjacent vertices.
                bool firstLastEqual = adjacencyInfo.Vertices[0] == adjacencyInfo.Vertices[adjacencyInfo.Vertices.Length - 1];
                var toRemove = adjacencyInfo.Vertices.Skip(1);
                if (!firstLastEqual)
                    toRemove = toRemove.Take(adjacencyInfo.Vertices.Length - 2);

                foreach (var vertex in toRemove)
                    Vertices.Remove(vertex);
                return;
            }
            else if (adjacencyInfo.Vertices.Length >= Vertices.Count)
            {
                // This polygon is entirely contained the other one one.
                // If it's fully wrapped, remove all its vertices, otherwise remove all but the first and last adjacent vertices.
                bool firstLastEqual = adjacencyInfo.Vertices[0] == adjacencyInfo.Vertices[adjacencyInfo.Vertices.Length - 1];
                var toRemove = adjacencyInfo.Vertices.Skip(1);
                if (!firstLastEqual)
                    toRemove = toRemove.Take(adjacencyInfo.Vertices.Length - 2);

                foreach (var vertex in toRemove)
                    otherPolygon.Vertices.Remove(vertex);

                // and then use those on this one
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