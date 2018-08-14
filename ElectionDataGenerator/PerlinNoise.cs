using System;

namespace ElectionDataGenerator
{
    public class PerlinNoise
    {
        public int Octaves { get; }
        public float Frequency { get; }
        public float Amplitude { get; }
        public float Persistence { get; }

        private readonly int[] permutation;
        private float NormalizedMaxValue { get; }

        public PerlinNoise(int seed, float frequency, float amplitude = 1, int numOctaves = 5, float persistence = 0.5f)
        {
            Frequency = frequency;
            Amplitude = amplitude;
            Octaves = numOctaves;
            Persistence = persistence;
            NormalizedMaxValue = DetermineNormalizedMaxValue(numOctaves, persistence);

            permutation = new int[257];

            for (var i = 0; i < 256; i++)
                permutation[i] = i;

            Shuffle(permutation, seed, 256);
            permutation[256] = permutation[0]; // wrap the first/last value
        }

        private static float DetermineNormalizedMaxValue(int numOctaves, float persistence)
        {
            // determine the maximum value for this number of octaves, so output can be scaled appropriately
            float amplitude = 1;
            float maxValue = 0;

            for (int i = 0; i < numOctaves; i++)
            {
                maxValue += amplitude;
                amplitude *= persistence;
            }

            return maxValue;
        }

        private void Shuffle<T>(T[] array, int seed, int numValues = 0)
        {
            var random = new Random(seed);

            if (numValues == 0)
                numValues = array.Length;

            for (var i = numValues - 1; i > 0; i -= 1)
            {
                int j = random.Next(i + 1);
                T temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
        }

        private float GetSingleValue(float x, float y)
        {
            int X = (int)Math.Floor(x) & 0xff;
            int Y = (int)Math.Floor(y) & 0xff;
            x -= (float)Math.Floor(x);
            y -= (float)Math.Floor(y);
            var u = Fade(x);
            var v = Fade(y);
            var A = (permutation[X] + Y) & 0xff;
            var B = (permutation[X + 1] + Y) & 0xff;
            return Lerp(v, Lerp(u, Grad(permutation[A], x, y), Grad(permutation[B], x - 1, y)),
                           Lerp(u, Grad(permutation[A + 1], x, y - 1), Grad(permutation[B + 1], x - 1, y - 1)));
        }

        public float GetValue(float x, float y)
        {
            float total = 0;
            float frequency = Frequency;
            float amplitude = Amplitude;

            for (int i = 0; i < Octaves; i++)
            {
                total += GetSingleValue(x * frequency, y * frequency) * amplitude;

                amplitude *= Persistence;
                frequency *= 2;
            }

            // normalize the result into the range 0 - Amplitude
            return total / NormalizedMaxValue; 
        }

        private static float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private static float Lerp(float t, float a, float b)
        {
            return a + t * (b - a);
        }

        private static float Grad(int hash, float x, float y)
        {
            return ((hash & 1) == 0 ? x : -x) + ((hash & 2) == 0 ? y : -y);
        }
    }
}
