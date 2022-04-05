﻿using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace CalamityMod.Projectiles.Rogue
{
    public class BurningStrifeProj : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shadow Flame Spiky Ball");
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.timeLeft = 720;
            Projectile.ignoreWater = true;
            Projectile.friendly = true;
            Projectile.extraUpdates = 1;
            Projectile.Calamity().rogue = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 40;
        }

        public override void AI()
        {
            Projectile.ai[0]++;
            //Rotation code
            Projectile.rotation += Projectile.velocity.X * 0.05f * Projectile.direction;
            //Gravity
            Projectile.velocity.Y += 0.05f;
            if (Projectile.velocity.Y > 16f)
                Projectile.velocity.Y = 16f;
            //Dust
            if (Projectile.ai[0] >= 25f)
            {
                Dust.NewDust(Projectile.Center, 1, 1, DustID.Shadowflame, -Projectile.velocity.X * 0.3f, -Projectile.velocity.Y * 0.3f, 0, default, 1.1f);
                Projectile.ai[0] = 0f;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = -oldVelocity.X;
            if (Projectile.velocity.Y != oldVelocity.Y)
                Projectile.velocity.Y = -oldVelocity.Y * 0.7f;
            Projectile.velocity.X *= 0.9f;
            return false;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(BuffID.ShadowFlame, 180);
            if (Projectile.Calamity().stealthStrike && Projectile.penetrate != 1)
            {
                SoundEngine.PlaySound(SoundID.Item, Projectile.Center, 103);
                int proj = Projectile.NewProjectile(Projectile.GetProjectileSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ShadowflameExplosionBig>(), (int)(Projectile.damage * 0.33), Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].timeLeft += 20;
                Main.projectile[proj].Center = Projectile.Center;
                Main.projectile[proj].Calamity().rogue = true;
            }
        }

        public override void OnHitPvp(Player target, int damage, bool crit)
        {
            target.AddBuff(BuffID.ShadowFlame, 180);
            if (Projectile.Calamity().stealthStrike && Projectile.penetrate != 1)
            {
                SoundEngine.PlaySound(SoundID.Item, Projectile.Center, 103);
                int proj = Projectile.NewProjectile(Projectile.GetProjectileSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ShadowflameExplosionBig>(), (int)(Projectile.damage * 0.33), Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].timeLeft += 20;
                Main.projectile[proj].Center = Projectile.Center;
                Main.projectile[proj].Calamity().rogue = true;
            }
        }

        public override void Kill(int timeLeft)
        {
            int proj;
            SoundEngine.PlaySound(SoundID.Item, Projectile.Center, 103);
            if(Projectile.Calamity().stealthStrike)
                proj = Projectile.NewProjectile(Projectile.GetProjectileSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ShadowflameExplosionBig>(), (int)(Projectile.damage * 0.33), Projectile.knockBack, Projectile.owner);
            else
                proj = Projectile.NewProjectile(Projectile.GetProjectileSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ShadowflameExplosion>(), (int)(Projectile.damage * 0.33), Projectile.knockBack, Projectile.owner);
            Main.projectile[proj].Center = Projectile.Center;
            Main.projectile[proj].Calamity().rogue = true;
        }
    }
}
