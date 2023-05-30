using CalamityMod.Tiles.Furniture.CraftingStations;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityMod.Items.Placeables.FurnitureEutrophic
{
    public class EutrophicPiano : ModItem, ILocalizedModType
    {
        public string LocalizationCategory => "Items.Placeables";
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 20;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<Tiles.FurnitureEutrophic.EutrophicPiano>();
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).AddIngredient(ModContent.ItemType<Navystone>(), 15).AddIngredient(ItemID.Bone, 4).AddIngredient(ItemID.Book).AddTile(ModContent.TileType<EutrophicShelf>()).Register();
        }
    }
}
