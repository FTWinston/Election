using ElectionDataGenerator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            seed = 1914506072;

            var timer = new Stopwatch();
            timer.Start();

            GenerateImage(seed, 5000, 500, 10);

            timer.Stop();
            Console.WriteLine(timer.Elapsed.TotalSeconds);
            Console.ReadKey();
        }

        private static void GenerateImage(int seed, int numInternalPoints, int numDistricts, int numRegions)
        {
            Console.WriteLine($"Generating with seed {seed}: {numInternalPoints} points, {numDistricts} districts in {numRegions} regions");

            var generator = new CountryGenerator(seed);
            List<DistrictGenerator> districts = generator.GenerateTerrainDistricts(numInternalPoints, numDistricts);
            generator.ApplyDistrictProperties(districts);
            List<RegionGenerator> regions = generator.AllocateRegions(districts, numRegions);

            var regionBrushes = new Dictionary<RegionGenerator, Brush>();
            var hueStep = 240.0 / regions.Count;
            var hue = 0.0;
            foreach (var region in regions)
            {
                var color = new HSLColor(hue, 140, 120);
                regionBrushes.Add(region, new SolidBrush(color));
                hue += hueStep;
            }

            var image = DrawTerrain(generator, regions, d => regionBrushes[d.Region]);
            image.Save($"regions_{seed}_{numInternalPoints}_{numDistricts}.png", ImageFormat.Png);

            image = DrawTerrain(generator, regions, d => {
                int popVal = (int)d.PopulationDensity + 128;
                if (popVal < 0)
                    popVal = 0;
                if (popVal > 255)
                    popVal = 255;

                return new SolidBrush(Color.FromArgb(popVal, popVal, popVal));
            });
            image.Save($"popDensity_{seed}_{numInternalPoints}_{numDistricts}.png", ImageFormat.Png);
        }

        private static Image DrawTerrain(CountryGenerator generator, List<RegionGenerator> regions, Func<DistrictGenerator, Brush> drawDistrict)
        {
            var image = new Bitmap(generator.Width, generator.Height);
            Graphics g = Graphics.FromImage(image);

            // draw rectangles on background to make the bounds clear
            Brush brush = new SolidBrush(Color.DarkBlue);
            g.FillRectangle(brush, 0, 0, generator.Width, generator.Height);

            foreach (var region in regions)
            {
                foreach (var district in region.Districts)
                {   
                    var path = new GraphicsPath();
                    path.AddLines(district.Vertices.Select(p => new System.Drawing.PointF(p.X, p.Y)).ToArray());

                    brush = drawDistrict(district);
                    g.FillPath(brush, path);
                }
            }

            return image;
        }
    }
}
