// ============================================================
//  EJERCICIO [PARTE 3 — BONUS]: Nodo decorador Inverter
// ============================================================
//
//  Un Inverter tiene UN único hijo y devuelve el resultado contrario:
//    · Si el hijo devuelve Success  → Inverter devuelve Failure
//    · Si el hijo devuelve Failure  → Inverter devuelve Success
//    · Si el hijo devuelve Running  → Inverter devuelve Running (sin cambios)
//
//  Ejemplo de uso:
//
//    new Sequence(
//        new Inverter(new Condition(CanSeePlayer)),  // "si NO veo al jugador"
//        new BTAction(Patrol)
//    )
//
//  TODO: Implementa la clase Inverter heredando de BTNode.
//    1. Añade un campo para almacenar el nodo hijo.
//    2. Escribe el constructor que reciba ese nodo hijo.
//    3. Implementa Tick() invirtiendo el resultado del hijo.
//
// ============================================================

public class Inverter : BTNode
{
    // TODO: declara aquí el campo para el nodo hijo
    BTNode childNode;
    public BTNode Child => childNode;

    // TODO: escribe el constructor
    public Inverter(BTNode childNode)
    {
        if (childNode == null)
            throw new System.ArgumentNullException(nameof(childNode));

        this.childNode = childNode;
    }

    public override NodeStatus Tick()
    {
        var childStatus = childNode.Tick();
        NodeStatus result;

        switch (childStatus)
        {
            case NodeStatus.Success: result = NodeStatus.Failure; break;
            case NodeStatus.Failure: result = NodeStatus.Success; break;
            default:                 result = NodeStatus.Running; break;
        }

        LastStatus = result;
        return result;
    }
}
