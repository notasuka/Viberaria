using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using Viberaria.VibrationManager;
using static Viberaria.bClient;
using static Viberaria.ViberariaConfig;
using static Viberaria.VibrationManager.VibrationManager;

namespace Viberaria;

public static class bVibration
{
    private static double _ammoConsumptionRate = 0f;
    private static bool _debuffActive = false;
    private static float _debuffDuration = 0f;
    private static LinkedList<(DateTime, int)> _manaUsages = new();
    private static LinkedList<DateTime> _ammoUsages = new();

    private static double AmmoConsumptionRate
    {
        get => _ammoConsumptionRate;
        set => _ammoConsumptionRate = value > Instance.HealthMaxIntensity ? Instance.HealthMaxIntensity : value;
    }

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
            damageStrength = Instance.DamageVibrationIntensity;
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

        int deathDelay;
        if (Instance.StaticDeathVibrationLength)
        {
            deathDelay = Instance.DeathVibrationDurationMsec;
        }
        else
        {
            deathDelay = respawnTimer / 60 * 1000; // timer / tickSpeed * (msec in a sec)
            // respawnTimer is independent of dayRate (daytime speed) thus will always be 60 (afaict)
        }

        AddEvent(VibrationPriority.Death, deathDelay, Instance.DeathVibrationIntensity, true);
    }

    public static async Task DebuffVibration(int durationTicks)
    {
        if(!Instance.ViberariaEnabled ||
           !Instance.DebuffVibrationEnabled ||
           !_client.Connected)
            return;

        if (_debuffActive)
            return;

        _debuffDuration = durationTicks / 60f; // secs
        _debuffActive = true;

        bool setHigh = true; // start with the max intensity
        while (_debuffDuration > 0)
        {
            float strength = setHigh ? Instance.DebuffMaxIntensity : Instance.DebuffMinIntensity;
            AddEvent(VibrationPriority.Debuff, Instance.DebuffDelayMsec+10, strength, false);
            // add 10 to the duration for a neater overlap to prevent gaps in the middle
            setHigh = !setHigh;
            _debuffDuration -= Instance.DebuffDelayMsec / 1000f;
            await Task.Delay(Instance.DebuffDelayMsec);
        }

        _debuffActive = false;
    }

    public static void PotionVibration(Item item)
    {
        if(!Instance.ViberariaEnabled ||
           !Instance.PotionUseVibrationEnabled ||
           !_client.Connected)
            return;

        AddEvent(VibrationPriority.Potion, Instance.PotionVibrationDurationMsec, Instance.PotionVibrationIntensity, true);
    }

    public static void SoIStartedBlasting(Item weapon, Item ammo)
    {
        if (!Instance.ViberariaEnabled ||
            // !Instance. blasting enabled ||
            !_client.Connected)
            return;
        AmmoConsumptionRate += .01;
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

    public static async void FishBite()
    {
        if (!Instance.ViberariaEnabled ||
            !Instance.FishingVibrationEnabled ||
            !_client.Connected)
            return;
        AddEvent(VibrationPriority.Fishing, Instance.FishingLengthMsec1, Instance.FishingIntensity1, false);
        await Task.Delay(Instance.FishingLengthMsec1 + Instance.FishingDelayMsec1);
        AddEvent(VibrationPriority.Fishing, Instance.FishingLengthMsec2, Instance.FishingIntensity2, false);
    }

    public static void Reset()
    {
        _debuffActive = false;
        _debuffDuration = 0;
    }
}