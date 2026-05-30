// ============================================================
//  EJERCICIO: COMUNICACIÓN BASADA EN EVENTOS — Unidad 9
// ============================================================
//
//  Cada SquadComunicacion es un agente del squad que:
//  · Publica eventos cuando detecta amenazas o recibe daño.
//  · Reacciona a eventos de otros miembros del squad.
//
//  FLUJO DE COMUNICACIÓN:
//  ──────────────────────────────────────────────────────────────────
//  AgentA detecta jugador  →  Publica "EnemigoDetectado"
//  AgentB y AgentC escuchan →  Cambian estado a Alerta
//  AgentA recibe daño       →  Publica "SolicitarAyuda"
//  AgentC (support)         →  Se mueve hacia AgentA
//
//  PARTES DEL EJERCICIO
//  ──────────────────────────────────────────────────────────────────
//
//  [PARTE 1 — OBLIGATORIO]
//    Añade este componente a 3 agentes en la escena.
//    Observa en la consola cómo los eventos se propagan
//    cuando uno de ellos detecta al jugador.
//    ¿Todos reaccionan aunque no vean al jugador?
//
//  [PARTE 2 — AMPLIACIÓN ✓ IMPLEMENTADO]
//    Evento "AliadoCaido":
//    Cuando la vida llega a 0, publica el evento con la posición.
//    Los demás agentes ignoran esa posición al recibir SolicitarAyuda
//    y cambian a Patrulla si estaban yendo a ayudar al caído.
//
//  [PARTE 3 — BONUS ✓ IMPLEMENTADO]
//    Silencio de radio por tipo de evento:
//    Cada tipo de evento tiene su propio cooldown independiente.
//    Se gestiona con un Dictionary<string, float> de timers.

// ── PARTE 3: cooldown por tipo de evento ──────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

public class SquadComunicacion : MonoBehaviour
{
    // Estados internos del agente reactivos a eventos
    public enum EstadoAgente { Patrulla, Alerta, AcudiendoAyuda, Combate, Caido }

    [Header("Identificación")]
    public string nombreAgente = "Agente";

    [Header("Referencias")]
    public Transform jugador;

    [Header("Detección")]
    public float rangoDeteccion = 5f;

    [Header("Salud")]
    public float vida      = 100f;
    public float vidaMaxima = 100f;

    [Header("Movimiento")]
    public float velocidad = 3.5f;

    [Header("Debug — simulación de daño")]
    [Tooltip("Solo este agente recibe daño al pulsar Q. Actívalo en uno a la vez.")]
    public bool recibirDañoConQ = false;

    [Header("Cooldown de radio (seg)")]
    [Tooltip("Tiempo mínimo entre publicaciones del mismo evento, por tipo.")]
    public float cooldownRadio = 2f;

    EstadoAgente _estado = EstadoAgente.Patrulla;
    Vector3      _posicionAyuda;
    GameObject   _emisorAyuda;

    // PARTE 3: un timer independiente por tipo de evento
    readonly Dictionary<string, float> _cooldowns = new Dictionary<string, float>();

    void OnEnable()
    {
        // Suscripción a eventos del bus al activarse
        EventBus.Suscribir(EventBus.EnemigoDetectado,  AlRecibirAlerta);
        EventBus.Suscribir(EventBus.SolicitarAyuda,    AlRecibirSolicitudAyuda);
        EventBus.Suscribir(EventBus.ObjetivoEliminado, AlObjetivoEliminado);
        // PARTE 2: reaccionar cuando un aliado cae
        EventBus.Suscribir(EventBus.AliadoCaido,       AlRecibirAliadoCaido);
    }

    void OnDisable()
    {
        // Cancelar suscripción al desactivarse para evitar memory leaks
        EventBus.Desuscribir(EventBus.EnemigoDetectado,  AlRecibirAlerta);
        EventBus.Desuscribir(EventBus.SolicitarAyuda,    AlRecibirSolicitudAyuda);
        EventBus.Desuscribir(EventBus.ObjetivoEliminado, AlObjetivoEliminado);
        EventBus.Desuscribir(EventBus.AliadoCaido,       AlRecibirAliadoCaido);
    }

    void Update()
    {
        // PARTE 3: avanza todos los cooldowns en cada frame
        var tipos = new List<string>(_cooldowns.Keys);
        foreach (var tipo in tipos)
            _cooldowns[tipo] -= Time.deltaTime;

        if (_estado == EstadoAgente.Caido) return;

        // Detección propia del jugador
        if (jugador != null && Vector3.Distance(transform.position, jugador.position) < rangoDeteccion)
            PercibioJugador();

        // Simulación de daño con tecla Q — solo afecta al agente con recibirDañoConQ activo
        if (recibirDañoConQ && UnityEngine.InputSystem.Keyboard.current.qKey.wasPressedThisFrame)
            RecibirDaño(30f);

        ActualizarComportamiento();
    }

    // ── Lógica de publicación ─────────────────────────────────────────────

    void PercibioJugador()
    {
        // PARTE 3: cooldown independiente por tipo de evento
        if (EnCooldown(EventBus.EnemigoDetectado)) return;
        ActivarCooldown(EventBus.EnemigoDetectado);

        Debug.Log($"[{nombreAgente}] Enemigo detectado — publicando evento.");
        EventBus.Publicar(new DatosEvento(
            EventBus.EnemigoDetectado,
            jugador.position,
            gameObject
        ));
        _estado = EstadoAgente.Combate;
    }

    void RecibirDaño(float cantidad)
    {
        vida = Mathf.Max(0f, vida - cantidad);
        Debug.Log($"[{nombreAgente}] Recibió daño. Vida: {vida}");

        if (!EnCooldown(EventBus.SolicitarAyuda))
        {
            ActivarCooldown(EventBus.SolicitarAyuda);
            Debug.Log($"[{nombreAgente}] Solicitando ayuda en {transform.position}.");
            EventBus.Publicar(new DatosEvento(
                EventBus.SolicitarAyuda,
                transform.position,
                gameObject
            ));
        }

        // PARTE 2: publicar AliadoCaido la primera vez que la vida llega a 0
        if (vida <= 0f && _estado != EstadoAgente.Caido)
        {
            _estado = EstadoAgente.Caido;
            Debug.Log($"[{nombreAgente}] Ha caído. Publicando AliadoCaido.");
            EventBus.Publicar(new DatosEvento(
                EventBus.AliadoCaido,
                transform.position,
                gameObject
            ));
        }
    }

    // ── Callbacks de eventos recibidos ────────────────────────────────────

    void AlRecibirAlerta(DatosEvento e)
    {
        // No reaccionamos a nuestros propios eventos
        if (e.emisor == gameObject) return;

        Debug.Log($"[{nombreAgente}] ¡Alerta recibida de {e.emisor?.name}! Posición del enemigo: {e.posicion}");
        _estado = EstadoAgente.Alerta;
    }

    void AlRecibirSolicitudAyuda(DatosEvento e)
    {
        if (e.emisor == gameObject) return;
        // PARTE 2: no acudir a un aliado que ya está caído
        var emisorAgente = e.emisor?.GetComponent<SquadComunicacion>();
        if (emisorAgente != null && emisorAgente._estado == EstadoAgente.Caido) return;

        Debug.Log($"[{nombreAgente}] Acudiendo a ayudar a {e.emisor?.name} en {e.posicion}.");
        _posicionAyuda = e.posicion;
        _emisorAyuda   = e.emisor;
        _estado = EstadoAgente.AcudiendoAyuda;
    }

    void AlObjetivoEliminado(DatosEvento e)
    {
        Debug.Log($"[{nombreAgente}] Objetivo eliminado. Volviendo a patrulla.");
        _estado = EstadoAgente.Patrulla;
    }

    // PARTE 2: un aliado ha caído — si estábamos yendo a ayudarle, cancelamos
    void AlRecibirAliadoCaido(DatosEvento e)
    {
        if (e.emisor == gameObject) return;
        Debug.Log($"[{nombreAgente}] Aliado caído: {e.emisor?.name}.");

        // Si estábamos yendo a ayudar al agente que acaba de caer, volvemos a Alerta
        if (_estado == EstadoAgente.AcudiendoAyuda && _emisorAyuda == e.emisor)
        {
            Debug.Log($"[{nombreAgente}] El aliado al que iba a ayudar ha caído. Volviendo a Alerta.");
            _estado = EstadoAgente.Alerta;
        }
    }

    // ── Comportamiento por estado ─────────────────────────────────────────

    void ActualizarComportamiento()
    {
        switch (_estado)
        {
            case EstadoAgente.AcudiendoAyuda:
                MoverHacia(_posicionAyuda);
                if (Vector3.Distance(transform.position, _posicionAyuda) < 1f)
                    _estado = EstadoAgente.Alerta;
                break;

            case EstadoAgente.Combate:
                if (jugador != null) MoverHacia(jugador.position);
                break;

            // PARTE 2: el agente caído no hace nada
            case EstadoAgente.Caido:
                break;
        }
    }

    void MoverHacia(Vector3 destino)
    {
        Vector3 dir = (destino - transform.position).normalized;
        transform.position += dir * velocidad * Time.deltaTime;
        if (dir.sqrMagnitude > 0.01f)
            transform.forward = Vector3.Lerp(transform.forward, dir, 10f * Time.deltaTime);
    }

    // ── PARTE 3: helpers de cooldown por tipo ─────────────────────────────

    bool EnCooldown(string tipo)
    {
        return _cooldowns.TryGetValue(tipo, out float t) && t > 0f;
    }

    void ActivarCooldown(string tipo)
    {
        _cooldowns[tipo] = cooldownRadio;
    }

    void OnDrawGizmos()
    {
        // Color según estado actual
        Gizmos.color = _estado switch
        {
            EstadoAgente.Combate         => Color.red,
            EstadoAgente.Alerta          => Color.yellow,
            EstadoAgente.AcudiendoAyuda  => Color.green,
            EstadoAgente.Caido           => Color.black,
            _                            => Color.white
        };
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);

        if (_estado == EstadoAgente.AcudiendoAyuda)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _posicionAyuda);
        }
    }
}
