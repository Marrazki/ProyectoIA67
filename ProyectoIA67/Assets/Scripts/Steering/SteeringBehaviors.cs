// ============================================================
//  STEERING BEHAVIORS — Unidad 6
// ============================================================
//
//  Steering behaviors son fuerzas que guían a un agente hacia (o lejos
//  de) un objetivo. Formalizados por Craig Reynolds (1999).
//
//  Cada método recibe la posición actual del agente y devuelve la
//  VELOCIDAD DESEADA (no aceleración): un vector que indica adónde
//  quiere ir el agente y a qué velocidad.
//
//  BEHAVIORS IMPLEMENTADOS:
//  ──────────────────────────────────────────────────────────────────────
//  · Seek    → perseguir: mueve el agente hacia el objetivo a maxSpeed.
//              No hay desaceleración → llega y lo sobrepasa (overshooting).
//
//  · Flee    → huir: mueve el agente LEJOS del objetivo a maxSpeed.
//
//  · Arrive  → llegar: como Seek pero desacelera suavemente.
//              Dos radios:
//                slowRadius   → empieza a frenar (speed = maxSpeed * dist/slow)
//                targetRadius → se detiene (speed = 0)
//
//  · Pursue  → perseguir predictivo: apunta a donde ESTARÁ el objetivo,
//              no donde está. Evita perseguir la cola.
//
//  · Evade   → huir predictivo: inverso de Pursue.
//
//  DIFERENCIA VISUAL SEEK vs ARRIVE:
//  ──────────────────────────────────────────────────────────────────────
//  Seek  → oscila alrededor del objetivo (overshooting).
//  Arrive → frena y se detiene suavemente (no oscila).
//
//  USO (ver SteeringAgent.cs y Enemigo_Steering_BT.cs):
//    Vector3 vel = SteeringBehaviors.Arrive(
//        transform.position, target.position,
//        maxSpeed: 4f, slowRadius: 3f, targetRadius: 0.2f);

using UnityEngine;

public static class SteeringBehaviors
{
    // ── Seek ──────────────────────────────────────────────────────────────

    public static Vector3 Seek(Vector3 agentPos, Vector3 targetPos, float maxSpeed)
    {
        Vector3 dir = targetPos - agentPos;
        if (dir.sqrMagnitude < 0.0001f) return Vector3.zero;
        return dir.normalized * maxSpeed;
    }

    // ── Flee ──────────────────────────────────────────────────────────────

    public static Vector3 Flee(Vector3 agentPos, Vector3 targetPos, float maxSpeed)
    {
        Vector3 dir = agentPos - targetPos;
        if (dir.sqrMagnitude < 0.0001f) return Vector3.zero;
        return dir.normalized * maxSpeed;
    }

    // ── Arrive ────────────────────────────────────────────────────────────
    //
    //  Fórmula de velocidad deseada:
    //    dist > slowRadius  → desiredSpeed = maxSpeed
    //    dist ≤ slowRadius  → desiredSpeed = maxSpeed * (dist / slowRadius)
    //    dist ≤ targetRadius → desiredSpeed = 0  (parado)

    public static Vector3 Arrive(
        Vector3 agentPos,
        Vector3 targetPos,
        float maxSpeed,
        float slowRadius = 3f,
        float targetRadius = 0.2f)
    {
        Vector3 toTarget = targetPos - agentPos;
        float distance = toTarget.magnitude;

        if (distance <= targetRadius)
            return Vector3.zero;

        float desiredSpeed = distance <= slowRadius
            ? maxSpeed * (distance / slowRadius)
            : maxSpeed;

        return toTarget.normalized * desiredSpeed;
    }

    // ── Pursue ────────────────────────────────────────────────────────────
    //
    //  Predice la posición futura del objetivo y aplica Seek hacia ella.
    //  predictionTime: cuántos segundos en el futuro predecir.

    public static Vector3 Pursue(
        Vector3 agentPos,
        Vector3 targetPos,
        Vector3 targetVelocity,
        float maxSpeed,
        float predictionTime = 0.5f)
    {
        Vector3 futurePos = targetPos + targetVelocity * predictionTime;
        return Seek(agentPos, futurePos, maxSpeed);
    }

    // ── Evade ─────────────────────────────────────────────────────────────

    public static Vector3 Evade(
        Vector3 agentPos,
        Vector3 targetPos,
        Vector3 targetVelocity,
        float maxSpeed,
        float predictionTime = 0.5f)
    {
        Vector3 futurePos = targetPos + targetVelocity * predictionTime;
        return Flee(agentPos, futurePos, maxSpeed);
    }

    public static Vector3 Wander(Vector3 agentPos,
        Vector3 agentForward,
        ref float wanderAngle,
        float maxSpeed,
        float wanderCircleDistance = 5f,
        float wanderCircleRadius = 3f)
    {
        wanderAngle += Random.Range(-1f, 1f) * Time.deltaTime;

        Vector3 circleCenter = agentPos + agentForward * wanderCircleDistance;

        Vector3 wanderPoint = circleCenter + new Vector3(
            Mathf.Cos(wanderAngle) * wanderCircleRadius,
            0,
            Mathf.Sin(wanderAngle) * wanderCircleRadius
        );

        return Seek(agentPos, wanderPoint, maxSpeed);
    }

    public static Vector3 FleeAndArrive(
    Vector3 agentPos,
    Vector3 threatPos,
    Vector3 evacuationPoint,
    float maxSpeed,
    float slowRadius,
    float targetRadius,
    float fleePriority = 0.7f,
    float arrivePriority = 0.3f)
    {
        Vector3 fleeForce = Flee(agentPos, threatPos, maxSpeed);
        Vector3 arriveForce = Arrive(agentPos, evacuationPoint, maxSpeed, slowRadius, targetRadius);

        // Ponderar fuerzas
        Vector3 combined = fleeForce * fleePriority + arriveForce * arrivePriority;

        // Normalizar y aplicar maxSpeed
        if (combined.magnitude > 0)
            combined = combined.normalized * maxSpeed;

        return combined;
    }
}
