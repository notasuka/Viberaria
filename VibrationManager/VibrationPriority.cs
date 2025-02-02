namespace Viberaria.VibrationManager;

/// <summary>
/// An enumerable indicating what <see cref="VibrationEvent"/>s take precedence over other VibrationEvents.
/// </summary>
public enum VibrationPriority
{
    Death = 8,
    Hurt = 7,
    Fishing = 6,
    Potion = 5,
    Debuff = 4,
    ManaUsage = 3,
    DamageDPS = 2,
    HealthPercent = 1
}