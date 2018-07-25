using System;

namespace ElectionData.Politics
{
    public class Issue : IComparable<Issue>
    {
        public Issue(int id)
        {
            ID = id;
        }

        public int ID { get; }
        public string Name { get; }

        public string Description { get; }

        public string MinMeaning { get; }
        public string MidMeaning { get; }
        public string MaxMeaning { get; }

        public int CompareTo(Issue other)
        {
            return ID.CompareTo(other.ID);
        }
    }
}
