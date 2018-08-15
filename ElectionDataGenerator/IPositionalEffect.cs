using System;

namespace ElectionDataGenerator
{
    public interface IPositionalEffect
    {
        float GetValue(PointF point);
    }
}
