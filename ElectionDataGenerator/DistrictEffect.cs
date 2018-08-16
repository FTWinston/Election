using System;

namespace ElectionDataGenerator
{
    public abstract class DistrictEffect
    {
        public abstract float AccumulateValue(float prevValue, DistrictGenerator district);
    }
}
