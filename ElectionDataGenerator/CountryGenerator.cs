using ElectionData.Geography;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace ElectionDataGenerator
{
    public class CountryGenerator
    {
        /*
         * Use noise to generate a terrain boundary (we don't care about elevation, really)
         *# Should we remove any particularly tiny islands?
         * 
         * Within that coastline, place a pre-determined number of "district nuclei", somewhat evenly distributed.
         * If there's any land masses that have no districts on them, move at least one nuclei to each.
         * Expand those nuclei to determine the outline of each district. That sounds rather like a voronio diagram,
         * but perhaps it should account for population density when determining distance, so cities get smaller districts etc.
         * 
         * Determine the "center" of each district, which its attributes will be calculated from.
         * This should really just be (min + max) / 2 for X and Y, thoguh it's probably fine to use the nuclei instead.
         * 
         * Somehow determine the following values for each district:
         *      how urban/rural it is
         *          elevation will make things more rural, but we really want big and small blobs of "urbanness"
         *      
         *      population density
         *          similar to urbanness, but with huge blurry overspill around the biggest cities
         *      
         *      how coastal/inland it is
         *          simply a matter of distance from the coast
         *      
         *      how wealthy/deprived it is
         *          ...wow this could have a lot of factors. Quite possibly dependent on all the others, tbh.
         *          
         *      distance from "capital" and other significant cities, each as a seperate consideration
         *          Just a big point gradient, really
         *          
         *      distance from adjacent countries
         *          If there are land borders, each country is a separate consideration
         *
         * 
         * Once the districts are all established, pick some distributed ones to be the nuclei for regions,
         * and then grow those regions around them, adding adjacent tiles to each.
         * Where there's multiple adjacent options, go with the adjacent district whose outlook most closely aligns with their own.
         * 
         * Hmm, but we also want the regions to represent equal populations. Quite how we grow them is a bit up in the air.
         */

        public Random Random { get; }
        public int Width { get; }
        public int Height { get; }

        public float CenterX { get; }
        public float CenterY { get; }
        public float EllipseWidth { get; }
        public float EllipseHeight { get; }

        private List<GraphicsPath> Landmasses { get; set; }

        private PerlinNoise TerrainNoiseX { get; }
        private PerlinNoise TerrainNoiseY { get; }

        public CountryGenerator(int seed)
        {
            Random = new Random(seed);

            Width = Random.Next(7, 11) * 100;
            Height = Random.Next(7, 11) * 100;

            CenterX = Width / 2f;
            CenterY = Height / 2f;

            EllipseWidth = Width * 0.75f;
            EllipseHeight = Height * 0.75f;

            TerrainNoiseX = new PerlinNoise(Random.Next(), 4);
            TerrainNoiseY = new PerlinNoise(Random.Next(), 4);
        }

        #region terrain outline
        public List<GraphicsPath> GenerateTerrain()
        {
            // travel around an ellipse, with noise applied to it

            // TODO: frequency & amplitude ought to be part of the noise itself, rather than separate to it
            // 0.001 to 0.01 seems to work best for noise frequency, 100 works well for noise amplitude
            float frequency = 0.005f;
            float amplitude = Math.Min(Width, Height) / 2;

            var landmasses = new List<List<PointF>>();

            var mainland = new List<PointF>();
            landmasses.Add(mainland);

            var prevPoint = new PointF(CenterX, CenterY);

            int step = 0;
            for (float angle = (float)Math.PI * 2; angle > 0; angle -= 0.01f /* roughly 500 steps */)
            {
                step++;

                float ellipseX = CenterX + (float)Math.Cos(angle) * EllipseWidth / 2;
                float ellipseY = CenterY + (float)Math.Sin(angle) * EllipseHeight / 2;

                float placeX = ellipseX + amplitude * TerrainNoiseX.GetValue(ellipseX * frequency, ellipseY * frequency);
                float placeY = ellipseY + amplitude * TerrainNoiseY.GetValue(ellipseX * frequency, ellipseY * frequency);

                var nextPoint = new PointF(placeX, placeY);

                // See if prevPoint - nextPoint crosses ANY of the previous lines. If it does, we have a loop / dangler.
                // Ignore the first line and the most recent line, cos we may well touch those.

                for (int i = mainland.Count - 3; i > 1; i--)
                {
                    var testEnd = mainland[i - 1];
                    var testStart = mainland[i];

                    if (LineSegmentsCross(prevPoint, nextPoint, testStart, testEnd))
                    {

                        // Because we are ALWAYS working our way anticlockwise around the main landmass:
                        // "outties" always have the "older" line cross the "newer" one from left to right.
                        // "innies" always have the "older" line cross the "newer" one from right to left.
                        // UNLESS we are dealing with "nested" outties or innies, in which case the direction swaps.
                        // But maybe its ok to swap what we do there, as we will get larger islands / inlets that way,
                        // as opposed to "chains" of smaller ones, which are perhaps less important - especially on a political map.

                        bool chopOff = IsLeftOfLine(prevPoint, nextPoint, testEnd);
                        HandleTerrainBoundaryLoop(landmasses, mainland, i, chopOff);
                        break;
                    }
                }

                mainland.Add(nextPoint);
                prevPoint = nextPoint;
            }

            ScaleToFitBounds(landmasses);

            Landmasses = landmasses
                .Select(points =>
                {
                    var types = new byte[points.Count];
                    for (var i = types.Length - 1; i >= 0; i--)
                        types[i] = 1;

                    return new GraphicsPath(points.ToArray(), types);
                })
                .ToList();

            return Landmasses;
        }

        private static bool LineSegmentsCross(PointF line1start, PointF line1end, PointF line2start, PointF line2end)
        {
            PointF CmP = new PointF(line2start.X - line1start.X, line2start.Y - line1start.Y);
            PointF r = new PointF(line1end.X - line1start.X, line1end.Y - line1start.Y);
            PointF s = new PointF(line2end.X - line2start.X, line2end.Y - line2start.Y);

            float CmPxr = CmP.X * r.Y - CmP.Y * r.X;
            float CmPxs = CmP.X * s.Y - CmP.Y * s.X;
            float rxs = r.X * s.Y - r.Y * s.X;

            if (CmPxr == 0f)
            {
                // Lines are collinear, and so intersect if they have any overlap
                return (line2start.X - line1start.X < 0f) != (line2start.X - line1end.X < 0f)
                    || (line2start.Y - line1start.Y < 0f) != (line2start.Y - line1end.Y < 0f);
            }

            if (rxs == 0f)
                return false; // Lines are parallel.

            float rxsr = 1f / rxs;
            float t = CmPxs * rxsr;
            float u = CmPxr * rxsr;

            return t >= 0f && t <= 1f && u >= 0f && u <= 1f;
        }

        private static bool IsLeftOfLine(PointF lineStart, PointF lineEnd, PointF test)
        {
            return ((lineEnd.X - lineStart.X) * (test.Y - lineStart.Y) - (lineEnd.Y - lineStart.Y) * (test.X - lineStart.X)) > 0;
        }

        private static void HandleTerrainBoundaryLoop(List<List<PointF>> landmasses, List<PointF> mainland, int startIndex, bool chopOff)
        {
            // is the end point of the "older" line on the right of the "newer" line? If so chop, otherwise reverse.
            if (chopOff)
            {
                int skipPoints = 2; // don't have islands start right adjacent to the mainland
                int islandPointsStart = startIndex + 1 + skipPoints;
                int numIslandPoints = mainland.Count - islandPointsStart - skipPoints;

                if (numIslandPoints > 3)
                {
                    landmasses.Add(
                        new List<PointF>(
                            mainland
                                .Skip(islandPointsStart)
                                .Take(numIslandPoints)
                        )
                    );
                }

                // snip this entire "loop" off!
                mainland.RemoveRange(startIndex + 1, mainland.Count - startIndex - 1);
            }
            else
            {
                // reverse the order of all points after #i
                var reversePoints = mainland
                    .Skip(startIndex + 1)
                    .Take(mainland.Count - startIndex - 1)
                    .Reverse()
                    .ToArray();

                mainland.RemoveRange(startIndex, mainland.Count - startIndex);
                mainland.AddRange(reversePoints);
            }
        }

        private void ScaleToFitBounds(List<List<PointF>> landmasses)
        {
            // first, determine the extent of the terrain we have
            float minX = Width, maxX = 0;
            float minY = Height, maxY = 0;

            foreach (var landmass in landmasses)
                foreach (var point in landmass)
                {
                    if (point.X < minX)
                        minX = point.X;
                    else if (point.X > maxX)
                        maxX = point.X;

                    if (point.Y < minY)
                        minY = point.Y;
                    else if (point.Y > maxY)
                        maxY = point.Y;
                }

            // adjust bounds so that some water will display all round
            float margin = Math.Min(Width, Height) * 0.1f;
            minX -= margin;
            maxX += margin;
            minY -= margin;
            maxY += margin;

            // then adjust every point so that they all fit onto our map nicely
            float scaleX = Width / (maxX - minX);
            float scaleY = Height / (maxY - minY);
            float offsetX = -minX;
            float offsetY = -minY;

            foreach (var landmass in landmasses)
                for (int i = landmass.Count - 1; i >= 0; i--)
                {
                    var point = landmass[i];
                    landmass[i] = new PointF(point.X * scaleX + offsetX, point.Y * scaleY + offsetY);
                }
        }
        #endregion terrain outline

        public List<PointF> PlaceDistricts(int numDistricts)
        {
            var districts = new List<PointF>();

            int xStart = Width / 10, yStart = Height / 10;
            int xRange = Width * 8 / 10, yRange = Height * 8 / 10;

            for (int i=0; i<numDistricts; i++)
            {
                PointF test;
                bool inAny = false;

                do
                {
                    test = new PointF(Random.Next(xRange) + xStart, Random.Next(yRange) + yStart);
                    
                    foreach (var landmass in Landmasses)
                        if (landmass.IsVisible(test))
                        {
                            inAny = true;
                            break;
                        }
                } while (!inAny);

                districts.Add(test);
            }

            return districts;
        }

        public List<TriangleF> GetDelauneyTriangulation(List<PointF> points)
        {
            var enclosingTriangle = new TriangleF(
                new PointF(0, 0),
                new PointF(Width * 2, 0),
                new PointF(0, Height * 2)
            );

            var triangulation = new List<TriangleF>();
            triangulation.Add(enclosingTriangle);

            foreach (var point in points)
            {
                // find all triangles that are no longer valid due to this node's insertion
                var badTriangles = new List<TriangleF>();
                foreach (var triangle in triangulation)
                {
                    if (InsideCircumcircle(point, triangle))
                    {
                        badTriangles.Add(triangle);
                    }
                }

                // Find the boundary of polygonal hole formed by these "bad" triangles...
                // Get the edges of the "bad" triangles which don't touch other bad triangles...
                // Each pair of nodes here represents a line.
                var polygon = new List<PointF>();
                foreach (var triangle in badTriangles)
                {
                    for (var i = 0; i < 3; i++)
                    {
                        var edgeFrom = triangle.Vertices[i];
                        var edgeTo = triangle.Vertices[i == 2 ? 0 : i + 1];

                        var sharedWithOther = false;
                        foreach (var other in badTriangles)
                        {
                            if (other == triangle)
                            {
                                continue;
                            }

                            if (!other.Vertices.Contains(edgeFrom))
                            {
                                continue;
                            }

                            if (!other.Vertices.Contains(edgeTo))
                            {
                                continue;
                            }

                            sharedWithOther = true;
                            break;
                        }

                        if (!sharedWithOther)
                        {
                            polygon.Add(edgeFrom);
                            polygon.Add(edgeTo);
                        }
                    }
                }

                // discard all bad triangles
                foreach (var triangle in badTriangles)
                {
                    triangulation.Remove(triangle);
                }

                // re-triangulate the polygonal hole ... create a new triangle for each edge
                for (var i = 0; i < polygon.Count - 1; i += 2)
                {
                    var triangle = new TriangleF(polygon[i], polygon[i + 1], point);
                    triangulation.Add(triangle);
                }
            }

            // remove all triangles that contain a vertex from the original super-triangle
            for (var i = 0; i < triangulation.Count; i++) {
                var triangle = triangulation[i];
                foreach (var vertex in triangle.Vertices) {
                    if (enclosingTriangle.Vertices.Contains(vertex)) {
                        triangulation.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }

            return triangulation;
        }

        public void LinkAdjacentTriangles(List<TriangleF> triangles)
        {
            for (int iLinking = triangles.Count - 1; iLinking >= 1; iLinking--)
            { 
                var linkingTriangle = triangles[iLinking];

                for (int iOther = iLinking - 1; iOther >= 0; iOther--)
                {
                    var otherTriangle = triangles[iOther];

                    if (linkingTriangle.IsAdjacent(otherTriangle))
                    {
                        linkingTriangle.AdjacentTriangles.Add(otherTriangle);
                        otherTriangle.AdjacentTriangles.Add(linkingTriangle);
                    }
                }
            }
        }
        
        public static List<Tuple<PointF, PointF>> GetDistinctLines(List<TriangleF> triangulation)
        {
            var links = new List<Tuple<PointF, PointF>>();

            // convert triangles to UNIQUE lines
            foreach (var triangle in triangulation)
            {
                PointF v0 = triangle.Vertices[0], v1 = triangle.Vertices[1], v2 = triangle.Vertices[2];

                bool firstDuplicate = false, secondDuplicate = false, thirdDuplicate = false;
                foreach (var link in links)
                {
                    if (link.Item1 == v0)
                    {
                        if (link.Item2 == v1)
                        {
                            firstDuplicate = true;
                        }
                        else if (link.Item2 == v2)
                        {
                            thirdDuplicate = true;
                        }
                    }
                    else if (link.Item1 == v1)
                    {
                        if (link.Item2 == v0)
                        {
                            firstDuplicate = true;
                        }
                        else if (link.Item2 == v2)
                        {
                            secondDuplicate = true;
                        }
                    }
                    else if (link.Item1 == v2)
                    {
                        if (link.Item2 == v0)
                        {
                            thirdDuplicate = true;
                        }
                        else if (link.Item2 == v1)
                        {
                            secondDuplicate = true;
                        }
                    }
                }

                if (!firstDuplicate)
                {
                    links.Add(new Tuple<PointF, PointF>(v0, v1));
                }
                if (!secondDuplicate)
                {
                    links.Add(new Tuple<PointF, PointF>(v1, v2));
                }
                if (!thirdDuplicate)
                {
                    links.Add(new Tuple<PointF, PointF>(v2, v0));
                }
            }

            return links;
        }

        private bool InsideCircumcircle(PointF point, TriangleF triangle)
        {
            var distSq = point.DistanceSqTo(triangle.CircumCenter);
            return distSq <= triangle.CircumRadiusSq;
        }
    }
}