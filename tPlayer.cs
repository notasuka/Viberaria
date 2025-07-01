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
    private readonly int[] _debuffs = { 20, 24, 44, 70 };

    public override void OnEnterWorld()
        => ClientConnect();

    public override void Load()
        => ClientHandles();

    public override void Unload()
        => ClientDisconnect();

    public override void NaturalLifeRegen(ref float regen)
    {
        if (Player == Main.LocalPlayer)
            HealthUpdated(Player.statLife, Player.statLifeMax2);
    }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        if (Player == Main.LocalPlayer)
            Died(damageSource, Player.respawnTimer );
    }
    public override void OnHurt(Player.HurtInfo hurtInfo)
    {
        if (Player == Main.LocalPlayer)
            Damaged(hurtInfo, !Player.dead, Player.statLifeMax2);
    }

    public override void OnConsumeAmmo(Item weapon, Item ammo)
    {
        if (Player == Main.LocalPlayer)
            SoIStartedBlasting(weapon, ammo);
    }
    
    public override void OnRespawn()
    {
        Reset(); // first reset to prevent _busy from blocking, then rerun health update
        HealthUpdated(Player.statLife, Player.statLifeMax2);
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
