// ============================================================
//  EJERCICIO: FORMACIONES — Unidad 8
// ============================================================
//
//  FormacionLider calcula las posiciones de los slots según
//  el tipo de formación activa y las expone a los miembros.
//
//  FORMACIONES DISPONIBLES:
//  ──────────────────────────────────────────────────────────
//  · Linea  : miembros alineados perpendicular al frente del líder.
//  · Cuña   : forma de V, líder al frente y miembros hacia atrás.
//  · Circulo: miembros distribuidos uniformemente alrededor del líder.
//  · Caja   : 4 slots en las esquinas de un cuadrado (PARTE 3).
//
//  ÁRBOL DE COMPORTAMIENTO DEL LÍDER:
//  ──────────────────────────────────────────────────────────
//  El líder patrulla waypoints mientras actualiza los slots.
//  Al detectar al jugador cambia a persecución en formación.
//
//  PARTES DEL EJERCICIO
//  ──────────────────────────────────────────────────────────
//
//  [PARTE 1 — OBLIGATORIO]
//    Abre la escena Unit8_FormacionesScene.
//    Crea un líder y 3-5 miembros (FormacionMiembro).
//    Cambia el tipo de formación en el Inspector durante Play.
//    ¿Los miembros se redistribuyen suavemente?
//
//  [PARTE 2 — AMPLIACIÓN ✓ IMPLEMENTADO]
//    Transición automática:
//    · Linea   → al patrullar (calma).
//    · Cuña    → al ver al jugador (avance).
//    · Circulo → al recibir daño (defensa).
//    Activa autoTransicion en el Inspector para habilitarlo.
//
//  [PARTE 3 — BONUS ✓ IMPLEMENTADO]
//    Formación "Caja":
//    · Frente-izquierda y frente-derecha van adelante.
//    · Atrás-izquierda y atrás-derecha van detrás.
//    · Con más de 4 miembros, los extras van al centro-atrás.

using UnityEngine;

public class FormacionLider : MonoBehaviour
{
    // Tipos de formación soportados — Caja añadido en PARTE 3
    public enum TipoFormacion { Linea, Cuña, Circulo, Caja }

    [Header("Formación")]
    public TipoFormacion formacion = TipoFormacion.Linea;
    [Tooltip("Distancia entre slots en la formación.")]
    public float separacion = 1.5f;

    [Header("Patrulla")]
    public Transform[] waypoints;
    public float velocidad = 3f;
    public float distanciaWaypoint = 0.5f;

    [Header("Detección")]
    public Transform jugador;
    public float rangoDeteccion = 6f;

    [Header("Salud (para transición por daño)")]
    public float vida = 100f;
    public float vidaMaxima = 100f;

    // ── PARTE 2: transición automática de formación ───────────────────────
    [Header("Parte 2 — Transición automática")]
    [Tooltip("Actívalo para la PARTE 2. Desactivado permite cambiar la formación manualmente desde el Inspector.")]
    public bool autoTransicion = false;

    int _waypointIndex;
    bool _recibioDaño;

    void Update()
    {
        // Simula daño con tecla Q para demostrar la transición a Circulo
        if (UnityEngine.InputSystem.Keyboard.current.qKey.wasPressedThisFrame)
        {
            vida = Mathf.Max(0f, vida - 25f);
            _recibioDaño = true;
        }

        // Regeneración lenta para que el estado de daño no sea permanente
        vida = Mathf.Min(vidaMaxima, vida + 5f * Time.deltaTime);
        if (vida > vidaMaxima * 0.8f) _recibioDaño = false;

        bool veJugador = jugador != null &&
                         Vector3.Distance(transform.position, jugador.position) < rangoDeteccion;

        // ── PARTE 2: cambia la formación automáticamente según el estado ──
        if (autoTransicion)
            ActualizarFormacionAutomatica(veJugador);

        // Movimiento del líder: patrulla o persigue al jugador
        if (veJugador)
            MoverHacia(jugador.position, velocidad * 1.5f);
        else
            Patrullar();
    }

    // ── PARTE 2: lógica de transición automática ──────────────────────────

    void ActualizarFormacionAutomatica(bool veJugador)
    {
        TipoFormacion nueva;

        if (_recibioDaño)
            nueva = TipoFormacion.Circulo;   // bajo fuego → defensa circular
        else if (veJugador)
            nueva = TipoFormacion.Cuña;      // avance en V hacia el objetivo
        else
            nueva = TipoFormacion.Linea;     // patrulla en calma → línea

        if (nueva != formacion)
        {
            Debug.Log($"[FormacionLider] Formación cambiada: {formacion} → {nueva}");
            formacion = nueva;
        }
    }

    // ── Cálculo de slots ──────────────────────────────────────────────────

    // Devuelve la posición mundial del slot indicado para el número total de miembros.
    public Vector3 ObtenerPosicionSlot(int indice, int totalMiembros)
    {
        return formacion switch
        {
            TipoFormacion.Linea   => SlotLinea(indice, totalMiembros),
            TipoFormacion.Cuña    => SlotCuña(indice, totalMiembros),
            TipoFormacion.Circulo => SlotCirculo(indice, totalMiembros),
            TipoFormacion.Caja    => SlotCaja(indice, totalMiembros),
            _                     => transform.position
        };
    }

    // Formación en línea: todos los miembros centrados a los lados del líder.
    Vector3 SlotLinea(int indice, int total)
    {
        // Offset lateral en el eje derecho del líder
        float offset = (indice - (total - 1) * 0.5f) * separacion;
        return transform.position + transform.right * offset - transform.forward * separacion;
    }

    // Formación en cuña (V): el líder va adelante, los miembros se abren en diagonal.
    Vector3 SlotCuña(int indice, int total)
    {
        // Mitad izquierda (índices pares) y mitad derecha (índices impares)
        int fila   = indice / 2 + 1;
        float lado = (indice % 2 == 0) ? -1f : 1f;
        return transform.position
               - transform.forward * (fila * separacion)
               + transform.right   * (lado * fila * separacion * 0.6f);
    }

    // Formación circular: miembros distribuidos equitativamente alrededor del líder.
    Vector3 SlotCirculo(int indice, int total)
    {
        float angulo = indice * (360f / total) * Mathf.Deg2Rad;
        float x = Mathf.Sin(angulo) * separacion * 1.5f;
        float z = Mathf.Cos(angulo) * separacion * 1.5f;
        return transform.position + new Vector3(x, 0f, z);
    }

    // ── PARTE 3: formación en caja ─────────────────────────────────────────
    // Los primeros 4 slots forman las esquinas del cuadrado.
    // Frente: índices 0 (izquierda) y 1 (derecha).
    // Atrás:  índices 2 (izquierda) y 3 (derecha).
    // Slots adicionales se colocan en una fila extra detrás.
    Vector3 SlotCaja(int indice, int total)
    {
        // Posiciones fijas para las 4 esquinas de la caja
        Vector3[] esquinas = new Vector3[]
        {
            transform.position + transform.forward * separacion - transform.right * separacion,  // frente-izq
            transform.position + transform.forward * separacion + transform.right * separacion,  // frente-der
            transform.position - transform.forward * separacion - transform.right * separacion,  // atrás-izq
            transform.position - transform.forward * separacion + transform.right * separacion,  // atrás-der
        };

        if (indice < 4)
            return esquinas[indice];

        // Miembros extra van en filas adicionales detrás
        int extraIndex = indice - 4;
        float offsetLateral = (extraIndex - 0.5f) * separacion;
        return transform.position - transform.forward * (separacion * 2f + extraIndex * separacion * 0.5f)
               + transform.right * offsetLateral;
    }

    // ── Utilidades ────────────────────────────────────────────────────────

    public void CambiarFormacion(TipoFormacion nueva) => formacion = nueva;

    void Patrullar()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        Transform destino = waypoints[_waypointIndex];
        MoverHacia(destino.position, velocidad);
        if (Vector3.Distance(transform.position, destino.position) < distanciaWaypoint)
            _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
    }

    void MoverHacia(Vector3 destino, float vel)
    {
        Vector3 dir = (destino - transform.position).normalized;
        transform.position += dir * vel * Time.deltaTime;
        if (dir.sqrMagnitude > 0.01f)
            transform.forward = Vector3.Lerp(transform.forward, dir, 10f * Time.deltaTime);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);

        // Muestra los slots en la escena para facilitar el debug
        var miembros = FindObjectsByType<FormacionMiembro>(FindObjectsSortMode.None);
        int total = miembros.Length;
        for (int i = 0; i < total; i++)
        {
            Vector3 slot = ObtenerPosicionSlot(i, total);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(slot, 0.25f);
            Gizmos.DrawLine(transform.position, slot);
        }

        // Indicador de estado de salud para la transición automática
        if (Application.isPlaying && _recibioDaño)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}
