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

            var captionFont = new Font("Segoe UI", 18);
            var captionBrush = new SolidBrush(Color.White);
            var captionFormat = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };

            var regionBrushes = new Dictionary<RegionGenerator, Brush>();
            var hueStep = 240.0 / regions.Count;
            var hue = 0.0;
            foreach (var region in regions)
            {
                var color = new HSLColor(hue, 140, 120);
                regionBrushes.Add(region, new SolidBrush(color));
                hue += hueStep;
            }

            var font = new Font("Segoe UI", 18);
            var textBrush = new SolidBrush(Color.Black);
            var textFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            // draw region allocation
            var image = DrawTerrain(generator, regions, (district, g, path) =>
            {
                var brush = regionBrushes[district.Region];
                g.FillPath(brush, path);
            }, (region, g) =>
            {
                var centerX = region.Districts.Sum(d => d.GetCenter().X) / region.Districts.Count;
                var centerY = region.Districts.Sum(d => d.GetCenter().Y) / region.Districts.Count;
                var totalPopulation = region.Districts.Sum(d => d.Population);
                g.DrawString(totalPopulation.ToString(), font, textBrush, centerX, centerY, textFormat);
            });
            Graphics.FromImage(image).DrawString("Region (color) and region population (text)", captionFont, captionBrush, 0, 0, captionFormat);
            image.Save($"{seed}_{numInternalPoints}_{numDistricts}_regions.png", ImageFormat.Png);


            // draw population and population density
            var maxPopulationDensity = districts.Max(d => d.PopulationDensity);
            font = new Font("Segoe UI", 6);
            textBrush = new SolidBrush(Color.DarkRed);

            image = DrawTerrain(generator, regions, (district, g, path) => {
                int popVal = (int)(255 * district.PopulationDensity / maxPopulationDensity);
                if (popVal < 0)
                    popVal = 0;
                
                var brush = new SolidBrush(Color.FromArgb(popVal, popVal, popVal));

                g.FillPath(brush, path);

                var center = district.GetCenter();
                g.DrawString(district.Population.ToString(), font, textBrush, center.X, center.Y, textFormat);
            }, null);
            Graphics.FromImage(image).DrawString("Population (text) and population density (color)", captionFont, captionBrush, 0, 0, captionFormat);
            image.Save($"{seed}_{numInternalPoints}_{numDistricts}_population.png", ImageFormat.Png);

            // draw urbanisation
            image = DrawTerrain(generator, regions, (district, g, path) => {
                int popVal = (int)(255 * district.Urbanisation);
                if (popVal < 0)
                    popVal = 0;

                var brush = new SolidBrush(Color.FromArgb(popVal, popVal, popVal));
                g.FillPath(brush, path);
            }, null);
            Graphics.FromImage(image).DrawString("Urbanisation", captionFont, captionBrush, 0, 0, captionFormat);
            image.Save($"{seed}_{numInternalPoints}_{numDistricts}_urbanisation.png", ImageFormat.Png);

            // draw coastalness
            image = DrawTerrain(generator, regions, (district, g, path) => {
                int popVal = (int)(255 * district.Coastalness);
                if (popVal < 0)
                    popVal = 0;

                var brush = new SolidBrush(Color.FromArgb(popVal, popVal, popVal));
                g.FillPath(brush, path);
            }, null);
            Graphics.FromImage(image).DrawString("Coastalness", captionFont, captionBrush, 0, 0, captionFormat);
            image.Save($"{seed}_{numInternalPoints}_{numDistricts}_coastalness.png", ImageFormat.Png);

            // draw wealth
            image = DrawTerrain(generator, regions, (district, g, path) => {
                int popVal = (int)(255 * district.Wealth);
                if (popVal < 0)
                    popVal = 0;

                var brush = new SolidBrush(Color.FromArgb(popVal, popVal, popVal));
                g.FillPath(brush, path);
            }, null);
            Graphics.FromImage(image).DrawString("Wealth", captionFont, captionBrush, 0, 0, captionFormat);
            image.Save($"{seed}_{numInternalPoints}_{numDistricts}_wealth.png", ImageFormat.Png);

            // draw age
            image = DrawTerrain(generator, regions, (district, g, path) => {
                int popVal = (int)(255 * district.Age);
                if (popVal < 0)
                    popVal = 0;

                var brush = new SolidBrush(Color.FromArgb(popVal, popVal, popVal));
                g.FillPath(brush, path);
            }, null);
            Graphics.FromImage(image).DrawString("Age", captionFont, captionBrush, 0, 0, captionFormat);
            image.Save($"{seed}_{numInternalPoints}_{numDistricts}_age.png", ImageFormat.Png);

            // draw education
            image = DrawTerrain(generator, regions, (district, g, path) => {
                int popVal = (int)(255 * district.Education);
                if (popVal < 0)
                    popVal = 0;

                var brush = new SolidBrush(Color.FromArgb(popVal, popVal, popVal));
                g.FillPath(brush, path);
            }, null);
            Graphics.FromImage(image).DrawString("Education", captionFont, captionBrush, 0, 0, captionFormat);
            image.Save($"{seed}_{numInternalPoints}_{numDistricts}_education.png", ImageFormat.Png);

            // draw health
            image = DrawTerrain(generator, regions, (district, g, path) => {
                int popVal = (int)(255 * district.Health);
                if (popVal < 0)
                    popVal = 0;

                var brush = new SolidBrush(Color.FromArgb(popVal, popVal, popVal));
                g.FillPath(brush, path);
            }, null);
            Graphics.FromImage(image).DrawString("Health", captionFont, captionBrush, 0, 0, captionFormat);
            image.Save($"{seed}_{numInternalPoints}_{numDistricts}_health.png", ImageFormat.Png);

            // draw geographic divide
            image = DrawTerrain(generator, regions, (district, g, path) => {
                int popVal = (int)(255 * district.GeographicDivide);
                if (popVal < 0)
                    popVal = 0;

                var brush = new SolidBrush(Color.FromArgb(popVal, popVal, popVal));
                g.FillPath(brush, path);
            }, null);
            Graphics.FromImage(image).DrawString("Geographic Divide", captionFont, captionBrush, 0, 0, captionFormat);
            image.Save($"{seed}_{numInternalPoints}_{numDistricts}_geographic_divide.png", ImageFormat.Png);
        }

        private static Image DrawTerrain(CountryGenerator generator, List<RegionGenerator> regions, Action<DistrictGenerator, Graphics, GraphicsPath> drawDistrict, Action<RegionGenerator, Graphics> drawRegion)
        {
            var image = new Bitmap(generator.Width, generator.Height);
            Graphics g = Graphics.FromImage(image);

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

                drawRegion?.Invoke(region, g);
            }

            return image;
        }
    }
}
