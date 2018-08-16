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
            //Console.ReadKey();
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

            var font = new Font("Segoe UI", 6);
            var textBrush = new SolidBrush(Color.Black);
            var textFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            var image = DrawTerrain(generator, regions, (district, g, path) =>
            {
                var brush = regionBrushes[district.Region];
                g.FillPath(brush, path);

                if (district != district.Region.Districts.First())
                    return;

                var center = district.GetCenter();
                var totalPopulation = district.Region.Districts.Sum(d => d.Population);
                g.DrawString(totalPopulation.ToString(), font, textBrush, center.X, center.Y, textFormat);
            });
            image.Save($"regions_{seed}_{numInternalPoints}_{numDistricts}.png", ImageFormat.Png);


            var maxPopulationDensity = districts.Max(d => d.PopulationDensity);
            textBrush = new SolidBrush(Color.DarkRed);

            image = DrawTerrain(generator, regions, (district, g, path) => {
                int popVal = (int)(255 * district.PopulationDensity / maxPopulationDensity);
                if (popVal < 0)
                    popVal = 0;
                
                var brush = new SolidBrush(Color.FromArgb(popVal, popVal, popVal));

                g.FillPath(brush, path);

                var center = district.GetCenter();
                g.DrawString(district.Population.ToString(), font, textBrush, center.X, center.Y, textFormat);
            });
            image.Save($"popDensity_{seed}_{numInternalPoints}_{numDistricts}.png", ImageFormat.Png);
        }

        private static Image DrawTerrain(CountryGenerator generator, List<RegionGenerator> regions, Action<DistrictGenerator, Graphics, GraphicsPath> drawDistrict)
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

                    drawDistrict(district, g, path);
                }
            }

            return image;
        }
    }
}
