using System;
using System.Threading.Tasks;
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

    public static void ManaUsageVibration(Item weapon)
    {
        if (!Instance.ViberariaEnabled ||
            // !Instance. mana usage vibration enabled ||
            !_client.Connected)
            return;
        
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