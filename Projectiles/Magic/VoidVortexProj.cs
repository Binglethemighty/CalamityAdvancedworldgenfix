using System;
using CalamityMod.Items.Weapons.Magic;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Ranged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Magic
{
    public class VoidVortexProj : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Magic";
        public int time = 0;
        public int timeOffset = 0;
        public bool doDamage = false;
        public bool fireBeam = true;
        public bool rotDirection = false;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 15;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 38;
            Projectile.height = 38;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            if (Projectile.ai[2] == 3)
            {
                if (time == 0)
                {
                    timeOffset = Main.rand.Next(20, 40 + 1);
                    Projectile.timeLeft = 500;
                    Projectile.extraUpdates = 2;
                    Projectile.scale = Main.rand.NextFloat(0.35f, 0.55f);
                    rotDirection = Main.rand.NextBool();
                }

                if (time >= 20)
                    doDamage = true;
                if (time >= timeOffset && time % 25 == 0)
                {
                    Projectile.velocity = Projectile.velocity.RotatedBy(rotDirection ? 0.65f : -0.65f);
                    rotDirection = !rotDirection;
                }
                CalamityUtils.HomeInOnNPC(Projectile, true, 1000f, 15, MathHelper.Clamp(100f - time * 0.3f, 40, 100));
                if (Main.rand.NextBool())
                {
                    Dust dust2 = Dust.NewDustPerfect(Projectile.Center, 226, -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.3f));
                    dust2.scale = Main.rand.NextFloat(0.35f, 0.75f);
                    dust2.noGravity = true;
                }

            }
            else
            {
                if (Projectile.timeLeft < 65)
                {
                    Projectile.velocity *= 0.98f;
                }
                else
                {
                    // Spin chaotically given a pre-defined spin direction. Choose one initially at random.
                    float spinTheta = 0.11f;
                    if (Projectile.localAI[0] == 0f)
                        Projectile.localAI[0] = Main.rand.NextBool() ? -spinTheta : spinTheta;
                    //Projectile.rotation += Projectile.localAI[0];

                    // Spiral outwards in an increasingly chaotic fashion.
                    float revolutionTheta = 0.14f * Projectile.ai[1];
                    if (Projectile.ai[0] % 2 == 0f)
                        Projectile.velocity = Projectile.velocity.RotatedBy(revolutionTheta) * 1.0092f;

                }
                if (time == 25 && Projectile.ai[2] == 1)
                {
                    Projectile.scale = 1.5f;
                    Projectile.alpha = 0;
                    for (int k = 0; k < 25; k++)
                    {
                        Dust dust2 = Dust.NewDustPerfect(Projectile.Center, 226, new Vector2(15, 15).RotatedByRandom(100) * Main.rand.NextFloat(0.05f, 0.8f));
                        dust2.scale = Main.rand.NextFloat(0.45f, 0.95f);
                        dust2.noGravity = true;
                    }
                    Projectile.ai[0] = 0;
                }
                if (time >= 25 && Projectile.ai[2] == 1 && time % 2 == 0 && Projectile.timeLeft > 20)
                {
                    Particle bolt = new CrackParticle(Projectile.Center, new Vector2(8, 8).RotatedByRandom(100), Color.Aqua * 0.65f, Vector2.One, 0, 0, Main.rand.NextFloat(0.4f, 0.65f), 11);
                    GeneralParticleHandler.SpawnParticle(bolt);
                }

                --Projectile.ai[0];

                NPC target = Projectile.Center.ClosestNPCAt(1000);

                if (fireBeam && Projectile.ai[0] == -30 && Projectile.ai[2] <= 0 && target != null)
                {
                    CalamityUtils.MagnetSphereHitscan(Projectile, Vector2.Distance(Projectile.Center, target.Center), 8f, 0, 1, ModContent.ProjectileType<ClimaxBeam>(), 1D, true);
                    fireBeam = false;
                }
            }

            // Animate the lightning orb.
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame > 4)
                {
                    Projectile.frame = 0;
                }
            }

            if (Projectile.ai[0] <= 0)
                time++;
        }
        public override void OnKill(int timeLeft)
        {
            Projectile.netUpdate = true;
            if (Projectile.ai[2] == 1)
            {
                doDamage = true;
                Projectile.ExpandHitboxBy(400);
                Projectile.Damage();

                for (int k = 0; k < 12; k++)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, new Vector2(10, 10).RotatedByRandom(100) * Main.rand.NextFloat(0.4f, 0.55f), ModContent.ProjectileType<VoidVortexProj>(), Projectile.damage / 6, 0f, Main.myPlayer, 0f, 0f, 3f);
                }
                for (int k = 0; k < 40; k++)
                {
                    Dust dust2 = Dust.NewDustPerfect(Projectile.Center, 226, new Vector2(25, 25).RotatedByRandom(100) * Main.rand.NextFloat(0.05f, 0.8f));
                    dust2.scale = Main.rand.NextFloat(0.65f, 1.15f);
                    dust2.noGravity = true;
                }
                SoundStyle fire = new("CalamityMod/Sounds/Item/AuricBulletHit");
                SoundEngine.PlaySound(fire with { Volume = 0.4f, Pitch = 0f }, Projectile.Center);
                Particle bolt = new CustomPulse(Projectile.Center, Vector2.Zero, Color.Aqua, "CalamityMod/Particles/HighResFoggyCircleHardEdge", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.03f, 0.16f, 16);
                GeneralParticleHandler.SpawnParticle(bolt);
            }
            if (Projectile.ai[2] == 3)
            {
                for (int k = 0; k < 7; k++)
                {
                    Dust dust2 = Dust.NewDustPerfect(Projectile.Center, 226, new Vector2(5, 5).RotatedByRandom(100) * Main.rand.NextFloat(0.05f, 0.8f));
                    dust2.scale = Main.rand.NextFloat(0.45f, 0.75f);
                    dust2.noGravity = true;
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 180);
            if (hit.Damage > 1 && Projectile.ai[2] == 3)
                Projectile.Kill();
        }

        public override Color? GetAlpha(Color lightColor)
        {
            if (Projectile.timeLeft < 20)
            {
                float timerAlpha = Projectile.timeLeft / 20f;
                Projectile.alpha = (int)(255f - 255f * timerAlpha);
            }
            if (time < 25 && Projectile.ai[2] == 1)
                Projectile.alpha = 255;
            return new Color(255 - Projectile.alpha, 255 - Projectile.alpha, 255 - Projectile.alpha, 0);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int framing = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type];
            int y6 = framing * Projectile.frame;
            Main.spriteBatch.Draw(texture2D13, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(new Rectangle(0, y6, texture2D13.Width, framing)), Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2(texture2D13.Width / 2f, framing / 2f), Projectile.scale * (Projectile.ai[2] == 3 ? 1.3f : 1f), SpriteEffects.None, 0);

            if (Projectile.ai[2] == 3)
                CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor * 0.3f, 1, texture2D13);
            return false;
        }

        public override bool? CanDamage() => doDamage ? null : false;
    }
}
