using System;
using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Boss
{
    public class ThornBallSpike : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Boss";

        private const float MaxVelocity = 12f;

        private const int TimeLeft = 600;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.hostile = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = TimeLeft;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            if (Projectile.velocity.Length() < MaxVelocity)
            {
                Projectile.velocity *= 1.045f;
                if (Projectile.velocity.Length() > MaxVelocity)
                {
                    Projectile.velocity.Normalize();
                    Projectile.velocity *= MaxVelocity;
                }
            }
            
            if (Projectile.timeLeft < TimeLeft - 30)
                Projectile.tileCollide = true;

            int dustType = DustID.Venom;
            int dust = Dust.NewDust(new Vector2(Projectile.position.X - Projectile.velocity.X + 2f, Projectile.position.Y + 2f - Projectile.velocity.Y), 8, 8, dustType, Projectile.oldVelocity.X, Projectile.oldVelocity.Y, Projectile.alpha);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity *= -0.25f;
            Main.dust[dust].fadeIn = 1.5f;

            Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) + MathHelper.PiOver2;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(BuffID.Venom, 90);

        public override Color? GetAlpha(Color lightColor) => new Color(200, 200, 200, Projectile.alpha);

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.timeLeft > TimeLeft - 5)
                return false;

            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);

            for (int i = 2; i < 16; i++)
            {
                float oldXVel = Projectile.oldVelocity.X * (15f / (float)i);
                float oldYVel = Projectile.oldVelocity.Y * (15f / (float)i);

                float dustScale = 1.4f;
                int dustType = DustID.Venom;
                int dust = Dust.NewDust(new Vector2(Projectile.oldPosition.X - oldXVel * 0.5f, Projectile.oldPosition.Y - oldYVel * 0.5f), 8, 8, dustType, Projectile.oldVelocity.X, Projectile.oldVelocity.Y, Projectile.alpha, default, dustScale);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].fadeIn = 1.5f;

                dustScale = 1.2f;
                dust = Dust.NewDust(new Vector2(Projectile.oldPosition.X - oldXVel * 0.5f, Projectile.oldPosition.Y - oldYVel * 0.5f), 8, 8, dustType, Projectile.oldVelocity.X, Projectile.oldVelocity.Y, Projectile.alpha, default, dustScale);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.1f;
                Main.dust[dust].fadeIn = 1.5f;
            }
        }
    }
}
