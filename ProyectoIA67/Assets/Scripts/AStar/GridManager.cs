using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Crea y gestiona el grid 3D que usa el algoritmo A*.
/// Adjunta este script a un GameObject vacío en la escena.
///
/// Configuración básica en el Inspector:
///   - Grid World Size : tamaño del grid en world units (ej. 20 x 20)
///   - Node Radius     : radio de cada celda (ej. 0.5 → celdas de 1x1)
///   - Unwalkable Mask : layer asignado a los obstáculos (cubos, paredes, etc.)
/// </summary>
[ExecuteAlways]
public class GridManager : MonoBehaviour
{
    public static GridManager instance;

    [Header("Tamaño del grid")]
    public Vector2 gridWorldSize = new Vector2(20f, 20f);
    public float nodeRadius = 0.5f;

    [Header("Obstáculos")]
    public LayerMask unwalkableMask;

    // ── Estado interno ────────────────────────────────────────────────────────
    Node[,] _grid;
    float _nodeDiameter;
    int _gridSizeX, _gridSizeY;

    /// <summary>Camino actual (lo asigna AStarPathfinder para mostrarlo en Gizmos).</summary>
    [HideInInspector] public List<Node> path;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        instance = this;

        _nodeDiameter = nodeRadius * 2f;
        _gridSizeX    = Mathf.RoundToInt(gridWorldSize.x / _nodeDiameter);
        _gridSizeY    = Mathf.RoundToInt(gridWorldSize.y / _nodeDiameter);

        CreateGrid();
    }

    /// <summary>Número total de nodos.</summary>
    public int MaxSize => _gridSizeX * _gridSizeY;

    /// <summary>Resetea gCost, hCost y parent de todos los nodos antes de cada búsqueda.</summary>
    public void ResetNodeCosts()
    {
        if (_grid == null) return;
        foreach (Node node in _grid)
        {
            node.gCost  = 0f;
            node.hCost  = 0f;
            node.parent = null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Grid construction
    // ─────────────────────────────────────────────────────────────────────────

    void CreateGrid()
    {
        _grid = new Node[_gridSizeX, _gridSizeY];

        // Bottom-left corner of the grid in world space
        Vector3 worldBottomLeft = transform.position
            - Vector3.right   * gridWorldSize.x * 0.5f
            - Vector3.forward * gridWorldSize.y * 0.5f;

        for (int x = 0; x < _gridSizeX; x++)
        {
            for (int y = 0; y < _gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft
                    + Vector3.right   * (x * _nodeDiameter + nodeRadius)
                    + Vector3.forward * (y * _nodeDiameter + nodeRadius);

                // A node is walkable if there is NO obstacle within its radius
                bool walkable = !Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask);
                _grid[x, y]   = new Node(walkable, worldPoint, x, y);
            }
        }
    }

    /// <summary>
    /// Applies a cost multiplier to all nodes whose world position lies inside the given bounds.
    /// Useful to mark "dark" areas that increase navigation cost for enemies.
    /// </summary>
    public void ApplyCostMultiplierToArea(Bounds area, float multiplier)
    {
        if (_grid == null) return;
        foreach (Node node in _grid)
        {
            if (area.Contains(node.worldPosition))
                node.costMultiplier = multiplier;
        }
    }

    /// <summary>
    /// Resets the cost multiplier to 1 for nodes inside the given bounds.
    /// </summary>
    public void ResetCostMultiplierInArea(Bounds area)
    {
        if (_grid == null) return;
        foreach (Node node in _grid)
        {
            if (area.Contains(node.worldPosition))
                node.costMultiplier = 1f;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public utilities
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns the node closest to the given world position.</summary>
    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = ((worldPosition.x - transform.position.x) + gridWorldSize.x * 0.5f) / gridWorldSize.x;
        float percentY = ((worldPosition.z - transform.position.z) + gridWorldSize.y * 0.5f) / gridWorldSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((_gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((_gridSizeY - 1) * percentY);
        return _grid[x, y];
    }

    /// <summary>Returns the 8 neighbours (cardinal + diagonal) of a node.</summary>
    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue; // skip self

                int checkX = node.gridX + dx;
                int checkY = node.gridY + dy;

                if (checkX >= 0 && checkX < _gridSizeX &&
                    checkY >= 0 && checkY < _gridSizeY)
                {
                    neighbours.Add(_grid[checkX, checkY]);
                }
            }
        }

        return neighbours;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Editor visualization (Gizmos)
    // ─────────────────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position,
            new Vector3(gridWorldSize.x, 1f, gridWorldSize.y));

        if (_grid == null) return;

        var pf      = AStarPathfinder.instance;
        var pathSet = (path != null) ? new HashSet<Node>(path) : null;

        foreach (Node node in _grid)
        {
            // Prioridad: current > path > open > closed > normal
            if (pf != null && node == pf.vizCurrentNode)
                Gizmos.color = Color.yellow;
            else if (pathSet != null && pathSet.Contains(node))
                Gizmos.color = Color.blue;
            else if (pf != null && pf.vizOpenSet.Contains(node))
                Gizmos.color = new Color(0f, 1f, 0f, 0.7f);            // Verde
            else if (pf != null && pf.vizClosedSet.Contains(node))
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);          // Naranja
            else
                Gizmos.color = node.walkable
                    ? new Color(1f, 1f, 1f, 0.3f)
                    : new Color(1f, 0f, 0f, 0.6f);

            Gizmos.DrawCube(node.worldPosition, Vector3.one * (_nodeDiameter - 0.1f));
        }

#if UNITY_EDITOR
        if (pf == null) return;

        var labelStyle = new GUIStyle();
        labelStyle.fontSize  = 9;
        labelStyle.alignment = TextAnchor.MiddleCenter;

        foreach (Node node in _grid)
        {
            bool show = pf.vizOpenSet.Contains(node)
                     || pf.vizClosedSet.Contains(node)
                     || node == pf.vizCurrentNode;
            if (!show) continue;

            labelStyle.normal.textColor = (node == pf.vizCurrentNode)
                ? Color.black : Color.white;

            string label = $"f:{node.fCost:0}\ng:{node.gCost:0} h:{node.hCost:0}";
            UnityEditor.Handles.Label(
                node.worldPosition + Vector3.up * 0.65f, label, labelStyle);
        }
#endif
    }
}
