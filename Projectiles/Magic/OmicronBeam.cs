using System.IO;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Magic
{
    public class OmicronBeam : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Magic";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public ref float time => ref Projectile.ai[0];
        public ref float isSplit => ref Projectile.ai[1];
        public bool splitShot
        {
            get => Projectile.ai[2] == 1f;
            set => Projectile.ai[2] = value == true ? 1f : 0f;
        }
        public bool HitDirect { get; set; }
        public Color mainColor { get; set; } = Color.MediumVioletRed;

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.extraUpdates = 75;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 91;
        }

        public override void AI()
        {
            if (isSplit == 0)
            {
                Projectile.netSpam = 0;
                Projectile.netUpdate = true;
                splitShot = true;
            }

            Player Owner = Main.player[Projectile.owner];
            float targetDist = Vector2.Distance(Owner.Center, Projectile.Center);

            if (Projectile.timeLeft % 2 == 0 && time > 2 && targetDist < 1400f)
            {
                Particle spark = new GlowSparkParticle(Projectile.Center, -Projectile.velocity * 0.05f, false, 25, MathHelper.Clamp(0.34f - time * 0.07f, 0.085f, 0.34f), mainColor, new Vector2(0.5f, 1.3f));
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (Main.rand.NextBool())
            {
                Vector2 trailPos = Projectile.Center;
                float trailScale = Main.rand.NextFloat(1.9f, 2.3f);
                Particle Trail = new SparkParticle(trailPos, Projectile.velocity * Main.rand.NextFloat(0.2f, 0.9f), false, Main.rand.Next(40, 50 + 1), trailScale, mainColor);
                GeneralParticleHandler.SpawnParticle(Trail);
            }

            Vector2 dustVel = new Vector2(2, 2).RotatedByRandom(100) * Main.rand.NextFloat(0.1f, 0.8f);
            Dust dust = Dust.NewDustPerfect(Projectile.Center + dustVel, Main.rand.NextBool(4) ? 264 : 66, dustVel, 0, default, Main.rand.NextFloat(0.9f, 1.2f));
            dust.noGravity = true;
            dust.color = Main.rand.NextBool() ? Color.Lerp(mainColor, Color.White, 0.5f) : mainColor;

            time++;

            if (Projectile.numUpdates == 1)
            {
                Projectile.netSpam = 0;
                Projectile.netUpdate = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            int numProj = 2;
            float rotation = MathHelper.ToRadians(10);
            if (splitShot && time < 250 && !HitDirect)
            {
                for (int i = 0; i < numProj; i++)
                {
                    Vector2 perturbedSpeed = Projectile.velocity.RotatedBy(MathHelper.Lerp(-rotation, rotation, i / (numProj - 1)));
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, perturbedSpeed, ModContent.ProjectileType<OmicronBeam>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 1f);
                    for (int k = 0; k < 3; k++)
                    {
                        Particle blastRing = new CustomPulse(Projectile.Center + Projectile.velocity * 2, Vector2.Zero, mainColor, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10, 10), 0.8f, 0.4f, 35);
                        GeneralParticleHandler.SpawnParticle(blastRing);
                        Particle blastRing2 = new CustomPulse(Projectile.Center + Projectile.velocity * 2, Vector2.Zero, Color.White, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10, 10), 0.7f, 0.3f, 35);
                        GeneralParticleHandler.SpawnParticle(blastRing2);
                    }
                }

                for (int i = 0; i <= 6; i++)
                {
                    Particle energy = new GlowSparkParticle(Projectile.Center, (Projectile.velocity * 15).RotatedByRandom(0.5f) * Main.rand.NextFloat(0.1f, 0.4f), false, 10, Main.rand.NextFloat(0.02f, 0.04f), mainColor, new Vector2(2, 0.7f), true);
                    GeneralParticleHandler.SpawnParticle(energy);
                }
                for (int i = 0; i <= 9; i++)
                {
                    Particle energy = new SparkParticle(Projectile.Center, (Projectile.velocity * 5).RotatedByRandom(0.5f) * Main.rand.NextFloat(0.1f, 0.4f), false, 25, Main.rand.NextFloat(0.7f, 0.9f), mainColor);
                    GeneralParticleHandler.SpawnParticle(energy);
                }
            }
            for (int i = 0; i < 28; i++)
            {
                Vector2 dustVel = Projectile.velocity * Main.rand.NextFloat(0.1f, 1.5f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + dustVel + Main.rand.NextVector2Circular(6, 6), Main.rand.NextBool(4) ? 264 : 66, dustVel, 0, default, Main.rand.NextFloat(0.9f, 1.2f));
                dust.noGravity = true;
                dust.color = Main.rand.NextBool() ? Color.Lerp(mainColor, Color.White, 0.5f) : mainColor;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (splitShot && time > 7 && !HitDirect)
                Projectile.Kill();

            Player Owner = Main.player[Projectile.owner];

            for (int i = 0; i <= 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(4) ? 264 : 66, (Projectile.velocity.SafeNormalize(Vector2.UnitY) * 15f).RotatedByRandom(MathHelper.ToRadians(15f)) * Main.rand.NextFloat(0.1f, 0.8f), 0, default, Main.rand.NextFloat(1.2f, 1.6f));
                dust.noGravity = true;
                dust.color = Main.rand.NextBool() ? Color.Lerp(mainColor, Color.White, 0.5f) : mainColor;
            }

            if (time <= 7 && splitShot) // This is the sweet spot
            {
                modifiers.SourceDamage *= 5;

                if (!HitDirect)
                {
                    Owner.velocity += -Projectile.velocity;
                    for (int i = 0; i <= 9; i++)
                    {
                        Particle energy = new GlowSparkParticle(Projectile.Center, (Projectile.velocity * 15).RotatedByRandom(0.5f) * Main.rand.NextFloat(0.1f, 0.4f), false, 11, Main.rand.NextFloat(0.05f, 0.07f), mainColor, new Vector2(2, 0.5f), true);
                        GeneralParticleHandler.SpawnParticle(energy);
                    }
                    for (int i = 0; i <= 13; i++)
                    {
                        Particle energy = new SparkParticle(Projectile.Center, (Projectile.velocity * 10).RotatedByRandom(0.5f) * Main.rand.NextFloat(0.1f, 0.4f), false, 25, Main.rand.NextFloat(0.7f, 0.9f), mainColor);
                        GeneralParticleHandler.SpawnParticle(energy);
                    }
                    for (int k = 0; k < 20; k++)
                    {
                        Vector2 shootVel = (Projectile.velocity * 20).RotatedByRandom(0.5f) * Main.rand.NextFloat(0.1f, 1.8f);

                        Dust dust2 = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(4) ? 267 : 66, shootVel);
                        dust2.scale = Main.rand.NextFloat(1.15f, 1.45f);
                        dust2.noGravity = true;
                        dust2.color = Main.rand.NextBool() ? Color.Lerp(mainColor, Color.White, 0.5f) : mainColor;
                    }

                    SoundStyle fire = new("CalamityMod/Sounds/Custom/ExoMechs/ArtemisApolloDash");
                    SoundEngine.PlaySound(fire with { Volume = 1.25f, Pitch = 0.6f }, Projectile.Center);
                }

                HitDirect = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(Projectile.Center, time <= 7 ? 90 : 20, targetHitbox);

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(HitDirect);

        public override void ReceiveExtraAI(BinaryReader reader) => HitDirect = reader.ReadBoolean();
    }
}
