// ============================================================
//  EJERCICIO: STEERING AGENT — Unidad 6
// ============================================================
//
//  SteeringAgent aplica uno de los behaviors de SteeringBehaviors.cs
//  para mover un agente de forma fluida. La velocidad actual se
//  interpola hacia la velocidad deseada (steering suave).
//
//  EXPERIMENTO SEEK vs ARRIVE:
//  ──────────────────────────────────────────────────────────────────────
//  1. Añade este componente a un GameObject con Renderer.
//  2. Asigna un Transform como "target".
//  3. Cambia Mode entre Seek y Arrive en el Inspector.
//  4. Observa: Seek llega y oscila; Arrive frena y para.
//
//  TODO [PARTE 1 — OBLIGATORIO]:
//    Cambia Mode a Seek. Observa el overshooting.
//    Luego cambia a Arrive. ¿En qué valor de slowRadius empieza a frenar?
//
//  TODO [PARTE 2 — AMPLIACIÓN]:
//    Implementa el behavior "Wander" (movimiento errático natural):
//    · Proyecta un círculo frente al agente (wanderCircleDistance adelante).
//    · Elige un punto en la circunferencia según un ángulo que varía suavemente.
//    · Aplica Seek hacia ese punto.
//    Parámetros necesarios: wanderCircleDistance, wanderCircleRadius, wanderAngle.
//
//  TODO [PARTE 3 — BONUS]:
//    Combina Flee con Arrive: el agente huye mientras hay amenaza (Flee),
//    pero llega suavemente a un destino de evacuación (Arrive).
//    Pondera ambas fuerzas: flee * 0.7f + arrive * 0.3f.

using UnityEngine;

public class SteeringAgent : MonoBehaviour
{
    public enum SteeringMode { Seek, Flee, Arrive }

    [Header("Target")]
    public Transform target;

    [Header("Behavior")]
    public SteeringMode mode = SteeringMode.Arrive;

    [Header("Velocidad")]
    public float maxSpeed = 4f;
    [Tooltip("Qué tan rápido cambia la velocidad actual hacia la deseada.")]
    public float steeringForce = 8f;

    [Header("Arrive")]
    [Tooltip("Radio exterior: el agente empieza a frenar dentro de este radio.")]
    public float slowRadius = 3f;
    [Tooltip("Radio interior: el agente se detiene dentro de este radio.")]
    public float targetRadius = 0.2f;

    Vector3 _velocity;
    public Vector3 Velocity => _velocity;

    void Update()
    {
        if (target == null) return;

        Vector3 desiredVelocity = ComputeDesiredVelocity();

        // Interpolación suave hacia la velocidad deseada (steering force).
        _velocity = Vector3.MoveTowards(_velocity, desiredVelocity, steeringForce * Time.deltaTime);

        transform.position += _velocity * Time.deltaTime;

        if (_velocity.sqrMagnitude > 0.01f)
            transform.forward = Vector3.Lerp(
                transform.forward, _velocity.normalized, 10f * Time.deltaTime);
    }

    Vector3 ComputeDesiredVelocity() => mode switch
    {
        SteeringMode.Seek   => SteeringBehaviors.Seek(transform.position, target.position, maxSpeed),
        SteeringMode.Flee   => SteeringBehaviors.Flee(transform.position, target.position, maxSpeed),
        SteeringMode.Arrive => SteeringBehaviors.Arrive(transform.position, target.position,
                                                         maxSpeed, slowRadius, targetRadius),
        _ => Vector3.zero
    };

    void OnDrawGizmos()
    {
        if (target == null) return;

        if (mode == SteeringMode.Arrive)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
            Gizmos.DrawWireSphere(target.position, slowRadius);
            Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
            Gizmos.DrawWireSphere(target.position, targetRadius);
        }

        if (Application.isPlaying && _velocity.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + _velocity);
        }
    }
}
