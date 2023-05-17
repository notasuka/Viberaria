using System;
using System.Threading.Tasks;
using Buttplug.Client;
using Microsoft.Xna.Framework;
using Terraria;
using Viberaria.Utilities;

namespace Viberaria.Buttplug;

public static class Vibrate
{
    private const double MaxIntensity = 1f;
    private static double _intensity = 0f;
    private static bool _vibrateBusy = false;
    
    public static double Intensity
    {
        get => _intensity;
        set
        {
            if (value > MaxIntensity)
                _intensity = MaxIntensity;
            else
                _intensity = value;
        }
    }

    public static void HealthChanged(double currentHp, double maxHp)
    {
        if (_vibrateBusy)
            return;

        double minValue = 0.2; // Minimum value
        double maxValue = 1.0; // Maximum value
        double range = maxValue - minValue;
        double normalizedValue = (1 - (currentHp / maxHp)) * range + minValue;
        double parsedValue = Double.Parse(normalizedValue.ToString("N2"));

        if (Math.Abs(_intensity - parsedValue) > .1)
        {
            Intensity = parsedValue;
            SendValue();
        }
    }

    private static void SendValue()
    {
        ButtplugClientDevice[] devices = Client.Buttplug.Devices;

        foreach (ButtplugClientDevice device in devices)
        { 
            device.VibrateAsync(_intensity);
        }
    }
    
    public static async Task SendValue(double speed, int duration)
    {
        if (_vibrateBusy)
            return;
        
        _vibrateBusy = true;
        ButtplugClientDevice[] devices = Client.Buttplug.Devices;
        Intensity = speed;

        foreach (ButtplugClientDevice device in devices)
        {
            await device.VibrateAsync(_intensity);
        }
        await Task.Delay(duration);
        _vibrateBusy = false;
    }
}