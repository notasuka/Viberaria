using System;
using System.Linq;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Viberaria.IntifaceConnection;
using static Viberaria.Config.ViberariaConfig;

namespace Viberaria.tModAdapters;

public class tMod : Mod
{
    public override void Load()
    {
        On_Player.PlayGuitarChord += OnGuitarPlay;
        On_SoundEngine.PlaySound_refSoundStyle_Nullable1_SoundUpdateCallback += OnSoundPlay;
        On_Player.PlayDrums += OnDrumsPlay;
        base.Load();
    }

    private void PlayInstrument(string instrumentName, float range)
    {
        if (Instance.Instruments.PrintInstrumentsToChatWhenUsed)
            tChat.LogToPlayer("Sound-playing instrument triggered: " + instrumentName, Color.YellowGreen);
        if (Instance.Instruments.InstrumentNames.Contains(instrumentName))
            Vibration.InstrumentVibration(range);
    }

    private void PlayInstrument(Player player, float range)
    {
        string instrumentName = player.HeldItem.Name;
        if (Instance.Instruments.PrintInstrumentsToChatWhenUsed)
            tChat.LogToPlayer("Instrument used: " + instrumentName, Color.Green);
        if (Instance.Instruments.InstrumentNames.Contains(instrumentName))
            Vibration.InstrumentVibration(range);
    }

    /// <summary>
    /// These are sounds that are played in the Player.ItemCheck_PlayInstruments function. These are checked in
    /// <see cref="OnSoundPlay"/> to trigger PlayInstrument. Depending on the player's held item, it will trigger
    /// <see cref="Vibration.InstrumentVibration"/>.
    /// </summary>
    private static readonly SoundStyle[] ValidInstrumentSounds = [
        SoundID.Item26, // Item.type = 508
        SoundID.Item35, // Item.type = 507
        SoundID.Item47 // Item.type = 1305
    ];

    private SlotId OnSoundPlay(On_SoundEngine.orig_PlaySound_refSoundStyle_Nullable1_SoundUpdateCallback orig, ref SoundStyle style, Vector2? position, SoundUpdateCallback updatecallback)
    {
        if (!ValidInstrumentSounds.Contains(style))
            return orig(ref style, position, updatecallback);

        // We don't actually know which player is holding the instrument item. So... Iterate through all of
        // them and see if one of them matches?
        foreach (Player player in Main.player)
        {
            if (player.name == "") continue; // Main starts with 256 players, all named "" by default.
            // recreate Player.ItemCheck_PlayInstruments position check
            position ??= new Vector2(Main.mouseX, Main.mouseY);
            int width = player.width;
            int height = player.height;

            Vector2 playerPosition = new Vector2(position.Value.X + (float)width * 0.5f, position.Value.Y + (float)height * 0.5f);
            float absoluteCursorX = Main.mouseX + Main.screenPosition.X - playerPosition.X;
            float absoluteCursorY = Main.mouseY + Main.screenPosition.Y - playerPosition.Y;
            float cursorDistance = (float)Math.Sqrt(absoluteCursorX * absoluteCursorX + absoluteCursorY * absoluteCursorY);
            float screenSizePosition = Main.screenHeight / Main.GameViewMatrix.Zoom.Y;
            cursorDistance /= screenSizePosition / 2f;
            if (cursorDistance > 1f)
                cursorDistance = 1f;

            PlayInstrument(player.HeldItem.Name, cursorDistance);
        }

        return orig(ref style, position, updatecallback);
    }

    private void OnDrumsPlay(On_Player.orig_PlayDrums orig, Player self, float range)
    {
        PlayInstrument(self.HeldItem.Name, range);
        orig(self, range);
    }

    private void OnGuitarPlay(On_Player.orig_PlayGuitarChord orig, Player self, float range)
    {
        PlayInstrument(self.HeldItem.Name, range);
        orig(self, range);
    }
}