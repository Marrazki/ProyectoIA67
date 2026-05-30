// Clase base para todos los estados del enemigo.
// Cada estado recibe una referencia al FSM propietario para poder
// leer sus datos y solicitar transiciones.

public abstract class EnemyStateBase
{
    protected Enemigo_FSM enemy;

    protected EnemyStateBase(Enemigo_FSM enemy) => this.enemy = enemy;

    public virtual void OnEnter() { }
    public abstract void Update();
    public virtual void OnExit()  { }
}
