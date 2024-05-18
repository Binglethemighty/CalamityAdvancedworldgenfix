﻿using System.Collections.Generic;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;

namespace CalamityMod.Items
{
    public class SirenproofEarMuffs : ModItem, ILocalizedModType
    {
        public static bool state = false;
        public new string LocalizationCategory => "Items.Misc";
        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 34;
            Item.value = CalamityGlobalItem.RarityBlueBuyPrice;
            Item.rare = ItemRarityID.Blue;
        }

        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup)
        {
            itemGroup = (ContentSamples.CreativeHelper.ItemGroup)CalamityResearchSorting.SpawnPrevention;
        }

        public override bool CanRightClick() => true;

        public override void RightClick(Player player)
        {
            if (player.Calamity().disableAnahitaSpawns == true)
                player.Calamity().disableAnahitaSpawns = false;
            else
                player.Calamity().disableAnahitaSpawns = true;
            Item.NetStateChanged();
            state = player.Calamity().disableAnahitaSpawns;

            bool favorited = Item.favorited;
            Item.SetDefaults(ModContent.ItemType<SirenproofEarMuffs>());
            Item.stack++;
            Item.favorited = favorited;
        }

        /*
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frameI, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture;

            if (state)
            {
                //Replace with enabled texture
                texture = ModContent.Request<Texture2D>("CalamityMod/Items/SirenproofEarMuffs").Value;
                spriteBatch.Draw(texture, position, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0);
            }
            else
            {
                texture = ModContent.Request<Texture2D>("CalamityMod/Items/SirenproofEarMuffs").Value;
                spriteBatch.Draw(texture, position, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0);
            }

            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture;

            if (state)
            {
                //Replace with enabled texture
                texture = ModContent.Request<Texture2D>("CalamityMod/Items/SirenproofEarMuffs").Value;
                spriteBatch.Draw(texture, Item.position - Main.screenPosition, null, lightColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
            }
            else
            {
                texture = ModContent.Request<Texture2D>("CalamityMod/Items/SirenproofEarMuffs").Value;
                spriteBatch.Draw(texture, Item.position - Main.screenPosition, null, lightColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
            }
            return false;
        }
        */

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string text;
            if (state == true)
                text = GetTextValue("Items.Misc.SpawnBlockersOn");
            else
                text = GetTextValue("Items.Misc.SpawnBlockersOff");
            tooltips.FindAndReplace("[STATE]", text);
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.FlinxFur, 2).
                AddIngredient(ItemID.Silk, 5).
                AddTile(TileID.Anvils).
                Register();
        }
    }
}
