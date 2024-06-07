using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Ranged
{
    public class DryadsTearMain : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Ranged";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.aiStyle = ProjAIStyleID.Arrow;
            AIType = ProjectileID.Bullet;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.extraUpdates = 5;
            Projectile.Calamity().pointBlankShotDuration = CalamityGlobalProjectile.DefaultPointBlankDuration;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.LimeGreen.ToVector3() * 0.25f);

            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] > 4f)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(5) ? 131 : 294, -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.3f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.45f, 0.65f);
                if (dust.type == 131)
                    dust.scale = Main.rand.NextFloat(0.35f, 0.55f);
                else
                    dust.fadeIn = 0.5f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesFromEdge(Projectile, 0, lightColor);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                for (int b = 0; b < 2; b++)
                {
                    Vector2 velocity = CalamityUtils.RandomVelocity(100f, 70f, 100f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<DryadsTearSplit>(), (int)(Projectile.damage * 0.1), 0f, Projectile.owner, 0f, 0f);
                }
                for (int i = 0; i < 3; i++)
                {
                    Vector2 velocity = new Vector2(4, 4).RotatedByRandom(100);
                    LineParticle spark2 = new LineParticle(Projectile.Center + velocity, velocity * Main.rand.NextFloat(0.5f, 1f), false, 11, 0.65f, Main.rand.NextBool(3) ? Color.MediumAquamarine : Color.Lime);
                    GeneralParticleHandler.SpawnParticle(spark2);
                }
            }
            SoundEngine.PlaySound(SoundID.Item118 with { Pitch = 0.5f }, Projectile.Center);
        }
    }
}
