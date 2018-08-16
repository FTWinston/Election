using System;

namespace ElectionDataGenerator
{
    public abstract class PositionalEffect : DistrictEffect
    {
        public abstract float GetValue(PointF point);

        public override float AccumulateValue(float prevValue, DistrictGenerator district)
        {
            return prevValue + GetValue(district.GetCenter());
        }
    }
}
