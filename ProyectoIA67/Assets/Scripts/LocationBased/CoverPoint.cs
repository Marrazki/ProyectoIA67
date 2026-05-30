// ============================================================
//  COVER POINT — Unidad 7
// ============================================================
//
//  Marca una posición de cobertura en la escena.
//  El CoverSystem los escanea automáticamente y elige el mejor
//  cuando un agente necesita cubrirse de una amenaza.
//
//  ANATOMÍA DE UN COVER POINT:
//  ──────────────────────────────────────────────────────────────────────
//  · Posición: el Transform del GameObject → donde se para el agente.
//  · Dirección (transform.forward): hacia dónde mira el agente en cobertura.
//    Debe apuntar hacia el campo (hacia la amenaza potencial).
//  · Estado: libre u ocupado (solo un agente a la vez).
//
//  SETUP EN UNITY:
//  ──────────────────────────────────────────────────────────────────────
//  1. Crea GameObjects vacíos DETRÁS de obstáculos (paredes, cajas, etc.).
//  2. Rota el Transform para que la flecha de Gizmos (transform.forward)
//     apunte hacia el área de combate (hacia donde mirará el agente).
//  3. Añade este componente. El CoverSystem los detecta en Start().
//
//  VISUALIZACIÓN: la flecha verde indica la dirección de cobertura.
//  Verde = libre, Rojo = ocupado.

using UnityEngine;

[SelectionBase]
public class CoverPoint : MonoBehaviour
{
    public bool IsOccupied { get; private set; }
    public MonoBehaviour Occupant { get; private set; }

    public void Occupy(MonoBehaviour occupant)
    {
        IsOccupied = true;
        Occupant = occupant;
    }

    public void Vacate()
    {
        IsOccupied = false;
        Occupant = null;
    }

    // ¿Este punto protege de una amenaza en threatPos?
    // Protege si la amenaza está en el LADO OPUESTO a transform.forward
    // (es decir, el obstáculo está entre el agente y la amenaza).
    public bool ProtectsFrom(Vector3 threatPos)
    {
        Vector3 toThreat = (threatPos - transform.position).normalized;
        // dot < 0.2 → la amenaza está detrás del punto → el obstáculo cubre
        return Vector3.Dot(toThreat, transform.forward) < 0.2f;
    }

    // Puntuación de calidad del punto para un agente y amenaza dados.
    //   Mayor puntuación = mejor cover.
    //   Premia distancia a la amenaza (más lejos = más seguro).
    //   Penaliza distancia al agente (más lejos = más caro de alcanzar).
    public float ScoreFor(Vector3 agentPos, Vector3 threatPos)
    {
        if (!ProtectsFrom(threatPos)) return float.MinValue;
        float distToAgent  = Vector3.Distance(agentPos, transform.position);
        float distToThreat = Vector3.Distance(transform.position, threatPos);
        return distToThreat - distToAgent * 1.5f;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = IsOccupied ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        // Flecha → dirección en que mira el agente cuando está en cobertura.
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.7f);
        Gizmos.DrawWireSphere(transform.position + transform.forward * 0.7f, 0.08f);
    }
}
