using UnityEngine;

public class InvestigateState : EnemyStateBase
{
    public InvestigateState(Enemigo_FSM enemy) : base(enemy) { }

    public override void OnEnter()
    {
        enemy.GetComponent<Renderer>().material.color = Color.red;
        Debug.Log($"[FSM] → Investigate @ {enemy.lastKnownPlayerPos}");
    }

    public override void Update()
    {
        if (enemy.LowHealth())    { enemy.ChangeState(enemy.FleeState);  return; }
        if (enemy.CanSeePlayer()) { enemy.ChangeState(enemy.ChaseState); return; }

        Vector3 dir = (enemy.lastKnownPlayerPos - enemy.transform.position).normalized;
        enemy.transform.position += dir * (enemy.velocidadInvestigacion * Time.deltaTime);
        enemy.transform.LookAt(enemy.lastKnownPlayerPos);

        if (Vector3.Distance(enemy.transform.position, enemy.lastKnownPlayerPos) < enemy.distanciaInvestigacion)
            enemy.ChangeState(enemy.PatrolState);
    }
}
