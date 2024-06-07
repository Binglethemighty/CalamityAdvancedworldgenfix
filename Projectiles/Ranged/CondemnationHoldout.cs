using System.IO;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Ranged
{
    public class CondemnationHoldout : BaseGunHoldoutProjectile
    {
        public override int AssociatedItemID => ModContent.ItemType<Condemnation>();
        public override float MaxOffsetLengthFromArm => 25f;
        public override float OffsetXUpwards => -5f;
        public override float BaseOffsetY => -5f;

        private ref float CurrentChargingFrames => ref Projectile.ai[0];
        private ref float ArrowsLoaded => ref Projectile.ai[1];
        private ref float FramesToLoadNextArrow => ref Projectile.localAI[0];

        private float storedVelocity = 1f;
        public const float velocityMultiplier = 1.2f;
        public bool homing = false;

        public override void KillHoldoutLogic()
        {
            // Fire arrows if the owner stops channeling or otherwise cannot use the weapon.
            if (Owner.CantUseHoldout())
            {
                // No arrows left to shoot? The bow disappears.
                if (ArrowsLoaded <= 0f)
                {
                    Projectile.Kill();
                    return;
                }

                // Fire one charged arrow every frame until you're out of arrows. 
                ShootProjectiles(homing);
                --ArrowsLoaded;
                if (ArrowsLoaded == 0 && homing == true)
                    homing = false;
            }
        }

        public override void HoldoutAI()
        {
            // Frame 1 effects: Record how fast the Condemnation item being used is, to determine how fast to load arrows.
            if (FramesToLoadNextArrow == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item20, Projectile.Center);
                FramesToLoadNextArrow = HeldItem.useAnimation;
            }

            // If no arrows are loaded, spawn a bit of dust to indicate it's not ready yet.
            // Spawn the same dust if the max number of arrows have been loaded or the player ran out of ammos to load.
            if (ArrowsLoaded <= 0f || ArrowsLoaded >= Condemnation.MaxLoadedArrows || !Owner.HasAmmo(Owner.ActiveItem()))
                SpawnCannotLoadArrowsDust(GunTipPosition);

            if (Owner.HasAmmo(HeldItem))
            {
                // Actually make progress towards loading more arrows.
                ++CurrentChargingFrames;

                // If it is time to load an arrow, produce a pulse of dust and add an arrow.
                // Also accelerate charging, because it's fucking awesome.
                // Take the ammo here as well
                if (CurrentChargingFrames >= FramesToLoadNextArrow && ArrowsLoaded < Condemnation.MaxLoadedArrows)
                {
                    // Save the stats here for later
                    Owner.PickAmmo(HeldItem, out _, out float shootSpeed, out int damage, out float knockback, out _);
                    Projectile.damage = damage;
                    Projectile.knockBack = knockback;
                    storedVelocity = shootSpeed * velocityMultiplier;

                    SpawnArrowLoadedDust();
                    CurrentChargingFrames = 0f;
                    ++ArrowsLoaded;
                    --FramesToLoadNextArrow;

                    // Play a sound for additional notification that an arrow has been loaded.
                    var loadSound = SoundEngine.PlaySound(SoundID.Item108 with { Volume = SoundID.Item108.Volume * 0.3f });

                    if (ArrowsLoaded >= Condemnation.MaxLoadedArrows)
                    {
                        SoundEngine.PlaySound(new("CalamityMod/Sounds/Custom/AbilitySounds/BrimflameRecharge"));
                        homing = true;
                    }
                }
            }
        }

        public void SpawnArrowLoadedDust()
        {
            if (Main.dedServ)
                return;

            //Special visuals for the final loaded arrow
            if (ArrowsLoaded >= Condemnation.MaxLoadedArrows - 1f)
            {
                //Star
                for (int i = 0; i < 5; i++)
                {
                    float angle = MathHelper.Pi * 1.5f - i * MathHelper.TwoPi / 5f;
                    float nextAngle = MathHelper.Pi * 1.5f - (i + 2) * MathHelper.TwoPi / 5f;
                    Vector2 start = angle.ToRotationVector2();
                    Vector2 end = nextAngle.ToRotationVector2();
                    for (int j = 0; j < 40; j++)
                    {
                        Dust starDust = Dust.NewDustPerfect(GunTipPosition, 267);
                        starDust.scale = 2.5f;
                        starDust.velocity = Vector2.Lerp(start, end, j / 40f) * 16f;
                        starDust.color = Color.Crimson;
                        starDust.noGravity = true;
                    }
                }
                return;
            }

            for (int i = 0; i < 36; i++)
            {
                Dust chargeMagic = Dust.NewDustPerfect(GunTipPosition, 267);
                chargeMagic.velocity = (MathHelper.TwoPi * i / 36f).ToRotationVector2() * 5f + Owner.velocity;
                chargeMagic.scale = Main.rand.NextFloat(1f, 1.5f);
                chargeMagic.color = Color.Violet;
                chargeMagic.noGravity = true;
            }
        }

        public void SpawnCannotLoadArrowsDust(Vector2 GunTipPosition)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 2; i++)
            {
                Dust chargeMagic = Dust.NewDustPerfect(GunTipPosition + Main.rand.NextVector2Circular(20f, 20f), 267);
                chargeMagic.velocity = (GunTipPosition - chargeMagic.position) * 0.1f + Owner.velocity;
                chargeMagic.scale = Main.rand.NextFloat(1f, 1.5f);
                chargeMagic.color = Projectile.GetAlpha(Color.White);
                chargeMagic.noGravity = true;
            }
        }

        public void ShootProjectiles(bool homing)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Vector2 shootVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * storedVelocity;
            int ArrowType = homing ? ModContent.ProjectileType<CondemnationArrowHoming>() : ModContent.ProjectileType<CondemnationArrow>();
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), GunTipPosition, shootVelocity, ArrowType, Projectile.damage, Projectile.knockBack, Projectile.owner);
        }

        public override void SendExtraAIHoldout(BinaryWriter writer)
        {
            writer.Write(FramesToLoadNextArrow);
            writer.Write(storedVelocity);
            writer.Write(homing);
        }

        public override void ReceiveExtraAIHoldout(BinaryReader reader)
        {
            FramesToLoadNextArrow = reader.ReadSingle();
            storedVelocity = reader.ReadSingle();
            homing = reader.ReadBoolean();
        }
    }
}
