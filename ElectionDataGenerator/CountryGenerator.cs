using ElectionData.Geography;
using System;
using System.Drawing;

namespace ElectionDataGenerator
{
    public class CountryGenerator
    {
        /*
         * Use simplex noise to simulate "elevation"
         *# Consider eroding a few river esturies.
         * Determine the coastline, including any inland bodies of water.
         * 
         * Within that coastline, place a pre-determined number of "district nuclei", somewhat evenly distributed.
         *# If there's any land masses that have no districts on them, move at least one nuclei to each. How to determine?
         * Expand those nuclei to determine the outline of each district. That sounds rather like a voronio diagram.
         *# Determine the "center" of each district, which its attributes will be calculated from.
         *# This should really just be (min + max) / 2 for X and Y, thoguh it's probably fine to use the nuclei instead.
         * 
         * Somehow determine the following values for each district:
         *      how urban/rural it is
         *          elevation will make things more rural, but we really want big and small blobs of "urbanness"
         *      
         *      population density
         *          similar to urbanness, but with huge blurry overspill around the biggest cities
         *      
         *      how coastal/inland it is
         *          simply a matter of distance from the coast
         *      
         *      how wealthy/deprived it is
         *          ...wow this could have a lot of factors. Quite possibly dependent on all the others, tbh.
         *          
         *      distance from "capital" and other significant cities, each as a seperate consideration
         *          Just a big point gradient, really
         *          
         *      distance from adjacent countries
         *          If there are land borders, each country is a separate consideration
         *
         * 
         * Once the districts are all established, pick some distributed ones to be the nuclei for regions,
         * and then grow those regions around them, adding adjacent tiles to each.
         * Where there's multiple adjacent options, go with the adjacent district whose outlook most closely aligns with their own.
         * 
         * Hmm, but we also want the regions to represent equal populations. Quite how we grow them is a bit up in the air.
         */

        public Random Random { get; }
        public int Width { get; }
        public int Height { get; }

        public static bool LineSegmentsCross(PointF line1start, PointF line1end, PointF line2start, PointF line2end)
        {
            PointF CmP = new PointF(line2start.X - line1start.X, line2start.Y - line1start.Y);
            PointF r = new PointF(line1end.X - line1start.X, line1end.Y - line1start.Y);
            PointF s = new PointF(line2end.X - line2start.X, line2end.Y - line2start.Y);

            float CmPxr = CmP.X * r.Y - CmP.Y * r.X;
            float CmPxs = CmP.X * s.Y - CmP.Y * s.X;
            float rxs = r.X * s.Y - r.Y * s.X;

            if (CmPxr == 0f)
            {
                // Lines are collinear, and so intersect if they have any overlap
                return ((line2start.X - line1start.X < 0f) != (line2start.X - line1end.X < 0f))
                    || ((line2start.Y - line1start.Y < 0f) != (line2start.Y - line1end.Y < 0f));
            }

            if (rxs == 0f)
                return false; // Lines are parallel.

            float rxsr = 1f / rxs;
            float t = CmPxs * rxsr;
            float u = CmPxr * rxsr;

            return (t >= 0f) && (t <= 1f) && (u >= 0f) && (u <= 1f);
        }

        public float CenterX { get; }
        public float CenterY { get; }
        public float EllipseWidth { get; }
        public float EllipseHeight { get; }

        private PerlinNoise TerrainNoiseX { get; }
        private PerlinNoise TerrainNoiseY { get; }

        public CountryGenerator(int seed)
        {
            Random = new Random(seed);

            Width = Random.Next(7, 11) * 100;
            Height = Random.Next(7, 11) * 100;

            CenterX = Width / 2f;
            CenterY = Height / 2f;

            EllipseWidth = Width * 0.75f;
            EllipseHeight = Height * 0.75f;

            TerrainNoiseX = new PerlinNoise(Random.Next(), 4);
            TerrainNoiseY = new PerlinNoise(Random.Next(), 4);
        }

        public float GetTerrainNoiseX(float x, float y)
        {
            return TerrainNoiseX.GetValue(x, y);
        }

        public float GetTerrainNoiseY(float x, float y)
        {
            return TerrainNoiseY.GetValue(x, y);
        }
    }
}