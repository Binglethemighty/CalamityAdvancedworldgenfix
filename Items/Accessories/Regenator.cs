using CalamityMod.CalPlayer;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Accessories
{
    public class Regenator : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Accessories";
        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 56;
            Item.value = CalamityGlobalItem.RarityLimeBuyPrice;
            Item.rare = ItemRarityID.Lime;
            Item.defense = 4; // This should have less defense due to the regen it provides
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityPlayer modPlayer = player.Calamity();
            modPlayer.regenator = true;

            //Hard-lock the player's health to 50%.
            //No lifesteal, no regen, no healing pots
            if (player.statLife >= (int)(player.statLifeMax2 * 0.5f))
            {
                player.statLife = (int)(player.statLifeMax2 * 0.5f);
                player.moonLeech = true;
                modPlayer.healingPotionMultiplier = 0;

                if (player.lifeRegenCount > 0)
                    player.lifeRegenCount = 0;
            }
        }
    }
}
