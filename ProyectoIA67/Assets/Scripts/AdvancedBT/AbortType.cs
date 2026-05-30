// Tipos de interrupción (abort) en árboles de comportamiento.
//
//  En un BT sin aborts, una vez que una rama devuelve Running, el árbol
//  la vuelve a tickear directamente sin reevaluar otras condiciones.
//  Los aborts permiten que el árbol "reaccione" a cambios en el mundo
//  mientras una acción está en progreso.
//
//  TIPOS:
//  ──────────────────────────────────────────────────────────────────────
//  None           → Sin abort. Comportamiento estándar (cursor fijo).
//
//  Self           → La condición del propio nodo se reevalúa cada tick.
//                   Si falla mientras la acción está en Running, la
//                   acción se interrumpe → el nodo devuelve Failure.
//                   Implementado en: ConditionalSequence.cs
//
//  LowerPriority  → El Selector siempre reevalúa desde la rama de MAYOR
//                   prioridad, permitiendo que una condición de alta
//                   prioridad interrumpa una rama de baja prioridad que
//                   estaba en Running.
//                   Ejemplo reactivo:   Core/Selector.cs (ya lo hace)
//                   Ejemplo no reactivo: StickySelector.cs
//
//  Both           → Combina Self y LowerPriority.
//
//  EN UNITY BEHAVIOR (paquete com.unity.behavior):
//  Los nodos Condition tienen un campo "Abort Type" en el Inspector
//  con estas mismas opciones.

public enum AbortType { None, Self, LowerPriority, Both }
