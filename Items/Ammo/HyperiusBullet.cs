using CalamityMod.Items.Materials;
using CalamityMod.Projectiles.Ranged;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Ammo
{
    public class HyperiusBullet : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Ammo";

        public static int SplitBulletBonusDamage = 16;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 99;
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 30;
            Item.damage = 13;
            Item.DamageType = DamageClass.Ranged;
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.knockBack = 1.5f;
            Item.value = Item.sellPrice(copper: 16);
            Item.rare = ItemRarityID.Yellow;
            Item.shoot = ModContent.ProjectileType<HyperiusBulletProj>();
            Item.shootSpeed = 5f;
            Item.ammo = AmmoID.Bullet;
        }

        public override void AddRecipes()
        {
            CreateRecipe(150).
                AddIngredient(ItemID.MusketBall, 150).
                AddIngredient<LifeAlloy>().
                AddTile(TileID.MythrilAnvil).
                Register();
        }
    }
}
