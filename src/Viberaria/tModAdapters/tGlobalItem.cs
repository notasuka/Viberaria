using Terraria;
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
}