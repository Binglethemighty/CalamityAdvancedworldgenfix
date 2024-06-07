using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Ranged
{
    public class BloodfireBulletProj : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Ranged";
        private const int Lifetime = 1200;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.extraUpdates = 12;
            Projectile.timeLeft = Lifetime;
            Projectile.Calamity().pointBlankShotDuration = CalamityGlobalProjectile.DefaultPointBlankDuration;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            Projectile.scale = 0.75f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.spriteDirection = Projectile.direction;
            if (Projectile.localAI[0] == 0)
            {
                Projectile.velocity *= 0.7f;
            }
            Player Owner = Main.player[Projectile.owner];
            float targetDist = Vector2.Distance(Owner.Center, Projectile.Center);

            // Lighting
            Lighting.AddLight(Projectile.Center, Color.Red.ToVector3() * 0.5f);

            // Dust
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] > 6f)
            {
                if (Main.rand.NextBool(3))
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(3) ? 130 : 60, -Projectile.velocity.RotatedByRandom(0.1f) * Main.rand.NextFloat(0.01f, 0.3f));
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(0.5f, 0.9f);
                    if (dust.type == 130)
                        dust.scale = Main.rand.NextFloat(0.35f, 0.55f);
                }
                if (targetDist < 1400f)
                {
                    SparkParticle spark = new SparkParticle(Projectile.Center - Projectile.velocity, -Projectile.velocity * 0.01f, false, 4, 0.4f, Color.Firebrick);
                    GeneralParticleHandler.SpawnParticle(spark);
                }
            }
        }

        // These bullets glow in the dark.
        public override Color? GetAlpha(Color lightColor) => new Color(255, 255, 255, 100);

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.localAI[0] > 6f)
            {
                CalamityUtils.DrawAfterimagesFromEdge(Projectile, 0, Color.Red);
            }
            return true;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.SourceDamage.Flat += OnHitEffect(Main.player[Projectile.owner]);

        // Returns the amount of bonus damage that should be dealt. Boosts life regeneration appropriately as a side effect.
        private int OnHitEffect(Player owner)
        {
            // Adds 3 frames to lifeRegenTime on every hit. This increased value is used for the damage calculation.
            owner.lifeRegenTime += 3;

            // Deals (1.00 + (0.1 * current lifeRegen))% of current lifeRegenTime as flat bonus damage on hit.
            // For example, at 0 life regen, you get 1% of lifeRegenTime as bonus damage.
            // At 10 life regen, you get 2%. At 20 life regen, you get 3%.
            // Negative life regen does not decrease damage.
            int regenForCalc = owner.lifeRegen > 0 ? owner.lifeRegen : 0;
            float regenDamageRatio = (1f + 0.1f * regenForCalc) / 100f;

            // For the sake of bonus damage, life regen time caps at 3600, aka 60 seconds. This is its natural cap in vanilla. 75% of this is taken for computing the final damage.
            int regenTimeForCalc = (int)(MathHelper.Clamp(owner.lifeRegenTime, 0f, 3600f) * 0.75f);

            int finalDamageBoost = (int)(regenDamageRatio * regenTimeForCalc);
            // Damage boost has a cap of 35 to prevent it from getting too crazy.
            int damageCap = 35;
            if (finalDamageBoost > damageCap)
                finalDamageBoost = damageCap;

            if (finalDamageBoost == damageCap) // Special hit visual if the bonus damage is at the cap.
            {
                for (int k = 0; k < 3; k++)
                {
                    BloodParticle blood = new BloodParticle(Projectile.Center, new Vector2(6.5f, 6.5f).RotatedByRandom(100) * Main.rand.NextFloat(0.8f, 1.2f), Main.rand.Next(8, 10 + 1), Main.rand.NextFloat(0.7f, 0.9f), Color.Red);
                    GeneralParticleHandler.SpawnParticle(blood);
                }
            }
            return finalDamageBoost;
        }

        public override void OnKill(int timeLeft)
        {
            for (int k = 0; k < 3; k++)
            {
                SparkParticle spark = new SparkParticle(Projectile.Center, -Projectile.velocity.RotatedByRandom(0.5) * Main.rand.NextFloat(1f, 3f), false, Main.rand.Next(5, 7 + 1), Main.rand.NextFloat(0.4f, 0.6f), Color.Red);
                GeneralParticleHandler.SpawnParticle(spark);
            }
            for (int k = 0; k < 8; k++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(3) ? 130 : 60, new Vector2(3, 3).RotatedByRandom(100) * Main.rand.NextFloat(0.5f, 1.5f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.5f, 0.9f);
                if (dust.type == 130)
                    dust.scale = Main.rand.NextFloat(0.35f, 0.55f);
            }
        }
    }
}
