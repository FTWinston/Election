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

            GenerateImage(seed, false);
            GenerateImage(seed, true);
            //Console.ReadKey();
        }

        private static void GenerateImage(int seed, bool sortDistricts)
        {
            Console.WriteLine($"Generating with seed {seed} - will {(sortDistricts ? "add to smallest" : "add to any")} district");

            var generator = new CountryGenerator(seed);

            var landmasses = generator.GenerateTerrain(sortDistricts);

            var internalPoints = generator.PlaceDistricts(100);

            var allPoints = landmasses
                .SelectMany(l => l.PathPoints)
                .ToList();
            allPoints.AddRange(internalPoints);

            var triangles = generator.DelauneyTriangulation(allPoints);

            // remove all triangles whose center isn't in any of the land masses
            var polygons = triangles.Where(t =>
            {
                foreach (var landmass in landmasses)
                    if (landmass.IsVisible(t.Centroid))
                        return true;
                return false;
            })
            .Select(t => new PolygonF(t.Vertices))
            .ToList();

            // pick certain triangles to be the start of districts, and merge adjacent triangles into them
            Random r = new Random();

            var districts = new List<PolygonF>();
            for (int i=0; i<10; i++)
            {
                int polygonIndex = r.Next(polygons.Count);
                districts.Add(polygons[polygonIndex]);
                polygons.RemoveAt(polygonIndex);
            }

            int numUnmerged;
            do
            {
                numUnmerged = polygons.Count;

                for (int i = polygons.Count - 2; i >= 0; i--)
                {
                    // If any adjacent districts, add this point to the one with the smallest area
                    foreach (var district in districts.OrderByDescending(d => d.Area))
                    {
                        if (district.MergeIfAdjacent(polygons[i]))
                        {
                            polygons.RemoveAt(i);
                            break;
                        }
                    }
                }
            } while (numUnmerged != polygons.Count); // if some polygon has been merged, keep going

            var image = DrawTerrain(generator, landmasses, internalPoints, districts.Concat(polygons));
            image.Save($"generated_{(sortDistricts ? "smallest" : "normal")}.png", ImageFormat.Png);
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
