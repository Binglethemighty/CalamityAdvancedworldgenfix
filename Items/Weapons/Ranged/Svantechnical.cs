﻿using System;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Ranged;
using CalamityMod.Rarities;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Ranged
{
    public class Svantechnical : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Ranged";
        public int SineCounter = 0;
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 60;
            Item.height = 26;
            Item.damage = 200;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 1;
            Item.useAnimation = 3;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 3.5f;

            Item.value = CalamityGlobalItem.RarityHotPinkBuyPrice;
            Item.rare = ModContent.RarityType<HotPink>();
            Item.Calamity().devItem = true;

            Item.UseSound = SoundID.Item31;
            Item.autoReuse = true;
            Item.shootSpeed = 6f;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.useAmmo = AmmoID.Bullet;
            Item.Calamity().canFirePointBlankShots = true;
        }

        public override Vector2? HoldoutOffset() => new Vector2(-5, 0);
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
        public override void HoldItem(Player player) => player.scope = false;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int shotType = (player.altFunctionUse == 2 ? type : ModContent.ProjectileType<ChargedBlast>());
            position = position + (player.Calamity().mouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX) * 65;
            float sine = (float)Math.Sin(SineCounter * 0.175f / MathHelper.Pi) * 4f;
            float sine2 = (float)Math.Sin(SineCounter * 0.275f / MathHelper.Pi) * 2f;
            SineCounter++;
            if (SineCounter % 4 == 0)
            {
                    Vector2 helixVel1 = (velocity * Main.rand.NextFloat(0.9f, 1.1f)).RotatedBy(MathHelper.ToRadians(sine));
                    Vector2 helixVel2 = (velocity * Main.rand.NextFloat(0.9f, 1.1f)).RotatedBy(MathHelper.ToRadians(-sine));
                    Vector2 helixVel3 = (velocity * Main.rand.NextFloat(0.9f, 1.1f)).RotatedBy(MathHelper.ToRadians(sine2));
                    int shot1 = Projectile.NewProjectile(source, position.X, position.Y, helixVel1.X, helixVel1.Y, shotType, damage, knockback, player.whoAmI, 0f, 0, 2f);
                    int shot2 = Projectile.NewProjectile(source, position.X, position.Y, helixVel2.X, helixVel2.Y, shotType, damage, knockback, player.whoAmI, 0f, 0, 4f);
                    int shot3 = Projectile.NewProjectile(source, position.X, position.Y, helixVel3.X, helixVel3.Y, shotType, damage, knockback, player.whoAmI, 0f, 0, 3f);
            }
            else if (player.altFunctionUse != 2)
            {
                Particle spark2 = new LineParticle(position + Main.rand.NextVector2Circular(6, 6), (velocity * 4).RotatedByRandom(0.35f) * Main.rand.NextFloat(0.8f, 1.2f), false, Main.rand.Next(15, 25 + 1), Main.rand.NextFloat(1.5f, 2f), Main.rand.NextBool() ? Color.MediumOrchid : Color.DarkViolet);
                GeneralParticleHandler.SpawnParticle(spark2);
            }
            return false;
        }
        public override bool CanConsumeAmmo(Item ammo, Player player)
        {
            if (Main.rand.Next(0, 100) > 90 && SineCounter % 4 == 0)
                return true;
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<Infinity>().
                AddIngredient<ShadowspecBar>(5).
                AddTile<DraedonsForge>().
                Register();
        }
    }
}
