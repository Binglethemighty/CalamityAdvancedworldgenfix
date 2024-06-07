using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using CalamityMod.Particles;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using SteelSeries.GameSense;
using CalamityMod.Buffs.DamageOverTime;
using Mono.Cecil;

namespace CalamityMod.Projectiles.Typeless
{
    public class PlaguePulse : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Typeless";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public ref float time => ref Projectile.ai[0];
        public ref float radius => ref Projectile.ai[1];
        public float maxRadius = 300;
        public int startDamage;
        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.extraUpdates = 1;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (time == 0)
                startDamage = Projectile.damage;

            Player player = Main.player[Projectile.owner];
            if (Projectile.ai[2] == 0)
                Projectile.Center = player.MountedCenter;
            if (Projectile.ai[2] == 1)
            {
                maxRadius = 200;
            }

            if (Main.rand.NextBool(5))
            {
                DirectionalPulseRing pulse = new DirectionalPulseRing(Projectile.Center + Main.rand.NextVector2Circular(radius, radius), Vector2.Zero, (Main.rand.NextBool(3) ? Color.LimeGreen : Color.Green) * 0.6f, new Vector2(1, 1), 0, Main.rand.NextFloat(0.07f, 0.23f), 0f, 20);
                GeneralParticleHandler.SpawnParticle(pulse);
            }
            if (Main.rand.NextBool(3))
            {
                for (int i = 0; i < 2; i++)
                {
                    int DustID = 89;
                    Vector2 spawnPos = Projectile.Center + Main.rand.NextVector2Circular(radius, radius);
                    Dust dust2 = Dust.NewDustPerfect(spawnPos, DustID);
                    dust2.scale = Main.rand.NextFloat(0.5f, 0.9f);
                    dust2.velocity = (spawnPos - Projectile.Center).SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(5, 10);
                    dust2.noGravity = true;
                }
            }

            radius = Utils.Remap(time, 0, 60, 30, maxRadius, true);
            time++;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
                Projectile.damage = (int)(Projectile.damage * 0.85f);
            if (Projectile.damage < 1)
                Projectile.damage = 1;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            target.AddBuff(ModContent.BuffType<Plague>(), 420);

            if (target.life <= 0)
            {
                player.Heal(10);
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<PlaguePulse>(), (int)(startDamage * 0.8f), 0f, Projectile.owner, 0, 0, 1);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Player player = Main.player[Projectile.owner];

            Texture2D rexture = ModContent.Request<Texture2D>("CalamityMod/Particles/HighResHollowCircleHardEdge").Value;
            Main.EntitySpriteDraw(rexture, Projectile.Center - Main.screenPosition, null, (Color.Green * Utils.Remap(time, 30, 60, 0.35f, 0, true)) with { A = 0 }, Projectile.rotation, rexture.Size() * 0.5f, (Utils.Remap(time, 0, 60, 1, 6, true) / 21) * (maxRadius == 300 ? 1 : 0.7f), SpriteEffects.None, 0);
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(Projectile.Center, radius, targetHitbox);
    }
}
