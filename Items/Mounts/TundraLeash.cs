﻿using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Mounts
{
    public class TundraLeash : ModItem, ILocalizedModType
    {
        public string LocalizationCategory => "Items.Mounts";
        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 16;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.buyPrice(0, 9, 0, 0);
            Item.UseSound = SoundID.NPCHit56;
            Item.noMelee = true;
            Item.mountType = ModContent.MountType<RimehoundMount>();
        }
    }
}
