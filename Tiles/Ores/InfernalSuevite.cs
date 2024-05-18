using System.Collections.Generic;
using CalamityMod.Tiles.Crags;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityMod.Tiles.Ores
{
    [LegacyName("CharredOre")]
    public class InfernalSuevite : ModTile, IMergeableTile
    {
        private int sheetWidth = 234;
        private int sheetHeight = 90;

        List<TileFraming.MergeFrameData> IMergeableTile.TileAdjacencies { get; } = [];

        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileOreFinderPriority[Type] = 675;

            CalamityUtils.MergeWithGeneral(Type);
            CalamityUtils.MergeWithHell(Type);

            TileID.Sets.Ore[Type] = true;

            AddMapEntry(new Color(17, 16, 26), CreateMapEntryName());
            MineResist = 2f;
            MinPick = 150;
            HitSound = SoundID.Tink;
            DustType = 235;
            Main.tileSpelunker[Type] = true;

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
            frameXOffset = i % 2 * sheetWidth;
            frameYOffset = j % 2 * sheetHeight;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = 0.5f;
            g = 0f;
            b = 0f;
        }
    }
}
