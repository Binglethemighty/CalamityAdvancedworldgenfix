using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Ranged
{
    public class CinderArrowProj : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Ranged";
        public override string Texture => "CalamityMod/Items/Ammo/CinderArrow";

        public ref float Time => ref Projectile.ai[1];
        public ref float isSplit => ref Projectile.ai[2];
        public bool splitShot = false;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 15;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.arrow = true;
            Projectile.penetrate = 1;
            Projectile.MaxUpdates = 2;
            Projectile.timeLeft = 600;
            Projectile.aiStyle = ProjAIStyleID.Arrow;
            Projectile.Calamity().pointBlankShotDuration = CalamityGlobalProjectile.DefaultPointBlankDuration;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Time++;
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            if (isSplit > 0)
            {
                Projectile.netUpdate = true;
                splitShot = true;
            }

            if (splitShot)
            {
                Lighting.AddLight(Projectile.Center, Color.Red.ToVector3() * 0.15f);
                if (Main.rand.NextBool(4))
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity * 2, 90, -Projectile.velocity * Main.rand.NextFloat(0.1f, 0.55f));
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(0.65f, 0.85f);
                    dust.noLight = true;
                    dust.noLightEmittence = true;
                }
            }
            else
            {
                Lighting.AddLight(Projectile.Center, Color.Red.ToVector3() * 0.3f);
                if (Time > 4f && Main.rand.NextBool(3))
                {
                    float velMulti = Main.rand.NextFloat(0.1f, 0.75f);
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity * 2, 90, -Projectile.velocity.RotatedBy(0.45) * velMulti);
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(0.45f, 0.75f);
                    dust.noLight = true;
                    dust.noLightEmittence = true;
                    Dust dust2 = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity * 2, 90, -Projectile.velocity.RotatedBy(-0.45) * velMulti);
                    dust2.noGravity = true;
                    dust2.scale = Main.rand.NextFloat(0.45f, 0.75f);
                    dust2.noLight = true;
                    dust2.noLightEmittence = true;
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (!splitShot)
            {
                int Dusts = 9;
                float radians = MathHelper.TwoPi / Dusts;
                Vector2 spinningPoint = Vector2.Normalize(new Vector2(-1f, -1f));
                for (int i = 0; i < Dusts; i++)
                {
                    Vector2 dustVelocity = spinningPoint.RotatedBy(radians * i).RotatedBy(0.5f) * 6.5f;
                    Particle smoke = new HeavySmokeParticle(Projectile.Center, dustVelocity * Main.rand.NextFloat(1f, 2.6f),Color.Crimson, 18, Main.rand.NextFloat(0.9f, 1.6f), 0.35f, Main.rand.NextFloat(-1, 1), true);
                    GeneralParticleHandler.SpawnParticle(smoke);
                }
                SoundEngine.PlaySound(SoundID.Item69 with { Volume = 0.35f, Pitch = 1f, PitchVariance = 0.15f }, Projectile.Center);

                if (Main.myPlayer != Projectile.owner)
                    return;

                for (int b = 0; b < 3; b++)
                {
                    Vector2 velocity = Vector2.UnitY.RotatedByRandom(0.8f) * Main.rand.NextFloat(-3.5f, -3f);
                    Projectile shrapnel = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<CinderArrowProj>(), (int)(Projectile.damage * 0.1f), 0f, Projectile.owner, ai2: 1f);
                    shrapnel.timeLeft = 300;
                    shrapnel.arrow = false;
                    shrapnel.MaxUpdates = 4;
                }

                Projectile.damage = (int)(Projectile.damage * 0.5f);
                Projectile.penetrate = -1;
                Projectile.ExpandHitboxBy(110);
                Projectile.Damage();
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (splitShot)
            {
                target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 180);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (splitShot)
            {
                Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Particles/DrainLine").Value;
                CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], Color.Crimson * 0.3f, 1, texture);
                return false;
            }
            else
            {
                Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Ranged/BloodfireBulletProj").Value;
                if (Time > 6f)
                    CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], Color.White * 0.3f, 1, texture);
                return true;
            }
        }

        public override bool? CanDamage() => splitShot && Time < 20 ? false : null;
    }
}
