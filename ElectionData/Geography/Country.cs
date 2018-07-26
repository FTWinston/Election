using System.Collections.Generic;
using System.Drawing;

namespace ElectionData.Geography
{
    public class Country
    {
        List<List<PointF>> Landmasses { get; } = new List<List<PointF>>();
        List<Region> Regions { get; } = new List<Region>();

        public float MinX { get; } = 0;
        public float MaxX { get; } = 1;
        public float MinY { get; } = 0;
        public float MaxY { get; } = 1;
    }
}
