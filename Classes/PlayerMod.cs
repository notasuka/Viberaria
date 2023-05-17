using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Viberaria.Buttplug;
using Viberaria.Utilities;

namespace Viberaria.Classes
{
    public class PlayerMod : ModPlayer
    {
        public override async void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
            => await Vibrate.SendValue(1f, (Player.respawnTimer / 60) * 1000); //Respawn timer is based on centiseconds, multiplied by 10 for milliseconds

        public override async void PostHurt(Player.HurtInfo info)
        {
            if (Player.dead && Player.statLife >= 0)
                return;
            await Vibrate.SendValue(1f, 1250);
        }

        public override void NaturalLifeRegen(ref float regen)
        {
            if (Player.dead)
                return;
            
            if (Main.myPlayer == Player.whoAmI)
            { 
                Vibrate.HealthChanged(Player.statLife, Player.statLifeMax);
            }
        }

        public override void OnEnterWorld()
            => Client.StartFindingDevices();

        public override void PreSavePlayer()
            => Client.StopFindingDevices();
    }
}