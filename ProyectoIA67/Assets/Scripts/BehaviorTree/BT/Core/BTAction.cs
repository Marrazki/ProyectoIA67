using System;

// Nodo hoja que envuelve una acción.
// La acción devuelve Running mientras se ejecuta, Success al terminar.

public class BTAction : BTNode
{
    readonly Func<NodeStatus> _action;

    public BTAction(Func<NodeStatus> action, string name = null)
    {
        _action = action;
        Name = name;
    }

    public override NodeStatus Tick()
    {
        LastStatus = _action();
        return LastStatus.Value;
    }
}
