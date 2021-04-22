using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityMod.Projectiles.Melee
{
	public class PlagueDust : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Dust");
			Main.projFrames[projectile.type] = 4;
			ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
			ProjectileID.Sets.TrailingMode[projectile.type] = 0;
		}

		public override void SetDefaults()
		{
			projectile.width = 70;
			projectile.height = 70;
			projectile.friendly = true;
			projectile.ignoreWater = true;
			projectile.tileCollide = false;
			projectile.melee = true;
			projectile.penetrate = -1;
			projectile.extraUpdates = 3;
			projectile.timeLeft = 150;
			projectile.alpha = 100;
		}

		public override void AI()
		{
			projectile.ai[1]++;
			Lighting.AddLight(projectile.Center, 0.05f, 0.4f, 0f);
			if (projectile.ai[1] < 60)
			{
				if (projectile.ai[0] > 7f)
				{
					float scalar = 1f;
					if (projectile.ai[0] == 8f)
					{
						scalar = 0.25f;
					}
					else if (projectile.ai[0] == 9f)
					{
						scalar = 0.5f;
					}
					else if (projectile.ai[0] == 10f)
					{
						scalar = 0.75f;
					}
					projectile.ai[0] += 1f;
					//Dust
					int dustType = 89;
					int plague = Dust.NewDust(projectile.position, projectile.width, projectile.height, dustType, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 100, default, 1f);
					Dust dust = Main.dust[plague];
					if (Main.rand.NextBool(3))
					{
						dust.noGravity = true;
						dust.scale *= 1.8f;
						dust.velocity.X *= 2f;
						dust.velocity.Y *= 2f;
					}
					else
					{
						dust.scale *= 1.3f;
					}
					dust.velocity.X *= 1.2f;
					dust.velocity.Y *= 1.2f;
					dust.scale *= scalar;
				}
				else
				{
					projectile.ai[0] += 1f;
				}
			}
			else
			{
				projectile.damage = (int)(projectile.damage * 0.6);
				projectile.velocity *= 0.85f;
				//Fade out
				if (projectile.alpha < 255)
					projectile.alpha += 5;
				if (projectile.alpha >= 255)
					projectile.Kill();
			}
			//Rotation
			projectile.spriteDirection = projectile.direction = (projectile.velocity.X > 0).ToDirectionInt();
			projectile.rotation = projectile.velocity.ToRotation() + (projectile.spriteDirection == 1 ? 0f : MathHelper.Pi);

			//Animation
			projectile.frameCounter++;
			if (projectile.frameCounter > 8)
			{
				projectile.frame++;
				projectile.frameCounter = 0;
			}
			if (projectile.frame > 3)
			{
				projectile.frame = 0;
			}
		}

		public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
		{
			target.immune[projectile.owner] = 5;
			target.AddBuff(ModContent.BuffType<Plague>(), 120);
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			CalamityUtils.DrawAfterimagesCentered(projectile, ProjectileID.Sets.TrailingMode[projectile.type], lightColor, 1);
			return false;
		}
	}
}
