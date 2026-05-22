using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implementa el algoritmo A* con soporte para tres variantes de heurística.
///
/// ┌─────────────────────────────────────────────────────────────────────────┐
/// │                        EJERCICIO PARA EL ALUMNO                         │
/// │                                                                         │
/// │  Implementa los métodos marcados con TODO en este archivo               │
/// │  y en GridManager.cs:                                                   │
/// │                                                                         │
/// │  GridManager.cs                                                         │
/// │    1. CreateGrid()          – construir el array de nodos               │
/// │    2. NodeFromWorldPoint()  – world position → node                     │
/// │    3. GetNeighbours()       – devolver los 8 neighbours de un nodo      │
/// │                                                                         │
/// │  AStarPathfinder.cs                                                     │
/// │    4. GetMoveCost()         – movement cost entre dos nodos             │
/// │    5. RetracePath()         – reconstruir el camino desde el destino    │
/// │    6. HeuristicEuclidean()  – heurística en línea recta                 │
/// │    7. HeuristicManhattan()  – heurística sin diagonales                 │
/// │    8. HeuristicDiagonal()   – heurística Chebyshev (diagonal = recto)   │
/// └─────────────────────────────────────────────────────────────────────────┘
/// </summary>
public class AStarPathfinder : MonoBehaviour
{
    public static AStarPathfinder instance;

    public enum HeuristicType
    {
        Euclidean,
        Manhattan,
        Diagonal
    }

    [Header("Heurística")]
    [Tooltip("Selecciona la variante de heurística que quieres probar")]
    public HeuristicType heuristicType = HeuristicType.Euclidean;

    void Awake()
    {
        instance = this;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ALGORITMO A*  (provisto — no necesitas modificar este método)
    //
    // Lee este código con atención: verás que llama a GetMoveCost(),
    // GetHeuristic() y RetracePath(), que son los métodos que debes implementar.
    // ─────────────────────────────────────────────────────────────────────────

    public List<Node> FindPath(Vector3 startPos, Vector3 targetPos)
    {
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

        List<Node>    openList  = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        startNode.gCost = 0f;
        startNode.hCost = GetHeuristic(startNode, targetNode);
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            // Seleccionar el nodo con menor fCost (en empate, menor hCost)
            Node current = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].fCost < current.fCost ||
                    (Mathf.Approximately(openList[i].fCost, current.fCost) &&
                     openList[i].hCost < current.hCost))
                {
                    current = openList[i];
                }
            }

            openList.Remove(current);
            closedSet.Add(current);

            if (current == targetNode)
                return RetracePath(startNode, targetNode);

            foreach (Node neighbour in GridManager.instance.GetNeighbours(current))
            {
                if (!neighbour.walkable || closedSet.Contains(neighbour))
                    continue;

                // Manhattan: no diagonal movement
                if (heuristicType == HeuristicType.Manhattan)
                {
                    int ndx = Mathf.Abs(current.gridX - neighbour.gridX);
                    int ndy = Mathf.Abs(current.gridY - neighbour.gridY);
                    if (ndx == 1 && ndy == 1) continue;
                }

                float newGCost = current.gCost + GetMoveCost(current, neighbour);

                if (newGCost < neighbour.gCost || !openList.Contains(neighbour))
                {
                    neighbour.gCost  = newGCost;
                    neighbour.hCost  = GetHeuristic(neighbour, targetNode);
                    neighbour.parent = current;

                    if (!openList.Contains(neighbour))
                        openList.Add(neighbour);
                }
            }
        }

        Debug.LogWarning("A*: No se encontró camino entre los puntos dados.");
        return null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Heuristic dispatch — provisto, no modificar
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

    // =========================================================================
    //  EJERCICIO 4 – Movement cost entre nodos adyacentes
    // =========================================================================
    //
    //  El movement cost de moverse a un neighbour depende de la dirección:
    //    - Movimiento RECTO    (solo cambia X o solo cambia Y) → cost 10
    //    - Movimiento DIAGONAL (cambian tanto X como Y)        → cost 14
    //
    //  El 14 aproxima 10×√2 ≈ 14.14 usando solo enteros, lo que mantiene
    //  consistencia con las heurísticas que también usan base 10.
    //
    //  Pista: si Mathf.Abs(a.gridX - b.gridX) == 1  Y
    //            Mathf.Abs(a.gridY - b.gridY) == 1  → es diagonal
    //
    // =========================================================================
    float GetMoveCost(Node a, Node b)
    {
        int dx = Mathf.Abs(a.gridX - b.gridX);
        int dy = Mathf.Abs(a.gridY - b.gridY);

        if (dx == 1 && dy == 1)
            return 14;

        return 10;
    }

    // =========================================================================
    //  EJERCICIO 5 – Reconstruir el camino (retrace path)
    // =========================================================================
    //
    //  Cuando A* llega al target node, necesitamos saber qué camino tomó.
    //  Cada nodo guarda una reference a su nodo anterior en node.parent.
    //  Siguiendo estas referencias desde el destino hasta el inicio obtenemos
    //  el camino en orden inverso.
    //
    //  Pasos:
    //    1. Crea una List<Node> vacía llamada "path".
    //    2. Empieza desde endNode y recorre las referencias parent hasta llegar
    //       a startNode (el startNode NO se incluye en la lista).
    //    3. En cada paso: añade el nodo actual a la lista y avanza al parent.
    //    4. Invierte la lista (path.Reverse()) para que vaya de inicio a fin.
    //    5. Asigna el camino a GridManager.instance.path para que lo visualice.
    //    6. Devuelve la lista.
    //
    // =========================================================================
    List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node current = endNode;

        while (current != startNode)
        {
            path.Add(current);
            current = current.parent;
        }

        path.Reverse();

        GridManager.instance.path = path;

        return path;
    }

    // =========================================================================
    //  EJERCICIO 6 – Heurística EUCLIDEA
    // =========================================================================
    //
    //  Distancia en línea recta entre dos nodos (Pitágoras).
    //  Fórmula:  h = √( dx² + dy² ) × 10
    //
    //  donde dx = |a.gridX - b.gridX|  y  dy = |a.gridY - b.gridY|
    //
    // =========================================================================
    float HeuristicEuclidean(Node a, Node b)
    {
        float dx = Mathf.Abs(a.gridX - b.gridX);
        float dy = Mathf.Abs(a.gridY - b.gridY);

        return Mathf.Sqrt(dx * dx + dy * dy) * 10f;
    }

    // =========================================================================
    //  EJERCICIO 7 – Heurística MANHATTAN
    // =========================================================================
    //
    //  Solo cuenta movimientos en 4 direcciones (sin diagonales).
    //  Fórmula:  h = ( |dx| + |dy| ) × 10
    //
    // =========================================================================
    float HeuristicManhattan(Node a, Node b)
    {
        float dx = Mathf.Abs(a.gridX - b.gridX);
        float dy = Mathf.Abs(a.gridY - b.gridY);

        return (dx + dy) * 10f;
    }

    // =========================================================================
    //  EJERCICIO 8 – Heurística DIAGONAL (Chebyshev distance)
    // =========================================================================
    //
    //  El movimiento en diagonal tiene el MISMO coste que en X o Y.
    //  En un solo paso diagonal avanzas tanto en X como en Y,
    //  por lo que el número de pasos es el máximo de los dos ejes.
    //
    //  Fórmula:  h = max( |dx|, |dy| ) × 10
    //
    //  Funciones útiles: Mathf.Max
    //
    // =========================================================================
    float HeuristicDiagonal(Node a, Node b)
    {
        float dx = Mathf.Abs(a.gridX - b.gridX);
        float dy = Mathf.Abs(a.gridY - b.gridY);

        return Mathf.Max(dx, dy) * 10f;
    }
}