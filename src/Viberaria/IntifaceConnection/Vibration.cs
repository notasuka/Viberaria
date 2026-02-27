using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Viberaria.tModAdapters;
using Viberaria.VibrationManager;
using static Viberaria.Config.ViberariaConfig;
using static Viberaria.VibrationManager.VibrationManager;

namespace Viberaria.IntifaceConnection;

public static class Vibration
{
    private static readonly LinkedList<(DateTime, int)> ManaUsages = new();
    private static readonly LinkedList<DateTime> AmmoUsages = new();
    private static int _expectedDebuffDurationTicks = 0;
    private static DateTime? _debuffLastPatternEnd = null;

    private static bool PlayerIsDead => Main.clientPlayer.dead;

    public static void HealthUpdated(int currentHp, int maxHp)
    {
        if(!Instance.ViberariaEnabled ||
           !Instance.HealthVibratationScalingEnabled)
            return;

        if (PlayerIsDead)
            return;

        float range = Instance.HealthMaxIntensity - Instance.HealthMinIntensity;
        float normalizedValue = (1 - currentHp / (float)maxHp) * range + Instance.HealthMinIntensity;
        normalizedValue = Math.Clamp(normalizedValue, 0f, 1f);

        // vibrate for 0.1 second, getting overwritten by other events of the same priority.
        // This should not create times when toys don't vibrate, since 1 tick is only 16.66 milliseconds.
        // And it will automatically renew every tick.
        AddEvent(VibrationPriority.HealthPercent, 100, normalizedValue, true, true);
    }

    public static void Damaged(Player.HurtInfo hurtInfo, int maxHp)
    {
        if(!Instance.ViberariaEnabled ||
           !Instance.DamageVibrationEnabled)
            return;

        if (hurtInfo.Damage < Instance.MinimumDamageForVibration) return;

        float damageFactor = Instance.StaticDamageVibration ? 1f : hurtInfo.Damage / (float)maxHp;
        // clear ongoing hurt vibrations so the upcoming damage event is played immediately
        ClearEvents(VibrationPriority.Hurt);
        Instance.DamagePattern.PlayPattern(VibrationPriority.Hurt, damageFactor);
    }

    public static void Died(int respawnTimer)
    {
        // todo add respawn timing death.
        if(!Instance.ViberariaEnabled ||
           !Instance.DeathVibrationEnabled)
            return;
        ClearEvents(VibrationPriority.Debuff);
        if (!Instance.DeathVibrationDuringRespawnTimer)
        {
            Instance.DeathPattern.PlayPattern(VibrationPriority.Death);
        }
        else
        {
            TimeSpan respawnTimerSpan = TimeSpan.FromMilliseconds(respawnTimer / 60.0 * 1000.0);

            TimeSpan offset = TimeSpan.Zero;
            while (offset < respawnTimerSpan)
            {
                Instance.DeathPattern.PlayPattern(
                    VibrationPriority.Death,
                    offset,
                    maxDuration: Math.Min(0, (int)(respawnTimerSpan - offset).TotalMilliseconds)
                );
                offset += TimeSpan.FromMilliseconds(Instance.DeathPattern.PatternLength);
            }
        }
    }

    /// <summary>
    /// Vibrate the user's toy when the user has a debuff.
    /// </summary>
    /// <param name="durationTicks">The length of the player's longest
    /// active debuff.</param>
    public static void DebuffVibration(int durationTicks)
    {
        if(!Instance.ViberariaEnabled ||
           !Instance.DebuffVibrationEnabled)
            return;

        bool ongoing = _expectedDebuffDurationTicks > 0;

        // This function is called every tick, so the longest debuff should
        // decrease by 1 every tick. This is to reduce unnecessarily clearing
        // the ongoing debuff vibration events.
        if (_expectedDebuffDurationTicks > 0)
            _expectedDebuffDurationTicks--;

        if (durationTicks == _expectedDebuffDurationTicks)
            return;
        _expectedDebuffDurationTicks = durationTicks;

        if (durationTicks == 0)
        {
            // The function did not return at the expected debuff duration,
            // which means longest debuff must have (unexpectedly) finished.
            // As such, clear and update the ongoing debuff vibrations.
            _debuffLastPatternEnd = null;
            ClearEvents(VibrationPriority.Debuff, update: true);
            return;
        }

        // At this point: A new (longest) debuff has been given to the player.
        // Clear the ongoing debuff vibration pattern and re-place it.
        // This can also be the case if no debuffs were ongoing previously.

        if (!ongoing || _debuffLastPatternEnd == null)
        {
            // No debuffs were ongoing previously, so start the pattern from the beginning.
            _debuffLastPatternEnd = DateTime.Now;
        }

        // To make the ongoing pattern flow smoothly, see how far along the current event is.

        TimeSpan offset = _debuffLastPatternEnd.Value - DateTime.Now;
        int durationMsec = (int)(durationTicks / 60.0 * 1000);
        TimeSpan patternLengthSpan = TimeSpan.FromMilliseconds(Instance.DebuffPattern.PatternLength);

        if (-offset.TotalMilliseconds > Instance.DebuffPattern.PatternLength)
        {
            // The offset is long enough ago that another pattern fits between it. As such, begin the new pattern.
            // Time since end of last pattern / Pattern length = number of patterns that fit between now and the last pattern.
            // Calculate this manually in case a debuff lasts longer than a pattern,
            //  and thus needs multiple patterns to fill that time since the last pattern.
            int newPatternCount = Math.DivRem(
                (int)-offset.TotalMilliseconds,
                Instance.DebuffPattern.PatternLength
            ).Item1;
            _debuffLastPatternEnd += newPatternCount * patternLengthSpan;
            // Recalculate the offset from the new last pattern ending.
            offset += newPatternCount * patternLengthSpan;
        }

        ClearEvents(VibrationPriority.Debuff);
        while (offset.TotalMilliseconds < durationMsec)
        {
            Instance.DebuffPattern.PlayPattern(
                VibrationPriority.Debuff,
                offset,
                // When the offset is negative, it should still play the entire pattern,
                //  up until the duration of the debuff.
                maxDuration: durationMsec + (int)Math.Max(0, -offset.TotalMilliseconds)
            );
            offset += patternLengthSpan;
        }
    }

    public static void PotionVibration()
    {
        if(!Instance.ViberariaEnabled ||
           !Instance.PotionUseVibrationEnabled)
            return;
        Instance.PotionPattern.PlayPattern(VibrationPriority.Potion);
    }

    /// <summary>
    /// Clear all events earlier than the given <paramref name="timespan"/> ago, then sum the remaining events.
    /// </summary>
    /// <param name="timespan">How long of a time to take the sum of.</param>
    /// <returns>The sum of the (remaining) events in the usage list.</returns>
    private static int GetAmmoUsageSum(TimeSpan timespan)
    {
        // same function, but without (DateTime, int), since linkedList has a .Count property for this, for efficiency.
        DateTime endTime = DateTime.Now - timespan;
        while (AmmoUsages.First != null && AmmoUsages.First.Value < endTime)
        {
            AmmoUsages.RemoveFirst();
        }

        return AmmoUsages.Count;
    }

    public static void SoIStartedBlasting(Item weapon, Item ammo)
    {
        // This function was largely copied from ManaUsageVibration().
        if (!Instance.ViberariaEnabled ||
            !Instance.BlastingEnabled)
            return;

        AmmoUsages.AddLast(DateTime.Now);

        TimeSpan timespan = TimeSpan.FromMilliseconds(Instance.BlastingBuildupTimeMsec);
        // Take the sum of the past BuildupTime seconds of usage, and divide it by the BuildupTime.
        // Note that BuildupTime is in milliseconds. That means we need to divide it by 1000 to get usage per second.
        double ammoPerSecond = GetAmmoUsageSum(timespan) / (Instance.BlastingBuildupTimeMsec / 1000.0);
        double vibrationStrength = ammoPerSecond * Instance.BlastingIntensityFactor;
        int vibrationDurationMsec = (int)(weapon.useTime / 60.0 * 1000) + Instance.BlastingFadeDelayMsec;

        if (Instance.Debug.Enabled && Instance.Debug.ManaAmmoUsageMessages)
            tChat.LogToPlayer($"adding event timespan={timespan.TotalMilliseconds}ms,mps={ammoPerSecond}, vs={vibrationStrength}, vdms={vibrationDurationMsec}", Color.Magenta);

        AddEvent(VibrationPriority.AmmoUsage,
                 vibrationDurationMsec, // ticks -> sec -> msec, + 0.5 sec for fade delay
                 (float)Math.Clamp(vibrationStrength, 0, Instance.MaxBlastingIntensity),
                 addToFront: false,
                 clearOthers: true);


        // add decrement vibrations for when the weapon stops being used. `clearOthers` means we can safely add
        // other events after this event, and any new `SoIStartedBlasting` will replace these events again.

        // decrement the intensity in 20 steps (since lovense has 20 vibration levels, I guess)
        // Also round up division to ensure the decrement delay is never 0
        int msecIncrementDelay = (int)Math.Ceiling(Instance.BlastingBuildupTimeMsec / 20.0);

        if (Instance.Debug.Enabled && Instance.Debug.ManaAmmoUsageMessages)
            tChat.LogToPlayer($"adding `{Instance.BlastingBuildupTimeMsec/(double)msecIncrementDelay}` events dur={vibrationDurationMsec}+offsets,offset={msecIncrementDelay}", Color.Magenta);

        for (int msecOffset = msecIncrementDelay; msecOffset < Instance.BlastingBuildupTimeMsec; msecOffset += msecIncrementDelay)
        {
            // strength * progress. The loop iterates in 20 steps, so it starts with
            // `strength * (1 - (x/20) / 20)` which is `strength * (1 - 1/20)` aka `strength * 19/20`.
            // Each iteration, x increases (msecOffset += msecIncrementDelay) so more is subtracted
            // until `strength * 1/20`.
            // And then VibrationManager sets the strength back to 0 since there are no more events left.
            double strength = vibrationStrength * (1 - msecOffset / (double)Instance.BlastingBuildupTimeMsec);
            float clampedStrength = (float)Math.Clamp(strength, 0, Instance.MaxBlastingIntensity);

            AddEvent(VibrationPriority.AmmoUsage,
                     vibrationDurationMsec + msecOffset,
                     clampedStrength,
                     false);
        }
    }


    /// <summary>
    /// Clear all events earlier than the given <paramref name="timespan"/> ago, then sum the remaining events.
    /// </summary>
    /// <param name="timespan">How long of a time to take the sum of.</param>
    /// <returns>The sum of the (remaining) events in the usage list.</returns>
    private static int GetManaUsageSum(TimeSpan timespan)
    {
        DateTime endTime = DateTime.Now - timespan;
        while (ManaUsages.First != null && ManaUsages.First.Value.Item1 < endTime)
        {
            ManaUsages.RemoveFirst();
        }

        int sum = 0;
        foreach ((DateTime _, int count) in ManaUsages)
        {
            sum += count;
        }

        return sum;
    }

    public static void ManaUsageVibration(Item item, Player player)
    {
        if (!Instance.ViberariaEnabled ||
            !Instance.ManaUsageVibrationEnabled)
            return;
        if (player.GetManaCost(item) == 0) return;

        ManaUsages.AddLast((DateTime.Now, player.GetManaCost(item)));

        TimeSpan timespan = TimeSpan.FromMilliseconds(Instance.ManaUsageBuildupTimeMsec);
        // Take the sum of the past BuildupTime seconds of usage, and divide it by the BuildupTime.
        // Note that BuildupTime is in milliseconds. That means we need to divide it by 1000 to get usage per second.
        double manaPerSecond = GetManaUsageSum(timespan) / (Instance.ManaUsageBuildupTimeMsec / 1000.0);
        double manaUsagePercentage = manaPerSecond / player.statManaMax2;
        double vibrationStrength = manaUsagePercentage * Instance.ManaUsageIntensityFactor;
        int vibrationDurationMsec = (int)(item.useTime / 60.0 * 1000) + Instance.ManaUsageFadeDelayMsec;

        if (Instance.Debug.Enabled && Instance.Debug.ManaAmmoUsageMessages)
            tChat.LogToPlayer($"adding event timespan={timespan.TotalMilliseconds}ms,mps={manaPerSecond}, mup={manaUsagePercentage}, vs={vibrationStrength}, vdms={vibrationDurationMsec}", Color.Magenta);

        AddEvent(VibrationPriority.ManaUsage,
                 vibrationDurationMsec, // ticks -> sec -> msec, + 0.5 sec for fade delay
                 (float)Math.Clamp(vibrationStrength, 0, Instance.MaxManaUsageIntensity),
                 addToFront: false,
                 clearOthers: true);


        // add decrement vibrations for when the weapon stops being used. `clearOthers` means we can safely add
        // other events after this event, and any new `ManaUsageVibration` will replace these events again.

        // decrement the intensity in 20 steps (since lovense has 20 vibration levels, I guess)
        // Also round up division to ensure the decrement delay is never 0
        int msecIncrementDelay = (int)Math.Ceiling(Instance.ManaUsageBuildupTimeMsec / 20.0);

        if (Instance.Debug.Enabled && Instance.Debug.ManaAmmoUsageMessages)
            tChat.LogToPlayer($"adding `{Instance.ManaUsageBuildupTimeMsec/(double)msecIncrementDelay}` events dur={vibrationDurationMsec}+offsets,offset={msecIncrementDelay}", Color.Magenta);

        for (int msecOffset = msecIncrementDelay; msecOffset < Instance.ManaUsageBuildupTimeMsec; msecOffset += msecIncrementDelay)
        {
            // strength * progress. The loop iterates in 20 steps, so it starts with
            // `strength * (1 - (x/20) / 20)` which is `strength * (1 - 1/20)` aka `strength * 19/20`.
            // Each iteration, x increases (msecOffset += msecIncrementDelay) so more is subtracted
            // until `strength * 1/20`.
            // And then VibrationManager sets the strength back to 0 since there are no more events left.
            double strength = vibrationStrength * (1 - msecOffset / (double)Instance.ManaUsageBuildupTimeMsec);
            float clampedStrength = (float)Math.Clamp(strength, 0, Instance.MaxManaUsageIntensity);

            AddEvent(VibrationPriority.ManaUsage,
                     vibrationDurationMsec + msecOffset,
                     clampedStrength,
                     false);
        }
    }

    public static void FishBite()
    {
        if (!Instance.ViberariaEnabled ||
            !Instance.FishingVibrationEnabled)
            return;

        Instance.FishingPattern.PlayPattern(VibrationPriority.Fishing);
    }

    public static void InstrumentVibration(float strength)
    {
        if (!Instance.ViberariaEnabled ||
            !Instance.InstrumentVibrationEnabled)
            return;

        // In case people aren't sure how strong they are making their (or other's) toys vibrate.
        if (Instance.Debug.Enabled && Instance.Debug.InstrumentMessages)
            tChat.LogToPlayer($"Instrument strength: {strength}", Color.LightGoldenrodYellow);

        // remove ongoing events, so the new strength plays as soon as the instrument is played
        ClearEvents(VibrationPriority.Instrument);
        Instance.InstrumentPattern.PlayPattern(VibrationPriority.Instrument, intensityFactor: strength);
    }

    public static void Reset()
    {
        _expectedDebuffDurationTicks = 0;
        AmmoUsages.Clear();
        ManaUsages.Clear();
        Halt();
    }
}
