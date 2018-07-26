using ElectionData.Geography;
using System;

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

        public bool IsPointWithinEllipse(int x, int y)
        {
            return
            (x - CenterX) * (x - CenterX) / (EllipseWidth * EllipseWidth * 0.25f) +
            (y - CenterY) * (y - CenterY) / (EllipseHeight * EllipseHeight * 0.25f)
            <= 1f;
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