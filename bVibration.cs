using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Viberaria.VibrationManager;
using static Viberaria.bClient;
using static Viberaria.Config.ViberariaConfig;
using static Viberaria.VibrationManager.VibrationManager;

namespace Viberaria;

public static class bVibration
{
    private static LinkedList<(DateTime, int)> _manaUsages = new();
    private static LinkedList<DateTime> _ammoUsages = new();
    private static int expectedDebuffDuration = 0;

    private static bool PlayerIsDead => Main.clientPlayer.dead;

    public static void HealthUpdated(int currentHp, int maxHp)
    {
        if(!Instance.ViberariaEnabled ||
           !Instance.HealthVibratationScalingEnabled ||
           !_client.Connected)
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
           !Instance.DamageVibrationEnabled ||
           !_client.Connected)
            return;

        if (hurtInfo.Damage < Instance.MinimumDamageForVibration) return;

        float damageStrength;
        if (Instance.StaticDamageVibration)
        {
            damageStrength = Instance.StaticDamageVibrationIntensity;
        }
        else
        {
            damageStrength = hurtInfo.Damage / (float)maxHp;
        }

        AddEvent(VibrationPriority.Hurt, Instance.DamageVibrationDurationMsec, damageStrength, true);
    }

    public static void Died(int respawnTimer)
    {
        if(!Instance.ViberariaEnabled ||
           !Instance.DeathVibrationEnabled ||
           !_client.Connected)
            return;
        ClearEvents(VibrationPriority.Debuff);
        Instance.DeathPattern.PlayPattern(VibrationPriority.Death);
    }

    public static void DebuffVibration(int durationTicks)
    {
        // durationTicks should be length of the player's longest active debuff.
        if(!Instance.ViberariaEnabled ||
           !Instance.DebuffVibrationEnabled ||
           !_client.Connected)
            return;

        // This function is called every tick, so the highest debuff duration should decrease by 1 every tick.
        // This is to reduce unnecessarily clearing the ongoing debuff vibration events.
        if (expectedDebuffDuration > 0) expectedDebuffDuration--;

        if (durationTicks == expectedDebuffDuration)
            return;
        expectedDebuffDuration = durationTicks;

        if (durationTicks == 0)
        {
            ClearEvents(VibrationPriority.Debuff, update: true);
            return;
        }

        ClearEvents(VibrationPriority.Debuff);
        int durationMsec = (int)(durationTicks / 60.0 * 1000);

        TimeSpan offset = TimeSpan.Zero;

        while (offset.TotalMilliseconds < durationMsec)
        {
            Instance.DebuffPattern.PlayPattern(VibrationPriority.Debuff, offset, durationMsec);
            offset += new TimeSpan(0,0,0,0,Instance.DebuffPattern.PatternLength);
        }
    }

    public static void PotionVibration()
    {
        if(!Instance.ViberariaEnabled ||
           !Instance.PotionUseVibrationEnabled ||
           !_client.Connected)
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
        while (_ammoUsages.First != null && _ammoUsages.First.Value < endTime)
        {
            _ammoUsages.RemoveFirst();
        }

        return _ammoUsages.Count;
    }

    public static void SoIStartedBlasting(Item weapon, Item ammo)
    {
        // This function was largely copied from ManaUsageVibration().
        if (!Instance.ViberariaEnabled ||
            !Instance.BlastingEnabled ||
            !_client.Connected)
            return;

        _ammoUsages.AddLast(DateTime.Now);

        TimeSpan timespan = new TimeSpan(ticks: Instance.BlastingBuildupTimeMsec * 10_000);
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
        while (_manaUsages.First != null && _manaUsages.First.Value.Item1 < endTime)
        {
            _manaUsages.RemoveFirst();
        }

        int sum = 0;
        foreach ((DateTime _, int count) in _manaUsages)
        {
            sum += count;
        }

        return sum;
    }

    public static void ManaUsageVibration(Item item, Player player)
    {
        if (!Instance.ViberariaEnabled ||
            !Instance.ManaUsageVibrationEnabled ||
            !_client.Connected)
            return;
        if (player.GetManaCost(item) == 0) return;

        _manaUsages.AddLast((DateTime.Now, player.GetManaCost(item)));

        TimeSpan timespan = new TimeSpan(ticks: Instance.ManaUsageBuildupTimeMsec * 10_000);
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
            !Instance.FishingVibrationEnabled ||
            !_client.Connected)
            return;

        Instance.FishingPattern.PlayPattern(VibrationPriority.Fishing);
    }

    public static void Instrument(float strength)
    {
        if (!Instance.ViberariaEnabled ||
            !Instance.InstrumentVibrationEnabled ||
            !_client.Connected)
            return;

        float multipliedStrength = strength * Instance.InstrumentIntensityFactor;
        if (Instance.Debug.Enabled && Instance.Debug.InstrumentMessages)
            tChat.LogToPlayer($"Instrument strength: {multipliedStrength}", Color.LightGoldenrodYellow);

        AddEvent(VibrationPriority.Instrument, Instance.InstrumentDurationMsec, multipliedStrength, true, clearOthers: true);
    }

    public static void Reset()
    {
        expectedDebuffDuration = 0;
    }
}