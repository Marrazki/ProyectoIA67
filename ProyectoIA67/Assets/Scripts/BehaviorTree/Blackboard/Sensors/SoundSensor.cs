// ============================================================
//  EJERCICIO: BLACKBOARD — Parte 3 (Bonus)
// ============================================================
//
//  SoundSensor simula el oído del enemigo.
//  El jugador emite ruido cuando se mueve (o cuando pulsas E).
//  Si ese ruido se produce dentro del rango auditivo, el enemigo
//  lo registra en la pizarra como una nueva pista.
//
//  CLAVES QUE DEBES ESCRIBIR:
//    BB.HeardNoise    (bool)    → true si se detectó ruido este frame
//    BB.NoisePosition (Vector3) → posición del ruido detectado
//
//  CÓMO INTEGRARLO EN Enemigo_Blackboard:
//    1. Añade un campo: SoundSensor _soundSensor;
//    2. En Start(), inicialízalo:
//         _soundSensor = new SoundSensor(transform, player, hearingRange, _blackboard);
//    3. En Update(), llámalo antes de UpdateFSM():
//         _soundSensor.Sense();
//    4. En UpdateFSM(), lee BB.HeardNoise y, si es true,
//       usa BB.NoisePosition como nueva pista (similar a HasClue).
//
//  PREGUNTA DE REFLEXIÓN:
//    ¿Qué ventaja tiene añadir este sensor sin tocar VisionSensor
//    ni los estados existentes?
//    ¿Qué ocurriría si VisionSensor y SoundSensor detectan
//    simultáneamente? ¿Qué pista debería tener prioridad?
//
// ============================================================

using UnityEngine;

public class SoundSensor : SensorBase
{
    readonly Transform _origin;
    readonly Transform _player;
    readonly float     _hearingRange;

    public SoundSensor(Transform origin, Transform player, float hearingRange, Blackboard blackboard)
        : base(blackboard)
    {
        _origin       = origin;
        _player       = player;
        _hearingRange = hearingRange;
    }

    public override void Sense()
    {
        if (_player == null)
        {
            _blackboard.Set<bool>(BB.HeardNoise, false);
            return;
        }

        bool makingNoise = UnityEngine.InputSystem.Keyboard.current.eKey.isPressed;
        bool inRange     = Vector3.Distance(_origin.position, _player.position) < _hearingRange;

        if (makingNoise && inRange)
        {
            _blackboard.Set<bool>(BB.HeardNoise, true);
            _blackboard.Set<Vector3>(BB.NoisePosition, _player.position);
            _blackboard.Set<Vector3>(BB.LastKnownPosition, _player.position);
            _blackboard.Set<bool>(BB.HasClue, true);
        }
        else
        {
            _blackboard.Set<bool>(BB.HeardNoise, false);
        }
    }
}
