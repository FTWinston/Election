using System.Collections;
using System.Collections.Generic;

namespace ElectionDataGenerator
{
    public class IndexOfList : ICollection<PointF>, IEnumerable<PointF>, IEnumerable, IList<PointF>, IReadOnlyCollection<PointF>, IReadOnlyList<PointF>
    {
        private List<PointF> underlying;

        public IndexOfList(IEnumerable<PointF> values)
        {
            underlying = new List<PointF>(values);
        }

        public int Count => underlying.Count;

        public bool IsReadOnly => false;

        public PointF this[int index] { get => underlying[index]; set => underlying[index] = value; }

        static int GetIndex(IndexOfList list, PointF value, int startIndex = 0)
        {
            for (int index = startIndex; index < list.Count; index++)
                if (list[index] == value)
                    return index;

            return -1;
        }

        public int IndexOf(PointF item)
        {
            return GetIndex(this, item);
        }

        public int IndexOf(PointF item, int startIndex)
        {
            return GetIndex(this, item, startIndex);
        }

        public int LastIndexOf(PointF item, int startIndex)
        {
            return underlying.LastIndexOf(item, startIndex);
        }

        public void Insert(int index, PointF item)
        {
            underlying.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            underlying.RemoveAt(index);
        }

        public void Add(PointF item)
        {
            underlying.Add(item);
        }

        public void Clear()
        {
            underlying.Clear();
        }

        public bool Contains(PointF item)
        {
            return underlying.Contains(item);
        }

        public void CopyTo(PointF[] array, int arrayIndex) => underlying.CopyTo(array, arrayIndex);

        public bool Remove(PointF item)
        {
            return underlying.Remove(item);
        }

        public IEnumerator<PointF> GetEnumerator() => underlying.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => underlying.GetEnumerator();

        internal void RemoveRange(int index, int count)
        {
            underlying.RemoveRange(index, count);
        }

        internal void AddRange(IEnumerable<PointF> items)
        {
            underlying.AddRange(items);
        }

        internal void InsertRange(int index, IEnumerable<PointF> items)
        {
            underlying.InsertRange(index, items);
        }
    }
}
