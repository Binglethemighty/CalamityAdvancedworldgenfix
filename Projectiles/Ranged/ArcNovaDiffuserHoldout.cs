using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Ranged
{
    public class ArcNovaDiffuserHoldout : BaseGunHoldoutProjectile
    {
        public override int AssociatedItemID => ModContent.ItemType<ArcNovaDiffuser>();
        public override float MaxOffsetLengthFromArm => 24f;
        public override float OffsetXUpwards => -5f;
        public override float BaseOffsetY => -5f;
        public override float OffsetYDownwards => 5f;

        public static int FramesPerLoad = 13;
        public static int MaxLoadableShots = 15;
        public static float BulletSpeed = 12f;
        public SlotId NovaChargeSlot;

        public ref float CurrentChargingFrames => ref Projectile.ai[0];
        public ref float ShotsLoaded => ref Projectile.ai[1];
        public ref float ShootRecoilTimer => ref Projectile.ai[2]; // Dual functions for rapid fire shooting cooldown and recoil
        public bool ChargeLV1 => CurrentChargingFrames >= ArcNovaDiffuser.Charge1Frames;
        public bool ChargeLV2 => CurrentChargingFrames >= ArcNovaDiffuser.Charge2Frames;

        public override void KillHoldoutLogic()
        {
            if (Owner.CantUseHoldout(false) || HeldItem.type != Owner.ActiveItem().type)
                Projectile.Kill();
        }

        public override void HoldoutAI()
        {
            if (SoundEngine.TryGetActiveSound(NovaChargeSlot, out var ChargeSound) && ChargeSound.IsPlaying)
                ChargeSound.Position = Projectile.Center;

            // Fire if the owner stops channeling or otherwise cannot use the weapon.
            if (Owner.CantUseHoldout())
            {
                KeepRefreshingLifetime = false;

                // Big Shot mode
                if (ChargeLV2)
                {
                    if (ShotsLoaded > 0)
                    {
                        // Set lifespan to double of remaining cooldown so that it can finish the recoil animation
                        Projectile.timeLeft = ArcNovaDiffuser.AftershotCooldownFrames * 2;

                        ChargeSound?.Stop();
                        SoundEngine.PlaySound(ArcNovaDiffuser.BigShot, Projectile.position);

                        Vector2 shootVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * BulletSpeed;
                        int charge2Damage = Projectile.damage * 20;
                        float charge2KB = Projectile.knockBack * 3f;
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), GunTipPosition, shootVelocity, ModContent.ProjectileType<NovaChargedShot>(), charge2Damage, charge2KB, Projectile.owner);
                        Owner.Calamity().GeneralScreenShakePower = 6.5f;
                        for (int i = 0; i <= 25; i++)
                        {
                            Dust dust = Dust.NewDustPerfect(GunTipPosition - Projectile.velocity * 15, 107, shootVelocity.RotatedByRandom(MathHelper.ToRadians(15f)) * Main.rand.NextFloat(0.2f, 1.2f), 0, default, Main.rand.NextFloat(1f, 2.3f));
                            dust.noGravity = true;
                        }

                        ShotsLoaded = 0;
                        ShootRecoilTimer = 24f;
                        OffsetLengthFromArm -= 25f;
                    }
                    // Retracting recoil
                    else if (ShootRecoilTimer > 0)
                        ShootRecoilTimer -= 2;
                }
                // Rapid Fire mode
                else if (ShotsLoaded > 0)
                {
                    // While bullets are remaining, refresh the lifespan; it will not refresh again after bullets run out
                    Projectile.timeLeft = ArcNovaDiffuser.AftershotCooldownFrames;

                    // Retract recoil & shoot faster if charged
                    ShootRecoilTimer -= ChargeLV1 ? 2.3f : 2f;

                    if (ShootRecoilTimer <= 0f)
                    {
                        ChargeSound?.Stop();
                        SoundEngine.PlaySound(ArcNovaDiffuser.SmallShot, Projectile.position);

                        Vector2 shootVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * BulletSpeed;
                        Vector2 fireVec = shootVelocity.RotatedByRandom(MathHelper.ToRadians(2f));
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), GunTipPosition, fireVec, ModContent.ProjectileType<NovaShot>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                        for (int i = 0; i <= 4; i++)
                        {
                            Dust dust = Dust.NewDustPerfect(GunTipPosition - Projectile.velocity * 15, 107, shootVelocity.RotatedByRandom(MathHelper.ToRadians(15f)) * Main.rand.NextFloat(0.9f, 1.2f), 0, default, Main.rand.NextFloat(0.8f, 1.8f));
                            dust.noGravity = true;
                        }

                        ShotsLoaded--;
                        ShootRecoilTimer = 16f;
                        OffsetLengthFromArm -= 5f;
                    }
                }
                // Retracting any remaining recoil
                else if (ShootRecoilTimer > 0)
                    ShootRecoilTimer -= 2;

                // Fires a burst of sparks on the last shot
                if (ShotsLoaded == 0)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        SparkParticle spark2 = new SparkParticle((GunTipPosition - Projectile.velocity * 3) + Main.rand.NextVector2Circular(10, 10), -Projectile.velocity * Main.rand.NextFloat(9.1f, 17.8f), false, Main.rand.Next(9, 12), Main.rand.NextFloat(0.2f, 0.3f), Main.rand.NextBool(4) ? Color.Chartreuse : Color.Lime);
                        GeneralParticleHandler.SpawnParticle(spark2);
                    }
                    ShotsLoaded--;
                }
            }
            else
            {
                // Loads shots until maxed out
                if (ShotsLoaded < MaxLoadableShots && CurrentChargingFrames % FramesPerLoad == 0)
                    ShotsLoaded++;

                if (ChargeLV1)
                    CurrentChargingFrames += 2;
                else
                    CurrentChargingFrames++;

                // Sounds
                if (ChargeLV1)
                {
                    // Pulse sounds play independently of the loop
                    if (CurrentChargingFrames == ArcNovaDiffuser.Charge2Frames)
                        SoundEngine.PlaySound(ArcNovaDiffuser.ChargeLV2, Projectile.Center);
                    else if (CurrentChargingFrames == ArcNovaDiffuser.Charge1Frames)
                    {
                        SoundEngine.PlaySound(ArcNovaDiffuser.ChargeLV1, Projectile.Center);
                        ShotsLoaded = MaxLoadableShots;
                    }

                    if ((CurrentChargingFrames - ArcNovaDiffuser.Charge1Frames) % (ArcNovaDiffuser.ChargeLoopSoundFrames * 2) == 0)
                        NovaChargeSlot = SoundEngine.PlaySound(ArcNovaDiffuser.ChargeLoop, Projectile.Center);
                }
                else if (CurrentChargingFrames == 10)
                    NovaChargeSlot = SoundEngine.PlaySound(ArcNovaDiffuser.ChargeStart, Projectile.Center);

                // Charge-up visuals
                if (CurrentChargingFrames >= 10)
                {
                    float particleScale = MathHelper.Clamp(CurrentChargingFrames, 0f, ArcNovaDiffuser.Charge2Frames);
                    for (int i = 0; i < (ChargeLV2 ? 4 : ChargeLV1 ? 3 : 2); i++)
                    {
                        SparkParticle spark2 = new SparkParticle((GunTipPosition - Projectile.velocity * 4) + Main.rand.NextVector2Circular(12, 12), -Projectile.velocity * Main.rand.NextFloat(16.1f, 30.8f), false, Main.rand.Next(2, 7), Main.rand.NextFloat(particleScale / 350f, particleScale / 270f), Main.rand.NextBool(4) ? Color.Chartreuse : Color.Lime);
                        GeneralParticleHandler.SpawnParticle(spark2);
                    }
                    Particle orb = new GenericBloom(GunTipPosition, Projectile.velocity, Color.Lime, particleScale / 270f, 2, false);
                    GeneralParticleHandler.SpawnParticle(orb);
                    Particle orb2 = new GenericBloom(GunTipPosition, Projectile.velocity, Color.White, particleScale / 400f, 2, false);
                    GeneralParticleHandler.SpawnParticle(orb2);

                    float strength = particleScale / 45f;
                    Vector3 DustLight = new Vector3(0.000f, 0.255f, 0.000f);
                    Lighting.AddLight(GunTipPosition, DustLight * strength);
                }

                // Full charge dusts
                if (CurrentChargingFrames == ArcNovaDiffuser.Charge1Frames)
                {
                    for (int i = 0; i < 36; i++)
                    {
                        Dust chargefull = Dust.NewDustPerfect(GunTipPosition, 107);
                        chargefull.velocity = (MathHelper.TwoPi * i / 36f).ToRotationVector2() * 8f + Owner.velocity;
                        chargefull.scale = Main.rand.NextFloat(1f, 1.3f);
                        chargefull.noGravity = true;
                    }
                }
                if (CurrentChargingFrames == ArcNovaDiffuser.Charge2Frames)
                {
                    for (int i = 0; i < 45; i++)
                    {
                        Dust chargefull = Dust.NewDustPerfect(GunTipPosition, 107);
                        chargefull.velocity = (MathHelper.TwoPi * i / 36f).ToRotationVector2() * 12f + Owner.velocity;
                        chargefull.scale = Main.rand.NextFloat(1.2f, 1.4f);
                        chargefull.noGravity = true;
                    }
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (SoundEngine.TryGetActiveSound(NovaChargeSlot, out var ChargeSound))
                ChargeSound?.Stop();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float drawRotation = Projectile.rotation + (Projectile.spriteDirection == -1 ? MathHelper.Pi : 0f);
            Vector2 rotationPoint = texture.Size() * 0.5f;
            SpriteEffects flipSprite = (Projectile.spriteDirection * Owner.gravDir == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            if (!Owner.CantUseHoldout())
            {
                float rumble = MathHelper.Clamp(CurrentChargingFrames, 0f, ArcNovaDiffuser.Charge2Frames);
                drawPosition += Main.rand.NextVector2Circular(rumble / 70f, rumble / 70f);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), drawRotation, rotationPoint, Projectile.scale * Owner.gravDir, flipSprite);

            return false;
        }
    }
}
