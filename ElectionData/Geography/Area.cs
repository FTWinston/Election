using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ElectionData.Geography
{
    public class Area
    {
        public Area(IEnumerable<PointF> bounds)
        {
            Bounds = bounds.ToArray();
        }

        PointF[] Bounds { get; }
    }
}
