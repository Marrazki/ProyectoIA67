/// <summary>
/// Interface required by MinHeap items.
/// Implementors must expose their heap index and define priority order via CompareTo.
/// </summary>
public interface IHeapItem<T> : System.IComparable<T>
{
    int HeapIndex { get; set; }
}

/// <summary>
/// Generic binary min-heap with O(log n) insertion, extraction, and priority update.
/// </summary>
public class MinHeap<T> where T : IHeapItem<T>
{
    T[] _items;
    int _count;

    public MinHeap(int maxSize) { _items = new T[maxSize]; }
    public int Count => _count;

    public void Add(T item)
    {
        item.HeapIndex = _count;
        _items[_count++] = item;
        HeapifyUp(item);
    }

    public T RemoveFirst()
    {
        T first = _items[0];
        _count--;
        if (_count > 0)
        {
            _items[0] = _items[_count];
            _items[0].HeapIndex = 0;
            HeapifyDown(_items[0]);
        }
        return first;
    }

    /// <summary>
    /// Call after an item's priority decreases (lower fCost).
    /// Restores heap order in O(log n).
    /// </summary>
    public void UpdateItem(T item) => HeapifyUp(item);

    // ── Internal helpers ──────────────────────────────────────────────────────

    void HeapifyUp(T item)
    {
        int i = item.HeapIndex;
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (item.CompareTo(_items[parent]) < 0)
            {
                Swap(item, _items[parent]);
                i = item.HeapIndex;
            }
            else break;
        }
    }

    void HeapifyDown(T item)
    {
        while (true)
        {
            int left = 2 * item.HeapIndex + 1;
            int right = 2 * item.HeapIndex + 2;
            int best = item.HeapIndex;

            if (left  < _count && _items[left].CompareTo(_items[best])  < 0) best = left;
            if (right < _count && _items[right].CompareTo(_items[best]) < 0) best = right;

            if (best == item.HeapIndex) break;
            Swap(item, _items[best]);
        }
    }

    void Swap(T a, T b)
    {
        _items[a.HeapIndex] = b;
        _items[b.HeapIndex] = a;
        (a.HeapIndex, b.HeapIndex) = (b.HeapIndex, a.HeapIndex);
    }
}
