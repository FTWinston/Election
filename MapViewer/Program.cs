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

            GenerateImage(seed, 1000, 100);
            //Console.ReadKey();
        }

        private static void GenerateImage(int seed, int numInternalPoints, int numDistricts)
        {
            Console.WriteLine($"Generating with seed {seed}: {numInternalPoints} points, {numDistricts} districts");

            var generator = new CountryGenerator(seed);
            List<PolygonF> districts = generator.GenerateTerrainDistricts(numInternalPoints, numDistricts);

            var image = DrawTerrain(generator, districts);
            image.Save($"generated_{seed}_{numInternalPoints}_{numDistricts}.png", ImageFormat.Png);
        }

        private static Image DrawTerrain(CountryGenerator generator, IEnumerable<PolygonF> polygons)
        {
            var image = new Bitmap(generator.Width, generator.Height);
            Graphics g = Graphics.FromImage(image);

            // draw rectangles on background to make the bounds clear
            var brush = new SolidBrush(Color.Blue);
            g.FillRectangle(brush, 0, 0, generator.Width, generator.Height);

            Random colors = new Random();

            // draw each region in a separate colour
            foreach (var polygon in polygons)
            {
                if (polygon.Vertices.Count == 0)
                    continue;

                var path = new GraphicsPath();
                path.AddLines(polygon.Vertices.ToArray());

                brush = new SolidBrush(Color.FromArgb(64 + colors.Next(192), 64 + colors.Next(192), 0));
                g.FillPath(brush, path);
            }

            return image;
        }
    }
}
