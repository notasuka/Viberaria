using Terraria;
using Terraria.ModLoader;
using Viberaria.IntifaceConnection;

namespace Viberaria.tModAdapters;

public class tMod : Mod
{
    public override void Load()
    {
        On_Player.PlayGuitarChord += OnGuitarPlay;
        base.Load();
    }

    private void OnGuitarPlay(On_Player.orig_PlayGuitarChord orig, Player self, float range)
    {
        Vibration.Instrument(range);
        orig(self, range);
    }
}