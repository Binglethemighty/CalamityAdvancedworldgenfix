using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;

namespace CalamityMod.Walls
{
    /// <summary>
    /// Walls that implement this interface may be revealed on the map through
    /// water. This allows them to show on the map despite always being
    /// submerged.
    /// </summary>
    public interface IVisibleThroughWater : ILoadable
    {
        int WaterMapEntry { get; set; }
    }

    internal static class VisibleThroughWater
    {
        [Autoload(Side = ModSide.Client)]
        private sealed class VisibleThroughWaterSystem : ModSystem
        {
            public override void AddRecipes()
            {
                InitializeWaterMapEntryLookups();
            }
        }
        
        public const float WaterTransparency = 0.5f;
        public static readonly Color WaterColor = new Color(9, 61, 191);

        public static void AddMapEntryWithWaterVisibility(this IVisibleThroughWater visibleThroughWater, Color baseColor, LocalizedText text = null)
        {
            AssertIsWall(visibleThroughWater, out var wall);
            wall.AddMapEntry(baseColor, text);
            wall.AddMapEntry(Color.Lerp(baseColor, WaterColor, WaterTransparency), text);
        }

        public static void InitializeWaterMapEntryLookups()
        {
            foreach (var visibleThroughWater in CalamityMod.Instance.GetContent<IVisibleThroughWater>())
            {
                AssertIsWall(visibleThroughWater, out var wall);
                visibleThroughWater.WaterMapEntry = MapHelper.wallLookup[wall.Type] + 1;
            }
        }

        [DebuggerStepThrough]
        [StackTraceHidden]
        private static void AssertIsWall(IVisibleThroughWater visibleThroughWater, out ModWall wall)
        {
            if (visibleThroughWater is not ModWall theWall)
                throw new InvalidCastException($"{nameof(IVisibleThroughWater)} is implemented on type {visibleThroughWater.GetType().FullName}; said type should inherit from {nameof(ModWall)}");

            wall = theWall;
        }
    }
}
