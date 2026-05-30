// VERSIÓN 1 - Enemigo monolítico: solo persigue al jugador
//
// Toda la lógica vive en Update(). Fácil de leer con un solo comportamiento,
// pero verás cómo crece y se complica en las siguientes versiones.

using UnityEngine;

public class Enemigo_v1_Perseguir : MonoBehaviour
{
    [Header("Referencias")]
    public Transform jugador;

    [Header("Detección")]
    public float rangoDeteccion = 5f;

    [Header("Movimiento")]
    public float velocidadPersecucion = 4f;

    void Update()
    {
        // ── LÓGICA PRINCIPAL ──────────────────────────────────────────────
        // Pregunta única: ¿veo al jugador?
        if (CanSeePlayer())
        {
            Chase();
            GetComponent<Renderer>().material.color = Color.yellow; // persiguiendo
        }
        else
        {
            Idle();
            GetComponent<Renderer>().material.color = Color.green; // quieto
        }
        // ─────────────────────────────────────────────────────────────────
    }

    void Chase()
    {
        Vector3 direccion = (jugador.position - transform.position).normalized;
        transform.position += direccion * (velocidadPersecucion * Time.deltaTime);
        transform.LookAt(jugador);

        Debug.Log("Persiguiendo al jugador");
    }

    void Idle()
    {
        Debug.Log("Sin objetivo. Esperando...");
    }

    bool CanSeePlayer()
    {
        if (jugador == null) return false;
        return Vector3.Distance(transform.position, jugador.position) < rangoDeteccion;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
    }
}
