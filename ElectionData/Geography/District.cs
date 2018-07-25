using System;
using System.Collections.Generic;

namespace ElectionData.Geography
{
    public class District : Area
    {
        public District(string name, IEnumerable<Point> bounds)
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
