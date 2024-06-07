using CalamityMod.Projectiles.Ranged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Ranged
{
    public class Leviatitan : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Ranged";
        public override void SetDefaults()
        {
            Item.width = 82;
            Item.height = 28;
            Item.damage = 77;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 5f;
            Item.value = CalamityGlobalItem.RarityLimeBuyPrice;
            Item.rare = ItemRarityID.Lime;
            Item.UseSound = SoundID.Item92;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<AquaBlast>();
            Item.shootSpeed = 12f;
            Item.useAmmo = AmmoID.Bullet;
            Item.Calamity().canFirePointBlankShots = true;
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-15, 0);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            float SpeedX = velocity.X + Main.rand.Next(-10, 11) * 0.05f;
            float SpeedY = velocity.Y + Main.rand.Next(-10, 11) * 0.05f;

            if (type == ProjectileID.Bullet)
            {
                if (Main.rand.NextBool(3))
                    Projectile.NewProjectile(source, position.X, position.Y, SpeedX, SpeedY, ModContent.ProjectileType<AquaBlastToxic>(), (int)(damage * 1.5), knockback, player.whoAmI);
                else
                    Projectile.NewProjectile(source, position.X, position.Y, SpeedX, SpeedY, ModContent.ProjectileType<AquaBlast>(), damage, knockback, player.whoAmI);
            }
            else
                Projectile.NewProjectile(source, position.X, position.Y, SpeedX, SpeedY, type, damage, knockback, player.whoAmI);

            return false;
        }
    }
}
