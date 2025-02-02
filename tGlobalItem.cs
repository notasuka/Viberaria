using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

using static Viberaria.bVibration;

namespace Viberaria;

public class tGlobalItem : GlobalItem
{
    public override void OnConsumeItem(Item item, Player player)
    {
        if (item.type == ItemID.HealingPotion ||
            item.type == ItemID.GreaterHealingPotion ||
            item.type == ItemID.SuperHealingPotion ||
            item.type == ItemID.ManaPotion ||
            item.type == ItemID.GreaterManaPotion ||
            item.type == ItemID.SuperManaPotion)
        {
            if (player == Main.LocalPlayer)
                PotionVibration(item);
        }
    }

    public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type,
        int damage, float knockback)
    {
        if (player == Main.LocalPlayer &&
            player.GetManaCost(item) > 0)
        {
            ManaUsageVibration(item, player);
        }

        return base.Shoot(item, player, source, position, velocity, type, damage, knockback);
    }
}