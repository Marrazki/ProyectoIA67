// ============================================================
//  EJERCICIO: BEHAVIOR TREE — Enemigo con IA por árbol
// ============================================================
//
//  NODOS DISPONIBLES en Core/:
//    · BTNode       → clase base abstracta (método Tick())
//    · NodeStatus   → enum: Running | Success | Failure
//    · Condition    → hoja: evalúa una condición bool
//    · BTAction     → hoja: ejecuta una acción
//    · Sequence     → compuesto AND: todos los hijos deben tener éxito
//    · Selector     → compuesto OR: devuelve el primer hijo que no falle
//
//  COMPORTAMIENTO ESPERADO (igual que la FSM de referencia):
//
//    Selector  ────────────────────── evalúa en orden (prioridad ↓)
//    ├── Sequence  "si vida baja → huir"
//    │   ├── Condition: LowHealth
//    │   └── BTAction:  Flee
//    ├── Sequence  "si veo al jugador → perseguir"
//    │   ├── Condition: CanSeePlayer
//    │   └── BTAction:  Chase
//    ├── Sequence  "si tengo pista → investigar"
//    │   ├── Condition: HasLastKnownPosition
//    │   └── BTAction:  Investigate
//    └── BTAction: Patrol   (fallback: siempre devuelve Running)
//
// ============================================================
//  PARTES DEL EJERCICIO
// ============================================================
//
//  [PARTE 1 — OBLIGATORIO]
//    Implementa BuildTree() conectando las condiciones y acciones
//    ya escritas con los nodos Selector y Sequence.
//    Las acciones y condiciones están escritas; sólo debes ensamblarlas.
//
//  [PARTE 2 — AMPLIACIÓN]
//    Añade el comportamiento ATACAR cuando el jugador está muy cerca.
//    Busca los TODO marcados con [PARTE 2] en este fichero.
//    Pasos:
//      a) Implementa la condición EstaCerca().
//      b) Implementa la acción Attack().
//      c) Añade la rama al árbol con la prioridad correcta.
//    Pregunta: ¿entre qué dos ramas existentes debería ir esta nueva rama?
//
//  [PARTE 3 — BONUS]
//    Abre Core/Inverter.cs e implementa el nodo decorador Inverter:
//    invierte el resultado de su hijo (Success↔Failure, Running sin cambios).
//    Luego úsalo en BuildTree() para expresar alguna condición negada.
//
// ============================================================

using UnityEngine;

public class Enemigo_BT : MonoBehaviour
{
    [Header("Referencias")]
    public Transform jugador;

    [Header("Salud")]
    public float vida = 100f;
    public float vidaMaxima = 100f;

    [Header("Detección")]
    public float rangoDeteccion = 5f;

    [Header("Patrulla")]
    public Transform[] waypoints;
    public float velocidadPatrulla = 2f;
    public float distanciaWaypoint = 0.5f;

    [Header("Persecución")]
    public float velocidadPersecucion = 4f;

    [Header("Investigación")]
    public float velocidadInvestigacion = 3f;
    public float distanciaInvestigacion = 0.5f;

    [Header("Huida")]
    public float velocidadHuida = 7f;

    // TODO [PARTE 2]: Descomenta y ajusta el valor si quieres
    [Header("Ataque")]
    public float rangoAtaque = 1.5f;

    // Blackboard: datos compartidos entre condición e investigación.
    Vector3 _lastKnownPos;
    bool _hasLastKnownPos;
    int _waypointIndex;

    BTNode _tree;
    public BTNode Tree => _tree;

    // ── Construcción del árbol ────────────────────────────────────────────

    void Start() => BuildTree();

    void BuildTree()
    {
        // TODO [PARTE 1]: Construye aquí el árbol de decisión.
        //
        // Recuerda:
        //   · Selector evalúa hijos en orden y devuelve el primero que NO falle.
        //     → Úsalo para expresar prioridades (el más importante va primero).
        //   · Sequence evalúa hijos en orden y falla si alguno falla.
        //     → Úsalo para expresar "si <condición>, entonces <acción>".
        //   · Condition(func)  toma un método que devuelva bool.
        //   · BTAction(func)   toma un método que devuelva NodeStatus.
        //
        // Ejemplo de una sola rama "si vida baja → huir":
        //
        //   new Sequence(
        //       new Condition(LowHealth),
        //       new BTAction(Flee)
        //   )
        //
        // ¿Cómo encajas todas las ramas en un único Selector raíz?

        _tree = new Selector(
            new Sequence(
                new Condition(LowHealth, "VidaBaja?"),
                new BTAction(Flee, "Huir")
            )
            { Name = "Huir si vida baja" },
            new Sequence(
                new Condition(EstaCerca, "EstaCerca?"),
                new BTAction(Attack, "Atacar")
            )
            { Name = "Atacar si cerca" },
            new Sequence(
                new Condition(CanSeePlayer, "VeoJugador?"),
                new BTAction(Chase, "Perseguir")
            )
            { Name = "Perseguir si veo" },
            new Sequence(
                new Condition(HasLastKnownPosition, "TengoPista?"),
                new BTAction(Investigate, "Investigar")
            )
            { Name = "Investigar pista" },
            new Sequence(
                new BTAction(Patrol, "Patrullar")
            )
            { Name = "Patrullar (fallback)" }
        )
        { Name = "Raíz (Selector)" };

        // TODO [PARTE 2c]: Añade la rama de ataque en el lugar correcto del Selector.
    }

    // ── Tick ─────────────────────────────────────────────────────────────

    void Update()
    {
        _tree?.Tick();
        SimulateDamage();
        Regenerate();
    }

    // ── Condiciones ───────────────────────────────────────────────────────

    bool LowHealth() => vida < vidaMaxima * 0.5f;

    bool CanSeePlayer()
    {
        if (jugador == null) return false;
        return Vector3.Distance(transform.position, jugador.position) < rangoDeteccion;
    }

    bool HasLastKnownPosition() => _hasLastKnownPos;

    // TODO [PARTE 2a]: Implementa EstaCerca().
    // Devuelve true si el jugador está dentro de rangoAtaque.
    // bool EstaCerca() { ... }
    bool EstaCerca()
    {
        if (jugador == null) return false;
        return Vector3.Distance(transform.position, jugador.position) < rangoAtaque;
    }

    // ── Acciones ──────────────────────────────────────────────────────────

    NodeStatus Flee()
    {
        GetComponent<Renderer>().material.color = Color.magenta;

        _hasLastKnownPos = false;

        Vector3 dir = (transform.position - jugador.position).normalized;
        transform.position += dir * (velocidadHuida * Time.deltaTime);
        transform.LookAt(transform.position + dir);

        return NodeStatus.Running;
    }

    NodeStatus Chase()
    {
        GetComponent<Renderer>().material.color = Color.yellow;

        _lastKnownPos = jugador.position;
        _hasLastKnownPos = true;

        Vector3 dir = (jugador.position - transform.position).normalized;
        transform.position += dir * (velocidadPersecucion * Time.deltaTime);
        transform.LookAt(jugador);

        return NodeStatus.Running;
    }

    NodeStatus Investigate()
    {
        GetComponent<Renderer>().material.color = Color.red;

        Vector3 dir = (_lastKnownPos - transform.position).normalized;
        transform.position += dir * (velocidadInvestigacion * Time.deltaTime);
        transform.LookAt(_lastKnownPos);

        if (Vector3.Distance(transform.position, _lastKnownPos) < distanciaInvestigacion)
        {
            _hasLastKnownPos = false;
            Debug.Log("[BT] Investigación sin resultado.");
            return NodeStatus.Success; // Success → el Selector pasa a Patrol
        }

        return NodeStatus.Running;
    }

    NodeStatus Patrol()
    {
        GetComponent<Renderer>().material.color = Color.cyan;

        if (waypoints == null || waypoints.Length == 0)
            return NodeStatus.Running;

        Transform destino = waypoints[_waypointIndex];
        Vector3 dir = (destino.position - transform.position).normalized;
        transform.position += dir * (velocidadPatrulla * Time.deltaTime);
        transform.LookAt(destino);

        if (Vector3.Distance(transform.position, destino.position) < distanciaWaypoint)
            _waypointIndex = (_waypointIndex + 1) % waypoints.Length;

        return NodeStatus.Running;
    }

    // TODO [PARTE 2b]: Implementa Attack().
    // El enemigo se detiene y cambia de color (Color.red oscuro o el que prefieras).
    // Devuelve NodeStatus.Running mientras el jugador siga cerca.
    // NodeStatus Attack() { ... }
    NodeStatus Attack()
    {
        GetComponent<Renderer>().material.color = Color.black;

        _lastKnownPos = jugador.position;
        _hasLastKnownPos = true;

        transform.LookAt(jugador);

        Debug.Log("Atacando");

        return NodeStatus.Running;
    }

    // ── Utilidades ────────────────────────────────────────────────────────

    void SimulateDamage()
    {
        if (UnityEngine.InputSystem.Keyboard.current.qKey.isPressed)
            vida -= 1f;
        vida = Mathf.Max(vida, 0f);
    }

    void Regenerate()
    {
        if (vida < vidaMaxima)
            vida += 5f * Time.deltaTime;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);

        if (_hasLastKnownPos)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_lastKnownPos, 0.3f);
            Gizmos.DrawLine(transform.position, _lastKnownPos);
        }

        if (waypoints == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.2f);
            Gizmos.DrawLine(waypoints[i].position, waypoints[(i + 1) % waypoints.Length].position);
        }
    }
}
