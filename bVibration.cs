using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using static Viberaria.bClient;
using static Viberaria.ViberariaConfig;

namespace Viberaria;

public static class bVibration
{
    private static int _busyCount = 0;
    private static bool _debuffActive = false;
    private static float _debuffDuration;
    private static double _ammoConsumptionRate = 0f;

    private static double AmmoConsumptionRate
    {
        get => _ammoConsumptionRate;
        set => _ammoConsumptionRate = value > Instance.HealthMaxIntensity ? Instance.HealthMaxIntensity : value;
    }
    private static bool CheckIfDead()
        => Main.clientPlayer.dead;

    public static void HealthUpdated(int currentHp, int maxHp)
    {
        if(!Instance.ViberariaEnabled)
            return;
        if(!Instance.HealthVibratationScalingEnabled)
            return;

        var dead = CheckIfDead();
        if (_busyCount > 0 || _client.Connected == false || dead || _debuffActive)
            return;

        float range = Instance.HealthMaxIntensity - Instance.HealthMinIntensity;
        float normalizedValue = (1 - currentHp / (float)maxHp) * range + Instance.HealthMinIntensity;

        VibrateAllDevices(normalizedValue);
    }

    public static async void Damaged(Player.HurtInfo hurtInfo, bool alive, int maxHp)
    {
        if(!Instance.ViberariaEnabled || !Instance.DamageVibrationEnabled) return;

        if (_client.Connected == false || _debuffActive || !alive)
            return;
        _busyCount++;

        float damageStrength;
        if (Instance.StaticDamageVibration)
        {
            damageStrength = Instance.DamageVibrationIntensity;
        }
        else
        {
            damageStrength = hurtInfo.Damage / (float)maxHp;
        }

        VibrateAllDevices(damageStrength);
        await Task.Delay(Instance.DamageVibrationDurationMsec).ConfigureAwait(false);

        _busyCount--;
        HandleNotBusy();
    }

    public static async void Died(PlayerDeathReason damageSource, int respawnTimer)
    {
        if(!Instance.ViberariaEnabled)
            return;
        if(!Instance.DeathVibrationEnabled)
            return;
        _debuffActive = false;
        _debuffDuration = 0;
        _busyCount++;
        await Task.Delay(200); // i assume "give some time for Damaged/Debuff to set vibration speed"
        VibrateAllDevices(Instance.DeathVibrationIntensity);

        int deathDelay;
        if (Instance.StaticDeathVibrationLength)
        {
            deathDelay = Instance.DeathVibrationDurationMsec - 200;
        }
        else
        {
            deathDelay = respawnTimer / 60 * 1000 - 200; // timer / tickSpeed * (msec in a sec) - alreadyPassedTime
            // respawnTimer is independent of dayRate (daytime speed) thus will always be 60 (afaict)

        }
        await Task.Delay(Math.Max(0, deathDelay));

        _busyCount = 0;
        HandleNotBusy();
    }

    public static async Task DamageOverTimeVibration(int durationTicks)
    {
        if(!Instance.ViberariaEnabled)
            return;
        if(!Instance.DebuffVibrationEnabled)
            return;
        if (_debuffActive)
            return;
        _debuffDuration = durationTicks / 60f; // secs
        _debuffActive = true;

        bool setHigh = true; // start with the max intensity
        while (_debuffDuration > 0)
        {
            VibrateAllDevices(setHigh ? Instance.DebuffMaxIntensity : Instance.DebuffMinIntensity);
            setHigh = !setHigh;
            await Task.Delay(Instance.DebuffDelayMsec);
            _debuffDuration -= Instance.DebuffDelayMsec / 1000f;
        }

        HandleNotBusy();
        _debuffActive = false;
    }

    public static async void PotionVibration(Item item)
    {
        if(!Instance.ViberariaEnabled)
            return;
        if(!Instance.PotionUseVibrationEnabled)
            return;
        _busyCount++;
        VibrateAllDevices(Instance.PotionVibrationIntensity);
        await Task.Delay(Instance.PotionVibrationDurationMsec);
        _busyCount--;
        HandleNotBusy();
    }

    public static void SoIStartedBlasting(Item weapon, Item ammo)
    {
        //Not implemented
        AmmoConsumptionRate += .01;
    }

    static async void VibrateAllDevices(float strength)
    {
        tChat.LogToPlayer($"Vibrating at `{strength}`", Color.Lime);

        foreach (var device in _client.Devices)
        {
            await device.VibrateAsync(strength * Instance.VibratorMaxIntensity).ConfigureAwait(false);
        }
    }

    static void HandleNotBusy()
    {
        if (_busyCount <= 0)
        {
            _busyCount = 0;
            Halt();
        }
    }

    public static void Halt()
    {
        VibrateAllDevices(0f);
    }

    public static void Reset()
    {
        _busyCount = 0;
        _debuffActive = false;
        _debuffDuration = 0;
    }
}