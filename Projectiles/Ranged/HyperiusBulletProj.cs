using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Ranged
{
    public class HyperiusBulletProj : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Ranged";
        private Color currentColor = Color.Black;
        public float dustAngle = 0f;
        public bool growing = false;
        public bool dustWave = false;
        public float variance;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.aiStyle = ProjAIStyleID.Arrow;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 1200;
            Projectile.extraUpdates = 7;
            AIType = ProjectileID.Bullet;
            Projectile.ignoreWater = true;
            Projectile.Calamity().pointBlankShotDuration = CalamityGlobalProjectile.DefaultPointBlankDuration;
        }

        public override void AI()
        {
            Player Owner = Main.player[Projectile.owner];
            float targetDist = Vector2.Distance(Owner.Center, Projectile.Center);

            variance = Main.rand.NextFloat(0.3f, 1.7f);
            if (currentColor == Color.Black)
            {
                dustWave = Main.rand.NextBool();
                Projectile.scale = 1.5f;
                Projectile.velocity *= 0.3f;
                switch (Main.rand.Next(0, 4 +1))
                {
                    case 4: // Yellow shot
                        currentColor = Color.Yellow * 0.65f;
                        break;
                    case 3: // Magenta shot
                        currentColor = Color.Magenta * 0.65f;
                        break;
                    case 2: // Red shot
                        currentColor = Color.Red * 0.65f;
                        break;
                    case 1: // Blue shot
                        currentColor = Color.Cyan * 0.65f;
                        break;
                    default: // Green shot
                        currentColor = Color.Lime * 0.65f;
                        break;
                }
            }
            if (dustAngle <= -0.5f)
            {
                growing = true;
            }
            if (dustAngle >= 0.5f)
            {
                growing = false;
            }
            dustAngle += (growing ? 0.07f * variance : -0.07f * variance);

            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] > 12f && targetDist < 1200f)
            {
                GlowOrbParticle orb = new GlowOrbParticle((Projectile.Center + Projectile.velocity.RotatedBy((dustWave ? 1 : -1) * dustAngle) * 4.5f) - Projectile.velocity * 5, Vector2.Zero, false, 5, 0.55f + MathF.Abs(dustAngle * 0.5f), currentColor, true, true);
                GeneralParticleHandler.SpawnParticle(orb);

                PointParticle spark = new PointParticle(Projectile.Center + Projectile.velocity * 3.5f, Projectile.velocity, false, 2, 0.6f, currentColor);
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        // This projectile is always fullbright.
        public override Color? GetAlpha(Color lightColor)
        {
            return currentColor;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.localAI[0] > 25f)
            {
                CalamityUtils.DrawAfterimagesFromEdge(Projectile, 0, lightColor);
            }
            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                for (int b = 0; b < 3; b++)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, -oldVelocity.RotatedByRandom(0.5f) * 0.7f, ModContent.ProjectileType<HyperiusSplit>(), (int)(Projectile.damage * 0.1), 0f, Projectile.owner, 0f, 0f, Main.rand.Next(0, 4 + 1));
                }
            }
            return true;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            OnHitEffects(target.Center);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            OnHitEffects(target.Center);
        }

        private void OnHitEffects(Vector2 targetPos)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                for (int b = 0; b < 3; b++)
                {
                    Vector2 velocity = Projectile.velocity.RotatedByRandom(0.5f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity * 0.7f, ModContent.ProjectileType<HyperiusSplit>(), (int)(Projectile.damage * 0.1), Projectile.knockBack * 2f, Projectile.owner, 0f, 0f, Main.rand.Next(0, 4 + 1));
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int b = 0; b < 4; b++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, 66, new Vector2(4, 4).RotatedByRandom(100) * Main.rand.NextFloat(0.2f, 1.5f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.7f, 1.4f);
                dust.color = currentColor;
                dust.fadeIn = 0;
            }
        }
    }
}
