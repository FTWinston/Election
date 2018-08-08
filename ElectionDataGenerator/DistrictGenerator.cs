using System;
using System.Collections.Generic;
using System.Linq;

namespace ElectionDataGenerator
{
    public class DistrictGenerator : PolygonF
    {
        public DistrictGenerator(params PointF[] points)
            : base(points)
        {

        }

        public RegionGenerator Region { get; set; }

        public HashSet<DistrictGenerator> AdjacentDistricts { get; } = new HashSet<DistrictGenerator>();

        public void MergeWithDistrict(DistrictGenerator other, AdjacencyInfo adjacency)
        {
            MergeWith(other, adjacency);

            AdjacentDistricts.Remove(other);

            var toAdd = other.AdjacentDistricts
                .Where(d => d != this && !AdjacentDistricts.Contains(d));

            foreach (var district in toAdd)
                AdjacentDistricts.Add(district);
        }
    }
}