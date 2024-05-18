﻿
using System.Collections.Generic;
using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Tiles.Astral
{
    public class AstralClay : ModTile, IMergeableTile
    {
        List<TileFraming.MergeFrameData> IMergeableTile.TileAdjacencies { get; } = [];
        
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;

            CalamityUtils.MergeWithGeneral(Type);
            CalamityUtils.MergeAstralTiles(Type);
            CalamityUtils.MergeWithOres(Type);

            DustType = ModContent.DustType<AstralBasic>();

            AddMapEntry(new Color(133, 69, 115));

            TileID.Sets.ChecksForMerge[Type] = true;
            TileID.Sets.CanBeClearedDuringOreRunner[Type] = true;
            TileID.Sets.CanBeDugByShovel[Type] = true;


            this.RegisterUniversalMerge(ModContent.TileType<AstralDirt>(), "CalamityMod/Tiles/Merges/AstralDirtMerge");
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }

        public override bool IsTileBiomeSightable(int i, int j, ref Color sightColor)
        {
            sightColor = Color.Cyan;
            return true;
        }
    }
}
