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

            // 0.001 to 0.01 seems to work best for noise frequency, 100 works well for noise amplitude
            float frequency = 0.005f;
            float amplitude = Math.Min(generator.Width, generator.Height) / 2;

            var image = new Bitmap(generator.Width, generator.Height);
            Graphics g = Graphics.FromImage(image);

            // draw rectangles on background to make the bounds clear
            var brush = new SolidBrush(Color.Gray);
            g.FillRectangle(brush, 0, 0, generator.CenterX, generator.CenterY);
            g.FillRectangle(brush, generator.CenterX, generator.CenterY, generator.CenterX, generator.CenterY);

            brush = new SolidBrush(Color.Green);

            // rather than looping over every tile, let's just loop around the elipse edge, adding noise and joining the dots
            var mainland = new List<PointF>();
            var landMasses = new List<List<PointF>>();
            landMasses.Add(mainland);

            var prevPoint = new PointF(generator.CenterX, generator.CenterY);

            int step = 0;
            for (float angle = (float)Math.PI * 2; angle > 0; angle -= 0.01f /* roughly 500 steps */)
            {
                step++;

                float ellipseX = generator.CenterX + (float)Math.Cos(angle) * generator.EllipseWidth / 2;
                float ellipseY = generator.CenterY + (float)Math.Sin(angle) * generator.EllipseHeight / 2;

                float placeX = ellipseX + amplitude * generator.GetTerrainNoiseX(ellipseX * frequency, ellipseY * frequency);
                float placeY = ellipseY + amplitude * generator.GetTerrainNoiseY(ellipseX * frequency, ellipseY * frequency);

                var nextPoint = new PointF(placeX, placeY);

                // See if prevPoint - nextPoint crosses ANY of the previous lines. If it does, we have a loop / dangler.
                // Ignore the first line and the most recent line, cos we may well touch those.

                for (int i = mainland.Count - 3; i > 1; i--)
                {
                    var testEnd = mainland[i - 1];
                    var testStart = mainland[i];

                    if (CountryGenerator.LineSegmentsCross(prevPoint, nextPoint, testStart, testEnd))
                    {
                        // where the path crosses over itself, we need to deal with the extra loop, in one of two ways
                        Console.WriteLine($"Found an intersection for step {step} with line segment #{i}");

                        // Because we are ALWAYS working our way anticlockwise around the main landmass:
                        // "outties" always have the "older" line cross the "newer" one from left to right.
                        // "innies" always have the "older" line cross the "newer" one from right to left.
                        // UNLESS we are dealing with "nested" outties or innies, in which case the direction swaps.
                        // But maybe its ok to swap what we do there, as we will get larger islands / inlets that way,
                        // as opposed to "chains" of smaller ones, which are perhaps less important - especially on a political map.

                        bool isLeft = CountryGenerator.IsLeftOfLine(prevPoint, nextPoint, testEnd);

                        if (reverseLoopHandling)
                            isLeft = !reverseLoopHandling;

                        // is the end point of the "older" line on the right of the "newer" line? If so chop, otherwise reverse.
                        if (isLeft)
                        {
                            int skipPoints = 2; // don't have islands start right adjacent to the mainland
                            int islandPointsStart = i + 1 + skipPoints;
                            int numIslandPoints = mainland.Count - islandPointsStart - skipPoints;

                            if (numIslandPoints > 3)
                            {
                                landMasses.Add(
                                    new List<PointF>(
                                        mainland
                                            .Skip(islandPointsStart)
                                            .Take(numIslandPoints)
                                    )
                                );
                            }

                            // snip this entire "loop" off!
                            mainland.RemoveRange(i + 1, mainland.Count - i - 1);
                        }
                        else
                        {
                            // reverse the order of all points after #i
                            var reversePoints = mainland
                                .Skip(i + 1)
                                .Take(mainland.Count - i - 1)
                                .Reverse()
                                .ToArray();

                            mainland.RemoveRange(i, mainland.Count - i);
                            mainland.AddRange(reversePoints);
                        }
                        break;
                    }
                }

                mainland.Add(nextPoint);
                prevPoint = nextPoint;
            }

            foreach (var island in landMasses)
                FillPoints(g, island);

            // draw transparent ellipse for comparison
            brush = new SolidBrush(Color.FromArgb(32, Color.Red));
            g.FillEllipse(brush, generator.CenterX - generator.EllipseWidth / 2, generator.CenterY - generator.EllipseHeight / 2, generator.EllipseWidth, generator.EllipseHeight);

            image.Save($"generated_{(reverseLoopHandling ? "normal" : "reverse")}.png", ImageFormat.Png);
        }

        private static void FillPoints(Graphics g, List<PointF> points)
        {
            var types = new byte[points.Count];
            Array.Fill<byte>(types, 1);

            var path = new GraphicsPath(points.ToArray(), types);
            g.FillPath(new SolidBrush(Color.Green), path);
        }
    }
}
