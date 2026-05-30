// ============================================================
//  EventBus — Unidad 9: Comunicación basada en eventos
// ============================================================
//
//  Sistema de mensajería desacoplado (Publish/Subscribe).
//  Los agentes publican eventos sin saber quién los escucha.
//  Los suscriptores reaccionan sin conocer al emisor.
//
//  VENTAJA frente a referencias directas:
//  · Añadir o quitar agentes no requiere modificar otros scripts.
//  · Los emisores y receptores son completamente independientes.
//
//  EVENTOS DISPONIBLES:
//  ─────────────────────────────────────────────────────────────
//  · EnemigoDetectado  : un agente ha visto al jugador.
//  · BajoFuego         : un agente está recibiendo daño.
//  · AliadoCaido       : un miembro del squad ha muerto.
//  · ObjetivoEliminado : el jugador ha sido eliminado.
//  · SolicitarAyuda    : un agente pide apoyo en su posición.

using System;
using System.Collections.Generic;
using UnityEngine;

// Datos que viajan con cada evento
public class DatosEvento
{
    public string tipo;           // nombre del evento
    public Vector3 posicion;      // posición del emisor o del evento
    public GameObject emisor;     // quién lo envió (puede ser null)
    public object datos;          // payload extra opcional

    public DatosEvento(string tipo, Vector3 posicion, GameObject emisor = null, object datos = null)
    {
        this.tipo     = tipo;
        this.posicion = posicion;
        this.emisor   = emisor;
        this.datos    = datos;
    }
}

// Bus de eventos global y estático
public static class EventBus
{
    // ── Constantes de tipo de evento ─────────────────────────────────────
    public const string EnemigoDetectado  = "EnemigoDetectado";
    public const string BajoFuego         = "BajoFuego";
    public const string AliadoCaido       = "AliadoCaido";
    public const string ObjetivoEliminado = "ObjetivoEliminado";
    public const string SolicitarAyuda    = "SolicitarAyuda";

    // ── Registro de suscriptores por tipo ────────────────────────────────
    static readonly Dictionary<string, List<Action<DatosEvento>>> _suscriptores =
        new Dictionary<string, List<Action<DatosEvento>>>();

    // Registra una función para escuchar un tipo de evento
    public static void Suscribir(string tipo, Action<DatosEvento> callback)
    {
        if (!_suscriptores.ContainsKey(tipo))
            _suscriptores[tipo] = new List<Action<DatosEvento>>();
        _suscriptores[tipo].Add(callback);
    }

    // Cancela la suscripción (importante al destruir el objeto)
    public static void Desuscribir(string tipo, Action<DatosEvento> callback)
    {
        if (_suscriptores.TryGetValue(tipo, out var lista))
            lista.Remove(callback);
    }

    // Emite un evento a todos los suscriptores registrados
    public static void Publicar(DatosEvento evento)
    {
        if (!_suscriptores.TryGetValue(evento.tipo, out var lista)) return;

        // Copia la lista para evitar problemas si un callback se desuscribe durante la iteración
        var copia = new List<Action<DatosEvento>>(lista);
        foreach (var cb in copia)
            cb?.Invoke(evento);
    }

    // Limpia todos los suscriptores (útil al cambiar de escena)
    public static void LimpiarTodo() => _suscriptores.Clear();
}
