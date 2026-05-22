// ============================================================
//  TACTICAL POINT — Unidad 7
// ============================================================
//
//  Un TacticalPoint marca una posición estratégica pre-definida en el mapa.
//  A diferencia de CoverPoint (que protege de una amenaza dinámica),
//  TacticalPoint tiene un rol táctico fijo asignado por el diseñador.
//
//  TIPOS DISPONIBLES:
//  ──────────────────────────────────────────────────────────────────────
//  · Flank    → flanquea al objetivo (llega por el lado).
//  · Elevated → posición elevada (ventaja de altura y visión).
//  · Support  → posición de apoyo / reagrupamiento de aliados.
//  · Ambush   → posición de emboscada (espera al objetivo).
//
//  SETUP EN UNITY:
//  ──────────────────────────────────────────────────────────────────────
//  1. Crea GameObjects vacíos en posiciones estratégicas del mapa.
//  2. Elige el Type en el Inspector.
//  3. El sistema táctico (TacticalSystem en Enemigo_Cover_BT.cs) los
//     detecta automáticamente.
//
//  COLORES EN GIZMOS:
//    Azul = Flank | Amarillo = Elevated | Verde = Support | Rojo = Ambush.

using UnityEngine;

public class TacticalPoint : MonoBehaviour
{
    public enum PointType { Flank, Elevated, Support, Ambush }

    [Header("Configuración")]
    public PointType type = PointType.Flank;
    [Tooltip("Peso de preferencia al elegir entre varios puntos del mismo tipo.")]
    public float weight = 1f;

    public bool IsOccupied { get; private set; }
    public MonoBehaviour Occupant { get; private set; }

    public void Occupy(MonoBehaviour occupant) { IsOccupied = true;  Occupant = occupant; }
    public void Vacate()                        { IsOccupied = false; Occupant = null; }

    // ¿Es buen punto de flanqueo para un agente que ataca a targetPos?
    // Un buen flanco está al LADO del objetivo (ángulo ≈ 45-135° respecto
    // al eje agente→objetivo). Dot ≈ 0 = perpendicular = flanqueo perfecto.
    public bool IsGoodFlankFor(Vector3 agentPos, Vector3 targetPos)
    {
        Vector3 agentToTarget = (targetPos - agentPos).normalized;
        Vector3 agentToPoint  = (transform.position - agentPos).normalized;
        float dot = Vector3.Dot(agentToTarget, agentToPoint);
        return dot is > -0.6f and < 0.6f; // ± 53° respecto al eje principal
    }

    // Puntuación táctica (mayor = más prioritario).
    // Aplica el weight del diseñador y penaliza puntos muy lejanos.
    public float ScoreFor(Vector3 agentPos)
    {
        float dist = Vector3.Distance(agentPos, transform.position);
        return weight / (1f + dist * 0.2f);
    }

    void OnDrawGizmos()
    {
        Color color = type switch
        {
            PointType.Flank    => Color.blue,
            PointType.Elevated => Color.yellow,
            PointType.Support  => Color.green,
            PointType.Ambush   => Color.red,
            _                  => Color.white
        };

        Gizmos.color = IsOccupied ? new Color(0.5f, 0.5f, 0.5f) : color;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1.3f,
            $"{type}{(IsOccupied ? " [X]" : "")}");
#endif
    }
}
