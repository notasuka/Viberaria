using System;
using System.Threading.Tasks;

using Buttplug.Client;
using Buttplug.Client.Connectors.WebsocketConnector;
using Buttplug.Core;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using static Viberaria.ViberariaConfig;


namespace Viberaria;

public static class bClient
{
    public static readonly ButtplugClient _client = new("Viberaria");
    private static ButtplugWebsocketConnector _connector;
    private static bool connected = false;

    public static void ClientHandles()
    {
        _client.DeviceAdded += HandleDeviceAdded;
        _client.DeviceRemoved += HandleDeviceRemoved;
        _client.ServerDisconnect += HandleServerDisconnect;
    }

    public static async void ClientConnect()
    {
        if (_client.Connected ||
            (tSystem.tSys != null && !tSystem.tSys.WorldLoaded) ||
            !Instance.ViberariaEnabled)
            return;

        try
        {
            _connector = new ButtplugWebsocketConnector(new Uri("ws://" + IntifaceConnectionAddress));
            await _client.ConnectAsync(_connector);
            connected = true;
            tChat.LogToPlayer("Connected to Intiface!", Color.Aqua);
            await _client.StartScanningAsync();
        }
        catch (ButtplugClientConnectorException ex)
        {
            tChat.LogToPlayer("Viberaria: ConnectorException. Make sure Intiface Central is running. Change the IP " +
                              "in the mod config or toggle 'Viberaria Enable'.",
                Color.Orange);
            ModContent.GetInstance<Viberaria>().Logger.WarnFormat("Couldn't connect to Intiface Client\n{0}: {1}\n{2}",
                ex.GetType(), ex.Message, ex.StackTrace);
            await Task.Delay(4000);
            ClientConnect();
        }
        catch (ButtplugHandshakeException ex)
        {
            tChat.LogToPlayer($"Viberaria: HandshakeException. \"{ex.Message}\". Rejoin the world to retry.",
                Color.Orange);
            // Don't attempt to reconnect.
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            // This is likely because there is already a connection.
            tChat.LogToPlayer($"Viberaria: SocketException. \"{ex.Message}\". You may have to restart your game to " +
                              $"fix this error.", Color.Orange);
            ModContent.GetInstance<Viberaria>().Logger.ErrorFormat("Couldn't connect to Intiface Client\n{0}: {1}\n{2}",
                ex.GetType(), ex.Message, ex.StackTrace);
            await Task.Delay(4000);
            ClientConnect();
        }
        catch (Exception ex)  // Generic error logging just in case..
        {
            tChat.LogToPlayer("Viberaria: Likely couldn't connect to Intiface. Make sure you have Intiface Central " +
                              "running on this pc or disable the mod in the mod configuration.", Color.Orange);
            ModContent.GetInstance<Viberaria>().Logger.ErrorFormat("Couldn't connect to Intiface Client\n{0}: {1}\n{2}",
                ex.GetType(), ex.Message, ex.StackTrace);
            await Task.Delay(4000);
            ClientConnect();
        }
    }

    public static async void ClientRemoveHandles()
    {
        try
        {
            connected = false;
            await _client.DisconnectAsync();
            _client.DeviceAdded -= HandleDeviceAdded;
            _client.DeviceRemoved -= HandleDeviceRemoved;
            _client.ServerDisconnect -= HandleServerDisconnect;
        }
        catch (Exception ex)
        {
            ModContent.GetInstance<Viberaria>().Logger.ErrorFormat("Couldn't disconnect from Intiface Client\n{0}: {1}\n{2}",
                ex.GetType(), ex.Message, ex.StackTrace);
        }
    }

    private static void HandleDeviceAdded(object obj, DeviceAddedEventArgs args)
        => tChat.LogToPlayer($"{args.Device.Name} has been added!", Color.Fuchsia);
    private static void HandleDeviceRemoved(object obj, DeviceRemovedEventArgs args)
        => tChat.LogToPlayer($"{args.Device.Name} has been removed!", Color.Aqua);

    private static void HandleServerDisconnect(object obj, EventArgs args)
    {
        tChat.LogToPlayer("Intiface server disconnected!" + (connected ? " Attempting reconnect..." : ""), Color.Aqua);
        if (connected) ClientConnect();
    }
}