﻿using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace CalamityMod.Projectiles.Typeless
{
    public class ShadowflameExplosionBig : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shadowflame Explosion");
        }

        public override void SetDefaults()
        {
            projectile.width = 130;
            projectile.height = 130;
            projectile.friendly = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 30;
            projectile.tileCollide = false;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            projectile.ai[1]++;
            if (projectile.ai[1] >= 3f)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 Dspeed = new Vector2(4.3f, 4.3f).RotatedBy(MathHelper.ToRadians(projectile.ai[0]));
                    float Dscale = Main.rand.NextFloat(1f, 1.5f);
                    Dust.NewDust(projectile.Center, 1, 1, DustID.Shadowflame, Dspeed.X, Dspeed.Y, 0, default, Dscale);
                    Vector2 Ds2 = Dspeed.RotatedBy(MathHelper.ToRadians(120));
                    Dust.NewDust(projectile.Center, 1, 1, DustID.Shadowflame, Ds2.X, Ds2.Y, 0, default, Dscale);
                    Vector2 Ds3 = Dspeed.RotatedBy(MathHelper.ToRadians(240));
                    Dust.NewDust(projectile.Center, 1, 1, DustID.Shadowflame, Ds3.X, Ds3.Y, 0, default, Dscale);
                    projectile.ai[0] += 19f;
                }
                projectile.ai[1] = 0f;
            }
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(BuffID.ShadowFlame, 180);
        }
    }
}
