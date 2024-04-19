using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

using static Viberaria.bVibration;
using static Viberaria.bClient;
using static Viberaria.tSystem;


namespace Viberaria;

public class tPlayer : ModPlayer
{
    public override  void OnEnterWorld()
        => ClientConnect();
    public override void NaturalLifeRegen(ref float regen)
        => HealthUpdated(Player.statLife, Player.statLifeMax);
    public override void Load()
        => ClientHandles();

    public override void PostUpdate()
    {
        if (tSys.WorldLoaded && _client.Connected != true)
        {
            ClientConnect();
        }
    }
}