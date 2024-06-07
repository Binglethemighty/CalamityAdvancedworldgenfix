using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Tiles
{
    internal static class MergeableTile
    {
        // CIT 25MAY2024: This custom tile merging system was repeatedly crashing the mod with an out of memory exception.
        // Because the person who made this system is no longer on the dev team, I am just going to disable it again.

        private sealed class MergeableTileGlobalTile : GlobalTile
        {
            /*public override void PostDraw(int i, int j, int type, SpriteBatch spriteBatch)
            {
                TileFraming.DrawUniversalMergeFrames(i, j, GetOrCreateTileAdjacencies(type));
            }

            public override bool TileFrame(int i, int j, int type, ref bool resetFrame, ref bool noBreak)
            {
                foreach (var adjacency in GetOrCreateTileAdjacencies(type))
                    TileFraming.GetAdjacencyData(i, j, adjacency);
                
                return base.TileFrame(i, j, type, ref resetFrame, ref noBreak);
            }*/
        }

        private sealed class MergeableTileSystem : ModSystem
        {
            /*public override void PostSetupContent()
            {
                // Tomat: @heartplusup, you can add vanilla merges like so:
                // RegisterUniversalMerge(TileID.Dirt, TileID.Stone, "CalamityMod/Tiles/Merges/StoneMerge"); - Already-merged tiles look weird; needs changes by someone else.
                // RegisterUniversalMerge(TileID.WoodBlock, TileID.Stone, "CalamityMod/Tiles/Merges/StoneMerge");
            }*/
        }

        private static readonly Dictionary<int, List<TileFraming.MergeFrameData>> tileAdjacencyMap = [];
        
        public static void RegisterUniversalMerge(this ModTile tile, int mergeType, string blendSheetPath)
        {
            // RegisterUniversalMerge(tile.Type, mergeType, blendSheetPath);
        }

        public static void RegisterUniversalMerge(int tileId, int mergeType, string blendSheetPath)
        {
            // TileFraming.SetUpUniversalMerge(tileId, mergeType, blendSheetPath, out var data);
            // GetOrCreateTileAdjacencies(tileId).Add(data);
        }
        
        /*private static List<TileFraming.MergeFrameData> GetOrCreateTileAdjacencies(int tileId)
        {
            /*if (!tileAdjacencyMap.TryGetValue(tileId, out var adjacencies))
                tileAdjacencyMap[tileId] = adjacencies = [];

            return adjacencies;
        }*/
    }
}
