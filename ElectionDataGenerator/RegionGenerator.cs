using System;
using System.Collections.Generic;

namespace ElectionDataGenerator
{
    public class RegionGenerator
    {
        private List<DistrictGenerator> districts { get; } = new List<DistrictGenerator>();
        public IReadOnlyList<DistrictGenerator> Districts => districts;
        public float Area { get; private set; } = 0;

        public void AddDistrict(DistrictGenerator district)
        {
            districts.Add(district);
            district.Region = this;
            Area += district.Area;
        }

        public void RemoveDistrict(DistrictGenerator district)
        {
            districts.Remove(district);
            district.Region = null;
            Area -= district.Area;
        }
    }
}