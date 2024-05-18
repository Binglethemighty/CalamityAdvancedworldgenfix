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
    public class OpalStrikerHoldout : BaseGunHoldoutProjectile
    {
        public override int AssociatedItemID => ModContent.ItemType<OpalStriker>();
        public override float MaxOffsetLengthFromArm => 15f;
        public override float OffsetXUpwards => -5f;
        public override float BaseOffsetY => -5f;

        private ref float CurrentChargingFrames => ref Projectile.ai[0];
        private bool FullyCharged => CurrentChargingFrames >= OpalStriker.FullChargeFrames;
        public SlotId OpalChargeSlot;
        public static float ChargedDamageMult = 5f;
        public static float ChargedKBMult = 3f;
        public static float BulletSpeed = 12f;

        public override void KillHoldoutLogic()
        {
            if (Owner.CantUseHoldout(false) || HeldItem.type != Owner.ActiveItem().type)
                Projectile.Kill();
        }

        public override void HoldoutAI()
        {
            if (SoundEngine.TryGetActiveSound(OpalChargeSlot, out var ChargeSound) && ChargeSound.IsPlaying)
                ChargeSound.Position = Projectile.Center;

            // Fire if the owner stops channeling or otherwise cannot use the weapon.
            if (Owner.CantUseHoldout())
            {
                KeepRefreshingLifetime = false;

                if (Projectile.ai[1] != 1f)
                {
                    Projectile.timeLeft = OpalStriker.AftershotCooldownFrames;

                    ChargeSound?.Stop();
                    SoundEngine.PlaySound(FullyCharged ? OpalStriker.ChargedFire : OpalStriker.Fire, Projectile.Center);

                    Vector2 shootVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * BulletSpeed;
                    if (FullyCharged)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), GunTipPosition, shootVelocity, ModContent.ProjectileType<OpalChargedStrike>(), (int)(Projectile.damage * ChargedDamageMult), Projectile.knockBack * ChargedKBMult, Projectile.owner);
                        for (int i = 0; i <= 10; i++)
                        {
                            Dust dust = Dust.NewDustPerfect(GunTipPosition, 162, shootVelocity.RotatedByRandom(MathHelper.ToRadians(20f)) * Main.rand.NextFloat(0.8f, 1.4f), 0, default, Main.rand.NextFloat(1.5f, 2.3f));
                            dust.noGravity = true;
                        }
                    }
                    else
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), GunTipPosition, shootVelocity, ModContent.ProjectileType<OpalStrike>(), Projectile.damage, Projectile.knockBack, Projectile.owner);

                    Projectile.ai[1] = 1f;
                }
            }
            else
            {
                CurrentChargingFrames++;

                // Sounds
                if (FullyCharged)
                {
                    if ((CurrentChargingFrames - OpalStriker.FullChargeFrames) % OpalStriker.ChargeLoopSoundFrames == 0)
                        OpalChargeSlot = SoundEngine.PlaySound(OpalStriker.ChargeLoop, Projectile.Center);
                }
                else if (CurrentChargingFrames == 10)
                    OpalChargeSlot = SoundEngine.PlaySound(OpalStriker.Charge, Projectile.Center);

                // Charge-up visuals
                if (CurrentChargingFrames >= 10)
                {
                    if (!FullyCharged)
                    {
                        Particle streak = new ManaDrainStreak(Owner, Main.rand.NextFloat(0.06f + (CurrentChargingFrames / 180), 0.08f + (CurrentChargingFrames / 180)), Main.rand.NextVector2CircularEdge(2f, 2f) * Main.rand.NextFloat(0.3f * CurrentChargingFrames, 0.3f * CurrentChargingFrames), 0f, Color.Gold, Color.Orange, 7, GunTipPosition);
                        GeneralParticleHandler.SpawnParticle(streak);
                    }

                    float orbScale = MathHelper.Clamp(CurrentChargingFrames, 0f, OpalStriker.FullChargeFrames);
                    Particle orb = new GenericBloom(GunTipPosition, Projectile.velocity, Color.OrangeRed, orbScale / 135f, 2);
                    GeneralParticleHandler.SpawnParticle(orb);
                    Particle orb2 = new GenericBloom(GunTipPosition, Projectile.velocity, Color.Coral, orbScale / 200f, 2);
                    GeneralParticleHandler.SpawnParticle(orb2);
                }

                // Full charge dusts
                if (CurrentChargingFrames == OpalStriker.FullChargeFrames)
                {
                    for (int i = 0; i < 36; i++)
                    {
                        Dust chargefull = Dust.NewDustPerfect(GunTipPosition, 162);
                        chargefull.velocity = (MathHelper.TwoPi * i / 36f).ToRotationVector2() * 13f + Owner.velocity;
                        chargefull.scale = Main.rand.NextFloat(2f, 2.5f);
                        chargefull.noGravity = true;
                    }
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (SoundEngine.TryGetActiveSound(OpalChargeSlot, out var ChargeSound))
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
                float rumble = MathHelper.Clamp(CurrentChargingFrames, 0f, OpalStriker.FullChargeFrames);
                drawPosition += Main.rand.NextVector2Circular(rumble / 30f, rumble / 30f);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), drawRotation, rotationPoint, Projectile.scale * Owner.gravDir, flipSprite);

            return false;
        }
    }
}
