// Constantes de clave para la pizarra.
// Usa siempre estas constantes en lugar de literales de string
// para evitar errores de tipeo difíciles de depurar.

public static class BB
{
    // ── Escritas por VisionSensor ─────────────────────────────────────────
    // ¿El enemigo ve al jugador en este frame?
    public const string CanSeePlayer      = "CanSeePlayer";
    // Última posición conocida del jugador (se conserva al perder la visión).
    public const string LastKnownPosition = "LastKnownPosition";
    // ¿Existe una pista válida para investigar?
    public const string HasClue           = "HasClue";

    // ── Escritas por HealthSensor ─────────────────────────────────────────
    // Valor numérico de la vida actual.
    public const string Health            = "Health";
    // true si vida < 50 % de la vida máxima.
    public const string LowHealth         = "LowHealth";

    // ── Escritas por SoundSensor (Parte 3 — Bonus) ────────────────────────
    // ¿El enemigo ha oído un ruido este frame?
    public const string HeardNoise        = "HeardNoise";
    // Posición del ruido detectado.
    public const string NoisePosition     = "NoisePosition";
}
