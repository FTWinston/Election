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
        public float PopulationDensity { get; set; }
        public int Population { get; set; }

        // The following are demographic properties, each with a range of 0 - 1
        public float Urbanisation { get; set; }
        public float Coastalness { get; set; }
        public float Wealth { get; set; }
        public float Age { get; set; }
        public float Education { get; set; }
        public float Health { get; set; }
        public float GeographicDivide { get; set; }

        public void MergeWithDistrict(DistrictGenerator other, AdjacencyInfo adjacency)
        {
            MergeWith(other, adjacency);

            foreach (var yetAnother in other.AdjacentDistricts)
            {
                yetAnother.AdjacentDistricts.Remove(other);

                if (yetAnother != this && !yetAnother.AdjacentDistricts.Contains(this))
                {
                    yetAnother.AdjacentDistricts.Add(this);
                }
            }

            var toAdd = other.AdjacentDistricts
                .Where(d => d != this && !AdjacentDistricts.Contains(d));

            foreach (var district in toAdd)
                AdjacentDistricts.Add(district);
        }
    }
}