using System.Threading;
using Terraria.ModLoader;
using Viberaria.Buttplug;

namespace Viberaria;

public class ViberariaSystem : ModSystem
{
    private readonly Thread _buttplugThread = new Thread(Client.Connect);

    public override void Load()
        => _buttplugThread.Start();

    public override void Unload()
        => Client.Kill();
}