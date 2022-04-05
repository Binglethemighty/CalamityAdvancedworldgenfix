﻿using CalamityMod.Items.Materials;
using CalamityMod.Tiles.DraedonSummoner;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using CalamityMod.TileEntities;
using CalamityMod.CustomRecipes;
using System.Collections.Generic;
using System;

namespace CalamityMod.Items.DraedonMisc
{
    public class VoltageRegulationSystem : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Voltage Regulation System");
            Tooltip.SetDefault("Can be placed on the Codebreaker");
        }

        public override void SetDefaults()
        {
            Item.width = 52;
            Item.height = 52;
            Item.maxStack = 999;
            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.rare = ItemRarityID.Purple;
            Item.Calamity().customRarity = CalamityRarity.Turquoise;
            Item.useTime = Item.useAnimation = 15;
        }

        public override bool? UseItem(Player player) => true;

        public override void ModifyTooltips(List<TooltipLine> tooltips) => CalamityGlobalItem.InsertKnowledgeTooltip(tooltips, 4);

        public override bool ConsumeItem(Player player)
        {
            Point placeTileCoords = Main.MouseWorld.ToTileCoordinates();
            Tile tile = CalamityUtils.ParanoidTileRetrieval(placeTileCoords.X, placeTileCoords.Y);
            float checkDistance = ((Player.tileRangeX + Player.tileRangeY) / 2f + player.blockRange) * 16f;

            if (Main.myPlayer == player.whoAmI && player.WithinRange(Main.MouseWorld, checkDistance) && tile.HasTile && tile.TileType == ModContent.TileType<CodebreakerTile>())
            {
                TECodebreaker codebreakerTileEntity = CalamityUtils.FindTileEntity<TECodebreaker>(placeTileCoords.X, placeTileCoords.Y, CodebreakerTile.Width, CodebreakerTile.Height, CodebreakerTile.SheetSquare);
                if (codebreakerTileEntity is null || codebreakerTileEntity.ContainsVoltageRegulationSystem)
                    return false;

                codebreakerTileEntity.ContainsVoltageRegulationSystem = true;
                codebreakerTileEntity.SyncConstituents((short)Main.myPlayer);
                return true;
            }

            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).
                AddIngredient(ModContent.ItemType<MysteriousCircuitry>(), 10).
                AddIngredient(ModContent.ItemType<DubiousPlating>(), 10).
                AddIngredient(ModContent.ItemType<UeliaceBar>(), 5).
                AddIngredient(ItemID.LunarBar, 5).
                AddCondition(ArsenalTierGatedRecipe.ConstructRecipeCondition(4, out Predicate<Recipe> condition), condition).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }
}
