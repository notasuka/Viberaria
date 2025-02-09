using System;
using System.Threading.Tasks;
using Buttplug.Client;
using Buttplug.Client.Connectors.WebsocketConnector;
using Buttplug.Core;
using Microsoft.Xna.Framework;
using Viberaria.tModAdapters;
using static Viberaria.Config.ViberariaConfig;


namespace Viberaria.IntifaceConnection;

public static class ClientHandler
{
    public static readonly ButtplugClient Client = new("Viberaria");
    private static ButtplugWebsocketConnector _connector;
    /// <summary>
    /// Indicate whether the mod is intentionally connected with Intiface. If Intiface disconnects while
    /// this variable is true, it tries to reconnect.
    /// </summary>
    private static bool _connected = false;

    /// <summary>
    /// Subscribe to intiface events.
    /// </summary>
    public static void ClientHandles()
    {
        Client.DeviceAdded += HandleDeviceAdded;
        Client.DeviceRemoved += HandleDeviceRemoved;
        Client.ServerDisconnect += HandleServerDisconnect;
    }

    public static async void ClientConnect()
    {
        if (Client.Connected ||
            (tSystem.Sys != null && !tSystem.Sys.WorldLoaded) ||
            !Instance.ViberariaEnabled)
            return;

        try
        {
            _connector = new ButtplugWebsocketConnector(new Uri("ws://" + IntifaceConnectionAddress));
            await Client.ConnectAsync(_connector);
            _connected = true;
            tChat.LogToPlayer("Connected to Intiface!", Color.Aqua);
            await Client.StartScanningAsync();
        }
        catch (ButtplugClientConnectorException ex)
        {
            tChat.LogToPlayer("Viberaria: ConnectorException. Make sure Intiface Central is running. Change the IP " +
                              "in the mod config or toggle 'Viberaria Enable'.",
                Color.Orange);
            tChat.Logger.WarnFormat("Couldn't connect to Intiface Client\n{0}: {1}\n{2}",
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
            tChat.Logger.ErrorFormat("Couldn't connect to Intiface Client\n{0}: {1}\n{2}",
                ex.GetType(), ex.Message, ex.StackTrace);
            await Task.Delay(4000);
            ClientConnect();
        }
        catch (Exception ex)  // Generic error logging just in case..
        {
            tChat.LogToPlayer("Viberaria: Likely couldn't connect to Intiface. Make sure you have Intiface Central " +
                              "running on this pc or disable the mod in the mod configuration.", Color.Orange);
            tChat.Logger.ErrorFormat("Couldn't connect to Intiface Client\n{0}: {1}\n{2}",
                ex.GetType(), ex.Message, ex.StackTrace);
            await Task.Delay(4000);
            ClientConnect();
        }
    }

    /// <summary>
    /// Handle disconnecting from Intiface Central and unsubscribing from its events.
    /// </summary>
    public static async void ClientRemoveHandles()
    {
        try
        {
            _connected = false;
            await Client.DisconnectAsync();
            Client.DeviceAdded -= HandleDeviceAdded;
            Client.DeviceRemoved -= HandleDeviceRemoved;
            Client.ServerDisconnect -= HandleServerDisconnect;
        }
        catch (Exception ex)
        {
            tChat.Logger.ErrorFormat("Couldn't disconnect from Intiface Client\n{0}: {1}\n{2}",
                ex.GetType(), ex.Message, ex.StackTrace);
        }
    }

    private static void HandleDeviceAdded(object obj, DeviceAddedEventArgs args)
        => tChat.LogToPlayer($"{args.Device.Name} has been added!", Color.Fuchsia);
    private static void HandleDeviceRemoved(object obj, DeviceRemovedEventArgs args)
        => tChat.LogToPlayer($"{args.Device.Name} has been removed!", Color.Aqua);

    /// <summary>
    /// Log and try to reconnect when intiface disconnects from Viberaria. Only reconnect if the disconnection was unintentional.
    /// </summary>
    /// <param name="obj">The ButtplugClient sender.</param>
    /// <param name="args">Empty event args.</param>
    private static void HandleServerDisconnect(object obj, EventArgs args)
    {
        tChat.LogToPlayer("Intiface server disconnected!" + (_connected ? " Attempting reconnect..." : ""), Color.Aqua);
        if (_connected) ClientConnect();
    }
}