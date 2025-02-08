using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

using static Viberaria.bVibration;
using static Viberaria.VibrationManager.VibrationManager;
using static Viberaria.bClient;
using static Viberaria.Config.ViberariaConfig;


namespace Viberaria;

public class tPlayer : ModPlayer
{
    private Dictionary<int, int> _activeDebuffsDuration = new();

    public override void OnEnterWorld()
    {
        DebuffsSelected = FindModBuffs(Instance.Debuffs.DebuffNames);
        ClientConnect();
    }

    public override void Load()
        => ClientHandles();

    public override void Unload()
        => ClientRemoveHandles();

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
        Reset(); // first reset to prevent _busy from blocking, then rerun health update
        HealthUpdated(Player.statLife, Player.statLifeMax);
    }

    public override void PostUpdateBuffs()
    {
        if (Main.myPlayer != Player.whoAmI) return;
        int debuffsTime = 0;
        foreach (var buffId in DebuffsSelected)
        {
            int index = Player.FindBuffIndex(buffId);
            if (index == -1)
            {
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