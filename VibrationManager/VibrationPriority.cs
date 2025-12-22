namespace Viberaria.VibrationManager;

/// <summary>
/// An enumerable indicating what <see cref="VibrationEvent"/>s take precedence over other VibrationEvents.
/// </summary>
public enum VibrationPriority
{
    Death = 10,
    Hurt = 9,
    Fishing = 8,
    Instrument = 7,
    Potion = 6,
    Debuff = 5,
    ManaUsage = 4,
    AmmoUsage = 3,
    DamageDPS = 2,
    HealthPercent = 1
}