using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using static Viberaria.bClient;
using static Viberaria.tPlayer;
using static Viberaria.ViberariaConfig;
using static Viberaria.tSystem;

namespace Viberaria;

public static class bVibration
{
    private static float _intensity = 0f;
    private static int _busyCount = 0;
    private static bool _dot = false;
    private static int _duration;
    private static double _ammoConsumptionRate = 0f;

    private static double AmmoConsumptionRate
    {
        get => _ammoConsumptionRate;
        set => _ammoConsumptionRate = value > Instance.MaxIntensity ? Instance.MaxIntensity : value;
    }
    private static float Intensity
    {
        get => _intensity;
        set => _intensity = value > Instance.MaxIntensity ? Instance.MaxIntensity : value;
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
        if (_busyCount > 0 || _client.Connected == false || dead || _dot)
            return;

        float range = Instance.MaxIntensity - Instance.MinIntensity;
        float normalizedValue = (1 - currentHp / (float)maxHp) * range + Instance.MinIntensity;
        if (Math.Abs(Intensity - normalizedValue) > .05f)
            Intensity = normalizedValue;

        VibrateAllDevices(Intensity);
    }

    public static async void Damaged(Player.HurtInfo hurtInfo, bool alive, int maxHp)
    {
        if(!Instance.ViberariaEnabled || !Instance.DamageVibrationEnabled) return;

        if (_client.Connected == false || _dot || !alive)
            return;
        _busyCount++;

        float relativeDamagePercentage = hurtInfo.Damage / (float)maxHp;
        VibrateAllDevices(relativeDamagePercentage);
        await Task.Delay(600).ConfigureAwait(false);

        _busyCount--;
        HandleNotBusy();
    }

    public static async void Died(PlayerDeathReason damageSource, int respawnTimer)
    {
        if(!Instance.ViberariaEnabled)
            return;
        if(!Instance.RespawnTimerVibrationEnabled)
            return;
        _dot = false;
        _duration = 0;
        _busyCount++;
        await Task.Delay(200); // i assume "give some time for Damaged to set vibration speed"
        VibrateAllDevices(0.7f);

        // int deathDelay = respawnTimer / 60 * 1000 - 200; // timer / tickSpeed * (msec in a sec) - alreadyPassedTime
        // // respawnTimer is independent of dayRate (daytime speed) thus will always be 60 (afaict)
        // tChat.LogToPlayer($"Respawn timer: {deathDelay/1000.0} seconds", Color.Aqua);
        int deathDelay = 1000;
        await Task.Delay(Math.Max(0, deathDelay));

        _busyCount = 0;
        HandleNotBusy();
    }

    public static async Task DamageOverTimeVibration(int duration)
    {
        if(!Instance.ViberariaEnabled)
            return;
        if(!Instance.EffectDamageVibrationEnabled)
            return;
        if (_dot)
            return;
        _duration = duration;
        _dot = true;
        while (_duration > 0)
        {
            tChat.LogToPlayer($"{_duration}", Color.Red);
            VibrateAllDevices(0.45f);
            await Task.Delay(500);
            VibrateAllDevices(0.2f);
            await Task.Delay(500);
            _duration -= 60;
        }

        HandleNotBusy();
        _dot = false;
    }

    public static async void PotionVibration(Item item)
    {
        if(!Instance.ViberariaEnabled)
            return;
        if(!Instance.PotionUseVibrationEnabled)
            return;
        _busyCount++;
        VibrateAllDevices(0.4f);
        await Task.Delay(400);
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
        foreach (var device in _client.Devices)
        {
            await device.VibrateAsync(strength).ConfigureAwait(false);
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
        _dot = false;
        _duration = 0;
    }
}