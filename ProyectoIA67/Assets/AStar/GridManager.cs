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

        // El diámetro es el doble del radio: cada celda ocupa nodeDiameter × nodeDiameter
        _nodeDiameter = nodeRadius * 2f;

        // Calculamos cuántas celdas caben en cada eje
        _gridSizeX = Mathf.RoundToInt(gridWorldSize.x / _nodeDiameter);
        _gridSizeY = Mathf.RoundToInt(gridWorldSize.y / _nodeDiameter);

        CreateGrid();
    }

    /// <summary>Número total de nodos (útil para estructuras de datos auxiliares).</summary>
    public int MaxSize => _gridSizeX * _gridSizeY;

    // =========================================================================
    //  EJERCICIO 1 – Construir el grid
    // =========================================================================
    //
    //  Debes rellenar el array _grid con objetos Node, uno por cada celda.
    //
    //  Pasos:
    //    1. Calcula el bottom-left corner del grid en world space.
    //       El grid está centrado en transform.position, así que la esquina es:
    //
    //         worldBottomLeft = transform.position
    //                         - Vector3.right   * gridWorldSize.x * 0.5f
    //                         - Vector3.forward * gridWorldSize.y * 0.5f
    //
    //    2. Itera con dos bucles (x de 0 a _gridSizeX, y de 0 a _gridSizeY).
    //
    //    3. Para cada celda (x, y), calcula su world position:
    //
    //         worldPoint = worldBottomLeft
    //                    + Vector3.right   * (x * _nodeDiameter + nodeRadius)
    //                    + Vector3.forward * (y * _nodeDiameter + nodeRadius)
    //
    //       (El "+ nodeRadius" centra el punto dentro de la celda)
    //
    //    4. Comprueba si hay un obstáculo en esa posición usando:
    //
    //         Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask)
    //
    //       → Devuelve true si hay colisión. El nodo es WALKABLE si NO hay colisión.
    //
    //    5. Crea el nodo: new Node(walkable, worldPoint, x, y)
    //       y guárdalo en _grid[x, y].
    //
    // =========================================================================
    void CreateGrid()
    {
        _grid = new Node[_gridSizeX, _gridSizeY];

        Vector3 worldBottomLeft = transform.position
            - Vector3.right * gridWorldSize.x / 2
            - Vector3.forward * gridWorldSize.y / 2;

        for (int x = 0; x < _gridSizeX; x++)
        {
            for (int y = 0; y < _gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft
                    + Vector3.right * (x * _nodeDiameter + nodeRadius)
                    + Vector3.forward * (y * _nodeDiameter + nodeRadius);

                bool walkable = !Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask);

                _grid[x, y] = new Node(walkable, worldPoint, x, y);
            }
        }
    }

    // =========================================================================
    //  EJERCICIO 2 – Convertir world position → nodo del grid
    // =========================================================================
    //
    //  Dado un punto en world space, encuentra la celda del grid
    //  más cercana a ese punto.
    //
    //  Pasos:
    //    1. Normaliza la world position a un porcentaje [0, 1] dentro del grid:
    //
    //         percentX = (worldPosition.x - transform.position.x + gridWorldSize.x * 0.5f) / gridWorldSize.x
    //         percentY = (worldPosition.z - transform.position.z + gridWorldSize.y * 0.5f) / gridWorldSize.y
    //
    //       (Usamos .z porque en Unity el plano horizontal es X-Z)
    //
    //    2. Asegúrate de que los porcentajes están entre 0 y 1:
    //         Mathf.Clamp01(percent)
    //
    //    3. Convierte el porcentaje a índice de celda:
    //         x = Mathf.RoundToInt((_gridSizeX - 1) * percentX)
    //         y = Mathf.RoundToInt((_gridSizeY - 1) * percentY)
    //
    //    4. Devuelve _grid[x, y]
    //
    // =========================================================================
    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = (worldPosition.x - transform.position.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (worldPosition.z - transform.position.z + gridWorldSize.y / 2) / gridWorldSize.y;

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((_gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((_gridSizeY - 1) * percentY);

        return _grid[x, y];
    }

    // =========================================================================
    //  EJERCICIO 3 – Obtener los neighbours de un nodo
    // =========================================================================
    //
    //  El algoritmo A* necesita explorar los nodos adyacentes al nodo actual.
    //  En nuestro grid permitimos 8 direcciones (horizontal, vertical y diagonal).
    //
    //  Pasos:
    //    1. Crea una List<Node> vacía para acumular los neighbours.
    //
    //    2. Itera con dos bucles:  dx de -1 a 1  y  dy de -1 a 1
    //       (esto cubre las 8 celdas alrededor, más el propio nodo)
    //
    //    3. Salta el caso dx == 0 && dy == 0 (sería el propio nodo).
    //
    //    4. Calcula las coordenadas del neighbour:
    //         checkX = node.gridX + dx
    //         checkY = node.gridY + dy
    //
    //    5. Comprueba que el neighbour está dentro de los límites del grid:
    //         checkX >= 0 && checkX < _gridSizeX
    //         checkY >= 0 && checkY < _gridSizeY
    //
    //    6. Si es válido, añádelo a la lista.
    //
    //    7. Devuelve la lista de neighbours.
    //
    // =========================================================================
    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

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
    // Visualización en el editor (Gizmos) — provisto, no modificar
    // ─────────────────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        // Contorno del grid
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position,
            new Vector3(gridWorldSize.x, 1f, gridWorldSize.y));

        if (_grid == null) return;

        foreach (Node node in _grid)
        {
            if (path != null && path.Contains(node))
                Gizmos.color = Color.blue;          // Path found
            else
                Gizmos.color = node.walkable
                    ? new Color(1f, 1f, 1f, 0.3f)  // Walkable (semi-transparent)
                    : new Color(1f, 0f, 0f, 0.6f);  // Unwalkable (red)

            Gizmos.DrawCube(node.worldPosition,
                Vector3.one * (_nodeDiameter - 0.1f));
        }
    }
}
