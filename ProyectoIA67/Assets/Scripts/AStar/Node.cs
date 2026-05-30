using UnityEngine;

/// <summary>
/// Representa un nodo (celda) del grid usado por el algoritmo A*.
/// </summary>
public class Node : IHeapItem<Node>
{
    // ── Grid data ─────────────────────────────────────────────────────────────
    public bool walkable;          // Is this node walkable?
    public Vector3 worldPosition;  // Position in world space
    public int gridX;              // Column in the grid
    public int gridY;              // Row in the grid

    // ── A* costs ──────────────────────────────────────────────────────────────
    public float gCost;  // Accumulated cost from the start node (g)
    public float hCost;  // Heuristic estimate to the target node (h)
    public float fCost => gCost + hCost;  // Total cost, used for prioritization (f = g + h)

    // Cost multiplier for this node (e.g. dark zones increase navigation cost)
    public float costMultiplier = 1f;

    public Node parent;  // Previous node in the optimal path (used to retrace it)

    // ── IHeapItem ─────────────────────────────────────────────────────────────
    public int HeapIndex { get; set; }

    public int CompareTo(Node other)
    {
        int cmp = fCost.CompareTo(other.fCost);
        if (cmp == 0) cmp = hCost.CompareTo(other.hCost);
        return cmp;
    }

    public Node(bool walkable, Vector3 worldPosition, int gridX, int gridY, float costMultiplier = 1f)
    {
        this.walkable       = walkable;
        this.worldPosition  = worldPosition;
        this.gridX          = gridX;
        this.gridY          = gridY;
        this.costMultiplier = costMultiplier;
    }
}
