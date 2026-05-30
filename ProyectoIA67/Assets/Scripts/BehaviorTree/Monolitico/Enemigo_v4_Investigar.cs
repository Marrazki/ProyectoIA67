// VERSIÓN 4 - Enemigo monolítico: patrulla, investiga, persigue y huye
//
// Nuevo comportamiento: si el enemigo pierde de vista al jugador, primero va
// a la ÚLTIMA POSICIÓN CONOCIDA antes de volver a patrullar (investiga).
//
// Observa que ahora necesitamos variables de estado extra (_ultimaPosJugador,
// _tieneUltimaPosicion) repartidas por el script para que los bloques se
// "comuniquen" entre sí. Esto es exactamente el problema que resuelven
// los Behavior Trees: separar cada comportamiento en su propio nodo con
// su propio estado encapsulado.

using UnityEngine;

public class Enemigo_v4_Investigar : MonoBehaviour
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
    private int _indiceWaypoint = 0;

    [Header("Persecución")]
    public float velocidadPersecucion = 4f;

    [Header("Investigación")]
    public float velocidadInvestigacion = 3f;
    public float distanciaInvestigacion = 0.5f;
    // Estado interno que necesita compartirse entre bloques → señal de alerta
    private Vector3 _ultimaPosJugador;
    private bool _tieneUltimaPosicion = false;

    [Header("Huida")]
    public float velocidadHuida = 7f;

    void Update()
    {
        // ── LÓGICA PRINCIPAL ──────────────────────────────────────────────
        // Orden de prioridad (de mayor a menor):
        //   1. Vida baja        →  huir
        //   2. Veo jugador      →  perseguir  (y guardar última posición)
        //   3. Última pos. conocida  →  investigar
        //   4. Sin pistas       →  patrullar
        //
        // Con 4 ramas y variables de "comunicación" entre ellas, el script
        // ya es difícil de ampliar sin romper algo. ¿Te imaginas con 10 ramas?
        if (LowHealth())
        {
            _tieneUltimaPosicion = false; // al huir olvidamos la pista
            Flee();
            GetComponent<Renderer>().material.color = Color.magenta;
        }
        else if (CanSeePlayer())
        {
            // Guardamos la posición antes de perseguir
            _ultimaPosJugador = jugador.position;
            _tieneUltimaPosicion = true;

            Chase();
            GetComponent<Renderer>().material.color = Color.yellow;
        }
        else if (_tieneUltimaPosicion)
        {
            Investigate();
            GetComponent<Renderer>().material.color = Color.red;
        }
        else
        {
            Patrol();
            GetComponent<Renderer>().material.color = Color.cyan;
        }
        // ─────────────────────────────────────────────────────────────────

        SimulateDamage();
        Regenerate();
    }

    void Flee()
    {
        Vector3 dir = (transform.position - jugador.position).normalized;
        transform.position += dir * (velocidadHuida * Time.deltaTime);
        transform.LookAt(transform.position + dir);
        Debug.Log("¡Vida baja! Huyendo...");
    }

    void Chase()
    {
        Vector3 dir = (jugador.position - transform.position).normalized;
        transform.position += dir * (velocidadPersecucion * Time.deltaTime);
        transform.LookAt(jugador);
        Debug.Log("Persiguiendo al jugador");
    }

    void Investigate()
    {
        Vector3 dir = (_ultimaPosJugador - transform.position).normalized;
        transform.position += dir * (velocidadInvestigacion * Time.deltaTime);
        transform.LookAt(_ultimaPosJugador);

        if (Vector3.Distance(transform.position, _ultimaPosJugador) < distanciaInvestigacion)
        {
            // Llegamos a la última posición conocida, sin rastro: volvemos a patrullar
            _tieneUltimaPosicion = false;
            Debug.Log("Investigación sin resultado. Volviendo a patrullar.");
        }
        else
        {
            Debug.Log("Investigando última posición conocida del jugador...");
        }
    }

    void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.Log("Sin waypoints. Quieto.");
            return;
        }

        Transform destino = waypoints[_indiceWaypoint];
        Vector3 dir = (destino.position - transform.position).normalized;
        transform.position += dir * (velocidadPatrulla * Time.deltaTime);
        transform.LookAt(destino);

        if (Vector3.Distance(transform.position, destino.position) < distanciaWaypoint)
            _indiceWaypoint = (_indiceWaypoint + 1) % waypoints.Length;

        Debug.Log($"Patrullando hacia waypoint {_indiceWaypoint}");
    }

    bool LowHealth() => vida < vidaMaxima * 0.5f;

    bool CanSeePlayer()
    {
        if (jugador == null) return false;
        return Vector3.Distance(transform.position, jugador.position) < rangoDeteccion;
    }

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

        if (_tieneUltimaPosicion)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_ultimaPosJugador, 0.3f);
            Gizmos.DrawLine(transform.position, _ultimaPosJugador);
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
