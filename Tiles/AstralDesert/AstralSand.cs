using System.Collections.Generic;
using CalamityMod.Projectiles.Typeless;
using CalamityMod.Tiles.Astral;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Metadata;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Tiles.AstralDesert
{
    public class AstralSand : ModTile
    {

        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;
            Main.tileSand[Type] = true;
            Main.tileBrick[Type] = true;
            TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Sand"]);

            CalamityUtils.MergeWithGeneral(Type);
            CalamityUtils.MergeWithDesert(Type);
            CalamityUtils.MergeAstralTiles(Type);

            DustType = 108;

            AddMapEntry(new Color(187, 220, 237));

            TileID.Sets.Suffocate[Type] = true;
            TileID.Sets.CanBeDugByShovel[Type] = true;
            TileID.Sets.Conversion.Sand[Type] = true;
            TileID.Sets.ForAdvancedCollision.ForSandshark[Type] = true;
            TileID.Sets.Falling[Type] = true;
            TileID.Sets.FallingBlockProjectile[Type] = new TileID.Sets.FallingBlockProjectileInfo(ModContent.ProjectileType<AstralSandBallFalling>(), 15);

            this.RegisterUniversalMerge(TileID.Dirt, "CalamityMod/Tiles/Merges/DirtMerge");
            this.RegisterUniversalMerge(TileID.Stone, "CalamityMod/Tiles/Merges/StoneMerge");
            this.RegisterUniversalMerge(ModContent.TileType<AstralDirt>(), "CalamityMod/Tiles/Merges/AstralDirtMerge");
            this.RegisterUniversalMerge(TileID.Sand, "CalamityMod/Tiles/Merges/SandMerge");
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }

        public override bool HasWalkDust()
        {
            return Main.rand.NextBool(3);
        }

        public override void WalkDust(ref int dustType, ref bool makeDust, ref Color color)
        {
            DustType = 108;
        }

        public override bool IsTileBiomeSightable(int i, int j, ref Color sightColor)
        {
            sightColor = Color.Cyan;
            return true;
        }
    }
}
