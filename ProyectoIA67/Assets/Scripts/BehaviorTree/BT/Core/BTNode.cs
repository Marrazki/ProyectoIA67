// Contrato mínimo de cualquier nodo del árbol.
// Todos los nodos —compuestos y hoja— implementan Tick().

public abstract class BTNode
{
    public string Name { get; set; }
    public NodeStatus? LastStatus { get; protected set; }

    public abstract NodeStatus Tick();
}
