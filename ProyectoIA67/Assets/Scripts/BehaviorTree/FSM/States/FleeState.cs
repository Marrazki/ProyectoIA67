using UnityEngine;

public class FleeState : EnemyStateBase
{
    public FleeState(Enemigo_FSM enemy) : base(enemy) { }

    public override void OnEnter()
    {
        enemy.GetComponent<Renderer>().material.color = Color.magenta;
        Debug.Log("[FSM] → Flee");
    }

    public override void Update()
    {
        if (!enemy.LowHealth()) { enemy.ChangeState(enemy.PatrolState); return; }

        Vector3 dir = (enemy.transform.position - enemy.jugador.position).normalized;
        enemy.transform.position += dir * (enemy.velocidadHuida * Time.deltaTime);
        enemy.transform.LookAt(enemy.transform.position + dir);
    }
}
