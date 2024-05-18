using System;
using CalamityMod.Balancing;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Ranged
{
    public class BloodfireArrowProj : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Ranged";

        public override string Texture => "CalamityMod/Items/Ammo/BloodfireArrow";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.arrow = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 6;
            Projectile.timeLeft = 1200;
            Projectile.Calamity().pointBlankShotDuration = CalamityGlobalProjectile.DefaultPointBlankDuration;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            Projectile.spriteDirection = Projectile.direction;

            if (Projectile.localAI[0] == 0)
            {
                player.statLife -= Main.player[Main.myPlayer].lifeSteal <= 0f ? 0 : 1;
                if (player.statLife <= 0)
                {
                    PlayerDeathReason pdr = PlayerDeathReason.ByCustomReason(CalamityUtils.GetText("Status.Death.BloodFireArrow" + Main.rand.Next(1, 2 + 1)).Format(player.name));
                    player.KillMe(pdr, 1000.0, 0, false);
                }
                Projectile.velocity *= 0.4f;
            }

            Player Owner = Main.player[Projectile.owner];
            float targetDist = Vector2.Distance(Owner.Center, Projectile.Center);

            // Lighting
            Lighting.AddLight(Projectile.Center, Color.Red.ToVector3() * 0.7f);

            // Dust
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] > 6f && targetDist < 1400f)
            {
                if (Main.rand.NextBool())
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(3) ? 130 : 60, -Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.2f, 0.6f));
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(0.3f, 0.7f);
                    if (dust.type == 130)
                        dust.scale = Main.rand.NextFloat(0.25f, 0.45f);
                }
                PointParticle spark = new PointParticle(Projectile.Center - Projectile.velocity, -Projectile.velocity * 0.01f, false, 2, 1.2f, Color.Firebrick);
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int b = 0; b < 9; b++)
            {
                int dustType = Main.rand.NextBool() ? 303 : 90;
                float velMulti = Main.rand.NextFloat(0.1f, 0.75f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, dustType, new Vector2(4, 4).RotatedByRandom(100) * velMulti);
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.75f, 1.35f);
                if (dust.type == 303)
                    dust.color = Color.Firebrick;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            player.lifeRegenTime += 2;

            if (player.moonLeech)
                return;

            if (target.lifeMax <= 5)
                return;

            float lifeRatio = (float)player.statLife / player.statLifeMax2;
            float averageHealAmount = MathHelper.Lerp(4.0f, 0.5f, lifeRatio); // Average heal increases from 1/2 to 4 HP based on missing health
            int guaranteedHeal = (int)averageHealAmount;

            float chanceOfOneMoreHP = averageHealAmount - guaranteedHeal;
            bool bonusHeal = Main.rand.NextFloat() < chanceOfOneMoreHP;
            int finalHeal = guaranteedHeal + (bonusHeal ? 1 : 0);
            if (finalHeal > BalancingConstants.LifeStealCap)
                finalHeal = BalancingConstants.LifeStealCap;

            if (finalHeal > 0)
                CalamityGlobalProjectile.SpawnLifeStealProjectile(Projectile, Main.player[Projectile.owner], finalHeal, ProjectileID.VampireHeal, BalancingConstants.LifeStealRange);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 2);
            return false;
        }
    }
}
