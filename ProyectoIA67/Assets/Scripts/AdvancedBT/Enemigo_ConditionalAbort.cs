// ============================================================
//  EJERCICIO: CONDITIONAL ABORTS — Unidad 5
// ============================================================
//
//  Demuestra la diferencia entre árboles reactivos (con abort) y
//  no reactivos (sin abort) en comportamientos de IA.
//
//  ÁRBOL:
//  ──────────────────────────────────────────────────────────────────────
//  [Selector | StickySelector]  ← cambiable con useStickySelector
//  ├── ConditionalSequence "si vida baja → huir"   (Self Abort)
//  │   ├── Condition: LowHealth   ← se reevalúa SIEMPRE
//  │   └── BTAction:  Flee
//  ├── ConditionalSequence "si veo jugador → perseguir"   (Self Abort)
//  │   ├── Condition: CanSeePlayer   ← se reevalúa SIEMPRE
//  │   └── BTAction:  Chase
//  └── BTAction: Patrol   ← fallback
//
//  EXPERIMENTO:
//  ──────────────────────────────────────────────────────────────────────
//  1. Activa useStickySelector = true.
//     El enemigo patrulla. Presiona Q (daño) → ¿Huye? ← NO (BUG del cursor)
//
//  2. Activa useStickySelector = false (Selector reactivo).
//     El enemigo patrulla. Presiona Q (daño) → ¿Huye? ← SÍ (LowerPriority Abort)
//
//  3. La persecución (ConditionalSequence) tiene Self Abort:
//     Mueve el jugador fuera del rango → la persecución se aborta inmediatamente.
//     Cambia ConditionalSequence por Sequence normal en la rama Chase y observa
//     qué pasa (sin abort, el enemigo sigue moviéndose hacia la última posición).
//
// ============================================================
//  PARTES DEL EJERCICIO
// ============================================================
//
//  [PARTE 1 — OBLIGATORIO]
//    Activa ambos modos (useStickySelector true/false) y documenta
//    la diferencia observable. ¿En qué frame exacto reacciona cada árbol?
//
//  [PARTE 2 — AMPLIACIÓN]
//    Añade una rama "Atacar" entre Flee y Chase usando ConditionalSequence.
//    Condición: CanSeePlayer AND distancia < rangoAtaque.
//    Pregunta: ¿qué tipo de abort necesita esta rama?

using UnityEngine;

public class Enemigo_ConditionalAbort : MonoBehaviour
{
    [Header("Referencias")]
    public Transform jugador;

    [Header("Config de árbol")]
    [Tooltip("true → StickySelector (no reactivo, muestra el BUG del cursor).\nfalse → Selector reactivo (LowerPriority Abort).")]
    public bool useStickySelector = false;

    [Header("Salud")]
    public float vida = 100f;
    public float vidaMaxima = 100f;

    [Header("Detección")]
    public float rangoDeteccion = 5f;
    public float rangoAtaque = 1.5f;

    [Header("Patrulla")]
    public Transform[] waypoints;
    public float velocidadPatrulla = 2f;
    public float distanciaWaypoint = 0.5f;

    [Header("Velocidades")]
    public float velocidadPersecucion = 4f;
    public float velocidadHuida = 7f;

    BTNode _tree;
    int _waypointIndex;
    bool _wasStickySelector;

    public BTNode Tree => _tree;

    void Start()
    {
        _wasStickySelector = useStickySelector;
        BuildTree();
    }

    void BuildTree()
    {
        // Las ramas usan ConditionalSequence (Self Abort):
        // la condición se reevalúa cada tick aunque la acción esté en Running.
        var branches = new BTNode[]
        {
            new ConditionalSequence(
                new Condition(LowHealth,    "VidaBaja?"),
                new BTAction(Flee,          "Huir")
            ) { Name = "Huir si vida baja  [Self Abort]" },

            new ConditionalSequence(
                new Condition(CanSeePlayer, "VeoJugador?"),
                new BTAction(Chase,         "Perseguir")
            ) { Name = "Perseguir si veo  [Self Abort]" },

            // TODO [PARTE 2]: Añade rama de Ataque aquí.

            new BTAction(Patrol, "Patrullar") { Name = "Patrullar (fallback)" }
        };

        // El tipo de Selector determina si hay LowerPriority Abort o no.
        _tree = useStickySelector
            ? (BTNode)new StickySelector(branches) { Name = "Raíz (StickySelector — NO reactivo)" }
            : (BTNode)new Selector(branches) { Name = "Raíz (Selector — LowerPriority Abort)" };

        _wasStickySelector = useStickySelector;
        Debug.Log($"[ConditionalAbort] Árbol reconstruido → {_tree.Name}");
    }

    void Update()
    {
        // Permite cambiar el toggle en caliente desde el Inspector.
        if (useStickySelector != _wasStickySelector)
            BuildTree();

        _tree?.Tick();
        SimulateDamage();
        Regenerate();
    }

    // ── Condiciones ────────────────────────────────────────────────────────

    bool LowHealth() => vida < vidaMaxima * 0.5f;

    bool CanSeePlayer()
    {
        if (jugador == null) return false;
        return Vector3.Distance(transform.position, jugador.position) < rangoDeteccion;
    }

    // ── Acciones ──────────────────────────────────────────────────────────

    NodeStatus Flee()
    {
        GetComponent<Renderer>().material.color = Color.magenta;
        if (jugador == null) return NodeStatus.Running;
        Vector3 dir = (transform.position - jugador.position).normalized;
        transform.position += dir * (velocidadHuida * Time.deltaTime);
        transform.LookAt(transform.position + dir);
        return NodeStatus.Running;
    }

    NodeStatus Chase()
    {
        GetComponent<Renderer>().material.color = Color.yellow;
        Vector3 dir = (jugador.position - transform.position).normalized;
        transform.position += dir * (velocidadPersecucion * Time.deltaTime);
        transform.LookAt(jugador);
        return NodeStatus.Running;
    }

    NodeStatus Patrol()
    {
        GetComponent<Renderer>().material.color = Color.cyan;
        if (waypoints == null || waypoints.Length == 0) return NodeStatus.Running;

        Transform destino = waypoints[_waypointIndex];
        Vector3 dir = (destino.position - transform.position).normalized;
        transform.position += dir * (velocidadPatrulla * Time.deltaTime);
        transform.LookAt(destino);

        if (Vector3.Distance(transform.position, destino.position) < distanciaWaypoint)
            _waypointIndex = (_waypointIndex + 1) % waypoints.Length;

        return NodeStatus.Running;
    }

    // ── Utilidades ────────────────────────────────────────────────────────

    void SimulateDamage()
    {
        if (UnityEngine.InputSystem.Keyboard.current.qKey.isPressed)
            vida -= 5f;
        vida = Mathf.Max(vida, 0f);
    }

    void Regenerate()
    {
        if (vida < vidaMaxima) vida += 3f * Time.deltaTime;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
        Gizmos.color = new Color(1f, 0.3f, 0f);
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);

        if (waypoints == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.2f);
            Gizmos.DrawLine(waypoints[i].position,
                            waypoints[(i + 1) % waypoints.Length].position);
        }
    }
}
