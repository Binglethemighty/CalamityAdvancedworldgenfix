using CalamityMod.Items.Materials;
using CalamityMod.Projectiles.Ranged;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Ammo
{
    [LegacyName("TerraBullet")]
    public class DryadsTear : ModItem, ILocalizedModType
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
            Item.damage = 10;
            Item.DamageType = DamageClass.Ranged;
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.knockBack = 1.25f;
            Item.value = Item.sellPrice(copper: 16);
            Item.rare = ItemRarityID.Lime;
            Item.shoot = ModContent.ProjectileType<DryadsTearMain>();
            Item.shootSpeed = 2f;
            Item.ammo = AmmoID.Bullet;
        }

        public override void AddRecipes()
        {
            CreateRecipe(100).
                AddIngredient(ItemID.CrystalBullet, 100).
                AddIngredient<LivingShard>().
                AddTile(TileID.MythrilAnvil).
                Register();
        }
    }
}
