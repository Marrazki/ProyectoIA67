// ============================================================
//  EJERCICIO: BLACKBOARD — Parte 2a (Ampliación)
// ============================================================
//
//  VisionSensor simula los "ojos" del enemigo.
//  Cada frame comprueba si el jugador está en rango y
//  escribe el resultado en la pizarra.
//
//  CLAVES QUE DEBES ESCRIBIR:
//    BB.CanSeePlayer      (bool)    → true si el jugador está en rango
//    BB.LastKnownPosition (Vector3) → posición del jugador cuando es visible
//    BB.HasClue           (bool)    → true en cuanto se ha visto al jugador
//
//  REGLA IMPORTANTE:
//    Cuando el jugador SALE del rango, pon BB.CanSeePlayer = false,
//    pero NO borres BB.LastKnownPosition ni BB.HasClue.
//    Esos datos son la "memoria" que usará el estado Investigate.
//
//  REFLEXIÓN:
//    Compara esta solución con ChaseState.OnExit() de la FSM:
//
//      // ChaseState.cs (Enemigo_FSM):
//      public override void OnExit()
//      {
//          enemy.lastKnownPlayerPos = enemy.jugador.position;
//      }
//
//    Con la pizarra + sensor, ya no hace falta capturar la posición
//    en OnExit(): el sensor la actualiza continuamente y la conserva
//    automáticamente al perder la visión.
//
// ============================================================

using UnityEngine;

public class VisionSensor : SensorBase
{
    readonly Transform _origin;
    readonly Transform _player;
    readonly float     _range;

    public VisionSensor(Transform origin, Transform player, float range, Blackboard blackboard)
        : base(blackboard)
    {
        _origin = origin;
        _player = player;
        _range  = range;
    }

    public override void Sense()
    {
        if (_player == null)
        {
            _blackboard.Set<bool>(BB.CanSeePlayer, false);
            return;
        }

        if (Vector3.Distance(_origin.position, _player.position) < _range)
        {
            _blackboard.Set<bool>(BB.CanSeePlayer, true);
            _blackboard.Set<Vector3>(BB.LastKnownPosition, _player.position);
            _blackboard.Set<bool>(BB.HasClue, true);
        }
        else
        {
            _blackboard.Set<bool>(BB.CanSeePlayer, false);
            // LastKnownPosition y HasClue se conservan intencionalmente.
        }
    }
}
