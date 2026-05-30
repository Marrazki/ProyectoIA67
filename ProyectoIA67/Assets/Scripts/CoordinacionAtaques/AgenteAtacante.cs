// ============================================================
//  AgenteAtacante — Unidad 11
// ============================================================
//
//  Agente individual que recibe órdenes del CoordinadorAtaque.
//  Tiene su propia máquina de estados simple:
//
//  Esperar → MoviéndoseAPos → Listo → Atacar → Completado
//                                            ↓
//                                        Retirada  (PARTE 3)
//                                            ↓
//                                         Muerto    (PARTE 3)
//
//  · Esperar       : aguarda la asignación de posición.
//  · MoviéndoseAPos: se desplaza a su punto de ataque.
//  · Listo         : en posición, esperando la señal de ataque.
//  · Atacar        : avanza y ataca al objetivo.
//  · Completado    : ataque terminado, espera reseteo.
//  · Retirada      : vuelve a la posición del coordinador (PARTE 3).
//  · Muerto        : el agente no puede actuar (PARTE 3).
//
//  EJERCICIO:
//  · Observa la diferencia visual entre ataque Simultaneo y Secuencial.
//  · El gizmo dibuja el vector de ataque en rojo durante Atacar.

using UnityEngine;

public class AgenteAtacante : MonoBehaviour
{
    enum Estado { Esperar, MoviéndoseAPos, Listo, Atacar, Completado, Retirada, Muerto }

    [Header("Movimiento")]
    public float velocidad    = 4f;
    public float radioLlegada = 0.5f;
    public float rangoAtaque  = 1.5f;

    // PARTE 3: salud del agente
    [Header("Salud")]
    public float vida      = 100f;
    public float vidaMaxima = 100f;

    [Header("Debug — simulación de daño")]
    [Tooltip("Solo este agente recibe daño al pulsar E. Actívalo en uno a la vez.")]
    public bool recibirDañoConE = false;

    [Header("Debug visual")]
    public Color colorEsperar    = Color.gray;
    public Color colorMovimiento = Color.yellow;
    public Color colorListo      = Color.green;
    public Color colorAtaque     = Color.red;
    public Color colorCompletado = Color.black;
    public Color colorRetirada   = new Color(1f, 0.5f, 0f);
    public Color colorMuerto     = new Color(0.2f, 0.2f, 0.2f);

    Estado    _estado    = Estado.Esperar;
    Vector3   _posAtaque;
    Vector3   _posRetirada;
    Transform _objetivo;
    Renderer  _renderer;

    void Awake() => _renderer = GetComponent<Renderer>();

    void Update()
    {
        // PARTE 3: simula daño con tecla E — solo afecta al agente con recibirDañoConE activo
        if (recibirDañoConE && UnityEngine.InputSystem.Keyboard.current.eKey.isPressed)
            RecibirDaño(20f * Time.deltaTime);

        switch (_estado)
        {
            case Estado.MoviéndoseAPos:
                MoverHacia(_posAtaque);
                if (Vector3.Distance(transform.position, _posAtaque) < radioLlegada)
                {
                    _estado = Estado.Listo;
                    ActualizarColor();
                }
                break;

            case Estado.Atacar:
                if (_objetivo == null) { _estado = Estado.Completado; break; }
                MoverHacia(_objetivo.position);
                Debug.DrawLine(transform.position, _objetivo.position, Color.red);
                if (Vector3.Distance(transform.position, _objetivo.position) < rangoAtaque)
                {
                    Debug.Log($"[{name}] ¡Golpe al objetivo!");
                    _estado = Estado.Completado;
                    ActualizarColor();
                }
                break;

            // PARTE 3: vuelve a la posición del coordinador
            case Estado.Retirada:
                MoverHacia(_posRetirada);
                if (Vector3.Distance(transform.position, _posRetirada) < radioLlegada)
                {
                    Debug.Log($"[{name}] Ha llegado a la zona de retirada.");
                    _estado = Estado.Esperar;
                    ActualizarColor();
                }
                break;
        }
    }

    // ── API pública para el coordinador ──────────────────────────────────

    // Asigna la posición táctica de ataque y el objetivo a golpear
    public void AsignarPosicionAtaque(Vector3 posicion, Transform objetivo)
    {
        if (!EstaVivo()) return;
        _posAtaque = posicion;
        _objetivo  = objetivo;
        _estado    = Estado.MoviéndoseAPos;
        ActualizarColor();
        Debug.Log($"[{name}] Posición asignada: {posicion}");
    }

    // El coordinador ordena iniciar el ataque
    public void OrdenaAtacar()
    {
        if (_estado != Estado.Listo) return;
        Debug.Log($"[{name}] ¡Atacando!");
        _estado = Estado.Atacar;
        ActualizarColor();
    }

    // PARTE 3: el coordinador ordena replegarse a una posición segura
    public void Retirarse(Vector3 posicionSegura)
    {
        if (!EstaVivo()) return;
        Debug.Log($"[{name}] ¡Retirándose!");
        _posRetirada = posicionSegura;
        _estado      = Estado.Retirada;
        ActualizarColor();
    }

    // Devuelve true si el agente está en su posición y listo
    public bool EstaEnPosicion() => _estado == Estado.Listo;

    // Devuelve true cuando el ataque ha concluido
    public bool AtaqueCompletado() => _estado == Estado.Completado;

    // PARTE 3: true si el agente sigue vivo
    public bool EstaVivo() => _estado != Estado.Muerto;

    // Reinicia el agente para el siguiente ciclo
    public void Resetear()
    {
        vida       = vidaMaxima;
        _estado    = Estado.Esperar;
        _posAtaque = Vector3.zero;
        _objetivo  = null;
        ActualizarColor();
    }

    // ── PARTE 3: gestión de daño ──────────────────────────────────────────

    public void RecibirDaño(float cantidad)
    {
        if (!EstaVivo()) return;
        vida = Mathf.Max(0f, vida - cantidad);
        if (vida <= 0f) Morir();
    }

    void Morir()
    {
        Debug.Log($"[{name}] Ha muerto.");
        _estado = Estado.Muerto;
        ActualizarColor();
    }

    // ── Utilidades ────────────────────────────────────────────────────────

    void MoverHacia(Vector3 destino)
    {
        Vector3 dir = (destino - transform.position).normalized;
        transform.position += dir * velocidad * Time.deltaTime;
        if (dir.sqrMagnitude > 0.01f)
            transform.forward = Vector3.Lerp(transform.forward, dir, 10f * Time.deltaTime);
    }

    void ActualizarColor()
    {
        if (_renderer == null) return;
        _renderer.material.color = _estado switch
        {
            Estado.Esperar         => colorEsperar,
            Estado.MoviéndoseAPos  => colorMovimiento,
            Estado.Listo           => colorListo,
            Estado.Atacar          => colorAtaque,
            Estado.Completado      => colorCompletado,
            Estado.Retirada        => colorRetirada,
            Estado.Muerto          => colorMuerto,
            _                      => Color.white
        };
    }

    void OnDrawGizmos()
    {
        if (_estado == Estado.MoviéndoseAPos || _estado == Estado.Listo)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_posAtaque, 0.3f);
            Gizmos.DrawLine(transform.position, _posAtaque);
        }

        // Gizmo del vector de ataque en rojo durante Atacar
        if (_estado == Estado.Atacar && _objetivo != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, _objetivo.position);
            Gizmos.DrawWireSphere(transform.position, rangoAtaque);
        }

        if (_estado == Estado.Retirada)
        {
            Gizmos.color = colorRetirada;
            Gizmos.DrawLine(transform.position, _posRetirada);
            Gizmos.DrawWireSphere(_posRetirada, 0.5f);
        }
    }
}
