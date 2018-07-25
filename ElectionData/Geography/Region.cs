using System.Collections.Generic;

namespace ElectionData.Geography
{
    public class Region : Area
    {
        public Region(string name, IEnumerable<Point> bounds)
            : base(bounds)
        {
            Name = name;
        }

        public string Name { get; }

        List<District> Districts { get; } = new List<District>();
    }
}
