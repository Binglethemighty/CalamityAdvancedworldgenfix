using System.Collections.Generic;
using CalamityMod.Tiles.Ores;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Tiles.Crags
{
    public class ScorchedRemains : ModTile, IMergeableTile
    {
        private int sheetWidth = 234;
        private int sheetHeight = 90;

        List<TileFraming.MergeFrameData> IMergeableTile.TileAdjacencies { get; } = [];
        
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;

            CalamityUtils.MergeWithGeneral(Type);
            CalamityUtils.MergeWithHell(Type);
            CalamityUtils.SetMerge(Type, ModContent.TileType<BrimstoneSlag>());

            DustType = 155;
            HitSound = SoundID.Dig;
            MinPick = 100;
            AddMapEntry(new Color(57, 52, 72));

            this.RegisterUniversalMerge(ModContent.TileType<BrimstoneSlag>(), "CalamityMod/Tiles/Merges/BrimstoneSlagMerge");
            this.RegisterUniversalMerge(TileID.Ash, "CalamityMod/Tiles/Merges/AshMerge");
        }

        public override void RandomUpdate(int i, int j)
        {
            Tile up = Main.tile[i, j - 1];
            Tile left = Main.tile[i - 1, j];
            Tile right = Main.tile[i + 1, j];

            if (WorldGen.genRand.NextBool(3)&& !up.HasTile && (left.TileType == ModContent.TileType<ScorchedRemainsGrass>() ||
            right.TileType == ModContent.TileType<ScorchedRemainsGrass>()))
            {
                Main.tile[i, j].TileType = (ushort)ModContent.TileType<ScorchedRemainsGrass>();
            }
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            Dust.NewDust(new Vector2(i, j) * 16f, 16, 16, DustID.Stone, 0f, 0f, 1, new Color(100, 100, 100), 1f);
            return false;
        }

        public override bool CanExplode(int i, int j)
        {
            return false;
        }

        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            frameXOffset = i % 3 * sheetWidth;
            frameYOffset = j % 3 * sheetHeight;
        }
    }
}
