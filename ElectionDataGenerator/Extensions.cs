using System;
using System.Collections.Generic;

namespace ElectionDataGenerator
{
    static class Extensions
    {
        public static float NextFloat(this Random random)
        {
            return (float)random.NextDouble();
        }

        public static float AccumulateValues(this IList<DistrictEffect> effects, DistrictGenerator district)
        {
            var value = 0f;
            foreach (var effect in effects)
                value = effect.AccumulateValue(value, district);
            return value;
        }
    }
}