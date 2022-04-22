﻿using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
namespace CalamityMod.Projectiles.Magic
{
    public class ElementBolt : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bolt");
        }

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.extraUpdates = 100;
            Projectile.friendly = true;
            Projectile.timeLeft = 30;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[1] += 1f;
            if (Projectile.localAI[1] >= 29f && Projectile.owner == Main.myPlayer)
            {
                Projectile.localAI[1] = 0f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, 0f, 0f, ModContent.ProjectileType<ElementOrb2>(), (int)(Projectile.damage * 0.7f), Projectile.knockBack, Projectile.owner, 0f, 0f);
            }

            for (int num447 = 0; num447 < 2; num447++)
            {
                Vector2 vector33 = Projectile.position;
                vector33 -= Projectile.velocity * ((float)num447 * 0.25f);
                int num448 = Dust.NewDust(vector33, 1, 1, 66, 0f, 0f, 0, new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB), 1.25f);
                Main.dust[num448].noGravity = true;
                Main.dust[num448].position = vector33;
                Main.dust[num448].scale = (float)Main.rand.Next(70, 110) * 0.013f;
                Main.dust[num448].velocity *= 0.1f;
            }
        }
    }
}
