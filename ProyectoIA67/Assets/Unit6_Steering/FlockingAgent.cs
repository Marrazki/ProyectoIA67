// ============================================================
//  EJERCICIO: FLOCKING (BOIDS) — Unidad 6
// ============================================================
//
//  Flocking simula el comportamiento de grupos: bandadas, cardúmenes,
//  manadas. Formalizado por Craig Reynolds (1986) con tres reglas:
//
//  1. COHESIÓN    → moverse hacia el centro de masa del grupo.
//  2. SEPARACIÓN  → alejarse de los vecinos demasiado cercanos.
//  3. ALINEACIÓN  → igualar la dirección de movimiento del grupo.
//
//  La combinación de estas fuerzas produce comportamiento emergente:
//  patrones complejos sin ningún control centralizado.
//
//  SETUP EN UNITY:
//  ──────────────────────────────────────────────────────────────────────
//  1. Crea un Prefab con este componente + MeshRenderer (cápsula o esfera).
//  2. Instancia 10-20 copias en la escena.
//  3. Ajusta los pesos en el Inspector para observar diferentes emergencias:
//     · separationWeight alto → todos se dispersan, no forman grupo.
//     · cohesionWeight alto   → colapsan en un punto.
//     · alignmentWeight alto  → se mueven en formación rígida.
//
//  LEER ANTES DE IMPLEMENTAR:
//  ──────────────────────────────────────────────────────────────────────
//  Los tres métodos (ComputeCohesion, ComputeSeparation, ComputeAlignment)
//  tienen su lógica implementada. Tu tarea en PARTE 1 es ENTENDER el código
//  y responder las preguntas de los TODO.
//
// ============================================================
//  PARTES DEL EJERCICIO
// ============================================================
//
//  [PARTE 1 — OBLIGATORIO]
//    Lee las implementaciones de Cohesión, Separación y Alineación.
//    Responde:
//      a) ¿Por qué ComputeSeparation divide por la distancia (1/dist)?
//      b) ¿Qué pasa si separationRadius >= neighborRadius?
//      c) ¿Por qué ComputeAlignment usa Velocity en vez de transform.forward?
//
//  [PARTE 2 — AMPLIACIÓN]
//    Añade un "líder" (leaderTarget): los boids usan Arrive hacia ese
//    punto. Descomenta el bloque de leaderTarget en Update() y ajusta
//    el peso leaderWeight para ver cómo el grupo sigue al líder.
//
//  [PARTE 3 — BONUS]
//    Añade obstacle avoidance con Physics.SphereCast:
//    · Lanza un SphereCast en la dirección de movimiento del agente.
//    · Si detecta un obstáculo (LayerMask = "Obstacle"), suma una
//      fuerza perpendicular al normal del impacto.
//    ¿Cómo afecta la distancia de detección al comportamiento del grupo?

using System.Collections.Generic;
using UnityEngine;

public class FlockingAgent : MonoBehaviour
{
    [Header("Radios")]
    public float neighborRadius = 3f;
    [Tooltip("Distancia mínima entre agentes antes de aplicar separación.")]
    public float separationRadius = 1.2f;

    [Header("Velocidad")]
    public float maxSpeed = 4f;
    public float steeringForce = 6f;

    [Header("Pesos de flocking")]
    public float cohesionWeight = 1f;
    public float separationWeight = 1.8f;
    public float alignmentWeight = 1f;

    [Header("Líder (Parte 2)")]
    public Transform leaderTarget;
    public float leaderWeight = 1.2f;

    Vector3 _velocity;
    public Vector3 Velocity => _velocity;

    static FlockingAgent[] _allAgents;

    void Start()
    {
        _velocity = Random.insideUnitSphere.normalized * maxSpeed * 0.5f;
        _velocity.y = 0f;
        _allAgents = null; // fuerza re-escaneo si se añaden boids en runtime
    }

    void Update()
    {
        var neighbors = FindNeighbors();

        Vector3 cohesion   = ComputeCohesion(neighbors)   * cohesionWeight;
        Vector3 separation = ComputeSeparation(neighbors)  * separationWeight;
        Vector3 alignment  = ComputeAlignment(neighbors)   * alignmentWeight;

        Vector3 steering = cohesion + separation + alignment;

        // [PARTE 2]: descomenta para activar la fuerza de líder.
        if (leaderTarget != null)
        {
            Vector3 toLeader = SteeringBehaviors.Arrive(
                transform.position, leaderTarget.position,
                maxSpeed, slowRadius: 5f, targetRadius: 1f);
            steering += toLeader * leaderWeight;
        }

        _velocity += steering * Time.deltaTime;
        _velocity.y = 0f;
        _velocity = Vector3.ClampMagnitude(_velocity, maxSpeed);

        transform.position += _velocity * Time.deltaTime;

        if (_velocity.sqrMagnitude > 0.01f)
            transform.forward = Vector3.Lerp(
                transform.forward, _velocity.normalized, 10f * Time.deltaTime);
    }

    // ── Búsqueda de vecinos ───────────────────────────────────────────────

    List<FlockingAgent> FindNeighbors()
    {
        if (_allAgents == null || _allAgents.Length == 0)
            _allAgents = FindObjectsByType<FlockingAgent>(FindObjectsSortMode.None);

        var neighbors = new List<FlockingAgent>();
        foreach (var agent in _allAgents)
        {
            if (agent == this) continue;
            if (Vector3.Distance(transform.position, agent.transform.position) < neighborRadius)
                neighbors.Add(agent);
        }
        return neighbors;
    }

    // ── Reglas de Boids ───────────────────────────────────────────────────

    // COHESIÓN: mueve el agente hacia el centro de masa del vecindario.
    // ¿Por qué se normaliza el resultado? → para que solo indique DIRECCIÓN,
    // no fuerza absoluta. El peso (cohesionWeight) controla la magnitud.
    Vector3 ComputeCohesion(List<FlockingAgent> neighbors)
    {
        if (neighbors.Count == 0) return Vector3.zero;

        Vector3 centerOfMass = Vector3.zero;
        foreach (var n in neighbors)
            centerOfMass += n.transform.position;
        centerOfMass /= neighbors.Count;

        return (centerOfMass - transform.position).normalized;
    }

    // SEPARACIÓN: empuja el agente lejos de vecinos demasiado próximos.
    // La ponderación (1/dist) amplifica la repulsión cuando el vecino es MUY cercano.
    // TODO [PARTE 1a]: ¿qué ocurre si eliminas la división por dist y usas solo .normalized?
    Vector3 ComputeSeparation(List<FlockingAgent> neighbors)
    {
        Vector3 steer = Vector3.zero;
        int count = 0;

        foreach (var n in neighbors)
        {
            float dist = Vector3.Distance(transform.position, n.transform.position);
            if (dist < separationRadius && dist > 0.001f)
            {
                steer += (transform.position - n.transform.position).normalized / dist;
                count++;
            }
        }

        return count > 0 ? (steer / count).normalized : Vector3.zero;
    }

    // ALINEACIÓN: iguala la velocidad/dirección del grupo.
    // Usa Velocity (no transform.forward) porque refleja la dirección REAL de movimiento.
    // TODO [PARTE 1c]: ¿en qué caso transform.forward y Velocity serían iguales?
    Vector3 ComputeAlignment(List<FlockingAgent> neighbors)
    {
        if (neighbors.Count == 0) return transform.forward;

        Vector3 avgVelocity = Vector3.zero;
        foreach (var n in neighbors)
            avgVelocity += n.Velocity;
        avgVelocity /= neighbors.Count;

        return avgVelocity.sqrMagnitude > 0.001f ? avgVelocity.normalized : transform.forward;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.12f);
        Gizmos.DrawWireSphere(transform.position, neighborRadius);

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        if (Application.isPlaying && _velocity.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + _velocity.normalized * 1.2f);
        }
    }
}
