using CalamityMod.Projectiles.Melee;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Melee
{
    public class GreatswordofJudgement : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Melee";
        public override void SetDefaults()
        {
            Item.width = 78;
            Item.height = 78;
            Item.damage = 85;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 18;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 18;
            Item.useTurn = true;
            Item.knockBack = 7f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = CalamityGlobalItem.RarityPurpleBuyPrice;
            Item.rare = ItemRarityID.Purple;
            Item.shoot = ModContent.ProjectileType<JudgementProj>();
            Item.shootSpeed = 10f;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.LunarBar, 7).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }
}
