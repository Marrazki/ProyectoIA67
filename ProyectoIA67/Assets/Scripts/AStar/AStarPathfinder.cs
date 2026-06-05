using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implementa el algoritmo A* con soporte para tres variantes de heurística.
/// </summary>
public class AStarPathfinder : MonoBehaviour
{
    public static AStarPathfinder instance;

    public enum HeuristicType
    {
        Euclidean,  // Straight line
        Manhattan,  // 4 directions only (no diagonals)
        Diagonal    // Chebyshev: diagonal has the same cost as straight
    }

    [Header("Heurística")]
    [Tooltip("Selecciona la variante de heurística que quieres probar")]
    public HeuristicType heuristicType = HeuristicType.Euclidean;

    [Header("Visualización paso a paso")]
    [Tooltip("Activa para ver cómo A* explora celda a celda")]
    public bool stepByStep = false;
    [Range(0f, 1f)]
    [Tooltip("Segundos entre cada paso (0 = un paso por frame)")]
    public float stepDelay = 0.1f;

    // ── Estado de visualización (leído por GridManager en OnDrawGizmos) ────────
    [HideInInspector] public Node            vizCurrentNode;
    [HideInInspector] public HashSet<Node>   vizOpenSet   = new HashSet<Node>();
    [HideInInspector] public HashSet<Node>   vizClosedSet = new HashSet<Node>();

    public void ClearVizState()
    {
        vizCurrentNode = null;
        vizOpenSet.Clear();
        vizClosedSet.Clear();
    }

    void Awake()
    {
        instance = this;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // A* algorithm
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Finds the optimal path between two world positions.
    /// Returns the list of nodes forming the path, or null if none exists.
    /// </summary>
    public List<Node> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        GridManager.instance.ResetNodeCosts();

        Node startNode  = GridManager.instance.NodeFromWorldPoint(startPos);
        Node targetNode = GridManager.instance.NodeFromWorldPoint(targetPos);

        if (!startNode.walkable)
        {
            Debug.LogWarning("A*: El nodo de INICIO no es walkable. Mueve el Seeker a una celda blanca del grid.");
            return null;
        }
        if (!targetNode.walkable)
        {
            Debug.LogWarning("A*: El nodo de DESTINO no es walkable. Mueve el Target a una celda blanca del grid.");
            return null;
        }

        if (startNode == targetNode)
        {
            var path = new List<Node> { startNode };
            GridManager.instance.path = path;
            return path;
        }

        MinHeap<Node> openHeap = new MinHeap<Node>(GridManager.instance.MaxSize);
        HashSet<Node> openSet  = new HashSet<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        startNode.gCost = 0f;
        startNode.hCost = GetHeuristic(startNode, targetNode);
        openHeap.Add(startNode);
        openSet.Add(startNode);

        while (openHeap.Count > 0)
        {
            // Extract the node with the lowest fCost in O(log n)
            Node current = openHeap.RemoveFirst();
            openSet.Remove(current);
            closedSet.Add(current);

            if (current == targetNode)
                return RetracePath(startNode, targetNode);

            foreach (Node neighbour in GridManager.instance.GetNeighbours(current))
            {
                if (!neighbour.walkable || closedSet.Contains(neighbour))
                    continue;

                // Manhattan: no diagonal movement
                //if (heuristicType == HeuristicType.Manhattan)
                //{
                //    int ndx = Mathf.Abs(current.gridX - neighbour.gridX);
                //    int ndy = Mathf.Abs(current.gridY - neighbour.gridY);
                //    if (ndx == 1 && ndy == 1) continue;
                //}

                float newGCost = current.gCost + GetMoveCost(current, neighbour);
                bool inOpenSet = openSet.Contains(neighbour);

                if (newGCost < neighbour.gCost || !inOpenSet)
                {
                    neighbour.gCost  = newGCost;
                    neighbour.hCost  = GetHeuristic(neighbour, targetNode);
                    neighbour.parent = current;

                    if (!inOpenSet)
                    {
                        openHeap.Add(neighbour);
                        openSet.Add(neighbour);
                    }
                    else
                    {
                        // Priority decreased: restore heap order in O(log n)
                        openHeap.UpdateItem(neighbour);
                    }
                }
            }
        }

        Debug.LogWarning("A*: No se encontró camino entre los puntos dados.");
        return null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // A* paso a paso (coroutine) — actualiza el estado de visualización
    // ─────────────────────────────────────────────────────────────────────────

    public IEnumerator FindPathAsync(Vector3 startPos, Vector3 targetPos,
                                     Action<List<Node>> onComplete)
    {
        ClearVizState();
        GridManager.instance.ResetNodeCosts();

        Node startNode  = GridManager.instance.NodeFromWorldPoint(startPos);
        Node targetNode = GridManager.instance.NodeFromWorldPoint(targetPos);

        if (!startNode.walkable)
        {
            Debug.LogWarning("A*: El nodo de INICIO no es walkable.");
            onComplete?.Invoke(null);
            yield break;
        }
        if (!targetNode.walkable)
        {
            Debug.LogWarning("A*: El nodo de DESTINO no es walkable.");
            onComplete?.Invoke(null);
            yield break;
        }

        if (startNode == targetNode)
        {
            var path = new List<Node> { startNode };
            GridManager.instance.path = path;
            onComplete?.Invoke(path);
            yield break;
        }

        MinHeap<Node> openHeap = new MinHeap<Node>(GridManager.instance.MaxSize);
        HashSet<Node> openSet  = new HashSet<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        startNode.gCost = 0f;
        startNode.hCost = GetHeuristic(startNode, targetNode);
        openHeap.Add(startNode);
        openSet.Add(startNode);
        vizOpenSet.Add(startNode);

        while (openHeap.Count > 0)
        {
            Node current = openHeap.RemoveFirst();
            openSet.Remove(current);
            vizOpenSet.Remove(current);
            closedSet.Add(current);
            vizClosedSet.Add(current);
            vizCurrentNode = current;

            // Yield entre pasos para visualizar
            if (stepDelay > 0f) yield return new WaitForSeconds(stepDelay);
            else                yield return null;

            if (current == targetNode)
            {
                vizCurrentNode = null;
                onComplete?.Invoke(RetracePath(startNode, targetNode));
                yield break;
            }

            foreach (Node neighbour in GridManager.instance.GetNeighbours(current))
            {
                if (!neighbour.walkable || closedSet.Contains(neighbour)) continue;

                if (heuristicType == HeuristicType.Manhattan)
                {
                    int ndx = Mathf.Abs(current.gridX - neighbour.gridX);
                    int ndy = Mathf.Abs(current.gridY - neighbour.gridY);
                    if (ndx == 1 && ndy == 1) continue;
                }

                float newGCost = current.gCost + GetMoveCost(current, neighbour);
                bool  inOpenSet = openSet.Contains(neighbour);

                if (newGCost < neighbour.gCost || !inOpenSet)
                {
                    neighbour.gCost  = newGCost;
                    neighbour.hCost  = GetHeuristic(neighbour, targetNode);
                    neighbour.parent = current;

                    if (!inOpenSet)
                    {
                        openHeap.Add(neighbour);
                        openSet.Add(neighbour);
                        vizOpenSet.Add(neighbour);
                    }
                    else
                    {
                        openHeap.UpdateItem(neighbour);
                    }
                }
            }
        }

        vizCurrentNode = null;
        Debug.LogWarning("A*: No se encontró camino entre los puntos dados.");
        onComplete?.Invoke(null);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Movement cost between adjacent nodes
    // Straight = 10  |  Diagonal = 14  (≈ 10 × √2)
    // ─────────────────────────────────────────────────────────────────────────

    float GetMoveCost(Node a, Node b)
    {
        int dx = Mathf.Abs(a.gridX - b.gridX);
        int dy = Mathf.Abs(a.gridY - b.gridY);
        float baseCost = (dx == 1 && dy == 1) ? 14f : 10f;
        // Apply destination node multiplier (e.g. darker tiles are harder to traverse)
        return baseCost * b.costMultiplier;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Heuristic dispatch
    // ─────────────────────────────────────────────────────────────────────────

    float GetHeuristic(Node a, Node b)
    {
        return heuristicType switch
        {
            HeuristicType.Euclidean => HeuristicEuclidean(a, b),
            HeuristicType.Manhattan => HeuristicManhattan(a, b),
            HeuristicType.Diagonal  => HeuristicDiagonal(a, b),
            _                       => 0f
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Heuristics
    // ─────────────────────────────────────────────────────────────────────────

    // Euclidean — straight-line distance (Pythagoras)
    // h = √(dx² + dy²) × 10
    float HeuristicEuclidean(Node a, Node b)
    {
        int dx = Mathf.Abs(a.gridX - b.gridX);
        int dy = Mathf.Abs(a.gridY - b.gridY);
        return Mathf.Sqrt(dx * dx + dy * dy) * 10f;
    }

    // Manhattan — 4-directional movement only
    // h = (|dx| + |dy|) × 10
    float HeuristicManhattan(Node a, Node b)
    {
        int dx = Mathf.Abs(a.gridX - b.gridX);
        int dy = Mathf.Abs(a.gridY - b.gridY);
        return (dx + dy) * 10f;
    }

    // Diagonal (Chebyshev distance) — diagonal has the same cost as straight
    // h = max(|dx|, |dy|) × 10
    float HeuristicDiagonal(Node a, Node b)
    {
        int dx = Mathf.Abs(a.gridX - b.gridX);
        int dy = Mathf.Abs(a.gridY - b.gridY);
        return Mathf.Max(dx, dy) * 10f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Retrace path
    // ─────────────────────────────────────────────────────────────────────────

    List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node current = endNode;

        while (current != startNode)
        {
            path.Add(current);
            current = current.parent;
        }

        if (path.Count == 0 && current == startNode)
        {
            path.Add(startNode);
        }

        path.Reverse();
        GridManager.instance.path = path;
        return path;
    }
}
