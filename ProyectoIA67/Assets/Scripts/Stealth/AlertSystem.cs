using System;
using UnityEngine;

/// <summary>
/// Sistema simple de alerta global entre guardias.
/// Otros componentes pueden suscribirse a <see cref="OnAlert"/>.
/// </summary>
public static class AlertSystem
{
    public static event Action<Vector3> OnAlert;

    public static void BroadcastAlert(Vector3 source)
    {
        OnAlert?.Invoke(source);
    }
}
