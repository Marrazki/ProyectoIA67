// ============================================================
//  StickySelector — Selector con cursor (NO reactivo)
// ============================================================
//
//  Un Selector estándar sin aborts usa un CURSOR: recuerda qué hijo
//  estaba en Running y vuelve directo a él el siguiente tick, SIN
//  reevaluar las ramas de mayor prioridad.
//
//  CONSECUENCIA: si una condición de alta prioridad se vuelve verdadera
//  mientras una de baja prioridad está en Running, el árbol NO reacciona.
//
//  EJEMPLO DEL FALLO:
//  ──────────────────────────────────────────────────────────────────────
//  Árbol:
//    StickySelector
//    ├── Seq(LowHealth, Flee)     ← prioridad 0 (mayor)
//    ├── Seq(CanSeePlayer, Chase) ← prioridad 1
//    └── BTAction(Patrol)         ← prioridad 2 (menor)
//
//  Frame 1: LowHealth=false, CanSeePlayer=false → Patrol=Running, cursor=2
//  Frame 2: LowHealth=TRUE (enemigo recibe daño)
//           cursor=2 → va directo a Patrol → IGNORA Flee ← BUG
//
//  SOLUCIÓN: usar el Selector reactivo de Core/Selector.cs (sin cursor).
//  El Selector reactivo siempre evalúa desde el hijo 0 → LowerPriority Abort.

public class StickySelector : BTNode
{
    readonly BTNode[] _children;
    int _runningIndex = -1;

    public System.Collections.Generic.IReadOnlyList<BTNode> Children => _children;

    public StickySelector(params BTNode[] children) => _children = children;

    public override NodeStatus Tick()
    {
        // Si hay un hijo en Running, vuelve a él directamente (sin reevaluar anteriores).
        int start = (_runningIndex >= 0) ? _runningIndex : 0;

        for (int i = start; i < _children.Length; i++)
        {
            var status = _children[i].Tick();
            if (status == NodeStatus.Running)
            {
                _runningIndex = i;
                LastStatus = NodeStatus.Running;
                return NodeStatus.Running;
            }
            if (status == NodeStatus.Success)
            {
                _runningIndex = -1;
                LastStatus = NodeStatus.Success;
                return NodeStatus.Success;
            }
            // Failure → continúa evaluando desde start (no desde 0)
        }

        _runningIndex = -1;
        LastStatus = NodeStatus.Failure;
        return NodeStatus.Failure;
    }

    public void ResetCursor() => _runningIndex = -1;
}
