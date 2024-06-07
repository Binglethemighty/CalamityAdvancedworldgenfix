using CalamityMod.Items.Materials;
using CalamityMod.Projectiles.Magic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Magic
{
    public class Tradewinds : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Magic";
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.damage = 27;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 5;
            Item.useTime = 12;
            Item.useAnimation = 12;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 5;
            Item.value = CalamityGlobalItem.RarityOrangeBuyPrice;
            Item.rare = ItemRarityID.Orange;
            Item.UseSound = SoundID.Item7;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<TradewindsProjectile>();
            Item.shootSpeed = 25.5f;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<AerialiteBar>(6).
                AddIngredient(ItemID.SunplateBlock, 5).
                AddTile(TileID.Bookcases).
                Register();
        }
    }
}
