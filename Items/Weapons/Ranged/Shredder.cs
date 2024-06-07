using CalamityMod.Items.Materials;
using CalamityMod.Projectiles.Ranged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using System;

namespace CalamityMod.Items.Weapons.Ranged
{
    public class Shredder : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Ranged";
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 56;
            Item.height = 24;
            Item.damage = 45;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 4;
            Item.useAnimation = 32;
            Item.reuseDelay = 35;
            Item.useLimitPerAnimation = 8;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 1.5f;
            Item.value = CalamityGlobalItem.RarityPurpleBuyPrice;
            Item.rare = ItemRarityID.Purple;
            Item.UseSound = SoundID.Item31;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.Bullet;
            Item.shootSpeed = 5f;
            Item.useAmmo = AmmoID.Bullet;
            Item.Calamity().canFirePointBlankShots = true;
        }

        public override Vector2? HoldoutOffset() => new Vector2(-5, 0);

        public override bool AltFunctionUse(Player player) => true;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int shotType = (player.altFunctionUse == 2 ? type : ModContent.ProjectileType<ChargedBlast>());
            int bulletAmt = 3;
            Vector2 newPosition = position + velocity.SafeNormalize(Vector2.UnitY) * 50f;
            for (int index = 0; index < bulletAmt; index++)
            {
                Projectile.NewProjectile(source, newPosition, (velocity * Main.rand.NextFloat(0.9f, 1.1f)).RotatedByRandom(0.2f), shotType, damage, knockback, player.whoAmI);
            }
            return false;
        }
        public override bool CanConsumeAmmo(Item ammo, Player player)
        {
            if (Main.rand.Next(0, 100) < 60)
                return false;
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<FrostbiteBlaster>().
                AddIngredient<BulletFilledShotgun>().
                AddIngredient(ItemID.LunarBar, 5).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }
}
