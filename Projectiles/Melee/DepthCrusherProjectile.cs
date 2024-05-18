using System;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Ranged;
using Humanizer;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;

namespace CalamityMod.Projectiles.Melee
{
    public class DepthCrusherProjectile : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Items/Weapons/Melee/DepthCrusher";

        public int ChargeupTime = 25;
        public int Lifetime = 255;
        public int startDamage;
        public bool setDamage = false;
        public int dustType1 = 104;
        public int dustType2 = 96;
        public float OverallProgress => 1 - Projectile.timeLeft / (float)Lifetime;
        public float ThrowProgress => 1 - Projectile.timeLeft / (float)(Lifetime);
        public float ChargeProgress => 1 - (Projectile.timeLeft - Lifetime) / (float)(ChargeupTime);

        public Player Owner => Main.player[Projectile.owner];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime + ChargeupTime;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }

        public override bool ShouldUpdatePosition()
        {
            return ChargeProgress >= 1;
        }

        public override bool? CanDamage()
        {
            //We don't want the anticipation to deal damage.
            if (ChargeProgress < 1)
                return false;

            return base.CanDamage();
        }

        //Swing animation keys
        public CurveSegment pullback = new CurveSegment(EasingType.PolyOut, 0f, 0f, MathHelper.PiOver4 * -1.2f, 2);
        public CurveSegment throwout = new CurveSegment(EasingType.PolyOut, 0.7f, MathHelper.PiOver4 * -1.2f, MathHelper.PiOver4 * 1.2f + MathHelper.PiOver2, 3);
        internal float ArmAnticipationMovement() => PiecewiseAnimation(ChargeProgress, new CurveSegment[] { pullback, throwout });

        public override void AI()
        {
            Projectile.spriteDirection = Projectile.direction;
            Vector3 Light = new Vector3(0.050f, 0.050f, 0.250f);
            Lighting.AddLight(Projectile.Center, Light * 2);

            //Anticipation animation. Make the player look like theyre holding the depth crusher
            if (ChargeProgress < 1)
            {
                Owner.ChangeDir(MathF.Sign(Main.MouseWorld.X - Owner.Center.X));

                float armRotation = ArmAnticipationMovement() * Owner.direction;

                Owner.heldProj = Projectile.whoAmI;

                Projectile.Center = Owner.MountedCenter + Vector2.UnitY.RotatedBy(armRotation * Owner.gravDir) * -45f * Owner.gravDir;
                Projectile.rotation = (-MathHelper.PiOver4 * Projectile.direction + armRotation) * Owner.gravDir;

                Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, MathHelper.Pi + armRotation);

                return;
            }

            //Play the throw sound when the throw ACTUALLY BEGINS.
            //Additionally, make the projectile collide and set its speed and velocity
            if (Projectile.timeLeft == Lifetime)
            {
                Projectile.netUpdate = true;
                SoundEngine.PlaySound(SoundID.Item1, Projectile.Center);
                Projectile.Center = Owner.MountedCenter + Projectile.velocity * 12f;
                Projectile.velocity = (Main.MouseWorld - Owner.Center).SafeNormalize(Vector2.UnitX * Owner.direction) * 15;
                startDamage = Projectile.damage;
                Projectile.spriteDirection = Projectile.direction;
            }

            Projectile.rotation += (0.4f * (MathF.Abs(Projectile.velocity.Y) * 0.2f + 0.6f)) * Projectile.direction;
            if (Projectile.velocity.Y < 16)
                Projectile.velocity.Y += Projectile.numHits >= 3 ? 0.7f : 0.4f;
            if (Projectile.numHits >= 3)
            {
                Projectile.penetrate = -1;
                if (!setDamage)
                {
                    Projectile.damage = (int)(Projectile.damage * 0.2f);
                    setDamage = true;
                }
            }
            if (Projectile.velocity.Y > 0)
                Projectile.velocity.X *= 0.975f;

            if (Collision.SolidCollision(Projectile.Center, 25, 25))
            {
                Projectile.tileCollide = true;
            }
            else
                Projectile.tileCollide = false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.timeLeft = 240;
            if (!setDamage)
            {
                if (Projectile.velocity.Y > 0)
                    Projectile.velocity.Y -= 6;
                else
                    Projectile.velocity.Y = -6;
            }

            for (int i = 0; i < 15; i++)
            {
                Vector2 dustPos = Projectile.Center;
                Dust dust = Dust.NewDustPerfect(dustPos, Main.rand.NextBool(3) ? dustType1 : dustType2);
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.5f, 1.1f);
                dust.velocity = new Vector2(3, 3).RotatedByRandom(100) * Main.rand.NextFloat(0.5f, 1.5f);
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 40; i++)
            {
                float dustMulti = Main.rand.NextFloat(0.3f, 1.5f);
                Vector2 dustPos = Projectile.Center;
                Dust dust = Dust.NewDustPerfect(dustPos, Main.rand.NextBool(3) ? dustType1 : dustType2);
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(1.6f, 2.5f) - dustMulti;
                dust.velocity = new Vector2(5, 5).RotatedByRandom(100) * Main.rand.NextFloat(0.3f, 1f) * dustMulti;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SoundStyle HitSound = new("CalamityMod/Sounds/Custom/AbyssGravelMine2") { Volume = 0.6f, PitchVariance = 0.3f };

            Projectile.timeLeft = 240;
            if (Projectile.numHits < 3)
            {
                Projectile.numHits++;

                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, new Vector2(0, -5).RotatedByRandom(0.6f) * Main.rand.NextFloat(0.9f, 1.1f), ModContent.ProjectileType<DepthCrusherSplitProjectile>(), startDamage / 4, Projectile.knockBack / 4, Projectile.owner);

                if (Projectile.velocity.X != oldVelocity.X)
                {
                    Projectile.velocity.X = -oldVelocity.X * 0.8f;
                }
                if (Projectile.velocity.Y != oldVelocity.Y)
                {
                    Projectile.velocity.Y = -oldVelocity.Y * 0.8f;
                }
                SoundEngine.PlaySound(HitSound with { Pitch = 0.15f }, Projectile.Center);
                for (int i = 0; i < 25; i++)
                {
                    float dustMulti = Main.rand.NextFloat(0.3f, 1.5f);
                    Vector2 dustPos = Projectile.Center;
                    Dust dust = Dust.NewDustPerfect(dustPos, Main.rand.NextBool(3) ? dustType1 : dustType2);
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(1.6f, 2.5f) - dustMulti;
                    dust.velocity = new Vector2(3, 3).RotatedByRandom(100) * Main.rand.NextFloat(0.3f, 1f) * dustMulti;
                }
            }
            else
            {
                SoundEngine.PlaySound(HitSound, Projectile.Center);
                for (int i = 0; i < 3; i++)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Bottom, new Vector2(0, -5).RotatedByRandom(0.6f) * Main.rand.NextFloat(0.9f, 1.1f), ModContent.ProjectileType<DepthCrusherSplitProjectile>(), (int)(startDamage * 0.3f), Projectile.knockBack / 4, Projectile.owner);
                }
                Projectile.Kill();
            }

            return false;
        }
    }
}
