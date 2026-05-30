// ============================================================
//  EJERCICIO: BLACKBOARD — Parte 2b (Ampliación)
// ============================================================
//
//  HealthSensor monitoriza la salud del enemigo y escribe
//  el resultado en la pizarra cada frame.
//
//  CLAVES QUE DEBES ESCRIBIR:
//    BB.Health    (float) → valor numérico actual de la vida
//    BB.LowHealth (bool)  → true si vida < 50 % de la vida máxima
//
//  NOTA SOBRE EL DISEÑO:
//    El sensor recibe la vida a través de una función delegada
//    (Func<float>) en lugar de una referencia directa al MonoBehaviour.
//    Esto desacopla el sensor del script concreto del enemigo:
//    mañana podrías conectarlo a cualquier otra fuente de salud
//    sin tocar el sensor.
//
//    Uso desde Enemigo_Blackboard.Start():
//      _healthSensor = new HealthSensor(() => health, maxHealth, _blackboard);
//                                       ↑
//                                 lambda que lee el campo "health"
//
// ============================================================

using System;

public class HealthSensor : SensorBase
{
    readonly Func<float> _getHealth;
    readonly float       _maxHealth;

    public HealthSensor(Func<float> getHealth, float maxHealth, Blackboard blackboard)
        : base(blackboard)
    {
        _getHealth = getHealth;
        _maxHealth = maxHealth;
    }

    public override void Sense()
    {
        float health = _getHealth();
        _blackboard.Set<float>(BB.Health, health);
        _blackboard.Set<bool>(BB.LowHealth, health < _maxHealth * 0.5f);
    }
}
