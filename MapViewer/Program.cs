using ElectionDataGenerator;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;

namespace MapViewer
{
    class Program
    {
        static void Main(string[] args)
        {
            Random r = new Random();
            int seed = r.Next();

            GenerateImage(seed, 50, 10);
            GenerateImage(seed, 100, 10);
            GenerateImage(seed, 500, 10);

            GenerateImage(seed, 50, 50);
            GenerateImage(seed, 100, 50);
            GenerateImage(seed, 100, 75);

            GenerateImage(seed, 100, 50);
            GenerateImage(seed, 200, 75);
            GenerateImage(seed, 200, 100);

            GenerateImage(seed, 500, 50);
            GenerateImage(seed, 500, 100);
            GenerateImage(seed, 500, 200);
            //Console.ReadKey();
        }

        private static void GenerateImage(int seed, int numInternalPoints, int numDistricts)
        {
            Console.WriteLine($"Generating with seed {seed}: {numInternalPoints} points, {numDistricts} districts");

            var generator = new CountryGenerator(seed);

            var landmasses = generator.GenerateTerrain();

            var internalPoints = generator.PlaceDistricts(numInternalPoints);

            var allPoints = landmasses
                .SelectMany(l => l.PathPoints)
                .ToList();
            allPoints.AddRange(internalPoints);

            var triangles = generator
                .GetDelauneyTriangulation(allPoints);
            generator.LinkAdjacentTriangles(triangles);

            // remove all triangles whose center isn't in any of the land masses, and convert the remainder to polygons
            var districts = triangles
                .Where(t =>
                {
                    foreach (var landmass in landmasses)
                        if (landmass.IsVisible(t.Centroid))
                            return true;
                    return false;
                })
                .Select(t => new PolygonF(t.Vertices))
                .ToList();

            var totalArea = districts.Sum(p => p.Area);
            var targetArea = totalArea / numDistricts;
            var minArea = targetArea / 5f;

            bool keepAdding;

            // each unfinished polygon should be merged onto the adjacent polygon that shares its longest edge
            do
            {
                keepAdding = false;

                for (int iPolygon = 0; iPolygon < districts.Count; iPolygon++)
                {
                    var testPolygon = districts[iPolygon];

                    if (testPolygon.Area > targetArea)
                        continue; // don't merge into something else if it's already big enough

                    var allAdjacent = districts
                        .Where(p => p != testPolygon)
                        .Select(p => new Tuple<PolygonF, AdjacencyInfo>(p, testPolygon.GetAdjacencyInfo(p)))
                        .Where(adjaceny => adjaceny.Item2 != null);

                    var longestEdgeAjacent = allAdjacent
                        .Where(a => a.Item1.Area < targetArea) // don't merge into something that's already big enough
                        .OrderByDescending(a => a.Item2.Length)
                        .FirstOrDefault();

                    if (longestEdgeAjacent == null)
                        continue;

                    longestEdgeAjacent.Item1.MergeWith(testPolygon, longestEdgeAjacent.Item2);

                    districts.RemoveAt(iPolygon);
                    iPolygon--;

                    keepAdding = true;
                }

            } while (keepAdding);

            /*
            // once we have districts, merge any very small districts (< 1/50 total?) onto anything with even one adjacent vertex
            for (int iDistrict = 0; iDistrict < districts.Count; iDistrict++)
            {
                var district = districts[iDistrict];
                if (district.Area >= minArea)
                    continue;

                // merge this district onto any adjacent one, if we can
                for (int iMergeWith = 0; iMergeWith < districts.Count; iMergeWith++)
                {
                    if (iMergeWith == iDistrict)
                        continue;

                    var mergeInto = districts[iMergeWith];
                    if (mergeInto.MergeIfAdjacent(district))
                    {
                        districts.RemoveAt(iDistrict);
                        iDistrict--;
                        break;
                    }
                }
            }
            */
            var image = DrawTerrain(generator, landmasses, internalPoints, districts);
            image.Save($"generated_{seed}_{numInternalPoints}_{numDistricts}.png", ImageFormat.Png);
        }

        private static Image DrawTerrain(CountryGenerator generator, List<GraphicsPath> landmasses, List<PointF> districts, IEnumerable<PolygonF> polygons)
        {
            var image = new Bitmap(generator.Width, generator.Height);
            Graphics g = Graphics.FromImage(image);

            // draw rectangles on background to make the bounds clear
            var brush = new SolidBrush(Color.Gray);
            g.FillRectangle(brush, 0, 0, generator.CenterX, generator.CenterY);
            g.FillRectangle(brush, generator.CenterX, generator.CenterY, generator.CenterX, generator.CenterY);


            //var lines = CountryGenerator.GetDistinctLines(polygons);

            /*
            // fill in the terrain
            brush = new SolidBrush(Color.Green);
            foreach (var island in landmasses)
                g.FillPath(brush, island);

            // draw the district divisions
            var pen = new Pen(Color.Yellow);
            foreach (var line in lines)
                g.DrawLine(pen, line.Item1, line.Item2);

            // draw the district center points
            brush = new SolidBrush(Color.Red);
            foreach (var district in districts)
                g.FillEllipse(brush, district.X - 1, district.Y - 1, 2, 2);
            */

            Random colors = new Random();

            // draw each region in a separate colour
            foreach (var polygon in polygons)
            {
                var path = new GraphicsPath();
                path.AddLines(polygon.Vertices.ToArray());

                brush = new SolidBrush(Color.FromArgb(64 + colors.Next(192), 64 + colors.Next(192), 64 + colors.Next(192)));
                g.FillPath(brush, path);
            }

            return image;
        }
    }
}
