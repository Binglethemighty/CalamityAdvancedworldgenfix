using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using static CalamityMod.Items.Weapons.Summon.LiliesOfFinality;

namespace CalamityMod.Projectiles.Summon
{
    public class LiliesOfFinalityAoE : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Summon";

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float ArianeID => ref Projectile.ai[0];

        private ref float Timer => ref Projectile.ai[1];

        private Projectile Ariane;

        private const int TimeToFullScale = 120;

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = Projectile.height = Ariane_AoESize;
            Projectile.localNPCHitCooldown = 30;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.netImportant = true;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.width / 2f, targetHitbox);

        public override void AI()
        {
            Ariane ??= Main.projectile[(int)ArianeID];

            if (Ariane != null && Ariane.active && Ariane.owner == Projectile.owner && Ariane.type == ModContent.ProjectileType<LiliesOfFinalityAriane>() && Ariane.ModProjectile<LiliesOfFinalityAriane>().State == LiliesOfFinalityAriane.AIState.Attack)
                Projectile.timeLeft = 2;

            Projectile.Center = Ariane.Center;

            // If on a dedicated server, don't bother running the visuals and sounds to save resources.
            if (!Main.dedServ)
            {
                float currentScale = Utils.Remap(Timer, 0f, TimeToFullScale, 0f, Projectile.width / 2f);

                int circleDustAmount = (int)Utils.Remap(Timer, 0f, TimeToFullScale, 5f, 40f) + Main.rand.Next(20);
                for (int i = 0; i < circleDustAmount; i++)
                {
                    if (Main.rand.NextBool(20))
                    {
                        float angle = MathHelper.TwoPi / circleDustAmount * i;
                        Vector2 position = angle.ToRotationVector2() * (currentScale + Main.rand.NextFloat(10f));

                        Particle fakeDust = new ArianeFakeDust(
                            Projectile,
                            position + Main.rand.NextVector2Circular(15f, 15f),
                            Vector2.Zero,
                            Color.Red,
                            Main.rand.NextFloat(1.5f, 2.5f),
                            120);
                        GeneralParticleHandler.SpawnParticle(fakeDust);
                    }
                }

                int eyeDustAmount = (int)Utils.Remap(Timer, 0f, TimeToFullScale, 0f, 20f);
                for (int i = 0; i < eyeDustAmount; i++)
                {
                    if (Main.rand.NextBool(15))
                    {
                        float interpolator = Main.rand.NextFloat();
                        float maxOffsetX = Utils.Remap(Timer, 0f, TimeToFullScale, 0f, 450f);
                        float maxOffsetY = Utils.Remap(Timer, 0f, TimeToFullScale, 0f, 200f);
                        Vector2 xPosition = Vector2.UnitX * MathHelper.Lerp(-maxOffsetX, maxOffsetX, interpolator);
                        Vector2 yPosition = Vector2.UnitY * MathHelper.Lerp(0f, maxOffsetY, MathF.Pow(CalamityUtils.Convert01To010(interpolator), 0.7f)) * Main.rand.NextBool().ToDirectionInt();

                        Particle fakeDust = new ArianeFakeDust(
                            Projectile,
                            xPosition + yPosition + Main.rand.NextVector2Circular(15f, 15f),
                            Vector2.Zero,
                            Color.Crimson * 0.8f,
                            Main.rand.NextFloat(2f, 2.5f),
                            120);
                        GeneralParticleHandler.SpawnParticle(fakeDust);
                    }
                }

                Dust insideDust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(currentScale, currentScale),
                    CommonDustID,
                    Main.rand.NextVector2Circular(3f, 3f),
                    Scale: Main.rand.NextFloat(0.8f, 1.2f));
                insideDust.noGravity = true;
            }

            Timer++;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.SourceDamage *= Ariane_AoEDMGMultiplier;
    }
}
