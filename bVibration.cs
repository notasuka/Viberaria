using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

using static Viberaria.bClient;
using static Viberaria.tPlayer;
using static Viberaria.ViberariaConfig;
using static Viberaria.tSystem;

namespace Viberaria;

public static class bVibration
{
    private static double _intensity = 0f;
    private static bool _busy = false;

    public static double Intensity
    {
        get => _intensity;
        set
        {
            if (value > Instance.MaxIntensity)
                _intensity = Instance.MaxIntensity;
            else
                _intensity = value;
        }
    }
    
    public static async void HealthUpdated(double currentHp, double maxHp)
    {
        if (_busy || _client.Connected == false)
            return;
        
        double range = Instance.MaxIntensity - Instance.MinIntensity;
        double normalizedValue = (1 - (currentHp / maxHp)) * range + Instance.MinIntensity;
        double parsedValue = Double.Parse(normalizedValue.ToString("N2"));
        if (Math.Abs(_intensity - parsedValue) > .1)
            Intensity = parsedValue;
        
        tChat.LogToPlayer($"I: {Intensity} - Min: {Instance.MinIntensity} / Max: {Instance.MaxIntensity}", Color.Gold);
        
        foreach (var device in _client.Devices)
        {
            await device.VibrateAsync(Intensity);
        }
    }

    public static async void Damaged()
    {
        if (_busy || _client.Connected == false)
            return;
        _busy = true;

        foreach (var device in _client.Devices)
        {
            await device.VibrateAsync(1f).ConfigureAwait(false);
        }
        await Task.Delay(1000).ConfigureAwait(false);
        _busy = false;
    }

    public static async void Halt()
    {
        foreach (var device in _client.Devices)
        {
            await device.VibrateAsync(0f).ConfigureAwait(false);
        }
        _busy = false;
    }
}