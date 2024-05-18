using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Rogue
{
    public class ExorcismProj : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Rogue";
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/Exorcism";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.alpha = 0;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 600;
            Projectile.DamageType = RogueDamageClass.Instance;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.1f;
            Projectile.rotation += 0.05f * Projectile.direction;

            // Dust Effects
            Vector2 dustLeft = (new Vector2(-1, 0)).RotatedBy(Projectile.rotation);
            Vector2 dustRight = (new Vector2(1, 0)).RotatedBy(Projectile.rotation);
            Vector2 dustUp = (new Vector2(0, -1)).RotatedBy(Projectile.rotation);
            Vector2 dustDown = (new Vector2(0, 1) * 2f).RotatedBy(Projectile.rotation);

            float minSpeed = 1.5f;
            float maxSpeed = 5f;
            float minScale = 0.8f;
            float maxScale = 1.4f;

            int dustType = 175;
            int dustCount = (int)(5 * (Projectile.ai[0]));

            for (int i = 0; i < dustCount; i++)
            {
                int left = Dust.NewDust(Projectile.Center, 1, 1, dustType, 0f, 0f);
                Main.dust[left].noGravity = true;
                Main.dust[left].position = Projectile.Center;
                Main.dust[left].velocity = dustLeft * Main.rand.NextFloat(minSpeed, maxSpeed) + Projectile.velocity;
                Main.dust[left].scale = Main.rand.NextFloat(minScale, maxScale);

                int right = Dust.NewDust(Projectile.Center, 1, 1, dustType, 0f, 0f);
                Main.dust[right].noGravity = true;
                Main.dust[right].position = Projectile.Center;
                Main.dust[right].velocity = dustRight * Main.rand.NextFloat(minSpeed, maxSpeed) + Projectile.velocity;
                Main.dust[right].scale = Main.rand.NextFloat(minScale, maxScale);

                int up = Dust.NewDust(Projectile.Center, 1, 1, dustType, 0f, 0f);
                Main.dust[up].noGravity = true;
                Main.dust[up].position = Projectile.Center;
                Main.dust[up].velocity = dustUp * Main.rand.NextFloat(minSpeed, maxSpeed) + Projectile.velocity;
                Main.dust[up].scale = Main.rand.NextFloat(minScale, maxScale);

                int down = Dust.NewDust(Projectile.Center, 1, 1, dustType, 0f, 0f);
                Main.dust[down].noGravity = true;
                Main.dust[down].position = Projectile.Center;
                Main.dust[down].velocity = dustDown * Main.rand.NextFloat(minSpeed, maxSpeed) + Projectile.velocity;
                Main.dust[down].scale = Main.rand.NextFloat(minScale, maxScale);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Stars
            if (Projectile.Calamity().stealthStrike)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 pos = new Vector2(Projectile.Center.X + (float)Projectile.width * 0.5f + (float)Main.rand.Next(-201, 201), Main.screenPosition.Y - 600f - Main.rand.Next(50));
                    Vector2 starVelocity = CalamityUtils.CalculatePredictiveAimToTargetMaxUpdates(pos, target, 30f, 2);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), pos, starVelocity, ModContent.ProjectileType<ExorcismStar>(), (int)(Projectile.damage * 0.75f), Projectile.knockBack * 0.5f, Projectile.owner, Main.rand.NextFloat(-3f, 3f), 0f);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            Projectile.Kill();
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            //Crystal smash sound
            SoundEngine.PlaySound(SoundID.Item27, Projectile.Center);
            // Light burst
            int p = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ExorcismShockwave>(), (int)(Projectile.damage * 0.8f), 0, Projectile.owner, Projectile.ai[0], 0);
            Main.projectile[p].rotation = Projectile.rotation;
            if (Projectile.Calamity().stealthStrike)
                Main.projectile[p].Calamity().stealthStrike = true;
        }
    }
}
