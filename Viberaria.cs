using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Viberaria;

public class Viberaria : Mod
{
    public override void Load()
    {
        On_Player.PlayGuitarChord += OnGuitarPlay;
        base.Load();
    }

    private void OnGuitarPlay(On_Player.orig_PlayGuitarChord orig, Player self, float range)
    {
        bVibration.Instrument(range);
        orig(self, range);
    }
}