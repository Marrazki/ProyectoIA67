// ============================================================
//  EJERCICIO: ROLES EN SQUAD — Unidad 10
// ============================================================
//
//  Cada miembro del squad tiene un rol que define su comportamiento
//  en combate. Los roles se coordinan entre sí a través del EventBus.
//
//  ROLES DISPONIBLES:
//  ──────────────────────────────────────────────────────────────────
//  · Tank    : avanza hacia el enemigo, absorbe daño, protege al equipo.
//              FSM: Patrulla → Avanzar → Aguantar → Retroceder
//  · DPS     : espera a que el tank enganche al enemigo y luego flanquea.
//              FSM: Patrulla → EsperarTank → Flanquear → Atacar
//  · Support : permanece alejado, cura al aliado más herido cercano.
//              FSM: Patrulla → SeguirSquad → Moverse_A_Herido → Curar → Replegarse
//  · Scout   : explora el mapa buscando al jugador, avisa al squad y vuelve.
//              FSM: Explorar → Detectado → Volver (PARTE 3)
//
//  COORDINACIÓN CON EVENTBUS:
//  ──────────────────────────────────────────────────────────────────
//  · Tank   publica "TankEnganchado" cuando llega a rango del enemigo.
//  · DPS    escucha "TankEnganchado" para iniciar el flanqueo.
//  · Support escucha "BajoFuego" para priorizar curas.
//  · Scout  publica "EnemigoDetectado" y vuelve al squad.
//
//  PARTES DEL EJERCICIO
//  ──────────────────────────────────────────────────────────────────
//
//  [PARTE 1 — OBLIGATORIO]
//    Crea 3 GameObjects en Unit10_RolesSquadScene con este componente.
//    Asigna rol Tank a uno, DPS a otro y Support al tercero.
//    Observa cómo el DPS espera al Tank antes de atacar.
//    ¿Qué pasa si hay dos Tanks?
//
//  [PARTE 2 — AMPLIACIÓN ✓ IMPLEMENTADO]
//    Support: se mueve hacia el aliado más herido dentro del rango
//    de curación y restaura su vida. Solo cura cuando está cerca.
//
//  [PARTE 3 — BONUS ✓ IMPLEMENTADO]
//    Rol Scout: explora alejándose del squad, detecta al jugador,
//    publica EnemigoDetectado y vuelve a la posición del squad.
//    Tiene más velocidad pero menos vida.

using System.Collections.Generic;
using UnityEngine;

public class SquadMiembro : MonoBehaviour
{
    // ── Definición de roles ───────────────────────────────────────────────
    public enum Rol { Tank, DPS, Support, Scout }

    // Evento interno para que el DPS sepa cuándo el Tank ha enganchado
    public const string TankEnganchado = "TankEnganchado";

    [Header("Rol e identidad")]
    public Rol    rol          = Rol.Tank;
    public string nombreAgente = "Miembro";

    [Header("Referencias")]
    public Transform jugador;

    [Header("Salud")]
    public float vida;
    public float vidaMaxima = 100f;

    [Header("Detección")]
    public float rangoDeteccion = 6f;
    public float rangoEnganche  = 2.5f;   // distancia a la que el Tank "engancha"
    public float rangoCuracion  = 4f;     // radio máximo para curar
    public float umbralCuracion = 0.6f;   // cura a aliados con vida < 60%
    public float radioCuracionEfectiva = 1.2f; // distancia para que la cura sea efectiva

    [Header("Movimiento")]
    public float velocidad = 3f;

    [Header("Patrulla")]
    public Transform[] waypoints;
    public float distanciaWaypoint = 0.5f;

    [Header("Support")]
    public float curacionPorSeg = 20f;

    [Header("Debug — simulación de daño")]
    [Tooltip("Solo este miembro recibe daño al pulsar Q. Actívalo en uno a la vez.")]
    public bool recibirDañoConQ = false;

    [Header("Scout")]
    [Tooltip("El Scout patrulla con esta velocidad extra.")]
    public float velocidadScout = 5f;
    [Tooltip("El Scout tiene menos vida base.")]
    public float vidaMaximaScout = 60f;
    [Tooltip("Distancia máxima a la que el Scout se aleja del squad.")]
    public float radioExploracion = 12f;

    // ── Estados por rol ───────────────────────────────────────────────────
    enum EstadoTank    { Patrulla, Avanzar, Aguantar, Retroceder }
    enum EstadoDPS     { Patrulla, EsperarTank, Flanquear, Atacar }
    enum EstadoSupport { Patrulla, SeguirSquad, MoverseAHerido, Curar, Replegarse }
    enum EstadoScout   { Explorar, Detectado, Volver }

    EstadoTank    _estadoTank    = EstadoTank.Patrulla;
    EstadoDPS     _estadoDPS     = EstadoDPS.Patrulla;
    EstadoSupport _estadoSupport = EstadoSupport.Patrulla;
    EstadoScout   _estadoScout   = EstadoScout.Explorar;

    int          _waypointIndex;
    bool         _tankHaEnganchado;
    SquadMiembro _objetivoCura;        // aliado al que el Support se dirige a curar
    Vector3      _posRetornoScout;     // posición de referencia para que el Scout vuelva
    bool         _alertaRecibida;      // el Scout avisó de un enemigo cercano
    Vector3      _posEnemigoComunicada; // posición del enemigo reportada por el Scout

    void Awake()
    {
        // El Scout tiene menos vida máxima
        vidaMaxima = (rol == Rol.Scout) ? vidaMaximaScout : vidaMaxima;
        vida = vidaMaxima;
    }

    void OnEnable()
    {
        EventBus.Suscribir(TankEnganchado,           AlTankEnganchado);
        EventBus.Suscribir(EventBus.BajoFuego,       AlBajoFuego);
        EventBus.Suscribir(EventBus.AliadoCaido,     AlAliadoCaido);
        EventBus.Suscribir(EventBus.EnemigoDetectado, AlEnemigoDetectado);
    }

    void OnDisable()
    {
        EventBus.Desuscribir(TankEnganchado,           AlTankEnganchado);
        EventBus.Desuscribir(EventBus.BajoFuego,       AlBajoFuego);
        EventBus.Desuscribir(EventBus.AliadoCaido,     AlAliadoCaido);
        EventBus.Desuscribir(EventBus.EnemigoDetectado, AlEnemigoDetectado);
    }

    void Update()
    {
        // Simula daño con Q — solo afecta al miembro con recibirDañoConQ activo
        if (recibirDañoConQ && UnityEngine.InputSystem.Keyboard.current.qKey.isPressed)
            vida = Mathf.Max(0f, vida - 10f * Time.deltaTime);

        switch (rol)
        {
            case Rol.Tank:    ActualizarTank();    break;
            case Rol.DPS:     ActualizarDPS();     break;
            case Rol.Support: ActualizarSupport(); break;
            case Rol.Scout:   ActualizarScout();   break;
        }
    }

    // ── Comportamiento TANK ───────────────────────────────────────────────

    void ActualizarTank()
    {
        bool veJugador = VerJugador();

        switch (_estadoTank)
        {
            case EstadoTank.Patrulla:
                GetComponent<Renderer>().material.color = Color.blue;
                Patrullar();
                if (veJugador) _estadoTank = EstadoTank.Avanzar;
                break;

            case EstadoTank.Avanzar:
                GetComponent<Renderer>().material.color = new Color(0.2f, 0.4f, 1f);
                // Si ve al jugador va directo; si no, va a la última posición comunicada
                Vector3 destinoTank = veJugador ? jugador.position : _posEnemigoComunicada;
                MoverHacia(destinoTank);
                float dist = Vector3.Distance(transform.position, destinoTank);
                if (veJugador && dist < rangoEnganche)
                {
                    // Tank ha llegado a rango — avisa al squad
                    Debug.Log($"[{nombreAgente}] Tank enganchó al enemigo.");
                    EventBus.Publicar(new DatosEvento(TankEnganchado, jugador.position, gameObject));
                    _alertaRecibida = false;
                    _estadoTank = EstadoTank.Aguantar;
                }
                // Vuelve a patrullar solo si perdió al jugador Y ya no tiene alerta activa
                if (!veJugador && !_alertaRecibida) _estadoTank = EstadoTank.Patrulla;
                // Cuando llega a la posición comunicada sin ver al jugador, cancela la alerta
                if (!veJugador && _alertaRecibida && dist < 1.5f)
                {
                    _alertaRecibida = false;
                    _estadoTank = EstadoTank.Patrulla;
                }
                break;

            case EstadoTank.Aguantar:
                // Se queda parado absorbiendo daño
                GetComponent<Renderer>().material.color = Color.red;
                if (vida < vidaMaxima * 0.3f) _estadoTank = EstadoTank.Retroceder;
                if (!veJugador) _estadoTank = EstadoTank.Patrulla;
                break;

            case EstadoTank.Retroceder:
                GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 0f);
                if (jugador != null)
                {
                    Vector3 huida = (transform.position - jugador.position).normalized;
                    transform.position += huida * velocidad * Time.deltaTime;
                }
                if (vida > vidaMaxima * 0.6f) _estadoTank = EstadoTank.Avanzar;
                break;
        }
    }

    // ── Comportamiento DPS ────────────────────────────────────────────────

    void ActualizarDPS()
    {
        switch (_estadoDPS)
        {
            case EstadoDPS.Patrulla:
                GetComponent<Renderer>().material.color = Color.green;
                Patrullar();
                if (VerJugador()) _estadoDPS = EstadoDPS.EsperarTank;
                break;

            case EstadoDPS.EsperarTank:
                // Se acerca a la posición del enemigo pero sin adelantarse al Tank
                GetComponent<Renderer>().material.color = Color.yellow;
                if (_alertaRecibida)
                    MoverHacia(_posEnemigoComunicada - transform.forward * 3f);
                if (_tankHaEnganchado) _estadoDPS = EstadoDPS.Flanquear;
                // Si pierde el contexto sin que el Tank haya enganchado, vuelve a patrulla
                if (!VerJugador() && !_alertaRecibida && !_tankHaEnganchado)
                    _estadoDPS = EstadoDPS.Patrulla;
                break;

            case EstadoDPS.Flanquear:
                GetComponent<Renderer>().material.color = new Color(0.8f, 1f, 0f);
                if (jugador != null)
                {
                    // Flanquea por el lado derecho del jugador
                    Vector3 flanco = jugador.position + jugador.right * 3f;
                    MoverHacia(flanco);
                    if (Vector3.Distance(transform.position, flanco) < 1f)
                        _estadoDPS = EstadoDPS.Atacar;
                }
                break;

            case EstadoDPS.Atacar:
                GetComponent<Renderer>().material.color = Color.red;
                if (jugador != null)
                {
                    Debug.DrawLine(transform.position, jugador.position, Color.red);
                    MoverHacia(jugador.position);
                }
                if (!VerJugador()) _estadoDPS = EstadoDPS.Patrulla;
                break;
        }
    }

    // ── Comportamiento SUPPORT ────────────────────────────────────────────

    void ActualizarSupport()
    {
        switch (_estadoSupport)
        {
            case EstadoSupport.Patrulla:
                GetComponent<Renderer>().material.color = Color.white;
                Patrullar();
                if (HayAliadoQueNecesitaCura()) _estadoSupport = EstadoSupport.MoverseAHerido;
                break;

            case EstadoSupport.SeguirSquad:
                GetComponent<Renderer>().material.color = new Color(0.8f, 0.8f, 1f);
                // Se mantiene detrás del Tank
                SquadMiembro tank = BuscarPorRol(Rol.Tank);
                if (tank != null)
                {
                    Vector3 posSegura = tank.transform.position - tank.transform.forward * 4f;
                    MoverHacia(posSegura);
                }
                if (HayAliadoQueNecesitaCura()) _estadoSupport = EstadoSupport.MoverseAHerido;
                if (VerJugador()) _estadoSupport = EstadoSupport.Replegarse;
                break;

            // PARTE 2: se mueve hacia el aliado más herido antes de curar
            case EstadoSupport.MoverseAHerido:
                GetComponent<Renderer>().material.color = new Color(0f, 0.8f, 0.8f);
                _objetivoCura = BuscarAliadoMasHerido();
                if (_objetivoCura == null)
                {
                    _estadoSupport = EstadoSupport.SeguirSquad;
                    break;
                }
                MoverHacia(_objetivoCura.transform.position);
                // Cuando está lo suficientemente cerca, pasa a curar activamente
                if (Vector3.Distance(transform.position, _objetivoCura.transform.position) < radioCuracionEfectiva)
                    _estadoSupport = EstadoSupport.Curar;
                break;

            case EstadoSupport.Curar:
                GetComponent<Renderer>().material.color = Color.cyan;
                if (_objetivoCura == null || _objetivoCura.vida >= _objetivoCura.vidaMaxima * umbralCuracion)
                {
                    // El objetivo ya está curado o desapareció
                    _objetivoCura  = null;
                    _estadoSupport = HayAliadoQueNecesitaCura()
                        ? EstadoSupport.MoverseAHerido
                        : EstadoSupport.SeguirSquad;
                    break;
                }
                // Cura activa: solo si sigue cerca
                if (Vector3.Distance(transform.position, _objetivoCura.transform.position) < radioCuracionEfectiva)
                {
                    _objetivoCura.vida = Mathf.Min(
                        _objetivoCura.vidaMaxima,
                        _objetivoCura.vida + curacionPorSeg * Time.deltaTime
                    );
                    Debug.DrawLine(transform.position, _objetivoCura.transform.position, Color.cyan);
                }
                else
                {
                    // El objetivo se movió — volver a acercarse
                    _estadoSupport = EstadoSupport.MoverseAHerido;
                }
                break;

            case EstadoSupport.Replegarse:
                GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 0.5f);
                if (jugador != null)
                {
                    Vector3 huida = (transform.position - jugador.position).normalized;
                    transform.position += huida * velocidad * 1.5f * Time.deltaTime;
                }
                if (!VerJugador()) _estadoSupport = EstadoSupport.SeguirSquad;
                break;
        }
    }

    // ── Comportamiento SCOUT (PARTE 3) ────────────────────────────────────

    void ActualizarScout()
    {
        switch (_estadoScout)
        {
            case EstadoScout.Explorar:
                GetComponent<Renderer>().material.color = Color.magenta;
                // Patrulla los waypoints a mayor velocidad que los demás roles
                PatrullarConVelocidad(velocidadScout);

                if (VerJugador())
                {
                    // Detectó al jugador — avisa al squad por el EventBus
                    Debug.Log($"[{nombreAgente}] ¡Scout detectó al enemigo! Publicando alerta.");
                    EventBus.Publicar(new DatosEvento(
                        EventBus.EnemigoDetectado,
                        jugador.position,
                        gameObject
                    ));
                    // Guarda dónde está el squad para volver
                    SquadMiembro tank = BuscarPorRol(Rol.Tank);
                    _posRetornoScout = tank != null
                        ? tank.transform.position
                        : transform.position;
                    _estadoScout = EstadoScout.Detectado;
                }
                break;

            case EstadoScout.Detectado:
                // Mantiene la alerta visible mientras siga viendo al jugador
                GetComponent<Renderer>().material.color = Color.red;
                Debug.DrawLine(transform.position, jugador.position, Color.magenta);
                if (!VerJugador())
                    _estadoScout = EstadoScout.Volver;
                break;

            case EstadoScout.Volver:
                // Regresa a la posición del squad
                GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 1f);
                MoverHacia(_posRetornoScout);
                if (Vector3.Distance(transform.position, _posRetornoScout) < 2f)
                {
                    Debug.Log($"[{nombreAgente}] Scout ha vuelto al squad.");
                    _estadoScout = EstadoScout.Explorar;
                }
                break;
        }
    }

    // ── Callbacks de eventos ──────────────────────────────────────────────

    void AlTankEnganchado(DatosEvento e)
    {
        if (rol == Rol.DPS)
        {
            Debug.Log($"[{nombreAgente}] Tank enganchó. ¡Iniciando flanqueo!");
            _tankHaEnganchado = true;
        }
    }

    void AlBajoFuego(DatosEvento e)
    {
        if (rol == Rol.Support)
        {
            Debug.Log($"[{nombreAgente}] Aliado bajo fuego. Priorizando cura.");
            _estadoSupport = EstadoSupport.MoverseAHerido;
        }
    }

    void AlAliadoCaido(DatosEvento e)
    {
        Debug.Log($"[{nombreAgente}] Un aliado ha caído. Ajustando estrategia.");

        // Si el Support estaba curando al aliado caído, busca otro objetivo
        if (rol == Rol.Support && _objetivoCura != null &&
            e.emisor == _objetivoCura.gameObject)
        {
            _objetivoCura  = null;
            _estadoSupport = EstadoSupport.SeguirSquad;
        }
    }

    void AlEnemigoDetectado(DatosEvento e)
    {
        if (e.emisor == gameObject) return;

        // Guarda la posición comunicada para moverse hacia ella aunque no se vea al jugador
        _alertaRecibida       = true;
        _posEnemigoComunicada = e.posicion;

        switch (rol)
        {
            case Rol.Tank:
                Debug.Log($"[{nombreAgente}] Tank recibió alerta del Scout. Avanzando.");
                if (_estadoTank == EstadoTank.Patrulla)
                    _estadoTank = EstadoTank.Avanzar;
                break;

            case Rol.DPS:
                Debug.Log($"[{nombreAgente}] DPS recibió alerta del Scout. Esperando al Tank.");
                if (_estadoDPS == EstadoDPS.Patrulla)
                    _estadoDPS = EstadoDPS.EsperarTank;
                break;

            case Rol.Scout:
                Debug.Log($"[{nombreAgente}] Scout recibió alerta de {e.emisor?.name}.");
                break;
        }
    }

    // ── Utilidades ────────────────────────────────────────────────────────

    bool VerJugador()
    {
        if (jugador == null) return false;
        return Vector3.Distance(transform.position, jugador.position) < rangoDeteccion;
    }

    bool HayAliadoQueNecesitaCura()
    {
        return BuscarAliadoMasHerido() != null;
    }

    // PARTE 2: devuelve el aliado más herido dentro del rango de curación
    SquadMiembro BuscarAliadoMasHerido()
    {
        SquadMiembro masHerido    = null;
        float        menorPorcentaje = umbralCuracion;

        foreach (var m in FindObjectsByType<SquadMiembro>(FindObjectsSortMode.None))
        {
            if (m == this) continue;
            float distancia   = Vector3.Distance(transform.position, m.transform.position);
            float porcentaje  = m.vida / m.vidaMaxima;
            if (distancia < rangoCuracion && porcentaje < menorPorcentaje)
            {
                menorPorcentaje = porcentaje;
                masHerido       = m;
            }
        }
        return masHerido;
    }

    SquadMiembro BuscarPorRol(Rol rolBuscado)
    {
        foreach (var m in FindObjectsByType<SquadMiembro>(FindObjectsSortMode.None))
            if (m != this && m.rol == rolBuscado) return m;
        return null;
    }

    void Patrullar() => PatrullarConVelocidad(velocidad);

    void PatrullarConVelocidad(float vel)
    {
        if (waypoints == null || waypoints.Length == 0) return;
        Transform destino = waypoints[_waypointIndex];
        Vector3 dir = (destino.position - transform.position).normalized;
        transform.position += dir * vel * Time.deltaTime;
        if (dir.sqrMagnitude > 0.01f)
            transform.forward = Vector3.Lerp(transform.forward, dir, 10f * Time.deltaTime);
        if (Vector3.Distance(transform.position, destino.position) < distanciaWaypoint)
            _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
    }

    void MoverHacia(Vector3 destino)
    {
        Vector3 dir = (destino - transform.position).normalized;
        transform.position += dir * velocidad * Time.deltaTime;
        if (dir.sqrMagnitude > 0.01f)
            transform.forward = Vector3.Lerp(transform.forward, dir, 10f * Time.deltaTime);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = rol switch
        {
            Rol.Tank    => Color.blue,
            Rol.DPS     => Color.green,
            Rol.Support => Color.white,
            Rol.Scout   => Color.magenta,
            _           => Color.gray
        };
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);

        if (rol == Rol.Support)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, rangoCuracion);
            // Radio de curación efectiva
            Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, radioCuracionEfectiva);

            if (_objetivoCura != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, _objetivoCura.transform.position);
            }
        }

        if (rol == Rol.Scout)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, radioExploracion);
        }
    }
}
