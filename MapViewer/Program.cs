using ElectionDataGenerator;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace MapViewer
{
    class Program
    {
        static void Main(string[] args)
        {
            Random r = new Random();

            // 0.001 to 0.01 seems to work best for noise scale, 100 works well for noise magnitude
            for (int i=1; i<=5; i++)
                GenerateImage(r.Next(), i);
        }

        private static void GenerateImage(int seed, int fileNumber)
        {
            var generator = new CountryGenerator(seed);
            float frequency = 0.005f;
            float amplitude = Math.Min(generator.Width, generator.Height) / 3;

            var image = new Bitmap(generator.Width, generator.Height);
            Graphics g = Graphics.FromImage(image);

            // draw rectangles on background to make the bounds clear
            var brush = new SolidBrush(Color.Gray);
            g.FillRectangle(brush, 0, 0, generator.CenterX, generator.CenterY);
            g.FillRectangle(brush, generator.CenterX, generator.CenterY, generator.CenterX, generator.CenterY);

            brush = new SolidBrush(Color.Green);
            /*
            // try every point in turn, determine if it should be land or not based on if its in an ellipse, then perturb
            for (int x = 0; x < generator.Width; x++)
                for (int y = 0; y < generator.Height; y++)
                {
                    if (generator.IsPointWithinEllipse(x, y))
                    {
                        var placeX = x + (int)(amplitude * generator.GetTerrainNoiseX(x * frequency, y * frequency));
                        var placeY = y + (int)(amplitude * generator.GetTerrainNoiseY(x * frequency, y * frequency));

                        if (placeX >= 0 && placeX < generator.Width && placeY >= 0 && placeY < generator.Height)
                            g.FillEllipse(brush, placeX, placeY, 20, 20);
                    }
                }
            */

            // rather than looping over every tile, let's just loop around the elipse edge, adding noise and joining the dots
            var points = new List<PointF>();
            for (float angle = (float)Math.PI * 2; angle > 0; angle -= 0.01f /* roughly 500 steps */)
            {
                float ellipseX = generator.CenterX + (float)Math.Cos(angle) * generator.EllipseWidth / 2;
                float ellipseY = generator.CenterY + (float)Math.Sin(angle) * generator.EllipseHeight / 2;

                float placeX = ellipseX + amplitude * generator.GetTerrainNoiseX(ellipseX * frequency, ellipseY * frequency);
                float placeY = ellipseY + amplitude * generator.GetTerrainNoiseY(ellipseX * frequency, ellipseY * frequency);

                points.Add(new PointF(placeX, placeY));
            }

            var types = new byte[points.Count];
            Array.Fill<byte>(types, 1);


            var path = new GraphicsPath(points.ToArray(), types);
            g.FillPath(new SolidBrush(Color.Green), path);

            // draw transparent ellipse for comparison
            brush = new SolidBrush(Color.FromArgb(32, Color.Red));
            g.FillEllipse(brush, generator.CenterX - generator.EllipseWidth / 2, generator.CenterY - generator.EllipseHeight / 2, generator.EllipseWidth, generator.EllipseHeight);

            image.Save($"generated_{fileNumber}.png", ImageFormat.Png);
        }
    }
}
