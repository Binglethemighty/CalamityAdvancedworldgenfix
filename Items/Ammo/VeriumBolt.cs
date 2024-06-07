using CalamityMod.Items.Materials;
using CalamityMod.Projectiles.Ranged;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Ammo
{
    [LegacyName("ArcticArrow", "VeriumBullet")]
    public class VeriumBolt : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Ammo";
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 99;
        }

        public override void SetDefaults()
        {
            Item.width = 8;
            Item.height = 8;
            Item.damage = 11;
            Item.DamageType = DamageClass.Ranged;
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.knockBack = 1.25f;
            Item.value = Item.sellPrice(copper: 12);
            Item.rare = ItemRarityID.Pink;
            Item.shoot = ModContent.ProjectileType<VeriumBoltProj>();
            Item.shootSpeed = 16f;
            Item.ammo = AmmoID.Arrow;
        }

        public override void AddRecipes()
        {
            CreateRecipe(100).
                AddIngredient(ItemID.WoodenArrow, 100).
                AddIngredient<CryonicBar>().
                AddTile(TileID.MythrilAnvil).
                Register();
        }
    }
}
