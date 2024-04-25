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
    private static double _intensity = 0f;
    private static bool _busy = false;
    private static bool _dot = false;
    private static int _duration;
    private static double _AmmoConsumptionRate = 0f;

    private static double AmmoConsumptionRate
    {
        get => _AmmoConsumptionRate;
        set => _AmmoConsumptionRate = value > Instance.MaxIntensity ? Instance.MaxIntensity : value;
    }
    private static double Intensity
    {
        get => _intensity;
        set => _intensity = value > Instance.MaxIntensity ? Instance.MaxIntensity : value;
    }

    private static bool CheckIfDead()
        => Main.clientPlayer.dead;
    
    public static async void HealthUpdated(double currentHp, double maxHp)
    {
        if(!Instance.ViberariaEnabled)
            return;
        if(!Instance.HealthVibratationScalingEnabled)
            return;
        
        var dead = CheckIfDead();
        if (_busy || _client.Connected == false || dead || _dot)
            return;
        
        double range = Instance.MaxIntensity - Instance.MinIntensity;
        double normalizedValue = (1 - (currentHp / maxHp)) * range + Instance.MinIntensity;
        double parsedValue = Double.Parse(normalizedValue.ToString("N2"));
        if (Math.Abs(_intensity - parsedValue) > .1)
            Intensity = parsedValue;
        
        foreach (var device in _client.Devices)
        {
            await device.VibrateAsync(Intensity);
        }
    }

    public static async void Damaged(Player.HurtInfo hurtInfo, bool alive)
    {
        if(!Instance.ViberariaEnabled)
            return;
        if(!Instance.DamageVibrationEnabled)
            return;
        if (_busy || _client.Connected == false || _dot || !alive)
            return;
        _busy = true;

        foreach (var device in _client.Devices)
        {
            await device.VibrateAsync(1f).ConfigureAwait(false);
        }

        var dead = CheckIfDead();
        if (dead || _dot)
            return;
        
        await Task.Delay(1000).ConfigureAwait(false);
        _busy = false;
    }

    public static async void Died(PlayerDeathReason damageSource, int respawnTimer)
    {
        if(!Instance.ViberariaEnabled)
            return;
        if(!Instance.RespawnTimerVibrationEnabled)
            return;
        _dot = false;
        _duration = 0;
        await Task.Delay(500);
        foreach (var device in _client.Devices)
        {
            await device.VibrateAsync(1f).ConfigureAwait(false);
        }
        await Task.Delay(respawnTimer);
        _busy = false;
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
        while (_duration > 0)
        {
            if (_duration == 0 || _dot == false)
                return;
            _dot = true;
            tChat.LogToPlayer($"{_duration}", Color.Red);
            foreach (var device in _client.Devices)
            {
                await device.VibrateAsync(1);
                await Task.Delay(500);
                await device.VibrateAsync(.5);
                await Task.Delay(500);
            }
            _duration -= 60;
        }
        if (_duration < 0)
            _dot = false;
    }

    public static async void PotionVibration(Item item, bool quickHeal, int healValue)
    {
        if(!Instance.ViberariaEnabled)
            return;
        if(!Instance.PotionUseVibrationEnabled)
            return;
        _busy = true;
        await Task.Delay(250);
        foreach (var device in _client.Devices)
        {
            await device.VibrateAsync(1);
            await Task.Delay(2500);
        }

        _busy = false;
    }

    public static void SoIStartedBlasting(Item weapon, Item ammo)
    {
        //Not implemented
        AmmoConsumptionRate += .01;
    }

    public static async void Halt()
    {
        foreach (var device in _client.Devices)
        {
            await device.VibrateAsync(0f).ConfigureAwait(false);
        }
        _busy = false;
    }

    public static void Reset()
    {
        _busy = false;
        _dot = false;
        _duration = 0;
    }
}