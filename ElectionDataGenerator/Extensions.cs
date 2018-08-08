using System;

namespace ElectionDataGenerator
{
    static class Extensions
    {
        public static float NextFloat(this Random random)
        {
            return (float)random.NextDouble();
        }
    }
}