// VERSIÓN 3 - Enemigo monolítico: patrulla, persigue y huye
//
// Añadimos patrulla: cuando no hay amenaza ni poca vida, el enemigo recorre
// waypoints en lugar de quedarse quieto.
// El bloque if-else ya tiene 3 ramas y la función de patrulla añade su propio
// estado interno (_indiceWaypoint). Empieza a oler a espagueti.

using UnityEngine;

public class Enemigo_v3_PatrullarPerseguirHuir : MonoBehaviour
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

    [Header("Huida")]
    public float velocidadHuida = 7f;

    void Update()
    {
        // ── LÓGICA PRINCIPAL ──────────────────────────────────────────────
        // Orden de prioridad (de mayor a menor):
        //   1. Vida baja  →  huir
        //   2. Veo jugador  →  perseguir
        //   3. No veo jugador  →  patrullar
        if (LowHealth())
        {
            Flee();
            GetComponent<Renderer>().material.color = Color.magenta;
        }
        else if (CanSeePlayer())
        {
            Chase();
            GetComponent<Renderer>().material.color = Color.yellow;
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

    void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.Log("Sin waypoints. Quieto.");
            return;
        }

        Transform destino = waypoints[_indiceWaypoint];
        Vector3 direccion = (destino.position - transform.position).normalized;
        transform.position += direccion * (velocidadPatrulla * Time.deltaTime);
        transform.LookAt(destino);

        if (Vector3.Distance(transform.position, destino.position) < distanciaWaypoint)
            _indiceWaypoint = (_indiceWaypoint + 1) % waypoints.Length;

        Debug.Log($"Patrullando hacia waypoint {_indiceWaypoint}");
    }

    void Chase()
    {
        Vector3 direccion = (jugador.position - transform.position).normalized;
        transform.position += direccion * (velocidadPersecucion * Time.deltaTime);
        transform.LookAt(jugador);

        Debug.Log("Persiguiendo al jugador");
    }

    void Flee()
    {
        Vector3 direccionHuida = (transform.position - jugador.position).normalized;
        transform.position += direccionHuida * (velocidadHuida * Time.deltaTime);
        transform.LookAt(transform.position + direccionHuida);

        Debug.Log("¡Vida baja! Huyendo...");
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
