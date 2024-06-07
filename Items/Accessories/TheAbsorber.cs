using CalamityMod.CalPlayer;
using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Accessories
{
    public class TheAbsorber : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Accessories";
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 24;
            Item.defense = 10;
            Item.value = CalamityGlobalItem.RarityRedBuyPrice;
            Item.rare = ItemRarityID.Red;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityPlayer modPlayer = player.Calamity();
            player.noKnockback = true; // Inherited from Giant Tortoise Shell
            modPlayer.absorber = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<GrandGelatin>().
                AddIngredient<Baroclaw>().
                AddIngredient<GiantTortoiseShell>().
                AddIngredient<MolluskHusk>(5).
                AddIngredient<MeldConstruct>(6).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }
}
