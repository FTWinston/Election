using System.Collections.Generic;

namespace ElectionData.Geography
{
    public class Country
    {
        List<Area> Regions { get; } = new List<Area>();

        public float MinX { get; } = 0;
        public float MaxX { get; } = 1;
        public float MinY { get; } = 0;
        public float MaxY { get; } = 1;
    }
}
