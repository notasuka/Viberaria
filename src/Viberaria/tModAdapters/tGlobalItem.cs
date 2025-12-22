using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Viberaria.Config;
using static Viberaria.IntifaceConnection.Vibration;

namespace Viberaria.tModAdapters;

public class tGlobalItem : GlobalItem
{
    public override void OnConsumeItem(Item item, Player player)
    {
        if (item.type is ItemID.HealingPotion
                or ItemID.GreaterHealingPotion
                or ItemID.SuperHealingPotion
                or ItemID.ManaPotion
                or ItemID.GreaterManaPotion
                or ItemID.SuperManaPotion)
        {
            if (Main.myPlayer == player.whoAmI)
                PotionVibration();
        }
    }

    public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type,
        int damage, float knockback)
    {
        if (ViberariaConfig.Instance.Debug.Enabled && ViberariaConfig.Instance.Debug.ManaAmmoUsageMessages)
            tChat.LogToPlayer($"Shoot() item:{item.type},cost:{item.mana},mana:{player.GetManaCost(item)},coold:{item.useTime}", Color.MediumPurple);

        if (Main.myPlayer == player.whoAmI &&
            player.GetManaCost(item) > 0)
        {
            ManaUsageVibration(item, player);
        }

        return base.Shoot(item, player, source, position, velocity, type, damage, knockback);
    }
}