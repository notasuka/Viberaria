using System.Runtime.Serialization;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Viberaria.Config;

public class VibrationStep
{
    private const int MinTime = 10;
    private const int MaxTime = 3000;
    private const int IncrementTime = 10;

    [Range(0f,1f), Increment(0.01f), DefaultValue(.5f)]
    public float Intensity;

    [Range(MinTime,MaxTime), Increment(IncrementTime), DefaultValue(500)]
    public int Duration;

    public override string ToString()
    {
        return $"({Duration},{Intensity})";
    }

    public override bool Equals(object obj) {
        if (obj is VibrationStep other)
            return Intensity == other.Intensity && Duration == other.Duration;
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return new { Intensity, Duration }.GetHashCode();
    }

    [OnDeserialized]
    internal void OnDeserializedMethod(StreamingContext context) {
        // Range is just a suggestion to the UI. If we want to enforce constraints, we need to validate the data here. Users can edit config files manually with values outside the RangeAttribute, so we fix here if necessary.
        // Both enforcing ranges and not enforcing ranges have uses in mods. Make sure you fix config values if values outside the range will mess up your mod.
        Intensity = Terraria.Utils.Clamp(Intensity, 0f, 1f);
    }
}