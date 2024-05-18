using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace CalamityMod.Tiles
{
    /// <summary>
    /// Interface for tiles that specify custom universal merge data.
    /// </summary>
    internal interface IMergeableTile
    {
        List<TileFraming.MergeFrameData> TileAdjacencies { get; }
    }

    internal static class MergeableTile
    {
        private sealed class MergeableTileGlobalTile : GlobalTile
        {
            public override void PostDraw(int i, int j, int type, SpriteBatch spriteBatch)
            {
                if (!TryGetMergeableTile(type, out var mergeableTile))
                    return;
                
                TileFraming.DrawUniversalMergeFrames(i, j, mergeableTile.TileAdjacencies);
            }

            public override bool TileFrame(int i, int j, int type, ref bool resetFrame, ref bool noBreak)
            {
                if (!TryGetMergeableTile(type, out var mergeableTile))
                    return base.TileFrame(i, j, type, ref resetFrame, ref noBreak);

                foreach (var adjacency in mergeableTile.TileAdjacencies)
                    TileFraming.GetAdjacencyData(i, j, adjacency);
                
                return base.TileFrame(i, j, type, ref resetFrame, ref noBreak);
            }

            private static bool TryGetMergeableTile(int type, [NotNullWhen(returnValue: true)] out IMergeableTile mergeableTile)
            {
                if (TileLoader.GetTile(type) is IMergeableTile theMergeableTile)
                {
                    mergeableTile = theMergeableTile;
                    return true;
                }
                
                mergeableTile = null;
                return false;
            }
        }
        
        public static void RegisterUniversalMerge(this IMergeableTile mergeableTile, int tileId, string blendSheetPath)
        {
            if (mergeableTile is not ModTile tile)
                throw new InvalidCastException($"{nameof(IMergeableTile)} is implemented on type {mergeableTile.GetType().FullName}; said type should inherit from {nameof(ModTile)}");

            TileFraming.SetUpUniversalMerge(tile.Type, tileId, blendSheetPath, out var data);
            mergeableTile.TileAdjacencies.Add(data);
        }
    }
}
