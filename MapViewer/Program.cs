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

        private static void GenerateImage(int seed, bool reverseLoopHandling)
        {
            Console.WriteLine($"Generating with seed {seed} - will {(reverseLoopHandling ? "reverse" : "process")} any loops");

            var generator = new CountryGenerator(seed);

            var landmasses = generator.GenerateTerrain(reverseLoopHandling);

            var districts = generator.PlaceDistricts(100);

            var allPoints = landmasses
                .SelectMany(l => l.PathPoints)
                .ToList();
            allPoints.AddRange(districts);

            var triangles = generator.DelauneyTriangulation(allPoints);

            // remove all triangles whose center isn't in any of the land masses
            triangles = triangles.Where(t =>
            {
                foreach (var landmass in landmasses)
                    if (landmass.IsVisible(t.Centroid))
                        return true;
                return false;
            })
            .ToList();

            var image = DrawTerrain(generator, landmasses, districts, triangles);

            image.Save($"generated_{(reverseLoopHandling ? "reverse" : "normal")}.png", ImageFormat.Png);
        }

        private static Image DrawTerrain(CountryGenerator generator, List<GraphicsPath> landmasses, List<PointF> districts, List<TriangleF> triangles)
        {
            var image = new Bitmap(generator.Width, generator.Height);
            Graphics g = Graphics.FromImage(image);

            // draw rectangles on background to make the bounds clear
            var brush = new SolidBrush(Color.Gray);
            g.FillRectangle(brush, 0, 0, generator.CenterX, generator.CenterY);
            g.FillRectangle(brush, generator.CenterX, generator.CenterY, generator.CenterX, generator.CenterY);


            var lines = CountryGenerator.GetDistinctLines(triangles);

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

            return image;
        }
    }
}
