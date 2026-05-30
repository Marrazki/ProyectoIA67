// ============================================================
//  SISTEMA DE COBERTURA — Unidad 7
// ============================================================
//
//  CoverSystem gestiona todos los CoverPoints de la escena y busca
//  el mejor punto dado la posición del agente y la amenaza.
//
//  Patrón de uso desde un BT (ver Enemigo_Cover_BT.cs):
//
//    // Condición: ¿hay cover disponible?
//    bool HasCover() {
//        _currentCover = CoverSystem.Instance.FindBestCover(
//            transform.position, jugador.position);
//        return _currentCover != null;
//    }
//
//    // Acción: mover al agente hacia el cover encontrado.
//    NodeStatus MoveToCover() {
//        _currentCover.Occupy(this);
//        ... Arrive hacia _currentCover.transform.position ...
//    }
//
//  SETUP:
//    Añade este componente a un GameObject vacío en la escena (singleton).
//    Los CoverPoints se detectan automáticamente en Awake().
//
//  TODO [EJERCICIO]:
//    Implementa FindNearestCover(): devuelve el cover más cercano al
//    agente sin importar si protege de la amenaza (útil para "buscar
//    cualquier cover rápidamente cuando la vida es crítica").

using UnityEngine;

public class CoverSystem : MonoBehaviour
{
    public static CoverSystem Instance { get; private set; }

    CoverPoint[] _coverPoints;

    void Awake()
    {
        Instance = this;
        _coverPoints = FindObjectsByType<CoverPoint>(FindObjectsSortMode.None);
        Debug.Log($"[CoverSystem] {_coverPoints.Length} cover points registrados.");
    }

    // Devuelve el mejor CoverPoint disponible que proteja de threatPos,
    // o null si no existe ninguno.
    public CoverPoint FindBestCover(Vector3 agentPos, Vector3 threatPos)
    {
        CoverPoint best = null;
        float bestScore = float.MinValue;

        foreach (var cp in _coverPoints)
        {
            if (cp.IsOccupied) continue;
            float score = cp.ScoreFor(agentPos, threatPos);
            if (score > bestScore)
            {
                bestScore = score;
                best = cp;
            }
        }

        return best;
    }

    // Igual que FindBestCover pero solo considera puntos dentro de maxDistance.
    // Útil cuando el agente no puede correr demasiado lejos bajo fuego.
    public CoverPoint FindBestCoverInRange(
        Vector3 agentPos, Vector3 threatPos, float maxDistance)
    {
        CoverPoint best = null;
        float bestScore = float.MinValue;

        foreach (var cp in _coverPoints)
        {
            if (cp.IsOccupied) continue;
            if (Vector3.Distance(agentPos, cp.transform.position) > maxDistance) continue;

            float score = cp.ScoreFor(agentPos, threatPos);
            if (score > bestScore)
            {
                bestScore = score;
                best = cp;
            }
        }

        return best;
    }

    // TODO [EJERCICIO]: implementa FindNearestCover.
    // Devuelve el CoverPoint no ocupado más cercano a agentPos,
    // sin filtrar por dirección de amenaza.
    public CoverPoint FindNearestCover(Vector3 agentPos)
    {
        CoverPoint nearest = null;
        float minDist = float.MaxValue;

        foreach (var cp in _coverPoints)
        {
            if (cp.IsOccupied) continue;
            float dist = Vector3.Distance(agentPos, cp.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = cp;
            }
        }

        return nearest;
    }

    // Libera el cover que ocupa un agente específico.
    public void ReleaseCover(MonoBehaviour occupant)
    {
        foreach (var cp in _coverPoints)
            if (cp.Occupant == occupant) cp.Vacate();
    }

    void OnDrawGizmos()
    {
        if (_coverPoints == null) return;
        foreach (var cp in _coverPoints)
        {
            if (cp == null) continue;
            Gizmos.color = cp.IsOccupied
                ? new Color(1f, 0f, 0f, 0.4f)
                : new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawCube(cp.transform.position + Vector3.up * 0.5f,
                            Vector3.one * 0.25f);
        }
    }
}
