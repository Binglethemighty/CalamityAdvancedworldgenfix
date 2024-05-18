using System;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using CalamityMod.Events;
using CalamityMod.World;
using CalamityMod.Particles;
using Terraria.ModLoader;
using Terraria.Audio;
using CalamityMod.NPCs.SupremeCalamitas;

namespace CalamityMod.Projectiles.Boss
{
    public class SupremeCatastropheSlash : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Boss";
        public ref float Time => ref Projectile.ai[0];
        public bool dashSlashExplode = false;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.Calamity().DealsDefenseDamage = true;

            // These never naturally use rotations, so this shouldn't be an issue.
            Projectile.width = 100;
            Projectile.height = 60;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1500;
            Projectile.Opacity = 0f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Time++;

            // Difficulty modes
            bool bossRush = BossRushEvent.BossRushActive;
            bool death = CalamityWorld.death || bossRush;
            bool revenge = CalamityWorld.revenge || bossRush;
            bool expertMode = Main.expertMode || bossRush;

            // Decide frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 7 % Main.projFrames[Projectile.type];

            // Fade in and handle visuals.
            if (Projectile.ai[2] < 4 && Projectile.ai[2] < 50)
                Projectile.Opacity = Utils.GetLerpValue(0f, 8f, Projectile.timeLeft, true) * Utils.GetLerpValue(1500f, 1492f, Projectile.timeLeft, true);

            if (Projectile.velocity.X < 0f)
            {
                Projectile.spriteDirection = -1;
                Projectile.rotation = (float)Math.Atan2(-Projectile.velocity.Y, -Projectile.velocity.X);
            }
            else
            {
                Projectile.spriteDirection = 1;
                Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X);
            }
            // Non dash slash trails
            if (Projectile.ai[2] == 50)
            {
                Projectile.extraUpdates = 0;
                if (Projectile.timeLeft > 30)
                    Projectile.timeLeft = 30;
                Projectile.Opacity = 0f;
                if (Main.rand.NextBool())
                {
                    Dust catastrophedust = Dust.NewDustPerfect(Projectile.Center, 66, -Projectile.velocity * Main.rand.NextFloat(0.1f, 1.5f));
                    catastrophedust.noGravity = true;
                    catastrophedust.scale = Main.rand.NextFloat(0.5f, 0.7f);
                    catastrophedust.color = Color.DeepSkyBlue;
                    catastrophedust.alpha = 100;
                }
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitY) * 0.1f, ModContent.ProjectileType<SupremeCatastropheSlash>(), Projectile.damage, 0f, Main.myPlayer, 0f, 5, 3 + Time);
                return;
            }
            if (Projectile.ai[2] == 1 || Projectile.ai[2] == 2)
            {
                // Rapid slashes
                if (Projectile.ai[2] == 1)
                    Projectile.extraUpdates = 2;
                else // Regular slashes
                    Projectile.velocity *= 1.0045f;
            }
            // Acceleration slashes
            else if (Projectile.ai[2] == 3)
            {
                Projectile.extraUpdates = 5;
                if (Time > 30)
                    Projectile.velocity *= 1.015f;
                if (Main.rand.NextBool(3))
                {
                    Dust catastrophedust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(50, 50) - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 8.5f, 66, -Projectile.velocity * Main.rand.NextFloat(0.2f, 1.2f));
                    catastrophedust.noGravity = true;
                    catastrophedust.scale = Main.rand.NextFloat(0.5f, 0.7f);
                    catastrophedust.color = Color.DeepSkyBlue;
                }
            }
            // Slash trails
            else if (Projectile.ai[2] >= 4 && Projectile.ai[2] < 50)
            {
                Projectile.extraUpdates = 0;
                if (Projectile.ai[1] == 5) // Non dash trails
                {
                    if (Projectile.timeLeft == 1500)
                    {
                        Projectile.timeLeft = (death ? 35 : 45) - (int)(Projectile.ai[2] - 4);
                        SparkParticle spark1 = new SparkParticle(Projectile.Center, Projectile.velocity, false, 25, 5f, Color.DeepSkyBlue * 0.35f);
                        GeneralParticleHandler.SpawnParticle(spark1);
                    }
                    else if (Projectile.timeLeft == 1 && NPC.AnyNPCs(ModContent.NPCType<SupremeCatastrophe>()) == true)
                    {
                        dashSlashExplode = true;
                        VoidSparkParticle spark = new VoidSparkParticle(Projectile.Center, Projectile.velocity, false, 9, 0.7f, Color.Cyan * 0.7f);
                        GeneralParticleHandler.SpawnParticle(spark);
                        if (Projectile.ai[2] >= 4)
                        {
                            SoundStyle charge = new("CalamityMod/Sounds/Item/ExobladeBeamSlash");
                            SoundEngine.PlaySound(charge with { Volume = 0.65f, Pitch = 0.8f }, Projectile.Center);
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 vel = new Vector2(14, 14).RotatedByRandom(100) * Main.rand.NextFloat(0.1f, 2.5f);
                            Dust catastrophedust = Dust.NewDustPerfect(Projectile.Center + vel * 2, 279, vel);
                            catastrophedust.noGravity = true;
                            catastrophedust.scale = Main.rand.NextFloat(1.2f, 1.8f);
                            catastrophedust.color = Color.DeepSkyBlue;
                        }
                    }
                }
                else // Dash trails
                {
                    if (Projectile.timeLeft == 1500)
                    {
                        Projectile.timeLeft = 40 - (int)(Projectile.ai[2] - 4);
                        SparkParticle spark1 = new SparkParticle(Projectile.Center, Projectile.velocity, false, 25, 5f, Color.DeepSkyBlue * 0.35f);
                        GeneralParticleHandler.SpawnParticle(spark1);
                        SparkParticle spark2 = new SparkParticle(Projectile.Center + Projectile.velocity * 50, Projectile.velocity, false, 25, 5f, Color.DeepSkyBlue * 0.35f);
                        GeneralParticleHandler.SpawnParticle(spark2);
                        SparkParticle spark3 = new SparkParticle(Projectile.Center - Projectile.velocity * 50, Projectile.velocity, false, 25, 5f, Color.DeepSkyBlue * 0.35f);
                        GeneralParticleHandler.SpawnParticle(spark3);
                    }
                    else if (Projectile.timeLeft == 1 && NPC.AnyNPCs(ModContent.NPCType<SupremeCatastrophe>()) == true)
                    {
                        dashSlashExplode = true;
                        VoidSparkParticle spark = new VoidSparkParticle(Projectile.Center, Projectile.velocity, false, 9, 1.3f, Color.Cyan * 0.7f);
                        GeneralParticleHandler.SpawnParticle(spark);
                        if (Projectile.ai[2] >= 4)
                        {
                            SoundStyle charge = new("CalamityMod/Sounds/Item/ExobladeBeamSlash");
                            SoundEngine.PlaySound(charge with { Volume = 0.65f, Pitch = 0.8f }, Projectile.Center);
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 vel = new Vector2(14, 14).RotatedByRandom(100) * Main.rand.NextFloat(0.1f, 2.5f);
                            Dust catastrophedust = Dust.NewDustPerfect(Projectile.Center + vel * 2, 279, vel);
                            catastrophedust.noGravity = true;
                            catastrophedust.scale = Main.rand.NextFloat(1.2f, 1.8f);
                            catastrophedust.color = Color.DeepSkyBlue;
                        }
                    }
                }
            }

            if (NPC.AnyNPCs(ModContent.NPCType<SupremeCatastrophe>()) == false && !Main.zenithWorld)
            {
                Projectile.timeLeft = 1;
                for (int k = 0; k < 10; k++)
                {
                    Vector2 velocity = new Vector2(7, 7).RotatedByRandom(100) * Main.rand.NextFloat(0.8f, 1.2f);

                    Dust dust = Dust.NewDustPerfect(Projectile.Center + velocity, 66, velocity * Main.rand.NextFloat(0.2f, 1f));
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(1.3f, 1.9f);
                    dust.color = Color.Cyan;
                }
            }

            // Emit light.
            Lighting.AddLight(Projectile.Center, 0.5f * Projectile.Opacity, 0f, 0f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects direction = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            if (Projectile.ai[1] == 0f)
                texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/SupremeCatastropheSlashAlt").Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            drawPosition -= Projectile.velocity.SafeNormalize(Vector2.UnitX) * 38f;
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

            for (int i = 0; i < 3; i++)
            {
                Color afterimageColor = Projectile.GetAlpha(lightColor) * (1f - i / 3f) * 0.5f;
                Vector2 afterimageOffset = Projectile.velocity * -i * 4f;
                Main.EntitySpriteDraw(texture, drawPosition + afterimageOffset, frame, afterimageColor, Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, direction, 0);
            }

            Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, direction, 0);
            return false;
        }

        public override bool CanHitPlayer(Player target) => Projectile.Opacity >= 1f && Projectile.ai[2] < 4 || dashSlashExplode && Projectile.ai[2] >= 4;

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (info.Damage <= 0 || Projectile.Opacity != 1f && Projectile.ai[2] < 4 || !dashSlashExplode && Projectile.ai[2] >= 4)
                return;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(Projectile.Center, (Projectile.ai[1] == 5 ? 70 : Projectile.ai[2] >= 4 ? 100 : 43), targetHitbox);
    }
}
