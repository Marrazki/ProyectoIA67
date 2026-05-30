// Clase base para todos los sensores del enemigo.
//
// Un sensor tiene una responsabilidad única: percibir el mundo
// y escribir sus conclusiones en la pizarra.
// No sabe quién leerá los datos ni cómo se usarán.

public abstract class SensorBase
{
    protected readonly Blackboard _blackboard;

    protected SensorBase(Blackboard blackboard)
    {
        _blackboard = blackboard;
    }

    // Evalúa el entorno y actualiza la pizarra.
    // Se llama una vez por frame desde Enemigo_Blackboard.Update().
    public abstract void Sense();
}
