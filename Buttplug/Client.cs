using System;
using System.Threading.Tasks;
using Buttplug.Client;
using Buttplug.Client.Connectors.WebsocketConnector;
using Buttplug.Core.Messages;
using Microsoft.Xna.Framework;
using Viberaria.Utilities;

namespace Viberaria.Buttplug
{
    public class Client
    {
        public static readonly ButtplugClient Buttplug = new ButtplugClient("Viberaria");
        private static readonly ButtplugWebsocketConnector Connector = new ButtplugWebsocketConnector(new Uri("ws://localhost:12345/"));

        public static void StartFindingDevices()
            => Buttplug.StartScanningAsync();
        public static void StopFindingDevices()
            => Buttplug.StartScanningAsync();
        
        static void HandleDeviceAdded(object obj, DeviceAddedEventArgs args)
        {
            Chat.Log($"{args.Device.Name} has been added!", Color.Green);
        }

        static void HandleDeviceRemoved(object obj, DeviceRemovedEventArgs args)
        {
            
        }

        public static void Connect()
        {
            Buttplug.DeviceAdded += HandleDeviceAdded;
            Buttplug.DeviceRemoved += HandleDeviceRemoved;
            Buttplug.ConnectAsync(Connector);
        }

        public static void Kill()
        {
            Buttplug.DisconnectAsync();
            Buttplug.Dispose();
        }
    }
}