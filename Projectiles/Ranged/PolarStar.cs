using System;
using System.IO;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatBuffs;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Dusts;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.Items.Weapons.Ranged.PolarisParrotfish;
using static Terraria.ModLoader.ModContent;

namespace CalamityMod.Projectiles.Ranged
{
    public class PolarStar : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Ranged";
        public override string Texture => "CalamityMod/Projectiles/Ranged/PolarStar";

        public ref float Time => ref Projectile.ai[0];
        private static float HitboxSize = 30f;

        public int dustTypeWhite = 91;
        public int tileBounces = 0;
        public float DustScaleMultiplier = 1; // Rocket type dust has to be smaller to look good with the others
        public bool DoSlowdown = true; // On hit projectiles exist for a few extra frames to prevent projectile spam from far away
        public Vector2 StartVelocity;
        public Color EffectsColor;
        public int DustEffectsID;

        public override void SetDefaults()
        {
            Projectile.width = 18; // Projectile size is higher than normal because hitting constantly is important for this weapon
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.penetrate = -1; // Only hits once, "pierces" so that it can last a bit after hitting
            Projectile.extraUpdates = 2;
            Projectile.timeLeft = 50;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.tileCollide = true;
        }

        public override void AI()
        {
            Time++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Time == 1) // Start of the shot visuals and other things that happen when it spawns
            {
                StartVelocity = Projectile.velocity;
                switch (Projectile.ai[1])
                {
                    case 2: // Orange shot
                        DustEffectsID = ModContent.DustType<AstralOrange>();
                        EffectsColor = Color.Coral;
                        DustScaleMultiplier = 1;
                        break;
                    case 1: // Blue shot
                        DustEffectsID = ModContent.DustType<AstralBlue>();
                        EffectsColor = Color.Turquoise;
                        DustScaleMultiplier = 1;
                        break;
                    default: // Pink shot
                        DustEffectsID = 223;
                        EffectsColor = Color.Violet;
                        DustScaleMultiplier = 0.4f;
                        break;
                }

                for (int i = 0; i <= 10; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(5) ? dustTypeWhite : DustEffectsID, Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.1f, 0.3f));
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(0.85f, 1.4f) * DustScaleMultiplier;
                }
                for (int i = 0; i <= 2; i++)
                {
                    SquishyLightParticle energy = new(Projectile.Center, Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.1f, 0.4f), Main.rand.NextFloat(0.2f, 0.6f), EffectsColor, Main.rand.Next(40, 50 + 1), 0.25f, 2f);
                    GeneralParticleHandler.SpawnParticle(energy);
                }
            }

            if (Time % 3 == 0)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(15, 15), Main.rand.NextBool(5) ? dustTypeWhite : DustEffectsID);
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.5f, 1.2f) * DustScaleMultiplier;
                dust.velocity = -Projectile.velocity * Main.rand.NextFloat(0.01f, 0.045f);
            }

            if (Projectile.timeLeft % 2 == 0 && Time > 4 && DoSlowdown) // Particle trail
            {
                PointParticle spark = new PointParticle(Projectile.Center - Projectile.velocity * 1f, -Projectile.velocity * 0.01f, false, 7, 2f, EffectsColor * 0.6f);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (Projectile.timeLeft <= 2 && DoSlowdown)
            {
                Projectile.timeLeft = Main.zenithWorld ? 3 : 7; // Adds extra projectile time on hit so that you can only fire faster if you're quite close
                Projectile.velocity = Vector2.Zero;
                DoSlowdown = false;
            }

            if (!DoSlowdown)
            {
                Projectile.extraUpdates = 0;
                Projectile.alpha = 255;
            }

            Lighting.AddLight(Projectile.Center, EffectsColor.ToVector3() * 1f);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (DoSlowdown)
            {
                Projectile.timeLeft = Main.zenithWorld ? 3 : 7; // Adds extra projectile time on hit so that you can only fire faster if you're quite close
                Projectile.velocity = Vector2.Zero;
                DoSlowdown = false;
            }
        }
        public override void OnKill(int timeLeft)
        {
            int points = 5;
            float radians = MathHelper.TwoPi / points;
            Vector2 spinningPoint = Vector2.Normalize(new Vector2(-1f, -1f));
            Vector2 addedPlacement = StartVelocity * Main.rand.NextFloat(0.1f, 1.5f);
            for (int k = 0; k < points; k++)
            {
                Vector2 velocity = spinningPoint.RotatedBy(radians * k).RotatedBy(-0.45f);
                PointParticle spark = new PointParticle((Projectile.Center + velocity * 7.5f) + addedPlacement, velocity * 2.5f, false, 13, 1.2f, EffectsColor);
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }
        public override bool? CanDamage() => Projectile.numHits > 1 ? false : null;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(Projectile.Center, HitboxSize, targetHitbox);
    }
}
