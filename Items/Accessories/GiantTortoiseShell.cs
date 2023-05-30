﻿using CalamityMod.Buffs.StatDebuffs;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace CalamityMod.Items.Accessories
{
    public class GiantTortoiseShell : ModItem, ILocalizedModType
    {
        public string LocalizationCategory => "Items.Accessories";
        public override void SetDefaults()
        {
            Item.defense = 14;
            Item.width = 20;
            Item.height = 24;
            Item.value = CalamityGlobalItem.Rarity4BuyPrice;
            Item.rare = ItemRarityID.LightRed;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.moveSpeed -= 0.1f;
            player.thorns += 0.25f;
            player.buffImmune[ModContent.BuffType<ArmorCrunch>()] = true;
        }
    }
}
