﻿using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Accessories
{
    public class CoinofDeceit : ModItem, ILocalizedModType
    {
        public string LocalizationCategory => "Items.Accessories";
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 22;
            Item.value = CalamityGlobalItem.Rarity1BuyPrice;
            Item.accessory = true;
            Item.rare = ItemRarityID.Blue;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.Calamity().stealthStrike85Cost = true;
            player.GetCritChance<ThrowingDamageClass>() += 6;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddRecipeGroup("AnyCopperBar", 12).
                AddRecipeGroup("AnyEvilBar", 8).
                AddTile(TileID.Anvils).
                Register();
        }
    }
}
