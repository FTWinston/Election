using System;
using System.Collections.Generic;
using System.Drawing;

namespace ElectionData.Geography
{
    public class District : Area
    {
        public District(string name, IEnumerable<PointF> bounds)
            : base(bounds)
        {
            Name = name;
        }

        public string Name { get; }

        public IEnumerable<District> GetAdjacentDistricts()
        {
            throw new NotImplementedException();
        }
    }
}
