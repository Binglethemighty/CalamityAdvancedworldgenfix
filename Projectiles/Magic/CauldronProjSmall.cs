using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Magic
{
    public class CauldronProjSmall : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Magic";

        public static Asset<Texture2D> Glow;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            if (!Main.dedServ)
            {
                Glow = ModContent.Request<Texture2D>(Texture + "Glow");
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.DamageType = DamageClass.Magic;
        }
        public override void AI()
        {
            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] > 8f)
            {
                Projectile.ai[0] = 8f;
                if (Projectile.velocity.Y == 0f && Projectile.velocity.X != 0f)
                {
                    Projectile.velocity.X = Projectile.velocity.X * 0.97f;
                    if (Projectile.velocity.X > -0.01f && Projectile.velocity.X < 0.01f)
                    {
                        Projectile.velocity.X = 0f;
                        Projectile.netUpdate = true;
                    }
                }
                Projectile.velocity.Y += 0.38f;
            }
            Projectile.rotation += Projectile.velocity.X * 0.05f;
            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, Scale: Main.rand.NextFloat(0.6f, 0.8f));
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity *= 0f;
        }
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = 0.8f, Volume = 0.6f }, Projectile.Center);
            Projectile.ExpandHitboxBy(46);
            Projectile.maxPenetrate = -1;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.damage /= 2;
            Projectile.Damage();
            for (int i = 0; i < 10; i++)
            {
                int size = 10;
                Vector2 position = Projectile.Center;
                Vector2 velocity = Main.rand.NextVector2Circular(size, size);
                SquishyLightParticle energy = new(position, velocity, Main.rand.NextFloat(0.1f, 0.2f), Color.Orange, Main.rand.Next(5, 8), 1, 1.5f);
                GeneralParticleHandler.SpawnParticle(energy);
                Dust dust = Dust.NewDustPerfect(position, DustID.Torch, velocity, 0, default, Main.rand.NextFloat(0.8f, 1.2f));
                dust.noGravity = true;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 2);
            Vector2 origin = Glow.Value.Size() / 2f;
            Main.EntitySpriteDraw(Glow.Value, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public override bool? CanDamage() => Projectile.velocity.Y < 0f ? false : null;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.OnFire3, 90);

        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(BuffID.OnFire3, 90);
    }
}
