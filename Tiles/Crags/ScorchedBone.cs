using System.Collections.Generic;
using CalamityMod.Tiles.Ores;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Tiles.Crags
{
    public class ScorchedBone : ModTile, IMergeableTile
    {
        private int sheetWidth = 450;
        private int sheetHeight = 198;

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
            AddMapEntry(new Color(87, 62, 67));

            this.RegisterUniversalMerge(ModContent.TileType<BrimstoneSlag>(), "CalamityMod/Tiles/Merges/BrimstoneSlagMerge");
            this.RegisterUniversalMerge(TileID.Ash, "CalamityMod/Tiles/Merges/AshMerge");
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

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            return TileFraming.BrimstoneFraming(i, j, resetFrame);
        }
    }
}
