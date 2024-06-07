using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Boss
{
    public class ShaderainHostile : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Boss";

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 300;
            Projectile.alpha = 50;
        }

        public override void OnKill(int timeLeft)
        {
            int shadeDust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y + (float)Projectile.height - 2f), 2, 2, DustID.Demonite, 0f, 0f, 0, default, 1f);
            Dust dust = Main.dust[shadeDust];
            dust.position.X -= 2f;
            dust.alpha = 38;
            dust.velocity *= 0.1f;
            dust.velocity += -Projectile.oldVelocity * 0.25f;
            dust.scale = 0.95f;
        }

        public override Color? GetAlpha(Color lightColor) => new Color(100, 255, 100, Projectile.alpha);

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (info.Damage <= 0)
                return;

            target.AddBuff(ModContent.BuffType<BrainRot>(), 120);
        }
    }
}
