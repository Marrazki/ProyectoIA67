// ============================================================
//  EJERCICIO: BT + STEERING — Unidad 6
// ============================================================
//
//  Integra los steering behaviors (Seek, Flee, Arrive) con el Behavior
//  Tree para producir movimiento fluido en lugar de teletransporte.
//
//  CLAVE CONCEPTUAL:
//  ──────────────────────────────────────────────────────────────────────
//  · El BT decide QUÉ hacer (perseguir, huir, patrullar).
//  · El steering behavior decide CÓMO moverse (con qué fuerza, en qué arco).
//
//  Comparación con Enemigo_BT.cs (el original):
//    Antes:  transform.position += dir * speed * Time.deltaTime   ← snap directo
//    Ahora:  _velocity → Arrive/Seek/Flee → MoveTowards            ← suave
//
//  COMPORTAMIENTO:
//  ──────────────────────────────────────────────────────────────────────
//  Selector (reactivo)
//  ├── ConditionalSequence "si vida baja → Flee steering"
//  │   ├── Condition: LowHealth
//  │   └── BTAction:  ApplyFlee
//  ├── ConditionalSequence "si veo jugador → Arrive al jugador"
//  │   ├── Condition: CanSeePlayer
//  │   └── BTAction:  ApplyArrive
//  └── BTAction: PatrolWithArrive   ← Arrive hacia waypoints
//
//  DIFERENCIA VISUAL (compara con Enemigo_BT):
//    · Al cambiar de estado, el movimiento es suave (no snap).
//    · Al llegar al jugador usa Arrive: desacelera en lugar de entrar en bucle.
//    · La patrulla es fluida: curva natural hacia el siguiente waypoint.
//
// ============================================================
//  PARTES DEL EJERCICIO
// ============================================================
//
//  [PARTE 1 — OBLIGATORIO]
//    Cambia ApplyArrive por ApplySeek en la rama de persecución.
//    Observa el overshooting. Documenta: ¿a qué distancia empieza
//    a frenar Arrive con slowRadius=3? ¿Y con slowRadius=1?
//
//  [PARTE 2 — AMPLIACIÓN]
//    Añade una condición EstaCerca() y una acción ApplyPursue():
//    · EstaCerca: distancia al jugador < attackRange.
//    · ApplyPursue: usa SteeringBehaviors.Pursue con la velocidad
//      del jugador. Necesitarás una referencia a Rigidbody del jugador
//      o calcular la velocidad aproximada.
//
//  [PARTE 3 — BONUS]
//    Combina flocking + BT: añade un FlockingAgent al mismo GameObject.
//    En la acción de patrulla, suma la fuerza de flocking a la velocidad
//    calculada por Arrive. ¿Qué fuerza tiene más peso?

using UnityEngine;

public class Enemigo_Steering_BT : MonoBehaviour
{
    [Header("Referencias")]
    public Transform jugador;

    [Header("Salud")]
    public float vida = 100f;
    public float vidaMaxima = 100f;

    [Header("Detección")]
    public float rangoDeteccion = 5f;
    public float rangoAtaque = 1.5f;

    [Header("Patrulla")]
    public Transform[] waypoints;
    public float distanciaWaypoint = 0.5f;

    [Header("Velocidades")]
    public float maxSpeed = 4f;
    public float fleeSpeed = 7f;
    public float steeringForce = 8f;

    [Header("Arrive")]
    public float slowRadius = 3f;
    public float targetRadius = 0.2f;

    BTNode _tree;
    Vector3 _velocity;
    int _waypointIndex;

    public BTNode Tree => _tree;

    void Start() => BuildTree();

    void BuildTree()
    {
        _tree = new Selector(
            new ConditionalSequence(
                new Condition(LowHealth,    "VidaBaja?"),
                new BTAction(ApplyFlee,     "Flee-Steering")
            ) { Name = "Huir con steering [Self Abort]" },

            new ConditionalSequence(
                new Condition(CanSeePlayer, "VeoJugador?"),
                new BTAction(ApplyArrive,   "Arrive-Jugador")
            ) { Name = "Arrive al jugador [Self Abort]" },

            new BTAction(PatrolWithArrive, "Patrulla-Arrive")
            { Name = "Patrullar con Arrive (fallback)" }
        )
        { Name = "Raíz (Selector reactivo)" };
    }

    void Update()
    {
        _tree?.Tick();
        SimulateDamage();
        Regenerate();

        // Aplica velocidad acumulada (las acciones la modifican).
        transform.position += _velocity * Time.deltaTime;
        if (_velocity.sqrMagnitude > 0.01f)
            transform.forward = Vector3.Lerp(
                transform.forward, _velocity.normalized, 10f * Time.deltaTime);
    }

    // ── Condiciones ────────────────────────────────────────────────────────

    bool LowHealth() => vida < vidaMaxima * 0.5f;

    bool CanSeePlayer()
    {
        if (jugador == null) return false;
        return Vector3.Distance(transform.position, jugador.position) < rangoDeteccion;
    }

    // ── Acciones (aplican steering) ────────────────────────────────────────

    NodeStatus ApplyFlee()
    {
        GetComponent<Renderer>().material.color = Color.magenta;
        Vector3 desired = SteeringBehaviors.Flee(transform.position, jugador.position, fleeSpeed);
        _velocity = Vector3.MoveTowards(_velocity, desired, steeringForce * Time.deltaTime);
        return NodeStatus.Running;
    }

    NodeStatus ApplyArrive()
    {
        GetComponent<Renderer>().material.color = Color.yellow;
        Vector3 desired = SteeringBehaviors.Arrive(
            transform.position, jugador.position, maxSpeed, slowRadius, targetRadius);
        _velocity = Vector3.MoveTowards(_velocity, desired, steeringForce * Time.deltaTime);
        return NodeStatus.Running;
    }

    NodeStatus PatrolWithArrive()
    {
        GetComponent<Renderer>().material.color = Color.cyan;
        if (waypoints == null || waypoints.Length == 0) return NodeStatus.Running;

        Transform destino = waypoints[_waypointIndex];
        Vector3 desired = SteeringBehaviors.Arrive(
            transform.position, destino.position, maxSpeed * 0.6f,
            slowRadius: 1.5f, targetRadius: distanciaWaypoint);
        _velocity = Vector3.MoveTowards(_velocity, desired, steeringForce * Time.deltaTime);

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

        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, slowRadius);

        if (Application.isPlaying && _velocity.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + _velocity);
        }

        if (waypoints == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.15f);
            Gizmos.DrawLine(waypoints[i].position,
                            waypoints[(i + 1) % waypoints.Length].position);
        }
    }
}
