using System.Collections.Generic;
using System.Drawing;

namespace ElectionData.Geography
{
    public class Region : Area
    {
        public Region(string name, IEnumerable<PointF> bounds)
            : base(bounds)
        {
            Name = name;
        }

        public string Name { get; }

        List<District> Districts { get; } = new List<District>();
    }
}
