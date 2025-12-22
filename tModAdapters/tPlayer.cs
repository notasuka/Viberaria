using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using static Viberaria.IntifaceConnection.Vibration;
using static Viberaria.VibrationManager.VibrationManager;
using static Viberaria.IntifaceConnection.ClientHandler;
using static Viberaria.Config.ViberariaConfig;


namespace Viberaria.tModAdapters;

public class tPlayer : ModPlayer
{
    private Dictionary<int, int> _activeDebuffsDuration = new();

    public override void OnEnterWorld()
    {
        if (Main.myPlayer != Player.whoAmI) return;
        DebuffsSelected = FindModBuffs(Instance.Debuffs.DebuffNames);
        ClientConnect();
    }

    public override void Unload()
        => ClientDisconnect();

    public override void NaturalLifeRegen(ref float regen)
    {
        if (Main.myPlayer != Player.whoAmI) return;
        HealthUpdated(Player.statLife, Player.statLifeMax2);
    }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        if (Main.myPlayer != Player.whoAmI) return;
        Died(Player.respawnTimer);
    }

    public override void OnHurt(Player.HurtInfo hurtInfo)
    {
        if (Main.myPlayer != Player.whoAmI) return;
        Damaged(hurtInfo, Player.statLifeMax2);
    }

    public override void OnConsumeAmmo(Item weapon, Item ammo)
    {
        if (Main.myPlayer != Player.whoAmI) return;
        SoIStartedBlasting(weapon, ammo);
    }

    public override void CatchFish(FishingAttempt attempt, ref int itemDrop, ref int npcSpawn, ref AdvancedPopupRequest sonar, ref Vector2 sonarPosition)
    {
        if (Main.myPlayer != Player.whoAmI ||
            itemDrop <= 0
            ) return;
        FishBite();
    }

    public override void OnRespawn()
    {
        if (Main.myPlayer != Player.whoAmI) return;
        Reset(); // Clear any remaining vibration cache.
        HealthUpdated(Player.statLife, Player.statLifeMax2);
    }

    public override void PostUpdateBuffs()
    {
        if (Main.myPlayer != Player.whoAmI) return;

        // Find the longest debuff
        int debuffsTime = 0;

        // Iterate (de)buffs from the config menu.
        foreach (var buffId in DebuffsSelected)
        {
            int index = Player.FindBuffIndex(buffId);
            if (index == -1)
            {
                // The buff is no longer active on the player. Reset its timer.
                _activeDebuffsDuration[buffId] = 0;
            }
            else
            {
                _activeDebuffsDuration[buffId] = Player.buffTime[index];
                if (Player.buffTime[index] > debuffsTime)
                    debuffsTime = Player.buffTime[index];
            }
        }

        DebuffVibration(debuffsTime);
    }

    public override void PostUpdate()
    {
        if (!Instance.ViberariaEnabled)
        {
            Reset();
            Halt();
        }
    }
}
