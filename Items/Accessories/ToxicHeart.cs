using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.CalPlayer;
using CalamityMod.Items.Potions.Alcohol;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityMod.Items.Accessories
{
    public class ToxicHeart : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Accessories";
        public float pulseCounter = 0;
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 32;
            Item.value = CalamityGlobalItem.RarityYellowBuyPrice;
            Item.rare = ItemRarityID.Yellow;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityPlayer modPlayer = player.Calamity();
            modPlayer.toxicHeart = true;
        }
    }
}
