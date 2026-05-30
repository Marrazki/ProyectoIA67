using UnityEngine;

public class PatrolState : EnemyStateBase
{
    // _waypointIndex vive aquí, no en el FSM: solo este estado lo necesita.
    int _waypointIndex;

    public PatrolState(Enemigo_FSM enemy) : base(enemy) { }

    public override void OnEnter()
    {
        enemy.GetComponent<Renderer>().material.color = Color.cyan;
        Debug.Log("[FSM] → Patrol");
    }

    public override void Update()
    {
        if (enemy.LowHealth())    { enemy.ChangeState(enemy.FleeState);  return; }
        if (enemy.CanSeePlayer()) { enemy.ChangeState(enemy.ChaseState); return; }

        if (enemy.waypoints == null || enemy.waypoints.Length == 0) return;

        Transform destino = enemy.waypoints[_waypointIndex];
        Vector3 dir = (destino.position - enemy.transform.position).normalized;
        enemy.transform.position += dir * (enemy.velocidadPatrulla * Time.deltaTime);
        enemy.transform.LookAt(destino);

        if (Vector3.Distance(enemy.transform.position, destino.position) < enemy.distanciaWaypoint)
            _waypointIndex = (_waypointIndex + 1) % enemy.waypoints.Length;
    }
}
