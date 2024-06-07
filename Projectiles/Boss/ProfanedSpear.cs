using System;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Providence;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Boss
{
    public class ProfanedSpear : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Boss";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.Calamity().DealsDefenseDamage = true;
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.45f, 0.35f, 0f);

            if (CalamityGlobalNPC.doughnutBoss != -1)
            {
                if (Main.npc[CalamityGlobalNPC.doughnutBoss].active)
                {
                    if (Main.npc[CalamityGlobalNPC.doughnutBoss].Calamity().CurrentlyEnraged)
                        Projectile.maxPenetrate = (int)Providence.BossMode.Night;
                    else
                        Projectile.maxPenetrate = (int)Providence.BossMode.Day;
                }
            }

            if (Projectile.timeLeft < 510)
                Projectile.tileCollide = true;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] <= 20f)
                Projectile.velocity *= 0.95f;
            else
                Projectile.velocity *= 1.06f;

            if (Projectile.ai[0] > 40f)
                Projectile.ai[0] = 0f;

            float maxVelocity = 20f;
            if (Projectile.velocity.Length() > maxVelocity)
            {
                Projectile.velocity.Normalize();
                Projectile.velocity *= maxVelocity;
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return ProvUtils.GetProjectileColor(Projectile.maxPenetrate, Projectile.alpha);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = (Projectile.maxPenetrate == (int)Providence.BossMode.Day) ? Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value : ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/ProfanedSpearNight").Value;
            Projectile.DrawBackglow(ProvUtils.GetProjectileColor(Projectile.maxPenetrate, Projectile.alpha, true), 4f, texture);
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(new Rectangle(0, 0, texture.Width, texture.Height)), Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2(texture.Width / 2f, texture.Height / 2f), Projectile.scale, SpriteEffects.None, 0);
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (info.Damage <= 0)
                return;

            target.AddBuff(ModContent.BuffType<HolyFlames>(), 120);
        }
    }
}
