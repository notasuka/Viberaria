using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Messages;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

using static Viberaria.bVibration;
using static Viberaria.bClient;
using static Viberaria.tSystem;
using static Viberaria.ViberariaConfig;


namespace Viberaria;

public class tPlayer : ModPlayer
{
    private readonly int[] _debuffs = new int[] { 20, 24, 44, 70 };
    public override  void OnEnterWorld()
        => ClientConnect();
    public override void Load()
        => ClientHandles();
    public override void NaturalLifeRegen(ref float regen)
        => HealthUpdated(Player.statLife, Player.statLifeMax);
    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        => Died(damageSource, Player.respawnTimer);
    public override void OnHurt(Player.HurtInfo hurtInfo)
        => Damaged(hurtInfo, Player.dead);
    public override void GetHealLife(Item item, bool quickHeal, ref int healValue)
        => PotionVibration(item, quickHeal, healValue);
    public override void GetHealMana(Item item, bool quickHeal, ref int healValue)
        => PotionVibration(item, quickHeal, healValue);
    public override void OnConsumeAmmo(Item weapon, Item ammo)
        => SoIStartedBlasting(weapon, ammo);
    
    public override void OnRespawn()
    {
        HealthUpdated(Player.statLife, Player.statLifeMax);
        Reset();
    }

    public override async void PreUpdateBuffs()
    {
        foreach (var buffId in _debuffs)
        {
            int index = Player.FindBuffIndex(buffId);
            if (index != -1)
                await DamageOverTimeVibration(Player.buffTime[index]);
        }
    }

    public override void PostUpdate()
    {
        if(!Instance.ViberariaEnabled)
        {
            Reset();
            Halt();
            return;
        }
        if (tSys.WorldLoaded && _client.Connected != true)
        {
            ClientConnect();
        }
    }
}