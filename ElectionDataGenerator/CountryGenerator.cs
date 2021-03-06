﻿using ElectionData.Geography;
using System;
using System.Collections.Generic;
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

        private List<GraphicsPath> Landmasses { get; set; }

        public CountryGenerator(int seed)
        {
            Random = new Random(seed);

            Width = Random.Next(7, 11) * 100;
            Height = Random.Next(7, 11) * 100;
        }

        public List<DistrictGenerator> GenerateTerrainDistricts(int numInternalPoints, int numDistricts)
        {
            var landmasses = GenerateTerrainOutline();
            TriangleF[] triangles = TriangulateLand(landmasses, numInternalPoints);
            List<DistrictGenerator> districts = CombineTrianglesIntoDistricts(triangles, numDistricts);
            return districts;
        }

        #region terrain outline
        private List<GraphicsPath> GenerateTerrainOutline()
        {
            // travel around an ellipse, with noise applied to it

            // 0.001 to 0.01 seems to work best for noise frequency, 100 works well for noise amplitude
            float frequency = 0.005f;
            float amplitude = Math.Min(Width, Height) / 2;
            var terrainNoiseX = new PerlinNoise(Random.Next(), frequency, amplitude, 4);
            var terrainNoiseY = new PerlinNoise(Random.Next(), frequency, amplitude, 4);

            var landmasses = new List<List<PointF>>();

            var mainland = new List<PointF>();
            landmasses.Add(mainland);

            var prevPoint = new PointF(Width / 2f, Height / 2f);

            float ellipseWidth = Width * 0.75f;
            float ellipseHeight = Height * 0.75f;

            int step = 0;
            for (float angle = (float)Math.PI * 2; angle > 0; angle -= 0.01f /* roughly 500 steps */)
            {
                step++;

                float ellipseX = Width / 2f + (float)Math.Cos(angle) * ellipseWidth / 2;
                float ellipseY = Height / 2f + (float)Math.Sin(angle) * ellipseHeight / 2;

                float placeX = ellipseX + terrainNoiseX.GetValue(ellipseX, ellipseY);
                float placeY = ellipseY + terrainNoiseY.GetValue(ellipseX, ellipseY);

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

                    return new GraphicsPath(points.Select(p => new System.Drawing.PointF(p.X, p.Y)).ToArray(), types);
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

        #region district generation
        private TriangleF[] TriangulateLand(IList<GraphicsPath> landmasses, int numInternalPoints)
        {
            var allPoints = landmasses
                .SelectMany(l => l.PathPoints)
                .Select(p => new PointF(p.X, p.Y))
                .ToList();

            var internalPoints = PlaceDistricts(numInternalPoints);
            allPoints.AddRange(internalPoints);

            var triangles = GetDelauneyTriangulation(allPoints);

            // remove all triangles whose center isn't in any of the land masses
            var landTriangles = triangles
                .Where(t =>
                {
                    foreach (var landmass in landmasses)
                    {
                        var centroid = t.Centroid;
                        if (landmass.IsVisible(centroid.X, centroid.Y))
                            return true;
                    }
                    return false;
                })
                .ToArray();

            LinkAdjacentTriangles(landTriangles);
            return landTriangles;
        }

        private List<PointF> PlaceDistricts(int numDistricts)
        {
            var districts = new List<PointF>();

            float xStart = Width / 10, yStart = Height / 10;
            float xRange = Width * 8 / 10, yRange = Height * 8 / 10;

            for (int i=0; i<numDistricts; i++)
            {
                PointF test;
                bool inAny = false;

                do
                {
                    test = new PointF(
                        Width * 0.8f * Random.NextFloat() + Width * 0.1f,
                        Height * 0.8f * Random.NextFloat() + Height * 0.1f
                    );
                    
                    foreach (var landmass in Landmasses)
                        if (landmass.IsVisible(test.X, test.Y))
                        {
                            inAny = true;
                            break;
                        }
                } while (!inAny);

                districts.Add(test);
            }

            return districts;
        }

        private List<TriangleF> GetDelauneyTriangulation(List<PointF> points)
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

        private bool InsideCircumcircle(PointF point, TriangleF triangle)
        {
            var distSq = point.DistanceSqTo(triangle.CircumCenter);
            return distSq <= triangle.CircumRadiusSq;
        }

        private void LinkAdjacentTriangles(IList<TriangleF> triangles)
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

        private List<DistrictGenerator> CombineTrianglesIntoDistricts(IList<TriangleF> triangles, int numDistricts)
        {
            List<DistrictGenerator> districts = CreateDistrictsWithAdjacency(triangles);
            
            var totalArea = districts.Sum(p => p.Area);
            var targetArea = totalArea / numDistricts;
            var minArea = targetArea / 5f;
            var deleteThreshold = minArea / 15f;

            // don't merge if its already big enough // TODO: instead of using area, use population? Don't want them too small, though!
            Func<DistrictGenerator, bool> belowTarget = d => d.Area < targetArea;
            Func<DistrictGenerator, bool> belowMinimum = d => d.Area < targetArea;

            while (MergeDistricts(districts, belowTarget, belowTarget))
                ;

            // Once we have grown all the districts as far as we can without going beyond the limit,
            // merge any very small districts onto anything that's adjacent.
            MergeDistricts(districts, belowMinimum, d => true);
            
            // Remove any remaining extremly small districts, as they will be tiny islands
            for (int iPolygon = 0; iPolygon < districts.Count; iPolygon++)
            {
                var testPolygon = districts[iPolygon];
                if (testPolygon.Area >= deleteThreshold)
                    continue;

                foreach (var other in testPolygon.AdjacentDistricts)
                    other.AdjacentDistricts.Remove(testPolygon);

                districts.RemoveAt(iPolygon);
                iPolygon--;
            }

            // TODO: any remaining small island districts should be merged with the nearest district, even if they don't touch.
            return districts;
        }

        private bool MergeDistricts(List<DistrictGenerator> districts, Func<DistrictGenerator, bool> districtFilter, Func<DistrictGenerator, bool> targetFilter)
        {
            bool addedAny = false;

            for (int iPolygon = 0; iPolygon < districts.Count; iPolygon++)
            {
                var testPolygon = districts[iPolygon];

                if (!districtFilter(testPolygon))
                    continue;

                // This polygon should be merged onto the adjacent polygon that shares its longest edge

                AdjacencyInfo bestAdjacency = null;
                DistrictGenerator bestPolygon = null;

                foreach (var polygon in districts)
                {
                    if (polygon == testPolygon)
                        continue; // don't merge with self

                    if (!targetFilter(polygon))
                        continue;

                    if (!testPolygon.AdjacentDistricts.Contains(polygon))
                        continue; // filter out non-adjacent districts quickly

                    var adjacency = testPolygon.GetAdjacencyInfo(polygon);
                    if (adjacency == null)
                        continue; // don't merge if not adjacent

                    if (bestAdjacency != null && adjacency.Length < bestAdjacency.Length)
                        continue; // don't merge if we already have a polygon we share longer edge(s) with

                    bestAdjacency = adjacency;
                    bestPolygon = polygon;
                }

                if (bestPolygon == null)
                    continue;

                bestPolygon.MergeWithDistrict(testPolygon, bestAdjacency);

                districts.RemoveAt(iPolygon);
                iPolygon--;

                addedAny = true;
            }

            return addedAny;
        }

        private static List<DistrictGenerator> CreateDistrictsWithAdjacency(IList<TriangleF> triangles)
        {
            // Prepare a list of the district for each triangle, and vice versa, so we can pass on adjacency information.
            var trianglesFromDistricts = new Dictionary<DistrictGenerator, TriangleF>(triangles.Count);
            var districtsFromTriangles = new Dictionary<TriangleF, DistrictGenerator>(triangles.Count);

            foreach (var triangle in triangles)
            {
                var district = new DistrictGenerator(triangle.Vertices);
                trianglesFromDistricts.Add(district, triangle);
                districtsFromTriangles.Add(triangle, district);
            }

            foreach (var kvp in trianglesFromDistricts)
            {
                var district = kvp.Key;
                var triangle = kvp.Value;

                var toAdd = triangle.AdjacentTriangles
                    .Select(t => districtsFromTriangles[t]);

                district.AdjacentDistricts.UnionWith(toAdd);
            }

            return trianglesFromDistricts.Keys.ToList();
        }
        #endregion

        #region combining into regions
        public List<RegionGenerator> AllocateRegions(List<DistrictGenerator> districts, int numRegions)
        {
            var regions = new List<RegionGenerator>(numRegions);
            Dictionary<PointF, int> regionCenters = new Dictionary<PointF, int>();

            for (int iRegion = 0; iRegion < numRegions; iRegion++)
            {
                var region = new RegionGenerator();
                regions.Add(region);

                // pick a random district to be the "seed" for each region
                var iDistrict = Random.Next(districts.Count);
                var seedDistrict = districts[iDistrict];
                districts.RemoveAt(iDistrict);

                region.AddDistrict(seedDistrict);
                regionCenters.Add(seedDistrict.GetCenter(), iRegion);
            }

            // each remaining district should be added to the closest region
            foreach (var district in districts)
            {
                var center = district.GetCenter();

                var closestCenter = center.GetClosest(regionCenters.Keys);
                int iClosestRegion = regionCenters[closestCenter];
                regions[iClosestRegion].AddDistrict(district);
            }

            EqualizeRegions(regions);

            return regions;
        }

        struct RegionChangeInfo
        {
            public DistrictGenerator District { get; set; }
            public RegionGenerator ToRegion { get; set; }
            public float NumOriginalAdjacent { get; set; }
            public float NumDestinationAdjacent { get; set; }
        }

        public void EqualizeRegions(IList<RegionGenerator> regions)
        {
            // Now that districts have been allocated, consider swapping "border" districts to adjacent regions
            // and see if that brings both swapped regions closer to the ideal area. (Eventually use population instead.)
            var districts = regions.SelectMany(r => r.Districts).ToArray();
            float targetPopulation = districts.Sum(d => d.Population) / regions.Count;
            bool anyChange = true;

            // Consider swapping districts into different regions, see if each swap would bring BOTH regions closer to the ideal.
            // Rather than considering each district in every pass, only consider those that are "edges" at the start of the pass.
            // And only consider one region at a time, so that districts are definitely still edges when we are considering them.
            // Additionally, consider the ones most surrounded by the target region first.
            while (anyChange)
            {
                anyChange = false;

                foreach (var region in regions)
                {
                    RegionChangeInfo[] possibleEdgeDistrictChanges = region.Districts
                        .SelectMany(d => d.AdjacentDistricts
                            .Where(a => a.Region != region)
                            .Distinct()
                            .Select(a => new RegionChangeInfo()
                            {
                                District = d,
                                ToRegion = a.Region,
                                NumOriginalAdjacent = d.AdjacentDistricts.Count(x => x.Region == d.Region),
                                NumDestinationAdjacent = d.AdjacentDistricts.Count(x => x.Region == a.Region),
                            })
                        )
                        .OrderByDescending(info => info.NumOriginalAdjacent == 0 ? float.MaxValue : info.NumDestinationAdjacent / info.NumOriginalAdjacent)
                        .ToArray(); // resolve this, as regions will be changed as we iterate

                    foreach (var possibleChange in possibleEdgeDistrictChanges)
                    {
                        var district = possibleChange.District;
                        var oldRegion = district.Region;
                        var newRegion = possibleChange.ToRegion;

                        bool shouldSwap = false;

                        // If a district doesn't touch any others from its region, it should be swapped even if this doesn't help the regions average out.
                        if (possibleChange.NumOriginalAdjacent == 0 && possibleChange.NumDestinationAdjacent > 0)
                            shouldSwap = true;
                        else
                        {
                            // Try swapping this district to the other region, see if it helps the regions average out
                            var changedOldRegionArea = oldRegion.Population - district.Population;
                            var changedNewRegionArea = newRegion.Population + district.Population;

                            var oldDelta = (district.Region.Population - targetPopulation) * (district.Region.Population - targetPopulation)
                                         + (newRegion.Population - targetPopulation) * (newRegion.Population - targetPopulation);
                            var newDelta = (changedOldRegionArea - targetPopulation) * (changedOldRegionArea - targetPopulation)
                                         + (changedNewRegionArea - targetPopulation) * (changedNewRegionArea - targetPopulation);

                            if (newDelta < oldDelta)
                                shouldSwap = true;
                        }

                        if (!shouldSwap)
                            continue;

                        // OK, swap this district's region
                        district.Region.RemoveDistrict(district);
                        newRegion.AddDistrict(district);
                        anyChange = true;
                    }
                }
            }
        }
        #endregion

        #region district properties
        const float PixelsPerSquareKm = 1;
        public void ApplyDistrictProperties(List<DistrictGenerator> districts)
        {
            IList<DistrictEffect> populationDensityEffects = GetPopulationDensityEffects(districts);
            IList<DistrictEffect> urbanisationEffects = GetUrbanisationEffects(districts);
            IList<DistrictEffect> coastalnessEffects = GetCoastalnessEffects(districts);
            IList<DistrictEffect> wealthEffects = GetWealthEffects(districts);
            IList<DistrictEffect> ageEffects = GetAgeEffects(districts);
            IList<DistrictEffect> educationEffects = GetEducationEffects(districts);
            IList<DistrictEffect> healthEffects = GetHealthEffects(districts);
            IList<DistrictEffect> geographicDivideEffects = GetGeographicDivideEffects(districts);

            foreach (var district in districts)
            {
                // 8 people per square km is the western isles.
                // 42 people per square km is Stirling (district).
                // 68 people per square km is South Lakeland.
                // 150 people per square km is Cornwall.
                // 179 people per square km is South Lanarkshire.
                // 280 people per square km is Fife.
                // 1500 per square km is London (greater metropolitan area average) or Sheffield.
                // 3550 per square km is Glasgow
                // 5000 per square km is Portsmouth, the highest district outside London.
                // 15000 per square km is Tower Hamlets.

                // The median for England as a whole is ~900.
                district.PopulationDensity = populationDensityEffects.AccumulateValues(district).Constrain(5, 15000);
                district.Population = (int)Math.Round(district.PopulationDensity * district.Area / PixelsPerSquareKm);

                district.Urbanisation = urbanisationEffects.AccumulateValues(district).Constrain(0, 1);
                district.Coastalness = coastalnessEffects.AccumulateValues(district).Constrain(0, 1);
                district.Wealth = wealthEffects.AccumulateValues(district).Constrain(0, 1);
                district.Age = ageEffects.AccumulateValues(district).Constrain(0, 1);
                district.Education = educationEffects.AccumulateValues(district).Constrain(0, 1);
                district.Health = healthEffects.AccumulateValues(district).Constrain(0, 1);
                district.GeographicDivide = geographicDivideEffects.AccumulateValues(district).Constrain(0, 1);
            }
        }

        private IList<DistrictEffect> GetPopulationDensityEffects(List<DistrictGenerator> districts)
        {
            // firstly, perlin noise
            float frequency = 0.005f;
            float amplitude = 255;
            var populationNoise = new PerlinNoise(Random.Next(), frequency, amplitude, 4);

            // additionally, a radial increase for a number of cities, with each one smaller than the last
            int numCityIncreases = Random.Next(2, 9);
            var effects = new DistrictEffect[numCityIncreases + 2];

            effects[0] = populationNoise;
            float cityMagnitude = Random.NextFloat() * 225 + 100;
            float cityFalloffDistance = Random.NextFloat() * Math.Max(Width, Height) * 0.5f + Math.Max(Width, Height) * 0.15f;

            for (int i = 1; i <= numCityIncreases; i++)
            {
                var centerDistrict = districts[Random.Next(districts.Count)];
                var cityFalloff = new RadialFalloff(centerDistrict.GetCenter(), cityMagnitude, cityFalloffDistance);
                cityMagnitude *= 0.7f;
                effects[i] = cityFalloff;
            }

            effects[effects.Length - 1] = new CustomDistrictEffect((density, district) =>
            {
                // Small islands and the ends of peninsulas have lower populations.
                switch (district.AdjacentDistricts.Count)
                {
                    case 0:
                        return density * 0.2f;
                    case 1:
                        return density * 0.6f;
                    case 2:
                        return density * 0.9f;
                    default:
                        return density;
                }
            });

            return effects;
        }

        private IList<DistrictEffect> GetUrbanisationEffects(List<DistrictGenerator> districts)
        {
            return new DistrictEffect[] {
                new CustomDistrictEffect((val, district) => (district.PopulationDensity - 10f) / 12500f)
            };
        }

        private IList<DistrictEffect> GetCoastalnessEffects(List<DistrictGenerator> districts)
        {
            return new DistrictEffect[] {
                new CustomDistrictEffect((val, district) =>
                {
                    // TODO: need to detect actual adjacency to the sea! ... or distance from sea-adjacent districts
                    switch (district.AdjacentDistricts.Count)
                    {
                        case 0:
                            return 1;
                        case 1:
                            return 0.85f;
                        case 2:
                            return 0.5f;
                        case 3:
                            return 0.2f;
                        default:
                            return 0;
                    }
                })
            };
        }

        private IList<DistrictEffect> GetWealthEffects(List<DistrictGenerator> districts)
        {
            return new DistrictEffect[] {
                new PerlinNoise(Random.Next(), 0.05f, 1, 4),
            };
        }

        private IList<DistrictEffect> GetHealthEffects(List<DistrictGenerator> districts)
        {
            return new DistrictEffect[] {
                new PerlinNoise(Random.Next(), 0.01f, 1, 4),
                new CustomDistrictEffect((val, district) => val - 0.5f * (1 - district.Urbanisation)), // urban areas are less healthy
            };
        }

        private IList<DistrictEffect> GetAgeEffects(List<DistrictGenerator> districts)
        {
            return new DistrictEffect[] {
                new PerlinNoise(Random.Next(), 0.005f, 1, 4),
            };
        }

        private IList<DistrictEffect> GetEducationEffects(List<DistrictGenerator> districts)
        {
            return new DistrictEffect[] {
                new PerlinNoise(Random.Next(), 0.005f, 1, 4),
                new CustomDistrictEffect((val, district) => val + 0.2f * district.Urbanisation), // urban areas are better educated ... ?
            };
        }

        private IList<DistrictEffect> GetGeographicDivideEffects(List<DistrictGenerator> districts)
        {
            return new DistrictEffect[] {
                new PerlinNoise(Random.Next(), 0.05f, 0.2f, 4),
                new CustomDistrictEffect((val, district) => val + 0.8f * district.GetCenter().Y / Height), // north-southness
            };
        }
        #endregion
    }
}