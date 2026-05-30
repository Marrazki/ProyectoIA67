// ============================================================
//  EJERCICIO: BLACKBOARD — Controlador del enemigo
// ============================================================
//
//  El patrón Blackboard separa la PERCEPCIÓN del COMPORTAMIENTO:
//
//    Mundo → [Sensores] → Pizarra → [Comportamientos] → Acciones
//
//  Cada sensor percibe una parte del mundo y escribe en la pizarra.
//  Los comportamientos (estados de la FSM) leen de la pizarra y actúan.
//  Nadie se habla directamente: todo pasa por la pizarra.
//
//  COMPARATIVA CON EJERCICIOS ANTERIORES:
//
//    Monolítico:  toda la lógica en Update() con variables de instancia
//    FSM:         estados separados, pero comparten datos vía el enemigo
//    BT:          nodos separados, pero los datos siguen en el mismo script
//    Blackboard:  percepción y comportamiento son componentes independientes
//
// ============================================================
//  PARTES DEL EJERCICIO
// ============================================================
//
//  [PARTE 1 — OBLIGATORIO]  → Core/Blackboard.cs
//    Implementa los cuatro métodos: Set, Get, Has, Remove.
//
//  [PARTE 2 — AMPLIACIÓN]   → Sensors/VisionSensor.cs y HealthSensor.cs
//    a) Implementa VisionSensor.Sense()
//    b) Implementa HealthSensor.Sense()
//    c) Inicializa los sensores en Start(), llámalos en Update() y
//       lee la pizarra en UpdateFSM(), Chase() e Investigate().
//
//  [PARTE 3 — BONUS]        → Sensors/SoundSensor.cs
//    a) Implementa SoundSensor.Sense()
//    b) Integra el sensor en este script siguiendo las instrucciones
//       de SoundSensor.cs.
//
// ============================================================

using UnityEngine;

public class Enemigo_Blackboard : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Health")]
    public float health    = 100f;
    public float maxHealth = 100f;

    [Header("Visual Detection")]
    public float detectionRange = 5f;

    [Header("Hearing Range (Part 3)")]
    public float hearingRange = 8f;

    [Header("Patrol")]
    public Transform[] waypoints;
    public float patrolSpeed      = 2f;
    public float waypointDistance = 0.5f;

    [Header("Chase")]
    public float chaseSpeed = 4f;

    [Header("Investigate")]
    public float investigateSpeed     = 3f;
    public float investigationDistance = 0.5f;

    [Header("Flee")]
    public float fleeSpeed = 7f;

    // ── Blackboard and sensors ─────────────────────────────────────────────
    Blackboard   _blackboard;
    VisionSensor _visionSensor;
    HealthSensor _healthSensor;
    SoundSensor  _soundSensor;

    // ── FSM state ─────────────────────────────────────────────────────────
    enum State { Patrol, Chase, Investigate, Flee }
    State _state;
    int   _waypointIndex;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Start()
    {
        _blackboard = new Blackboard();

        _visionSensor = new VisionSensor(transform, player, detectionRange, _blackboard);
        _healthSensor = new HealthSensor(() => health, maxHealth, _blackboard);
        _soundSensor  = new SoundSensor(transform, player, hearingRange, _blackboard);

        _state = State.Patrol;
    }

    void Update()
    {
        _visionSensor.Sense();
        _healthSensor.Sense();
        _soundSensor.Sense();

        SimulateDamage();
        Regenerate();
        UpdateFSM();
    }

    // ── FSM ────────────────────────────────────────────────────────────────

    void UpdateFSM()
    {
        bool lowHealth    = _blackboard.Get<bool>(BB.LowHealth);
        bool canSeePlayer = _blackboard.Get<bool>(BB.CanSeePlayer);
        bool hasClue      = _blackboard.Get<bool>(BB.HasClue);

        switch (_state)
        {
            case State.Patrol:
                Patrol();
                if (lowHealth)          ChangeState(State.Flee);
                else if (canSeePlayer)  ChangeState(State.Chase);
                else if (hasClue)       ChangeState(State.Investigate);
                break;
            case State.Chase:
                Chase();
                if (lowHealth)          ChangeState(State.Flee);
                else if (!canSeePlayer) ChangeState(State.Investigate);
                break;
            case State.Investigate:
                bool arrived = Investigate();
                if (arrived)      ChangeState(State.Patrol);
                if (canSeePlayer) ChangeState(State.Chase);
                if (lowHealth)    ChangeState(State.Flee);
                break;
            case State.Flee:
                Flee();
                if (!lowHealth) ChangeState(State.Patrol);
                break;
        }
    }

    void ChangeState(State next)
    {
        Debug.Log($"[BB] {_state} → {next}");
        _state = next;
    }

    // ── Behaviors ─────────────────────────────────────────────────────────
    // Los comportamientos leen de la pizarra para saber adónde moverse.
    // No acceden directamente a "player".

    void Patrol()
    {
        GetComponent<Renderer>().material.color = Color.cyan;
        if (waypoints == null || waypoints.Length == 0) return;

        Transform target = waypoints[_waypointIndex];
        MoveTo(target.position, patrolSpeed);
        if (Vector3.Distance(transform.position, target.position) < waypointDistance)
            _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
    }

    void Chase()
    {
        GetComponent<Renderer>().material.color = Color.yellow;

        Vector3 targetPos = _blackboard.Get<Vector3>(BB.LastKnownPosition);

        MoveTo(targetPos, chaseSpeed);
        transform.LookAt(targetPos);
    }

    // Returns true when the enemy reaches the investigation point.
    bool Investigate()
    {
        GetComponent<Renderer>().material.color = Color.red;

        Vector3 lastPos = _blackboard.Get<Vector3>(BB.LastKnownPosition);

        MoveTo(lastPos, investigateSpeed);
        transform.LookAt(lastPos);

        if (Vector3.Distance(transform.position, lastPos) < investigationDistance)
        {
            _blackboard.Remove(BB.HasClue);
            Debug.Log("[BB] Investigation complete: no target found.");
            return true;
        }

        return false;
    }

    void Flee()
    {
        GetComponent<Renderer>().material.color = Color.magenta;
        if (player == null) return;
        Vector3 dir = (transform.position - player.position).normalized;
        transform.position += dir * (fleeSpeed * Time.deltaTime);
        transform.LookAt(transform.position + dir);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    void MoveTo(Vector3 destination, float speed)
    {
        Vector3 dir = (destination - transform.position).normalized;
        transform.position += dir * (speed * Time.deltaTime);
    }

    void SimulateDamage()
    {
        if (UnityEngine.InputSystem.Keyboard.current.qKey.isPressed)
            health -= 1f;
        health = Mathf.Max(health, 0f);
    }

    void Regenerate()
    {
        if (health < maxHealth)
            health += 5f * Time.deltaTime;
    }

    void OnDrawGizmos()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Hearing range (Part 3)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        // Investigation clue
        if (_blackboard != null && _blackboard.Has(BB.LastKnownPosition))
        {
            Gizmos.color = Color.red;
            Vector3 p = _blackboard.Get<Vector3>(BB.LastKnownPosition);
            Gizmos.DrawSphere(p, 0.3f);
            Gizmos.DrawLine(transform.position, p);
        }

        // Waypoints
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
