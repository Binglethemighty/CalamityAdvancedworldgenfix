using System;
using System.Collections.Generic;
using CalamityMod.Items.Materials;
using CalamityMod.Projectiles.Ranged;
using CalamityMod.Rarities;
using CalamityMod.Sounds;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Ranged
{
    public class NitroExpressRifle : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Ranged";
        public static readonly SoundStyle FireSound = new("CalamityMod/Sounds/Item/NitroExpressRifleFire") { Volume = 0.6f };
        public override void SetDefaults()
        {
            Item.width = 100;
            Item.height = 22;
            Item.damage = 210;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 70;
            Item.useAnimation = 70;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 7.5f;
            Item.value = CalamityGlobalItem.RarityLightRedBuyPrice;
            Item.UseSound = FireSound;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.Bullet;
            Item.shootSpeed = 12f;
            Item.useAmmo = AmmoID.Bullet;
            Item.rare = ItemRarityID.LightRed;
            Item.Calamity().canFirePointBlankShots = true;
        }

        public override Vector2? HoldoutOffset() => new Vector2(-10, 8);

        // Terraria seems to really dislike high crit values in SetDefaults
        public override void ModifyWeaponCrit(Player player, ref float crit) => crit += 10;

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.Musket).
                AddIngredient<EssenceofHavoc>(3).
                AddIngredient<EssenceofSunlight>(3).
                AddIngredient(ItemID.ExplosivePowder, 5).
                AddTile(TileID.Anvils).
                Register();

            CreateRecipe().
                AddIngredient(ItemID.TheUndertaker).
                AddIngredient<EssenceofHavoc>(3).
                AddIngredient<EssenceofSunlight>(3).
                AddIngredient(ItemID.ExplosivePowder, 5).
                AddTile(TileID.Anvils).
                Register();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.velocity += -velocity * 0.3f;
            if (type == ProjectileID.Bullet)
                type = ModContent.ProjectileType<NitroShot>();
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f);
            return false;
        }

        public override void HoldItem(Player player) => player.Calamity().mouseWorldListener = true;

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            player.ChangeDir(Math.Sign((player.Calamity().mouseWorld - player.Center).X));
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 35f;
            Vector2 itemSize = new Vector2(Item.width, Item.height);
            Vector2 itemOrigin = new Vector2(-5, 6);


            //Sniper's horizontal recoil; can be a bit subtle but it is noticeable
            if (player.altFunctionUse == 2)
            {
                //Recoil:
                int anim = 0;
                for (int r = 0; r < Item.useAnimation; ++r)
                {
                    if (anim == 10 && r < Item.useAnimation / 2) //animates every 10 frames so that the player notices a recoil because this happens way too fast
                    {
                        itemPosition.X -= player.direction * 0.025f;
                        itemPosition.Y -= player.direction * 0.025f;
                        anim = 0;
                    }
                    else if (anim == 10 && r > Item.useAnimation / 2)
                    {
                        itemPosition.X += player.direction * 0.025f;
                        itemPosition.Y += player.direction * 0.025f;
                        anim = 0;
                    }
                    ++anim;
                }
            }

            CalamityUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
            base.UseStyle(player, heldItemFrame);
        }

        // Recoil + Not having the gun aim downwards
        public override void UseItemFrame(Player player)
        {
            player.ChangeDir(Math.Sign((player.Calamity().mouseWorld - player.Center).X));

            float animProgress = 1 - player.itemTime / (float)player.itemTimeMax;
            float rotation = (player.Center - player.Calamity().mouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            if (animProgress < 0.5)
                rotation += (player.altFunctionUse == 2 ? -1f : -0.45f) * (float)Math.Pow((0.5f - animProgress) / 0.5f, 2) * player.direction;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation); //must be here otherwise it will vibrate


            //Reloads the gun 
            if (animProgress > 0.5f)
            {
                float backArmRotation = rotation + 0.52f * player.direction;

                Player.CompositeArmStretchAmount stretch = ((float)Math.Sin(MathHelper.Pi * (animProgress - 0.5f) / 0.36f)).ToStretchAmount();
                player.SetCompositeArmBack(true, stretch, backArmRotation);
            }

        }
    }
}
