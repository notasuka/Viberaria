using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader.Config;
using Viberaria.VibrationManager;
using static Viberaria.VibrationManager.VibrationManager;

namespace Viberaria.Config;


public class VibrationPattern
{
    [Header("VibrationPattern")]
    public bool ZerosOverrideLowerPriority;

    public List<VibrationStep> Pattern = new();

    internal int PatternLength => Pattern.Sum(step => step.Duration);

    public override string ToString()
    {
        string truncationEpsilon = Pattern.Count > 3 ? $" + {Pattern.Count-3} more" : "";

        return String.Join(", ", Pattern.Take(3).Select(step => step.ToString())) + truncationEpsilon;
    }

    public override bool Equals(object obj) {
        if (obj is VibrationPattern other)
            return ZerosOverrideLowerPriority == other.ZerosOverrideLowerPriority && Pattern == other.Pattern;
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return new { ZerosOverrideLowerPriority, Pattern.Count }.GetHashCode();
    }

    /// <summary>
    /// Play a vibration pattern. A max duration can be provided to interrupt the pattern when it exceeds a certain duration.
    /// </summary>
    /// <param name="priority">The priority of the pattern.</param>
    /// <param name="maxDuration">An optional maximum length that the pattern may play before being cut short.</param>
    /// <returns>Whether the pattern was interrupted due to exceeding the <paramref name="maxDuration"/></returns>
    public bool PlayPattern(VibrationPriority priority, int maxDuration = -1)
    {
        TimeSpan stepOffset = TimeSpan.Zero;
        return PlayPattern(priority, stepOffset, maxDuration);
    }

    /// <summary>
    /// Play a vibration pattern at a given offset. A max duration can be provided to interrupt the pattern when it exceeds a certain duration.
    /// </summary>
    /// <param name="priority">The priority of the pattern.</param>
    /// <param name="patternOffset">How far into the future the pattern should be played.</param>
    /// <param name="maxDuration">An optional maximum length that the pattern may play before being cut short.</param>
    /// <returns>Whether the pattern was interrupted due to exceeding the <paramref name="maxDuration"/></returns>
    public bool PlayPattern(VibrationPriority priority, TimeSpan patternOffset, int maxDuration = -1)
    {
        TimeSpan stepOffset = TimeSpan.Zero;
        bool canInterrupt = maxDuration != -1;

        foreach (VibrationStep step in Pattern)
        {
            if (canInterrupt && stepOffset.TotalMilliseconds + step.Duration > maxDuration)
            {
                // check if pattern length is shorter than a vibration step
                int cutDuration = (int)(maxDuration - stepOffset.TotalMilliseconds);
                if (step.Intensity != 0 || ZerosOverrideLowerPriority) // see comment below.
                    AddEvent(priority, patternOffset + stepOffset, cutDuration, step.Intensity, addToFront: false);
                return true;
            }

            // If ZerosOverrideLowerPriority is enabled, allow it to set intensity to 0, overriding
            // potential events with lower priorities playing vibrations.
            if (step.Intensity != 0 || ZerosOverrideLowerPriority)
                AddEvent(priority, patternOffset + stepOffset, step.Duration, step.Intensity, addToFront: false);
            stepOffset += new TimeSpan(0, 0, 0, 0, step.Duration);
        }
        return false;
    }
}