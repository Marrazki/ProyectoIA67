// Controlador de la máquina de estados.
// Esta clase solo contiene: datos del inspector, instancias de estados,
// el dispatcher de Update() y el método ChangeState().
// Toda la lógica de comportamiento y transiciones vive en cada estado.

using UnityEngine;

public class Enemigo_FSM : MonoBehaviour
{
    [Header("Referencias")]
    public Transform jugador;

    [Header("Salud")]
    public float vida      = 100f;
    public float vidaMaxima = 100f;

    [Header("Detección")]
    public float rangoDeteccion = 5f;

    [Header("Patrulla")]
    public Transform[] waypoints;
    public float velocidadPatrulla  = 2f;
    public float distanciaWaypoint  = 0.5f;

    [Header("Persecución")]
    public float velocidadPersecucion = 4f;

    [Header("Investigación")]
    public float velocidadInvestigacion = 3f;
    public float distanciaInvestigacion  = 0.5f;
    // Dato compartido: escrito por ChaseState.OnExit, leído por InvestigateState.
    [HideInInspector] public Vector3 lastKnownPlayerPos;

    [Header("Huida")]
    public float velocidadHuida = 7f;

    // ── Instancias de estado (cacheadas para evitar GC) ───────────────────
    public PatrolState      PatrolState      { get; private set; }
    public ChaseState       ChaseState       { get; private set; }
    public InvestigateState InvestigateState { get; private set; }
    public FleeState        FleeState        { get; private set; }

    EnemyStateBase _currentState;

    void Start()
    {
        PatrolState      = new PatrolState(this);
        ChaseState       = new ChaseState(this);
        InvestigateState = new InvestigateState(this);
        FleeState        = new FleeState(this);

        ChangeState(PatrolState);
    }

    // ── Dispatcher ────────────────────────────────────────────────────────
    void Update()
    {
        _currentState?.Update();
        SimulateDamage();
        Regenerate();
    }

    // ── Máquina de estados ────────────────────────────────────────────────
    public void ChangeState(EnemyStateBase newState)
    {
        _currentState?.OnExit();
        _currentState = newState;
        _currentState.OnEnter();
    }

    // ── Condiciones compartidas ───────────────────────────────────────────
    public bool LowHealth() => vida < vidaMaxima * 0.5f;

    public bool CanSeePlayer()
    {
        if (jugador == null) return false;
        return Vector3.Distance(transform.position, jugador.position) < rangoDeteccion;
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

        if (_currentState is InvestigateState)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(lastKnownPlayerPos, 0.3f);
            Gizmos.DrawLine(transform.position, lastKnownPlayerPos);
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
