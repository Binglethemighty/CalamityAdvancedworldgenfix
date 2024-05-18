using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Items.Ammo;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Ranged
{
    public class HolyFireBulletProj : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Ranged";
        private const int Lifetime = 600;
        private static readonly Color Alpha = new Color(1f, 1f, 1f, 0f);
        private bool ColorStyle = false;
        private float SizeVariance;
        private float SizeBonus = 2;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.MaxUpdates = 5;
            Projectile.timeLeft = Lifetime;
            Projectile.Calamity().pointBlankShotDuration = CalamityGlobalProjectile.DefaultPointBlankDuration;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0)
            {
                SizeVariance = Main.rand.NextFloat(0.95f, 1.05f);
                ColorStyle = Main.rand.NextBool();
                Projectile.velocity *= 0.7f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(90f);
            Projectile.spriteDirection = Projectile.direction;
            if (Projectile.localAI[0] >= 30 && SizeBonus > 1)
            {
                SizeBonus -= 0.02f;
            }
            // Flaking dust
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] > 4f)
            {
                if (Main.rand.NextBool(3))
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5, 5), Main.rand.NextBool() ? 262 : 87, -Projectile.velocity.RotatedByRandom(0.05f) * Main.rand.NextFloat(0.01f, 0.1f));
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(0.5f, 0.85f);
                    dust.alpha = 235;
                }
                if (Main.rand.NextBool(15))
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, 278, -Projectile.velocity.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.2f, 0.4f));
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(0.4f, 0.7f);
                    dust.color = ColorStyle ? Color.Orange : Color.Khaki;
                }
            }
        }

        public override Color? GetAlpha(Color lightColor) => Alpha;

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesFromEdge(Projectile, 0, ColorStyle ? Color.OrangeRed * 8 : Color.Khaki * 8);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            // Spawn an on-hit explosion which deals 75% of the projectile's damage.
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.damage = (int)(Projectile.damage * HolyFireBullet.ExplosionMultiplier);
                Projectile.penetrate = -1;
                Projectile.ExpandHitboxBy((25 * SizeVariance) * SizeBonus);
                Projectile.Damage();
            }
            SoundEngine.PlaySound(HolyFireBullet.Explosion with { Pitch = -0.15f, Volume = 0.3f }, Projectile.Center);

            Vector2 Offset = Main.rand.NextVector2Circular(15, 15);

            Particle explosion = new DetailedExplosion(Projectile.Center + Offset, Vector2.Zero, (ColorStyle ? Color.Orange : Color.Khaki) * 0.9f, Vector2.One, Main.rand.NextFloat(-5, 5), 0f, (0.28f * SizeVariance) * SizeBonus, 10);
            GeneralParticleHandler.SpawnParticle(explosion);

            SparkleParticle impactParticle = new SparkleParticle(Projectile.Center + Offset, Vector2.Zero, Color.White, ColorStyle ? Color.Orange : Color.OrangeRed, 2.5f * (SizeBonus * 0.3f), 7, 0f, 2f);
            GeneralParticleHandler.SpawnParticle(impactParticle);

            for (int k = 0; k < 9; k++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool() ? 262 : 87, new Vector2(4, 4).RotatedByRandom(100) * Main.rand.NextFloat(0.5f, 1.5f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.7f, 1.25f);
                dust.alpha = 235;
                if (Main.rand.NextBool())
                {
                    Dust dust2 = Dust.NewDustPerfect(Projectile.Center, 303, new Vector2(3, 3).RotatedByRandom(100) * Main.rand.NextFloat(0.5f, 1.5f));
                    dust2.noGravity = true;
                    dust2.scale = Main.rand.NextFloat(0.8f, 1.5f);
                    dust2.alpha = 70;
                }
            }
            for (int k = 0; k < 3; k++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, 278, new Vector2(4, 4).RotatedByRandom(100) * Main.rand.NextFloat(0.5f, 1.5f) + new Vector2(0, -3));
                dust.noGravity = false;
                dust.scale = Main.rand.NextFloat(0.85f, 1f);
                dust.color = ColorStyle ? Color.Orange : Color.Khaki;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 300);
        }
    }
}
