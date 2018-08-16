using System;

namespace ElectionDataGenerator
{
    public class CustomDistrictEffect : DistrictEffect
    {
        public CustomDistrictEffect(Func<float, DistrictGenerator, float> effect)
        {
            Effect = effect;
        }

        public override float AccumulateValue(float prevValue, DistrictGenerator district) => Effect(prevValue, district);

        Func<float, DistrictGenerator, float> Effect { get; }
    }
}
