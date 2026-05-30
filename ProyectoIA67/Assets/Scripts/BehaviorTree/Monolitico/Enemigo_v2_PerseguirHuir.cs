// VERSIÓN 2 - Enemigo monolítico: persigue al jugador y huye si tiene poca vida
//
// Añadimos una condición de prioridad: la vida. Si está baja, huye sin importar
// nada más. Observa que el bloque if-else crece y el orden empieza a importar.

using UnityEngine;

public class Enemigo_v2_PerseguirHuir : MonoBehaviour
{
    [Header("Referencias")]
    public Transform jugador;

    [Header("Salud")]
    public float vida = 100f;
    public float vidaMaxima = 100f;

    [Header("Detección")]
    public float rangoDeteccion = 5f;

    [Header("Movimiento")]
    public float velocidadPersecucion = 4f;
    public float velocidadHuida = 7f;

    void Update()
    {
        // ── LÓGICA PRINCIPAL ──────────────────────────────────────────────
        // Primero comprobamos la condición de mayor prioridad: vida baja.
        // Solo si no se cumple pasamos a evaluar las siguientes.
        if (LowHealth())
        {
            Flee();
            GetComponent<Renderer>().material.color = Color.magenta; // huyendo
        }
        else if (CanSeePlayer())
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

        SimulateDamage();
        Regenerate();
    }

    void Flee()
    {
        Vector3 direccionHuida = (transform.position - jugador.position).normalized;
        transform.position += direccionHuida * (velocidadHuida * Time.deltaTime);
        transform.LookAt(transform.position + direccionHuida);

        Debug.Log("¡Vida baja! Huyendo...");
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
        Gizmos.color = LowHealth() ? Color.magenta : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
    }
}
