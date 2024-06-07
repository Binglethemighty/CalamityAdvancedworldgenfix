using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using SteelSeries.GameSense;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityMod.Projectiles.Melee
{
    public class RespiteblockBlood : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public Color mainColor = Color.White;
        public int time = 0;

        public override void SetDefaults()
        {
            Projectile.width = 45;
            Projectile.height = 45;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false; // Because it has a large hitbox it has custom tile kill
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900;
            Projectile.extraUpdates = 7;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Melee;
        }
        public override void AI()
        {
            if (mainColor == Color.White)
            {
                mainColor = Main.rand.NextBool() ? Color.Green : Color.Purple;
            }

            if (Collision.SolidCollision(Projectile.Center, 5, 5))
                Projectile.Kill();

            Lighting.AddLight(Projectile.Center, Color.Lerp(mainColor, Color.White, 0.5f).ToVector3());

            Player Owner = Main.player[Projectile.owner];
            float targetDist = Vector2.Distance(Owner.Center, Projectile.Center); //used for some drawing prevention for when it's offscreen since it makes a fuck load of particles
            if (Projectile.timeLeft % 2 == 0 && time >= 1f && targetDist < 1400f)
            {
                Particle spark = new WaterFlavoredParticle(Projectile.Center, -Projectile.velocity * 0.05f, false, 6, 0.8f, (mainColor * 0.65f) * Utils.GetLerpValue(0, 90, time, true));
                GeneralParticleHandler.SpawnParticle(spark);
            }
            if (Main.rand.NextBool(4))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, mainColor == Color.Green ? 89 : 86, -Projectile.velocity * Main.rand.NextFloat(0.1f, 0.8f), 0, default, Main.rand.NextFloat(0.8f, 1.1f));
                dust.noGravity = true;
                dust.noLight = true;
                dust.noLightEmittence = true;
                dust.alpha = 90;
            }

            Projectile.velocity.X *= 0.998f;
            Projectile.velocity.Y += 0.01f;

            time++;
        }
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 4; i++)
            {
                Particle blood = new PointParticle(Projectile.Center, (-Projectile.velocity * 2).RotatedByRandom(0.6f) * Main.rand.NextFloat(0.2f, 0.9f) + new Vector2(0, Main.rand.NextFloat(-5, -1 + 1)), true, 20, Main.rand.NextFloat(0.7f, 1.2f), mainColor * 0.5f, false);
                GeneralParticleHandler.SpawnParticle(blood);
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<BurningBlood>(), 180);
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(Projectile.Center, 45, targetHitbox);
    }
}
