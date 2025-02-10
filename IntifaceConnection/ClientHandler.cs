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

    private static bool _attemptingConnection = false;

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
        if (_attemptingConnection) return;
        // to prevent ClientConnect() being called multiple times at the same time.
        _attemptingConnection = true;
        // Using a while loop instead of recursively calling ClientConnect after a Task.Delay()

        while (!Client.Connected &&
               tSystem.Sys != null && tSystem.Sys.WorldLoaded &&
               Instance.ViberariaEnabled)
        {
            try
            {
                _connector = new ButtplugWebsocketConnector(new Uri("ws://" + IntifaceConnectionAddress));
                await Client.ConnectAsync(_connector);
                _connected = true;
                _attemptingConnection = false;
                tChat.LogToPlayer("Connected to Intiface!", Color.Aqua);
                ClientHandles();
                await Client.StartScanningAsync();
            }
            catch (ButtplugClientConnectorException ex)
            {
                tChat.LogToPlayer(
                    "Viberaria: ConnectorException. Make sure Intiface Central is running. Change the IP " +
                    "in the mod config or toggle 'Viberaria Enable'.",
                    Color.Orange);
                tChat.Logger.WarnFormat("Couldn't connect to Intiface Client\n{0}: {1}\n{2}",
                    ex.GetType(), ex.Message, ex.StackTrace);
                await Task.Delay(4000);
            }
            catch (ButtplugHandshakeException ex)
            {
                tChat.LogToPlayer($"Viberaria: HandshakeException. \"{ex.Message}\". Rejoin the world to retry.",
                    Color.Orange);
                break;
                // Don't attempt to reconnect.
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                // This is likely because there is already a connection.
                tChat.LogToPlayer(
                    $"Viberaria: SocketException. \"{ex.Message}\". You may have to restart your game to " +
                    $"fix this error.", Color.Orange);
                tChat.Logger.ErrorFormat("Couldn't connect to Intiface Client\n{0}: {1}\n{2}",
                    ex.GetType(), ex.Message, ex.StackTrace);
                await Task.Delay(4000);
            }
            catch (Exception ex) // Generic error logging just in case..
            {
                tChat.LogToPlayer(
                    "Viberaria: Likely couldn't connect to Intiface. Make sure you have Intiface Central " +
                    "running on this pc or disable the mod in the mod configuration.", Color.Orange);
                tChat.Logger.ErrorFormat("Couldn't connect to Intiface Client\n{0}: {1}\n{2}",
                    ex.GetType(), ex.Message, ex.StackTrace);
                await Task.Delay(4000);
            }

        }

        _attemptingConnection = false;
    }

    /// <summary>
    /// Handle disconnecting from Intiface Central and unsubscribing from its events.
    /// </summary>
    public static async void ClientDisconnect()
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