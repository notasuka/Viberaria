namespace Viberaria.VibrationManager;

/// <summary>
/// An enumerable indicating what <see cref="VibrationEvent"/>s take precedence over other VibrationEvents.
/// </summary>
public enum VibrationPriority
{
    Death = 9,
    Hurt = 8,
    Fishing = 7,
    Potion = 6,
    Debuff = 5,
    ManaUsage = 4,
    AmmoUsage = 3,
    DamageDPS = 2,
    HealthPercent = 1
}