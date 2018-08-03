﻿using ElectionDataGenerator;
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

            var landmasses = generator.GenerateTerrain();

            var internalPoints = generator.PlaceDistricts(numInternalPoints);

            var allPoints = landmasses
                .SelectMany(l => l.PathPoints)
                .ToList();
            allPoints.AddRange(internalPoints);

            var triangles = generator
                .GetDelauneyTriangulation(allPoints);
            generator.LinkAdjacentTriangles(triangles);

            // remove all triangles whose center isn't in any of the land masses, and convert the remainder to polygons
            var districts = triangles
                .Where(t =>
                {
                    foreach (var landmass in landmasses)
                        if (landmass.IsVisible(t.Centroid))
                            return true;
                    return false;
                })
                .Select(t => new PolygonF(t.Vertices))
                .ToList();

            var totalArea = districts.Sum(p => p.Area);
            var targetArea = totalArea / numDistricts;
            var minArea = targetArea / 5f;

            bool keepAdding;

            // each unfinished polygon should be merged onto the adjacent polygon that shares its longest edge
            do
            {
                keepAdding = false;

                for (int iPolygon = 0; iPolygon < districts.Count; iPolygon++)
                {
                    var testPolygon = districts[iPolygon];

                    if (testPolygon.Area > targetArea)
                        continue; // don't merge if it's already big enough

                    AdjacencyInfo bestAdjacency = null;
                    PolygonF bestPolygon = null;

                    foreach (var polygon in districts)
                    {
                        if (polygon == testPolygon)
                            continue; // don't merge with self

                        if (polygon.Area > targetArea)
                            continue; // don't merge if target is already big enough
                        
                        var adjacency = testPolygon.GetAdjacencyInfo(polygon);
                        if (adjacency == null)
                            continue; // don't merge if not adjacent

                        if (bestAdjacency != null && adjacency.Length < bestAdjacency.Length)
                            continue; // don't merge if we already have a polygon we share longer edge(s) with

                        bestAdjacency = adjacency;
                        bestPolygon = polygon;
                    }

                    if (bestPolygon == null)
                        continue;

                    bestPolygon.MergeWith(testPolygon, bestAdjacency);

                    districts.RemoveAt(iPolygon);
                    iPolygon--;

                    keepAdding = true;
                }

            } while (keepAdding);

            var image = DrawTerrain(generator, landmasses, districts);
            image.Save($"generated_{seed}_{numInternalPoints}_{numDistricts}_beforeTiny.png", ImageFormat.Png);

            // Once we have grown all the districts as far as we can without going beyond the limit,
            // merge any very small districts onto anything that's adjacent.
            for (int iPolygon = 0; iPolygon < districts.Count; iPolygon++)
            {
                var testPolygon = districts[iPolygon];
                if (testPolygon.Area >= minArea)
                    continue;

                // merge this district onto any adjacent one, if we can
                AdjacencyInfo bestAdjacency = null;
                PolygonF bestPolygon = null;

                foreach (var polygon in districts)
                {
                    if (polygon == testPolygon)
                        continue; // don't merge with self

                    var adjacency = testPolygon.GetAdjacencyInfo(polygon);
                    if (adjacency == null)
                        continue; // don't merge if not adjacent

                    if (bestAdjacency != null && adjacency.Length < bestAdjacency.Length)
                        continue; // don't merge if we already have a polygon we share longer edge(s) with

                    bestAdjacency = adjacency;
                    bestPolygon = polygon;
                }

                if (bestPolygon == null)
                    continue;

                bestPolygon.MergeWith(testPolygon, bestAdjacency);

                districts.RemoveAt(iPolygon);
                iPolygon--;
            }

            // TODO: any remaining tiny island districts should be merged with the nearest district, even if they don't touch.

            image = DrawTerrain(generator, landmasses, districts);
            image.Save($"generated_{seed}_{numInternalPoints}_{numDistricts}.png", ImageFormat.Png);
        }

        private static Image DrawTerrain(CountryGenerator generator, List<GraphicsPath> landmasses, IEnumerable<PolygonF> polygons)
        {
            var image = new Bitmap(generator.Width, generator.Height);
            Graphics g = Graphics.FromImage(image);

            // draw rectangles on background to make the bounds clear
            var brush = new SolidBrush(Color.Gray);
            g.FillRectangle(brush, 0, 0, generator.CenterX, generator.CenterY);
            g.FillRectangle(brush, generator.CenterX, generator.CenterY, generator.CenterX, generator.CenterY);

            /*
            // draw the terrain outline
            var pen = new Pen(Color.LightGray, 5);
            foreach (var island in landmasses)
                g.DrawPath(pen, island);
            */

            Random colors = new Random();

            // draw each region in a separate colour
            foreach (var polygon in polygons)
            {
                if (polygon.Vertices.Count == 0)
                    continue;

                var path = new GraphicsPath();
                path.AddLines(polygon.Vertices.ToArray());

                brush = new SolidBrush(Color.FromArgb(0, 64 + colors.Next(192), 64 + colors.Next(192)));
                g.FillPath(brush, path);
            }

            return image;
        }
    }
}
