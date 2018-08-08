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

            GenerateImage(seed, 5000, 500, 10);
            //Console.ReadKey();
        }

        private static void GenerateImage(int seed, int numInternalPoints, int numDistricts, int numRegions)
        {
            Console.WriteLine($"Generating with seed {seed}: {numInternalPoints} points, {numDistricts} districts in {numRegions} regions");

            var generator = new CountryGenerator(seed);
            List<DistrictGenerator> districts = generator.GenerateTerrainDistricts(numInternalPoints, numDistricts);

            List<RegionGenerator> regions = generator.AllocateRegions(districts, numRegions);

            var image = DrawTerrain(generator, regions);
            image.Save($"generated_{seed}_{numInternalPoints}_{numDistricts}.png", ImageFormat.Png);
        }

        private static Image DrawTerrain(CountryGenerator generator, List<RegionGenerator> regions)
        {
            var image = new Bitmap(generator.Width, generator.Height);
            Graphics g = Graphics.FromImage(image);

            // draw rectangles on background to make the bounds clear
            var brush = new SolidBrush(Color.DarkBlue);
            g.FillRectangle(brush, 0, 0, generator.Width, generator.Height);

            Random colors = new Random();

            // draw each region in a separate hue, evenly spaced
            var hueStep = 240.0 / regions.Count;
            var hue = 0.0;

            foreach (var region in regions)
            {
                foreach (var district in region.Districts)
                {   
                    var path = new GraphicsPath();
                    path.AddLines(district.Vertices.Select(p => new System.Drawing.PointF(p.X, p.Y)).ToArray());

                    // each district is a different color, with its region's hue
                    var saturation = colors.NextDouble() * 60 + 90;
                    var luminosity = colors.NextDouble() * 60 + 90;
                    var color = new HSLColor(hue, saturation, luminosity);

                    brush = new SolidBrush(color);
                    g.FillPath(brush, path);
                }

                hue += hueStep;
            }

            return image;
        }
    }
}
