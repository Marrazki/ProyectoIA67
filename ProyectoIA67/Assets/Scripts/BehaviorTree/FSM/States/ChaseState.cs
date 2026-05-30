using UnityEngine;

public class ChaseState : EnemyStateBase
{
    public ChaseState(Enemigo_FSM enemy) : base(enemy) { }

    public override void OnEnter()
    {
        enemy.GetComponent<Renderer>().material.color = Color.yellow;
        Debug.Log("[FSM] → Chase");
    }

    public override void Update()
    {
        if (enemy.LowHealth())     { enemy.ChangeState(enemy.FleeState);        return; }
        if (!enemy.CanSeePlayer()) { enemy.ChangeState(enemy.InvestigateState); return; }

        Vector3 dir = (enemy.jugador.position - enemy.transform.position).normalized;
        enemy.transform.position += dir * (enemy.velocidadPersecucion * Time.deltaTime);
        enemy.transform.LookAt(enemy.jugador);
    }

    public override void OnExit()
    {
        // Capturamos la última posición conocida en el único momento correcto:
        // justo cuando dejamos de ver al jugador (o huimos).
        enemy.lastKnownPlayerPos = enemy.jugador.position;
    }
}
