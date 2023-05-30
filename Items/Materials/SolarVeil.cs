﻿using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace CalamityMod.Items.Materials
{
    public class SolarVeil : ModItem, ILocalizedModType
    {
        public string LocalizationCategory => "Items.Materials";
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 25;
        }

        public override void SetDefaults()
        {
            Item.width = 46;
            Item.height = 32;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(silver: 80);
            Item.rare = ItemRarityID.Lime;
        }    }
}
