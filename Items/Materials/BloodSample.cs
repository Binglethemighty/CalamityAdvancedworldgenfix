﻿using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace CalamityMod.Items.Materials
{
    public class BloodSample : ModItem, ILocalizedModType
    {
        public string LocalizationCategory => "Items.Materials";
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 25;
			ItemID.Sets.SortingPriorityMaterials[Type] = 68; // Sturdy Fossil
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 34;
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Orange;
        }    }
}
