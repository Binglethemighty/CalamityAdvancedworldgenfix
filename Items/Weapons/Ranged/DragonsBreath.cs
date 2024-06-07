using System;
using CalamityMod.Projectiles.Ranged;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Ranged
{
    public class DragonsBreath : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Ranged";

        public static readonly SoundStyle FireballSound = new("CalamityMod/Sounds/Custom/Yharon/YharonFireball", 3) { PitchVariance = 0.3f, Volume = 0.75f };
        public static readonly SoundStyle WeldingStart = new("CalamityMod/Sounds/Item/DragonsBreathStrongStart") { Volume = 1.75f };
        public static readonly SoundStyle WeldingBurn = new("CalamityMod/Sounds/Item/WeldingBurn") { Volume = 0.65f };
        public static readonly SoundStyle WeldingShoot = new("CalamityMod/Sounds/Item/WeldingShoot") { Volume = 0.45f };

        public SlotId WeldSoundSlot;

        public static int BaseReuseDelay = 14;
        public static int MinReuseDelay = 4;
        public static int ShotsToMaxFirerate = 20;
        public static int ShotsToFireBeams = ShotsToMaxFirerate + 20;
        public static int ShotsToReset = ShotsToFireBeams + 50;

        public int Counter = 0;

        public override void SetDefaults()
        {
            Item.width = 124;
            Item.height = 72;
            Item.damage = 478;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 5;
            Item.useAnimation = 9;
            Item.reuseDelay = BaseReuseDelay;
            Item.autoReuse = true;
            Item.useStyle = ItemUseStyleID.Shoot;

            Item.noMelee = true;
            Item.knockBack = 4.5f;
            Item.UseSound = null;
            Item.shoot = ModContent.ProjectileType<DragonsBreathFlames>();
            Item.shootSpeed = 3.5f;
            Item.useAmmo = AmmoID.Gel;

            Item.rare = ModContent.RarityType<Violet>();
            Item.value = CalamityGlobalItem.RarityVioletBuyPrice;
        }
        public override bool CanConsumeAmmo(Item ammo, Player player) => Counter < ShotsToFireBeams && Main.rand.NextFloat() > 0.80f;

        public override Vector2? HoldoutOffset() => new Vector2(27, 6);

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Counter++;
            if (Counter < ShotsToFireBeams)
            {
                if (Counter % 2 == 1)
                    SoundEngine.PlaySound(FireballSound, player.Center);

                Vector2 newVel = velocity.RotatedByRandom(MathHelper.ToRadians(2.5f));
                Projectile.NewProjectile(source, position, newVel * Main.rand.NextFloat(1.2f, 0.8f), type, damage, knockback, player.whoAmI);

                Item.reuseDelay = (int)Utils.Remap(Counter, 0, ShotsToMaxFirerate, BaseReuseDelay, MinReuseDelay);
            }
            else
            {
                if (Counter == ShotsToFireBeams)
                    SoundEngine.PlaySound(WeldingStart, player.Center);

                if (SoundEngine.TryGetActiveSound(WeldSoundSlot, out var WeldSound) && WeldSound.IsPlaying)
                    WeldSound.Position = player.Center;
                if (player.Calamity().DragonsBreathAudioCooldown2 == 0 && Counter >= ShotsToFireBeams + 5)
                {
                    player.Calamity().DragonsBreathAudioCooldown2 = 30;
                    WeldSoundSlot = SoundEngine.PlaySound(WeldingShoot, player.Center);
                }

                Projectile.NewProjectile(source, position, velocity * 2f, type, damage, knockback, player.whoAmI, 1);
                Item.reuseDelay = 0;

                if (Counter >= ShotsToReset)
                {
                    Counter = 0;
                    Item.reuseDelay = BaseReuseDelay;
                    SoundEngine.PlaySound(SpeedBlaster.Empty, player.Center);
                    Projectile.NewProjectile(source, player.Center, new Vector2(5 * -player.direction, -5), ModContent.ProjectileType<DragonsBreathMag>(), Main.zenithWorld ? 250000 : 1, knockback, player.whoAmI);
                    WeldSound?.Stop();
                }
            }
            return false;
        }

        public override void UpdateInventory(Player player)
        {
            // Reset speed back to initial once swapped to another item (including one of itself)
            if (player.ActiveItem() != Item)
                Counter = 0;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            player.ChangeDir(Math.Sign((player.Calamity().mouseWorld - player.Center).X));
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 7f;
            Vector2 itemSize = new Vector2(124, 72);
            Vector2 itemOrigin = new Vector2(-17, 3);

            CalamityUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);

            base.UseStyle(player, heldItemFrame);
        }

        public override void UseItemFrame(Player player)
        {
            player.ChangeDir(Math.Sign((player.Calamity().mouseWorld - player.Center).X));

            float animProgress = 0.5f - player.itemTime / (float)player.itemTimeMax;
            float rotation = (player.Center - player.Calamity().mouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            if (animProgress < 0.4f)
                rotation += Counter == 0 ? -0.25f * (float)Math.Pow((0.6f - animProgress) / 0.6f, 2) * player.direction : 0;

            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
        }
    }
}
