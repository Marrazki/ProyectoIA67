// ============================================================
//  ConditionalSequence — Sequence con Self Abort
// ============================================================
//
//  En un Sequence estándar con cursor, una vez que la condición pasa
//  y la acción empieza, la condición NO se reevalúa mientras la acción
//  esté en Running.
//
//  ConditionalSequence hace lo opuesto: en CADA tick reevalúa el primer
//  nodo hijo (la condición) antes de continuar con las acciones.
//  Si la condición falla → las acciones se abortan → devuelve Failure.
//  Esto se llama "Self Abort" o "Conditional Abort".
//
//  COMPARACIÓN CON SEQUENCE NORMAL:
//  ──────────────────────────────────────────────────────────────────────
//  Sequence normal (ya reactivo en este proyecto — nuestro Sequence base
//  siempre empieza desde el hijo 0, así que ya tiene Self Abort implícito).
//  En engines como UE4 o Unity Behavior, un Sequence con cursor sería:
//
//  StickySequence (imaginario, con cursor):
//    Frame 1: CanSeePlayer=true  → cursor pasa a Chase → Chase=Running
//    Frame 2: CanSeePlayer=false → cursor=Chase (salta condición!) → sigue ✗
//
//  ConditionalSequence (Self Abort explícito):
//    Frame 1: CanSeePlayer=true  → Chase=Running
//    Frame 2: CanSeePlayer=false → Condition=Failure → Chase ABORTADA ✓
//
//  EN UNITY BEHAVIOR (paquete com.unity.behavior):
//  Equivale a un nodo Condition con Abort Type = Self conectado como
//  primer nodo de una secuencia.

public class ConditionalSequence : BTNode
{
    readonly BTNode _condition;
    readonly BTNode[] _actions;
    int _runningActionIndex;

    public BTNode Condition => _condition;
    public System.Collections.Generic.IReadOnlyList<BTNode> Actions => _actions;

    public ConditionalSequence(BTNode condition, params BTNode[] actions)
    {
        _condition = condition;
        _actions = actions;
        _runningActionIndex = 0;
    }

    public override NodeStatus Tick()
    {
        // Paso 1: Siempre reevalúa la condición (Self Abort).
        var condStatus = _condition.Tick();
        if (condStatus != NodeStatus.Success)
        {
            _runningActionIndex = 0;
            LastStatus = NodeStatus.Failure;
            return NodeStatus.Failure; // ← abort: la condición dejó de cumplirse
        }

        // Paso 2: Continúa ejecutando las acciones desde donde se quedó.
        for (int i = _runningActionIndex; i < _actions.Length; i++)
        {
            var actionStatus = _actions[i].Tick();
            if (actionStatus == NodeStatus.Running)
            {
                _runningActionIndex = i;
                LastStatus = NodeStatus.Running;
                return NodeStatus.Running;
            }
            if (actionStatus == NodeStatus.Failure)
            {
                _runningActionIndex = 0;
                LastStatus = NodeStatus.Failure;
                return NodeStatus.Failure;
            }
        }

        _runningActionIndex = 0;
        LastStatus = NodeStatus.Success;
        return NodeStatus.Success;
    }
}
