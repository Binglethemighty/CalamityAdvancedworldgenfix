using System.Collections.Generic;
using CalamityMod.Tiles.Crags;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityMod.Tiles.Crags
{
    public class BrimstoneSlab : ModTile, IMergeableTile
    {
        private int subsheetWidth = 450;
        private int subsheetHeight = 198;

        List<TileFraming.MergeFrameData> IMergeableTile.TileAdjacencies { get; } = [];

        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;

            CalamityUtils.MergeWithGeneral(Type);
            CalamityUtils.MergeWithHell(Type);

            AddMapEntry(new Color(79, 55, 70));
            MineResist = 2f;
            MinPick = 100;
            HitSound = SoundID.Tink;
            DustType = 235;

            this.RegisterUniversalMerge(ModContent.TileType<BrimstoneSlag>(), "CalamityMod/Tiles/Merges/BrimstoneSlagMerge");
            this.RegisterUniversalMerge(TileID.Ash, "CalamityMod/Tiles/Merges/AshMerge");
        }

        public override bool CanExplode(int i, int j)
        {
            return false;
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }

        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            frameXOffset = i % 2 * subsheetWidth;
            frameYOffset = j % 2 * subsheetHeight;
        }

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            return TileFraming.BrimstoneFraming(i, j, resetFrame);
        }
    }
}
