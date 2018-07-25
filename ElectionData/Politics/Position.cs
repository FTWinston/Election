using System;

namespace ElectionData.Politics
{
    public class Position
    {
        public Position(Issue issue, float value)
        {
            Issue = issue;
            Value = value;
        }

        public Issue Issue { get; }
        public float Value { get; set; }

        public const float MinValue = 0f;
        public const float MaxValue = 1f;

        public float SimilarityWith(float otherValue)
        {
            return 1f - Math.Abs(Value - otherValue);
        }
    }
}
