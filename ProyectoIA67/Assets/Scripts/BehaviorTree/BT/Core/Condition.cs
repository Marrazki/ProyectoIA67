using System;

// Nodo hoja que envuelve una condición booleana.
// Devuelve Success si la condición es verdadera, Failure si es falsa.

public class Condition : BTNode
{
    readonly Func<bool> _condition;

    public Condition(Func<bool> condition, string name = null)
    {
        _condition = condition;
        Name = name;
    }

    public override NodeStatus Tick()
    {
        LastStatus = _condition() ? NodeStatus.Success : NodeStatus.Failure;
        return LastStatus.Value;
    }
}
