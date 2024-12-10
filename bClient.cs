using System;
using System.Threading;
using System.Threading.Tasks;

using Buttplug.Client;
using Buttplug.Client.Connectors.WebsocketConnector;
using Buttplug.Core.Messages;
using Terraria;
using Color = Microsoft.Xna.Framework.Color;

using static Viberaria.tPlayer;
using static Viberaria.bVibration;
using static Viberaria.tSystem;

namespace Viberaria;

public static class bClient
{
    public static readonly ButtplugClient _client = new("Viberaria");
    private static readonly ButtplugWebsocketConnector _connector = new(new Uri("ws://localhost:12345"));

    public static void ClientHandles()
    {
        _client.DeviceAdded += HandleDeviceAdded;
        _client.DeviceRemoved += HandleDeviceRemoved;
    }

    public static async void ClientConnect()
    {
        if (_client.Connected == true)
            return;

        try
        {
            await _client.ConnectAsync(_connector);
            await Task.Delay(1000);
            await _client.StartScanningAsync();
        }
        catch (Exception ex)
        {
            tChat.LogToPlayer($"Viberaria Error: {ex.Message}", Color.OrangeRed);
            tChat.LogToPlayer("Viberaria: Likely couldn't connect to Intiface. Make sure you have " +
                              "Intiface Central running on this pc or turn off this mod.", Color.Orange);
            ClientConnect();
        }
    }
    public static async void ClientDisconnect()
        => await _client.DisconnectAsync();
    public static async void ClientStartScanning()
        => await _client.StartScanningAsync();

    public static async void ClientStopScanning()
        => await _client.StopScanningAsync();
    private static void HandleDeviceAdded(object obj, DeviceAddedEventArgs args)
        => tChat.LogToPlayer($"{args.Device.Name} has been added!", Color.Fuchsia);
    private static void HandleDeviceRemoved(object obj, DeviceRemovedEventArgs args)
        => tChat.LogToPlayer($"{args.Device.Name} has been removed!", Color.Aqua);
}