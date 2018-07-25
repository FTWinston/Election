using System.Collections.Generic;
using System.Linq;

namespace ElectionData.Geography
{
    public class Area
    {
        public Area(IEnumerable<Point> bounds)
        {
            Bounds = bounds.ToArray();
        }

        Point[] Bounds { get; }
    }
}
