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
            Console.ReadKey();
        }

        private static void GenerateImage(int seed, bool reverseLoopHandling)
        {
            Console.WriteLine($"Generating with seed {seed} - will {(reverseLoopHandling ? "reverse" : "process")} any loops");

            var generator = new CountryGenerator(seed);

            var image = new Bitmap(generator.Width, generator.Height);
            Graphics g = Graphics.FromImage(image);

            // draw rectangles on background to make the bounds clear
            var brush = new SolidBrush(Color.Gray);
            g.FillRectangle(brush, 0, 0, generator.CenterX, generator.CenterY);
            g.FillRectangle(brush, generator.CenterX, generator.CenterY, generator.CenterX, generator.CenterY);

            var landMasses = generator.GenerateTerrain(reverseLoopHandling);

            var districts = generator.PlaceDistricts(100);
            var lines = generator.DelauneyTriangulation(districts);

            // fill in the terrain
            brush = new SolidBrush(Color.Green);
            foreach (var island in landMasses)
                g.FillPath(brush, island);

            // draw the district divisions
            var pen = new Pen(Color.Yellow);
            foreach (var line in lines)
                g.DrawLine(pen, line.Item1, line.Item2);

            // draw the district center points
            brush = new SolidBrush(Color.Red);
            foreach (var district in districts)
                g.FillEllipse(brush, district.X - 1, district.Y - 1, 2, 2);

            image.Save($"generated_{(reverseLoopHandling ? "normal" : "reverse")}.png", ImageFormat.Png);
        }
    }
}
