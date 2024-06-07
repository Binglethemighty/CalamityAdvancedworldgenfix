using System;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.CalPlayer;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.NPCs.Abyss;
using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.NPCs.BrimstoneElemental;
using CalamityMod.NPCs.Bumblebirb;
using CalamityMod.NPCs.CalClone;
using CalamityMod.NPCs.CeaselessVoid;
using CalamityMod.NPCs.Crags;
using CalamityMod.NPCs.NormalNPCs;
using CalamityMod.NPCs.OldDuke;
using CalamityMod.NPCs.SulphurousSea;
using CalamityMod.NPCs.SunkenSea;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Projectiles.Enemy;
using CalamityMod.Sounds;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.NPCs.CalamityAIs.CalamityBossAIs
{
    public static class CalamitasCloneAI
    {
        public static void VanillaCalamitasCloneAI(NPC npc, Mod mod)
        {
            CalamityGlobalNPC calamityGlobalNPC = npc.Calamity();

            // Emit light
            Lighting.AddLight((int)((npc.position.X + (npc.width / 2)) / 16f), (int)((npc.position.Y + (npc.height / 2)) / 16f), 1f, 0f, 0f);

            // Variables for increasing difficulty
            bool bossRush = BossRushEvent.BossRushActive;
            bool death = CalamityWorld.death || bossRush;
            bool revenge = CalamityWorld.revenge || bossRush;
            bool expertMode = Main.expertMode || bossRush;

            // Percent life remaining
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Phases
            bool phase2 = lifeRatio < 0.7f || death;
            bool phase3 = lifeRatio < 0.35f;
            bool phase4 = lifeRatio <= 0.1f && death;

            // Don't take damage during bullet hells
            npc.dontTakeDamage = calamityGlobalNPC.newAI[2] > 0f;

            // Variable for live brothers
            bool brotherAlive = false;

            // For seekers
            CalamityGlobalNPC.calamitas = npc.whoAmI;

            // Seeker ring
            if (calamityGlobalNPC.newAI[1] == 0f && phase3 && expertMode)
            {
                SoundEngine.PlaySound(SoundID.Item72, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int seekerAmt = death ? 10 : 5;
                    int seekerSpread = 360 / seekerAmt;
                    int seekerDistance = death ? 180 : 150;
                    for (int i = 0; i < seekerAmt; i++)
                    {
                        int spawn = NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X + (Math.Sin(i * seekerSpread) * seekerDistance)), (int)(npc.Center.Y + (Math.Cos(i * seekerSpread) * seekerDistance)), ModContent.NPCType<SoulSeeker>(), npc.whoAmI, 0, 0, 0, -1);
                        Main.npc[spawn].ai[0] = i * seekerSpread;
                    }
                }

                string key = "Mods.CalamityMod.Status.Boss.CalamitasBossText3";
                Color messageColor = Color.Orange;
                CalamityUtils.DisplayLocalizedText(key, messageColor);

                calamityGlobalNPC.newAI[1] = 1f;
            }

            // Do bullet hell or spawn brothers
            if (calamityGlobalNPC.newAI[0] == 0f && npc.life > 0)
                calamityGlobalNPC.newAI[0] = npc.lifeMax;

            // Bullet hells at 70% and 10%, brothers at 40%
            if (npc.life > 0)
            {
                int calClonePhaseThreshold = (int)(npc.lifeMax * 0.3);
                if ((npc.life + calClonePhaseThreshold) < calamityGlobalNPC.newAI[0])
                {
                    calamityGlobalNPC.newAI[0] = npc.life;
                    if (calamityGlobalNPC.newAI[0] <= npc.lifeMax * 0.1)
                    {
                        SoundEngine.PlaySound(SoundID.Item109, npc.Center);
                        calamityGlobalNPC.newAI[2] = 2f;

                        if (CalamityWorld.LegendaryMode && CalamityWorld.revenge)
                            calamityGlobalNPC.newAI[3] = 0f;

                        SpawnDust();
                    }
                    else if (calamityGlobalNPC.newAI[0] <= npc.lifeMax * 0.4)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.position.Y + npc.height, ModContent.NPCType<Cataclysm>(), npc.whoAmI);
                            NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.position.Y + npc.height, ModContent.NPCType<Catastrophe>(), npc.whoAmI);
                        }

                        string key = "Mods.CalamityMod.Status.Boss.CalamitasBossText2";
                        Color messageColor = Color.Orange;
                        CalamityUtils.DisplayLocalizedText(key, messageColor);

                        SpawnDust();
                    }
                    else
                    {
                        SoundEngine.PlaySound(SoundID.Item109, npc.Center);
                        calamityGlobalNPC.newAI[2] = 1f;

                        if (CalamityWorld.LegendaryMode && CalamityWorld.revenge)
                            calamityGlobalNPC.newAI[3] = 0f;

                        SpawnDust();
                    }
                }
            }

            // Immunity if brothers are alive
            if (CalamityGlobalNPC.cataclysm != -1)
            {
                if (Main.npc[CalamityGlobalNPC.cataclysm].active)
                    brotherAlive = true;
            }
            if (CalamityGlobalNPC.catastrophe != -1)
            {
                if (Main.npc[CalamityGlobalNPC.catastrophe].active)
                    brotherAlive = true;
            }

            if (brotherAlive)
                npc.dontTakeDamage = true;

            void SpawnDust()
            {
                int dustAmt = 50;
                int random = 3;

                for (int j = 0; j < 10; j++)
                {
                    random += j;
                    int dustAmtSpawned = 0;
                    int scale = random * 6;
                    float dustPositionX = npc.Center.X - (scale / 2);
                    float dustPositionY = npc.Center.Y - (scale / 2);
                    while (dustAmtSpawned < dustAmt)
                    {
                        float dustVelocityX = Main.rand.Next(-random, random);
                        float dustVelocityY = Main.rand.Next(-random, random);
                        float dustVelocityScalar = random * 2f;
                        float dustVelocity = (float)Math.Sqrt(dustVelocityX * dustVelocityX + dustVelocityY * dustVelocityY);
                        dustVelocity = dustVelocityScalar / dustVelocity;
                        dustVelocityX *= dustVelocity;
                        dustVelocityY *= dustVelocity;
                        int dust = Dust.NewDust(new Vector2(dustPositionX, dustPositionY), scale, scale, (int)CalamityDusts.Brimstone, 0f, 0f, 100, default, 2f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].position.X = npc.Center.X;
                        Main.dust[dust].position.Y = npc.Center.Y;
                        Main.dust[dust].position.X += Main.rand.Next(-10, 11);
                        Main.dust[dust].position.Y += Main.rand.Next(-10, 11);
                        Main.dust[dust].velocity.X = dustVelocityX;
                        Main.dust[dust].velocity.Y = dustVelocityY;
                        dustAmtSpawned++;
                    }
                }
            }

            // Get a target
            if (npc.target < 0 || npc.target == Main.maxPlayers || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            // Target variable
            Player player = Main.player[npc.target];

            float enrageScale = bossRush ? 1f : 0f;
            if (Main.IsItDay() || bossRush)
            {
                npc.Calamity().CurrentlyEnraged = !bossRush;
                enrageScale += 2f;
            }

            // Rotation
            Vector2 npcCenter = new Vector2(npc.Center.X, npc.position.Y + npc.height - 59f);
            Vector2 lookAt = new Vector2(player.position.X - (player.width / 2), player.position.Y - (player.height / 2));
            Vector2 rotationVector = npcCenter - lookAt;

            // Boss Rush predictive charge rotation
            if (npc.ai[1] == 4f && phase4 && bossRush)
            {
                // Velocity
                float chargeVelocity = 30f;
                chargeVelocity += 5f * enrageScale;
                lookAt += Main.player[npc.target].velocity * 20f;
                rotationVector = Vector2.Normalize(npcCenter - lookAt) * chargeVelocity;
            }

            float rotation = (float)Math.Atan2(rotationVector.Y, rotationVector.X) + MathHelper.PiOver2;
            if (rotation < 0f)
                rotation += MathHelper.TwoPi;
            else if (rotation > MathHelper.TwoPi)
                rotation -= MathHelper.TwoPi;

            float rotationAmt = 0.1f;
            if (npc.rotation < rotation)
            {
                if ((rotation - npc.rotation) > MathHelper.Pi)
                    npc.rotation -= rotationAmt;
                else
                    npc.rotation += rotationAmt;
            }
            else if (npc.rotation > rotation)
            {
                if ((npc.rotation - rotation) > MathHelper.Pi)
                    npc.rotation += rotationAmt;
                else
                    npc.rotation -= rotationAmt;
            }

            if (npc.rotation > rotation - rotationAmt && npc.rotation < rotation + rotationAmt)
                npc.rotation = rotation;
            if (npc.rotation < 0f)
                npc.rotation += MathHelper.TwoPi;
            else if (npc.rotation > MathHelper.TwoPi)
                npc.rotation -= MathHelper.TwoPi;
            if (npc.rotation > rotation - rotationAmt && npc.rotation < rotation + rotationAmt)
                npc.rotation = rotation;

            // Despawn
            if (!player.active || player.dead)
            {
                npc.TargetClosest(false);
                player = Main.player[npc.target];
                if (!player.active || player.dead)
                {
                    if (npc.velocity.Y > 3f)
                        npc.velocity.Y = 3f;
                    npc.velocity.Y -= 0.1f;
                    if (npc.velocity.Y < -12f)
                        npc.velocity.Y = -12f;

                    if (npc.timeLeft > 60)
                        npc.timeLeft = 60;

                    if (npc.ai[1] != 0f)
                    {
                        npc.ai[1] = 0f;
                        npc.ai[2] = 0f;
                        npc.ai[3] = 0f;
                        calamityGlobalNPC.newAI[2] = 0f;
                        calamityGlobalNPC.newAI[3] = 0f;
                        npc.alpha = 0;
                        npc.netUpdate = true;
                    }
                    return;
                }
            }
            else if (npc.timeLeft < 1800)
                npc.timeLeft = 1800;

            // Distance from destination where Cal Clone stops moving
            float movementDistanceGateValue = 100f;

            // How fast Cal Clone moves to the destination
            float baseVelocity = (expertMode ? 10f : 8.5f) * (npc.ai[1] == 4f ? 1.4f : 1f);
            float baseAcceleration = (expertMode ? 0.18f : 0.155f) * (npc.ai[1] == 4f ? 1.4f : 1f);
            baseVelocity += 4f * enrageScale;
            baseAcceleration += 0.1f * enrageScale;
            if (revenge)
            {
                baseVelocity += 1.5f * (1f - lifeRatio);
                baseAcceleration += 0.03f * (1f - lifeRatio);
            }
            if (death)
            {
                baseVelocity += 1.5f * (1f - lifeRatio);
                baseAcceleration += 0.03f * (1f - lifeRatio);
            }
            if (Main.getGoodWorld)
            {
                baseVelocity *= 1.15f;
                baseAcceleration *= 1.15f;
            }

            // What side Cal Clone should be on, relative to the target
            int xPos = 1;
            if (npc.Center.X < player.Center.X)
                xPos = -1;

            // How far Cal Clone should be from the target
            float averageDistance = 500f;
            float chargeDistance = phase4 ? 300f : 400f;

            // This is where Cal Clone should be
            Vector2 destination = (calamityGlobalNPC.newAI[2] > 0f || npc.ai[1] == 0f) ? new Vector2(player.Center.X, player.Center.Y - averageDistance) :
                npc.ai[1] == 1f ? new Vector2(player.Center.X + averageDistance * xPos, player.Center.Y) :
                new Vector2(player.Center.X + chargeDistance * xPos, player.Center.Y);

            // Add some random distance to the destination after certain attacks
            if (npc.localAI[0] == 1f)
            {
                npc.localAI[0] = 0f;
                npc.localAI[2] = Main.rand.Next(-50, 51);
                npc.localAI[3] = Main.rand.Next(-300, 301);
                npc.netUpdate = true;
            }

            // Add a bit of randomness to the destination
            if (death)
            {
                destination.X += npc.ai[1] == 0f ? npc.localAI[3] : npc.localAI[2];
                destination.Y += npc.ai[1] == 0f ? npc.localAI[2] : npc.localAI[3];
            }

            // How far Cal Clone is from where she's supposed to be
            Vector2 distanceFromDestination = destination - npc.Center;

            // Movement
            if (npc.ai[1] == 0f || npc.ai[1] == 1f || npc.ai[1] == 4f || calamityGlobalNPC.newAI[2] > 0f)
                CalamityUtils.SmoothMovement(npc, movementDistanceGateValue, distanceFromDestination, baseVelocity, baseAcceleration, true);

            // Bullet hell phase
            if (calamityGlobalNPC.newAI[2] > 0f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                if (calamityGlobalNPC.newAI[3] < 900f)
                {
                    calamityGlobalNPC.newAI[3] += 1f;
                    npc.dontTakeDamage = true;
                    npc.alpha = 255;

                    float rotX = player.Center.X - npc.Center.X;
                    float rotY = player.Center.Y - npc.Center.Y;
                    npc.rotation = (float)Math.Atan2(rotY, rotX) - MathHelper.PiOver2;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (calamityGlobalNPC.newAI[2] == 2f)
                        {
                            int type = ModContent.ProjectileType<SCalBrimstoneFireblast>();
                            int damage = npc.GetProjectileDamage(type);
                            if (Main.zenithWorld)
                                type = ModContent.ProjectileType<SCalBrimstoneGigablast>();

                            float gigaBlastFrequency = (Main.getGoodWorld ? 120f : expertMode ? 180f : 240f) - enrageScale * 15f;
                            float projSpeed = bossRush ? 6.25f : 5f;
                            if (calamityGlobalNPC.newAI[3] <= 300f)
                            {
                                if (calamityGlobalNPC.newAI[3] % gigaBlastFrequency == 0f) // Blasts from top
                                    Projectile.NewProjectile(npc.GetSource_FromAI(), player.position.X + Main.rand.Next(-1000, 1001), player.position.Y - 1000f, 0f, projSpeed, type, damage, 0f, Main.myPlayer);
                            }
                            else if (calamityGlobalNPC.newAI[3] <= 600f && calamityGlobalNPC.newAI[3] > 300f)
                            {
                                if (calamityGlobalNPC.newAI[3] % gigaBlastFrequency == 0f) // Blasts from right
                                    Projectile.NewProjectile(npc.GetSource_FromAI(), player.position.X + 1000f, player.position.Y + Main.rand.Next(-1000, 1001), -projSpeed, 0f, type, damage, 0f, Main.myPlayer);
                            }
                            else if (calamityGlobalNPC.newAI[3] > 600f)
                            {
                                if (calamityGlobalNPC.newAI[3] % gigaBlastFrequency == 0f) // Blasts from top
                                    Projectile.NewProjectile(npc.GetSource_FromAI(), player.position.X + Main.rand.Next(-1000, 1001), player.position.Y - 1000f, 0f, projSpeed, type, damage, 0f, Main.myPlayer);
                            }
                        }
                    }

                    npc.ai[0] += 1f;
                    float hellblastGateValue = (expertMode ? 12f : 16f) - enrageScale;
                    if (npc.ai[0] >= hellblastGateValue)
                    {
                        npc.ai[0] = 0f;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int type = ModContent.ProjectileType<BrimstoneHellblast2>();
                            int damage = npc.GetProjectileDamage(type);
                            float projSpeed = bossRush ? 5f : 4f;
                            if (calamityGlobalNPC.newAI[3] % (hellblastGateValue * 6f) == 0f)
                            {
                                float distance = Main.rand.NextBool() ? -1000f : 1000f;
                                float velocity = distance == -1000f ? projSpeed : -projSpeed;
                                Projectile.NewProjectile(npc.GetSource_FromAI(), player.position.X + distance, player.position.Y, velocity, 0f, type, damage, 0f, Main.myPlayer, 2f, 0f);
                            }

                            if (calamityGlobalNPC.newAI[3] < 300f) // Blasts from above
                            {
                                Projectile.NewProjectile(npc.GetSource_FromAI(), player.position.X + Main.rand.Next(-1000, 1001), player.position.Y - 1000f, 0f, projSpeed, type, damage, 0f, Main.myPlayer, 2f, 0f);
                            }
                            else if (calamityGlobalNPC.newAI[3] < 600f) // Blasts from left and right
                            {
                                Projectile.NewProjectile(npc.GetSource_FromAI(), player.position.X + 1000f, player.position.Y + Main.rand.Next(-1000, 1001), -(projSpeed - 0.5f), 0f, type, damage, 0f, Main.myPlayer, 2f, 0f);
                                Projectile.NewProjectile(npc.GetSource_FromAI(), player.position.X - 1000f, player.position.Y + Main.rand.Next(-1000, 1001), projSpeed - 0.5f, 0f, type, damage, 0f, Main.myPlayer, 2f, 0f);
                            }
                            else // Blasts from above, left, and right
                            {
                                Projectile.NewProjectile(npc.GetSource_FromAI(), player.position.X + Main.rand.Next(-1000, 1001), player.position.Y - 1000f, 0f, projSpeed - 1f, type, damage, 0f, Main.myPlayer, 2f, 0f);
                                Projectile.NewProjectile(npc.GetSource_FromAI(), player.position.X + 1000f, player.position.Y + Main.rand.Next(-1000, 1001), -(projSpeed - 1f), 0f, type, damage, 0f, Main.myPlayer, 2f, 0f);
                                Projectile.NewProjectile(npc.GetSource_FromAI(), player.position.X - 1000f, player.position.Y + Main.rand.Next(-1000, 1001), projSpeed - 1f, 0f, type, damage, 0f, Main.myPlayer, 2f, 0f);
                            }
                        }
                    }
                }
                else
                {
                    npc.ai[0] = 0f;
                    npc.ai[3] = 0f;
                    npc.localAI[1] = 0f;
                    calamityGlobalNPC.newAI[2] = 0f;
                    calamityGlobalNPC.newAI[3] = 0f;

                    // Prevent bullshit charge hits when second bullet hell ends.
                    if (phase4)
                    {
                        npc.ai[1] = 4f;
                        npc.ai[2] = -105f;
                        npc.TargetClosest();
                    }
                    else
                    {
                        if (death)
                        {
                            int AIState = Main.rand.Next(3);
                            switch (AIState)
                            {
                                case 0:
                                    npc.ai[1] = 0f;
                                    npc.ai[2] = 0f;
                                    break;
                                case 1:
                                    npc.ai[1] = 1f;
                                    npc.ai[2] = 0f;
                                    break;
                                case 2:
                                    npc.ai[1] = 4f;
                                    npc.ai[2] = -105f;
                                    npc.TargetClosest();
                                    break;
                            }
                        }
                        else
                        {
                            npc.ai[1] = 0f;
                            npc.ai[2] = 0f;
                        }

                        if (death)
                            npc.localAI[0] = 1f;
                    }

                    npc.netUpdate = true;

                    for (int x = 0; x < Main.maxProjectiles; x++)
                    {
                        Projectile projectile = Main.projectile[x];
                        if (projectile.active)
                        {
                            if (projectile.type == ModContent.ProjectileType<BrimstoneHellblast2>() || projectile.type == ModContent.ProjectileType<BrimstoneBarrage>())
                            {
                                if (projectile.timeLeft > 60)
                                    projectile.timeLeft = 60;
                            }
                            else if (projectile.type == ModContent.ProjectileType<SCalBrimstoneFireblast>())
                            {
                                projectile.ai[2] = 1f;

                                if (projectile.timeLeft > 60)
                                    projectile.timeLeft = 60;
                            }
                        }
                    }
                }

                return;
            }
            else if (CalamityWorld.LegendaryMode && CalamityWorld.revenge)
            {
                if (calamityGlobalNPC.newAI[3] < 900f)
                    calamityGlobalNPC.newAI[3] += 1f;
                else
                    calamityGlobalNPC.newAI[3] = 0f;

                npc.ai[0] += 1f;
                float hellblastGateValue = 30f - enrageScale;
                if (npc.ai[0] >= hellblastGateValue)
                {
                    npc.ai[0] = 0f;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int type = ModContent.ProjectileType<BrimstoneHellblast2>();
                        int damage = npc.GetProjectileDamage(type);
                        float projSpeed = bossRush ? 5f : 4f;
                        if (calamityGlobalNPC.newAI[3] % (hellblastGateValue * 6f) == 0f)
                        {
                            float distance = Main.rand.NextBool() ? -1000f : 1000f;
                            float velocity = distance == -1000f ? projSpeed : -projSpeed;
                            Projectile.NewProjectile(npc.GetSource_FromAI(), player.position.X + distance, player.position.Y, velocity, 0f, type, damage, 0f, Main.myPlayer, 2f, 0f);
                        }

                        if (calamityGlobalNPC.newAI[3] < 300f) // Blasts from above
                        {
                            Projectile.NewProjectile(npc.GetSource_FromAI(), player.position.X + Main.rand.Next(-1000, 1001), player.position.Y - 1000f, 0f, projSpeed, type, damage, 0f, Main.myPlayer, 2f, 0f);
                        }
                        else if (calamityGlobalNPC.newAI[3] < 600f) // Blasts from left and right
                        {
                            Projectile.NewProjectile(npc.GetSource_FromAI(), player.position.X + 1000f, player.position.Y + Main.rand.Next(-1000, 1001), -(projSpeed - 0.5f), 0f, type, damage, 0f, Main.myPlayer, 2f, 0f);
                            Projectile.NewProjectile(npc.GetSource_FromAI(), player.position.X - 1000f, player.position.Y + Main.rand.Next(-1000, 1001), projSpeed - 0.5f, 0f, type, damage, 0f, Main.myPlayer, 2f, 0f);
                        }
                        else // Blasts from above, left, and right
                        {
                            Projectile.NewProjectile(npc.GetSource_FromAI(), player.position.X + Main.rand.Next(-1000, 1001), player.position.Y - 1000f, 0f, projSpeed - 1f, type, damage, 0f, Main.myPlayer, 2f, 0f);
                            Projectile.NewProjectile(npc.GetSource_FromAI(), player.position.X + 1000f, player.position.Y + Main.rand.Next(-1000, 1001), -(projSpeed - 1f), 0f, type, damage, 0f, Main.myPlayer, 2f, 0f);
                            Projectile.NewProjectile(npc.GetSource_FromAI(), player.position.X - 1000f, player.position.Y + Main.rand.Next(-1000, 1001), projSpeed - 1f, 0f, type, damage, 0f, Main.myPlayer, 2f, 0f);
                        }
                    }
                }
            }

            npc.alpha = npc.dontTakeDamage ? 255 : 0;

            // Float above target and fire lasers or fireballs
            if (npc.ai[1] == 0f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                npc.ai[2] += 1f;
                float phaseTimer = 400f - (death ? 120f * (1f - lifeRatio) : 0f);
                if (npc.ai[2] >= phaseTimer || phase4)
                {
                    if (death && !phase4 && Main.rand.NextBool() && !brotherAlive)
                        npc.ai[1] = 4f;
                    else
                        npc.ai[1] = 1f;

                    npc.ai[2] = 0f;
                    if (death)
                        npc.localAI[0] = 1f;

                    npc.netUpdate = true;
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.localAI[1] += 1f;
                    if (!brotherAlive)
                    {
                        if (expertMode)
                            npc.localAI[1] += death ? 2f * (1f - lifeRatio) : 1f - lifeRatio;
                        if (revenge)
                            npc.localAI[1] += 0.5f;
                    }

                    if (npc.localAI[1] >= (brotherAlive ? 180f : 120f))
                    {
                        npc.localAI[1] = 0f;

                        float projectileVelocity = expertMode ? 14f : 12.5f;
                        projectileVelocity += 3f * enrageScale;
                        int type = ModContent.ProjectileType<BrimstoneHellfireball>();
                        int damage = npc.GetProjectileDamage(type);
                        bool shootPredictiveShot = CalamityWorld.LegendaryMode && CalamityWorld.revenge && Main.rand.NextBool();
                        Vector2 predictionVector = shootPredictiveShot ? player.velocity * 20f : Vector2.Zero;
                        Vector2 fireballVelocity = Vector2.Normalize(player.Center + predictionVector - npc.Center) * projectileVelocity;
                        Vector2 offset = Vector2.Normalize(fireballVelocity) * 40f;
                        int proj = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + offset, fireballVelocity, type, damage, 0f, Main.myPlayer, player.position.X, player.position.Y);
                        Main.projectile[proj].netUpdate = true;
                    }
                }
            }

            // Float to the side of the target and fire
            else if (npc.ai[1] == 1f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.localAI[1] += 1f;
                    if (!brotherAlive)
                    {
                        if (revenge)
                            npc.localAI[1] += 0.5f;
                        if (expertMode)
                            npc.localAI[1] += 0.5f;
                    }

                    if (npc.localAI[1] >= (brotherAlive ? 75f : 50f) && Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height))
                    {
                        npc.localAI[1] = 0f;

                        float projectileVelocity = expertMode ? 12.5f : 11f;
                        projectileVelocity += 3f * enrageScale;
                        int type = brotherAlive ? ModContent.ProjectileType<BrimstoneHellfireball>() : ModContent.ProjectileType<BrimstoneHellblast>();
                        int damage = npc.GetProjectileDamage(type);
                        Vector2 fireballVelocity = Vector2.Normalize(player.Center - npc.Center) * projectileVelocity;
                        Vector2 offset = Vector2.Normalize(fireballVelocity) * 40f;

                        if (!Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height))
                        {
                            type = ModContent.ProjectileType<BrimstoneHellfireball>();
                            damage = npc.GetProjectileDamage(type);
                            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + offset, fireballVelocity, type, damage, 0f, Main.myPlayer, player.position.X, player.position.Y);
                        }
                        else
                        {
                            float ai0 = type == ModContent.ProjectileType<BrimstoneHellblast>() ? 1f : player.position.X;
                            float ai1 = type == ModContent.ProjectileType<BrimstoneHellblast>() ? 0f : player.position.Y;
                            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + offset, fireballVelocity, type, damage, 0f, Main.myPlayer, ai0, ai1);
                        }
                    }
                }

                npc.ai[2] += 1f;
                float phaseTimer = 240f - (death ? 60f * (1f - lifeRatio) : 0f);
                if (npc.ai[2] >= phaseTimer || phase4)
                {
                    if (death && !phase4 && Main.rand.NextBool() && !brotherAlive)
                        npc.ai[1] = 0f;
                    else
                        npc.ai[1] = !brotherAlive && phase2 && revenge ? 4f : 0f;

                    npc.ai[2] = 0f;
                    if (death)
                        npc.localAI[0] = 1f;

                    npc.netUpdate = true;
                }
            }
            else if (npc.ai[1] == 2f)
            {
                // Set damage
                npc.damage = npc.defDamage;

                npc.rotation = rotation;

                float chargeVelocity = phase4 ? 30f : death ? 28f : 25f;
                chargeVelocity += 5f * enrageScale;

                Vector2 vector = Vector2.Normalize(player.Center + (phase4 && bossRush ? player.velocity * 20f : Vector2.Zero) - npc.Center);
                npc.velocity = vector * chargeVelocity;

                npc.ai[1] = 3f;
                npc.netUpdate = true;
            }
            else if (npc.ai[1] == 3f)
            {
                // Set damage
                npc.damage = npc.defDamage;

                npc.ai[2] += 1f;

                float chargeTime = phase4 ? 35f : death ? 40f : 45f;
                if (npc.ai[2] >= chargeTime)
                {
                    // Avoid cheap bullshit
                    npc.damage = 0;

                    npc.velocity *= 0.9f;
                    if (npc.velocity.X > -0.1 && npc.velocity.X < 0.1)
                        npc.velocity.X = 0f;
                    if (npc.velocity.Y > -0.1 && npc.velocity.Y < 0.1)
                        npc.velocity.Y = 0f;
                }
                else
                {
                    npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) - MathHelper.PiOver2;

                    // Leave behind slow hellblasts in Death Mode
                    if (Main.netMode != NetmodeID.MultiplayerClient && death && phase3 && npc.ai[2] % (phase4 ? 6f : 10f) == 0f)
                    {
                        int type = ModContent.ProjectileType<BrimstoneHellblast>();
                        int damage = npc.GetProjectileDamage(type);
                        Vector2 fireballVelocity = CalamityWorld.LegendaryMode ? Main.rand.NextVector2CircularEdge(0.02f, 0.02f) : npc.velocity * 0.01f;
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, fireballVelocity, type, damage, 0f, Main.myPlayer, 1f, 0f);
                    }
                }

                if (npc.ai[2] >= chargeTime + 10f)
                {
                    if (!phase4)
                        npc.ai[3] += 1f;

                    npc.ai[2] = 0f;

                    npc.rotation = rotation;
                    npc.netUpdate = true;

                    if (npc.ai[3] >= 2f)
                    {
                        npc.TargetClosest();
                        npc.ai[1] = 0f;
                        npc.ai[3] = 0f;
                        return;
                    }

                    npc.ai[1] = 4f;
                }
            }
            else
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                npc.ai[2] += 1f;
                if (npc.ai[2] >= (phase4 ? 15f : 30f))
                {
                    npc.ai[1] = 2f;
                    npc.ai[2] = 0f;
                    if (death)
                        npc.localAI[0] = 1f;

                    npc.netUpdate = true;
                }
            }
        }

        public static void VanillaCataclysmAI(NPC npc, Mod mod)
        {
            CalamityGlobalNPC calamityGlobalNPC = npc.Calamity();

            if (CalamityGlobalNPC.calamitas < 0 || !Main.npc[CalamityGlobalNPC.calamitas].active)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    npc.StrikeInstantKill();

                return;
            }

            // Percent life remaining
            float lifeRatio = npc.life / (float)npc.lifeMax;

            CalamityGlobalNPC.cataclysm = npc.whoAmI;

            // Emit light
            Lighting.AddLight((int)((npc.position.X + (npc.width / 2)) / 16f), (int)((npc.position.Y + (npc.height / 2)) / 16f), 1f, 0f, 0f);

            bool bossRush = BossRushEvent.BossRushActive;
            bool death = CalamityWorld.death || bossRush;
            bool revenge = CalamityWorld.revenge || bossRush;
            bool expertMode = Main.expertMode || bossRush;

            // Get a target
            if (npc.target < 0 || npc.target == Main.maxPlayers || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            Player player = Main.player[npc.target];

            float enrageScale = bossRush ? 1f : 0f;
            if (Main.IsItDay() || bossRush)
            {
                npc.Calamity().CurrentlyEnraged = !bossRush;
                enrageScale += 2f;
            }

            float calCloneBroPlayerXDist = npc.position.X + (npc.width / 2) - player.position.X - (player.width / 2);
            float calCloneBroPlayerYDist = npc.position.Y + npc.height - 59f - player.position.Y - (player.height / 2);
            float calCloneBroRotation = (float)Math.Atan2(calCloneBroPlayerYDist, calCloneBroPlayerXDist) + MathHelper.PiOver2;
            if (calCloneBroRotation < 0f)
                calCloneBroRotation += MathHelper.TwoPi;
            else if (calCloneBroRotation > MathHelper.TwoPi)
                calCloneBroRotation -= MathHelper.TwoPi;

            float calCloneBroRotationSpeed = 0.15f;
            if (npc.rotation < calCloneBroRotation)
            {
                if ((calCloneBroRotation - npc.rotation) > MathHelper.Pi)
                    npc.rotation -= calCloneBroRotationSpeed;
                else
                    npc.rotation += calCloneBroRotationSpeed;
            }
            else if (npc.rotation > calCloneBroRotation)
            {
                if ((npc.rotation - calCloneBroRotation) > MathHelper.Pi)
                    npc.rotation += calCloneBroRotationSpeed;
                else
                    npc.rotation -= calCloneBroRotationSpeed;
            }

            if (npc.rotation > calCloneBroRotation - calCloneBroRotationSpeed && npc.rotation < calCloneBroRotation + calCloneBroRotationSpeed)
                npc.rotation = calCloneBroRotation;
            if (npc.rotation < 0f)
                npc.rotation += MathHelper.TwoPi;
            else if (npc.rotation > MathHelper.TwoPi)
                npc.rotation -= MathHelper.TwoPi;
            if (npc.rotation > calCloneBroRotation - calCloneBroRotationSpeed && npc.rotation < calCloneBroRotation + calCloneBroRotationSpeed)
                npc.rotation = calCloneBroRotation;

            if (!player.active || player.dead)
            {
                npc.TargetClosest(false);
                player = Main.player[npc.target];
                if (!player.active || player.dead)
                {
                    if (npc.velocity.Y > 3f)
                        npc.velocity.Y = 3f;
                    npc.velocity.Y -= 0.1f;
                    if (npc.velocity.Y < -12f)
                        npc.velocity.Y = -12f;

                    if (npc.timeLeft > 60)
                        npc.timeLeft = 60;

                    if (npc.ai[1] != 0f)
                    {
                        npc.ai[1] = 0f;
                        npc.ai[2] = 0f;
                        npc.ai[3] = 0f;
                        npc.netUpdate = true;
                    }

                    return;
                }
            }

            if (npc.ai[1] == 0f)
            {
                // Avoid cheap bullshit
                npc.damage = (int)Math.Round(npc.defDamage * 0.5);

                float calCloneBroProjAttackMaxSpeed = 5f;
                float calCloneBroProjAttackAccel = 0.1f;
                calCloneBroProjAttackMaxSpeed += 2f * enrageScale;
                calCloneBroProjAttackAccel += 0.06f * enrageScale;

                if (Main.getGoodWorld)
                {
                    calCloneBroProjAttackMaxSpeed *= 1.15f;
                    calCloneBroProjAttackAccel *= 1.15f;
                }

                int calCloneBroProjAttackDirection = 1;
                if (npc.position.X + (npc.width / 2) < player.position.X + player.width)
                    calCloneBroProjAttackDirection = -1;

                Vector2 calCloneBroProjLocation = new Vector2(npc.position.X + npc.width * 0.5f, npc.position.Y + npc.height * 0.5f);
                float calCloneBroProjTargetX = player.position.X + (player.width / 2) + (calCloneBroProjAttackDirection * 180) - calCloneBroProjLocation.X;
                float calCloneBroProjTargetY = player.position.Y + (player.height / 2) - calCloneBroProjLocation.Y;
                float calCloneBroProjTargetDist = (float)Math.Sqrt(calCloneBroProjTargetX * calCloneBroProjTargetX + calCloneBroProjTargetY * calCloneBroProjTargetY);

                if (expertMode)
                {
                    if (calCloneBroProjTargetDist > 300f)
                        calCloneBroProjAttackMaxSpeed += 0.5f;
                    if (calCloneBroProjTargetDist > 400f)
                        calCloneBroProjAttackMaxSpeed += 0.5f;
                    if (calCloneBroProjTargetDist > 500f)
                        calCloneBroProjAttackMaxSpeed += 0.55f;
                    if (calCloneBroProjTargetDist > 600f)
                        calCloneBroProjAttackMaxSpeed += 0.55f;
                    if (calCloneBroProjTargetDist > 700f)
                        calCloneBroProjAttackMaxSpeed += 0.6f;
                    if (calCloneBroProjTargetDist > 800f)
                        calCloneBroProjAttackMaxSpeed += 0.6f;
                }

                calCloneBroProjTargetDist = calCloneBroProjAttackMaxSpeed / calCloneBroProjTargetDist;
                calCloneBroProjTargetX *= calCloneBroProjTargetDist;
                calCloneBroProjTargetY *= calCloneBroProjTargetDist;

                if (npc.velocity.X < calCloneBroProjTargetX)
                {
                    npc.velocity.X += calCloneBroProjAttackAccel;
                    if (npc.velocity.X < 0f && calCloneBroProjTargetX > 0f)
                        npc.velocity.X += calCloneBroProjAttackAccel;
                }
                else if (npc.velocity.X > calCloneBroProjTargetX)
                {
                    npc.velocity.X -= calCloneBroProjAttackAccel;
                    if (npc.velocity.X > 0f && calCloneBroProjTargetX < 0f)
                        npc.velocity.X -= calCloneBroProjAttackAccel;
                }
                if (npc.velocity.Y < calCloneBroProjTargetY)
                {
                    npc.velocity.Y += calCloneBroProjAttackAccel;
                    if (npc.velocity.Y < 0f && calCloneBroProjTargetY > 0f)
                        npc.velocity.Y += calCloneBroProjAttackAccel;
                }
                else if (npc.velocity.Y > calCloneBroProjTargetY)
                {
                    npc.velocity.Y -= calCloneBroProjAttackAccel;
                    if (npc.velocity.Y > 0f && calCloneBroProjTargetY < 0f)
                        npc.velocity.Y -= calCloneBroProjAttackAccel;
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= (240f - (death ? 120f * (1f - lifeRatio) : 0f)))
                {
                    npc.ai[1] = 1f;
                    npc.ai[2] = 0f;
                    npc.target = 255;
                    npc.netUpdate = true;
                }

                bool fireDelay = npc.ai[2] > 120f || npc.life < npc.lifeMax * 0.9;
                if (Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height) && fireDelay)
                {
                    npc.localAI[2] += 1f;
                    if (npc.localAI[2] > 22f)
                    {
                        npc.localAI[2] = 0f;
                        SoundEngine.PlaySound(SoundID.Item34, npc.Center);
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        npc.localAI[1] += 3f;
                        if (revenge)
                            npc.localAI[1] += 1f;

                        if (npc.localAI[1] > 12f)
                        {
                            npc.localAI[1] = 0f;
                            float calCloneBroProjSpeed = NPC.AnyNPCs(ModContent.NPCType<Catastrophe>()) ? 4f : 6f;
                            calCloneBroProjSpeed += enrageScale;
                            int type = ModContent.ProjectileType<BrimstoneFire>();
                            int damage = npc.GetProjectileDamage(type);
                            calCloneBroProjLocation = new Vector2(npc.position.X + npc.width * 0.5f, npc.position.Y + npc.height * 0.5f);
                            calCloneBroProjTargetX = player.position.X + (player.width / 2) - calCloneBroProjLocation.X;
                            calCloneBroProjTargetY = player.position.Y + (player.height / 2) - calCloneBroProjLocation.Y;
                            calCloneBroProjTargetDist = (float)Math.Sqrt(calCloneBroProjTargetX * calCloneBroProjTargetX + calCloneBroProjTargetY * calCloneBroProjTargetY);
                            calCloneBroProjTargetDist = calCloneBroProjSpeed / calCloneBroProjTargetDist;
                            calCloneBroProjTargetX *= calCloneBroProjTargetDist;
                            calCloneBroProjTargetY *= calCloneBroProjTargetDist;
                            calCloneBroProjTargetY += npc.velocity.Y * 0.5f;
                            calCloneBroProjTargetX += npc.velocity.X * 0.5f;
                            calCloneBroProjLocation.X -= calCloneBroProjTargetX;
                            calCloneBroProjLocation.Y -= calCloneBroProjTargetY;
                            Projectile.NewProjectile(npc.GetSource_FromAI(), calCloneBroProjLocation.X, calCloneBroProjLocation.Y, calCloneBroProjTargetX, calCloneBroProjTargetY, type, damage, 0f, Main.myPlayer, 0f, 0f);
                        }
                    }
                }
            }
            else
            {
                if (npc.ai[1] == 1f)
                {
                    // Set damage
                    npc.damage = npc.defDamage;

                    SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                    npc.rotation = calCloneBroRotation;

                    float calCloneBroChargeSpeed = 14f + (death ? 4f * (1f - lifeRatio) : 0f);
                    calCloneBroChargeSpeed += 3f * enrageScale;
                    if (expertMode)
                        calCloneBroChargeSpeed += 2f;
                    if (revenge)
                        calCloneBroChargeSpeed += 2f;
                    if (Main.getGoodWorld)
                        calCloneBroChargeSpeed *= 1.25f;

                    Vector2 calCloneBroChargeCenter = npc.Center;
                    float calCloneBroChargeTargetXDist = player.Center.X - calCloneBroChargeCenter.X;
                    float calCloneBroChargeTargetYDist = player.Center.Y - calCloneBroChargeCenter.Y;
                    float calCloneBroChargeTargetDistance = (float)Math.Sqrt(calCloneBroChargeTargetXDist * calCloneBroChargeTargetXDist + calCloneBroChargeTargetYDist * calCloneBroChargeTargetYDist);
                    calCloneBroChargeTargetDistance = calCloneBroChargeSpeed / calCloneBroChargeTargetDistance;
                    npc.velocity.X = calCloneBroChargeTargetXDist * calCloneBroChargeTargetDistance;
                    npc.velocity.Y = calCloneBroChargeTargetYDist * calCloneBroChargeTargetDistance;
                    npc.ai[1] = 2f;

                    if (Main.zenithWorld)
                    {
                        SoundEngine.PlaySound(SupremeCalamitas.SupremeCalamitas.BrimstoneShotSound, npc.Center);

                        int type = ModContent.ProjectileType<BrimstoneBarrage>();
                        int damage = npc.GetProjectileDamage(ModContent.ProjectileType<BrimstoneFire>());
                        if (bossRush)
                            damage /= 2;

                        int totalProjectiles = bossRush ? 12 : death ? 10 : revenge ? 8 : expertMode ? 6 : 4;
                        float radians = MathHelper.TwoPi / totalProjectiles;
                        float velocity = 5f;
                        Vector2 spinningPoint = new Vector2(0f, -velocity);
                        float projectileVelocityToPass = velocity * 3f;
                        for (int k = 0; k < totalProjectiles; k++)
                        {
                            Vector2 velocity2 = spinningPoint.RotatedBy(radians * k);
                            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, velocity2, type, damage, 0f, Main.myPlayer, 0f, 0f, projectileVelocityToPass);
                        }

                        for (int i = 0; i < 6; i++)
                            Dust.NewDust(npc.position + npc.velocity, npc.width, npc.height, (int)CalamityDusts.Brimstone, 0f, 0f);
                    }
                    return;
                }

                if (npc.ai[1] == 2f)
                {
                    // Set damage
                    npc.damage = npc.defDamage;

                    npc.ai[2] += 1f + (death ? 0.5f * (1f - lifeRatio) : 0f);
                    if (expertMode)
                        npc.ai[2] += 0.25f;
                    if (revenge)
                        npc.ai[2] += 0.25f;

                    if (npc.ai[2] >= 75f)
                    {
                        // Avoid cheap bullshit
                        npc.damage = (int)Math.Round(npc.defDamage * 0.5);

                        npc.velocity.X *= 0.93f;
                        npc.velocity.Y *= 0.93f;

                        if (npc.velocity.X > -0.1 && npc.velocity.X < 0.1)
                            npc.velocity.X = 0f;
                        if (npc.velocity.Y > -0.1 && npc.velocity.Y < 0.1)
                            npc.velocity.Y = 0f;
                    }
                    else
                        npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) - MathHelper.PiOver2;

                    if (npc.ai[2] >= 105f)
                    {
                        npc.ai[3] += 1f;
                        npc.ai[2] = 0f;
                        npc.target = 255;
                        npc.rotation = calCloneBroRotation;
                        if (npc.ai[3] >= 3f)
                        {
                            npc.ai[1] = 0f;
                            npc.ai[3] = 0f;
                            return;
                        }
                        npc.ai[1] = 1f;
                    }
                }
            }
        }

        public static void VanillaCatastropheAI(NPC npc, Mod mod)
        {
            CalamityGlobalNPC calamityGlobalNPC = npc.Calamity();

            if (CalamityGlobalNPC.calamitas < 0 || !Main.npc[CalamityGlobalNPC.calamitas].active)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    npc.StrikeInstantKill();

                return;
            }

            // Percent life remaining
            float lifeRatio = npc.life / (float)npc.lifeMax;

            CalamityGlobalNPC.catastrophe = npc.whoAmI;

            // Emit light
            Lighting.AddLight((int)((npc.position.X + (npc.width / 2)) / 16f), (int)((npc.position.Y + (npc.height / 2)) / 16f), 1f, 0f, 0f);

            bool bossRush = BossRushEvent.BossRushActive;
            bool death = CalamityWorld.death || bossRush;
            bool revenge = CalamityWorld.revenge || bossRush;
            bool expertMode = Main.expertMode || bossRush;

            // Get a target
            if (npc.target < 0 || npc.target == Main.maxPlayers || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            Player player = Main.player[npc.target];

            float enrageScale = bossRush ? 1f : 0f;
            if (Main.IsItDay() || bossRush)
            {
                npc.Calamity().CurrentlyEnraged = !bossRush;
                enrageScale += 2f;
            }

            float calCloneBroPlayerXDist = npc.position.X + (npc.width / 2) - player.position.X - (player.width / 2);
            float calCloneBroPlayerYDist = npc.position.Y + npc.height - 59f - player.position.Y - (player.height / 2);
            float calCloneBroRotation = (float)Math.Atan2(calCloneBroPlayerYDist, calCloneBroPlayerXDist) + MathHelper.PiOver2;
            if (calCloneBroRotation < 0f)
                calCloneBroRotation += MathHelper.TwoPi;
            else if (calCloneBroRotation > MathHelper.TwoPi)
                calCloneBroRotation -= MathHelper.TwoPi;

            float calCloneBroRotationSpeed = 0.15f;
            if (npc.rotation < calCloneBroRotation)
            {
                if ((calCloneBroRotation - npc.rotation) > MathHelper.Pi)
                    npc.rotation -= calCloneBroRotationSpeed;
                else
                    npc.rotation += calCloneBroRotationSpeed;
            }
            else if (npc.rotation > calCloneBroRotation)
            {
                if ((npc.rotation - calCloneBroRotation) > MathHelper.Pi)
                    npc.rotation += calCloneBroRotationSpeed;
                else
                    npc.rotation -= calCloneBroRotationSpeed;
            }

            if (npc.rotation > calCloneBroRotation - calCloneBroRotationSpeed && npc.rotation < calCloneBroRotation + calCloneBroRotationSpeed)
                npc.rotation = calCloneBroRotation;
            if (npc.rotation < 0f)
                npc.rotation += MathHelper.TwoPi;
            else if (npc.rotation > MathHelper.TwoPi)
                npc.rotation -= MathHelper.TwoPi;
            if (npc.rotation > calCloneBroRotation - calCloneBroRotationSpeed && npc.rotation < calCloneBroRotation + calCloneBroRotationSpeed)
                npc.rotation = calCloneBroRotation;

            if (!player.active || player.dead)
            {
                npc.TargetClosest(false);
                player = Main.player[npc.target];
                if (!player.active || player.dead)
                {
                    if (npc.velocity.Y > 3f)
                        npc.velocity.Y = 3f;
                    npc.velocity.Y -= 0.1f;
                    if (npc.velocity.Y < -12f)
                        npc.velocity.Y = -12f;

                    if (npc.timeLeft > 60)
                        npc.timeLeft = 60;

                    if (npc.ai[1] != 0f)
                    {
                        npc.ai[1] = 0f;
                        npc.ai[2] = 0f;
                        npc.ai[3] = 0f;
                        npc.netUpdate = true;
                    }

                    return;
                }
            }

            if (npc.ai[1] == 0f)
            {
                // Avoid cheap bullshit
                npc.damage = (int)Math.Round(npc.defDamage * 0.5);

                float calCloneBroProjAttackMaxSpeed = 4.5f;
                float calCloneBroProjAttackAccel = 0.2f;
                calCloneBroProjAttackMaxSpeed += 2f * enrageScale;
                calCloneBroProjAttackAccel += 0.1f * enrageScale;

                if (Main.getGoodWorld)
                {
                    calCloneBroProjAttackMaxSpeed *= 1.15f;
                    calCloneBroProjAttackAccel *= 1.15f;
                }

                int calCloneBroProjAttackDirection = 1;
                if (npc.Center.X < player.Center.X)
                    calCloneBroProjAttackDirection = -1;

                Vector2 calCloneBroProjLocation = npc.Center;
                float calCloneBroProjTargetX = player.Center.X + (calCloneBroProjAttackDirection * 180) - calCloneBroProjLocation.X;
                float calCloneBroProjTargetY = player.Center.Y - calCloneBroProjLocation.Y;
                float calCloneBroProjTargetDist = (float)Math.Sqrt(calCloneBroProjTargetX * calCloneBroProjTargetX + calCloneBroProjTargetY * calCloneBroProjTargetY);

                if (expertMode)
                {
                    if (calCloneBroProjTargetDist > 300f)
                        calCloneBroProjAttackMaxSpeed += 0.5f;
                    if (calCloneBroProjTargetDist > 400f)
                        calCloneBroProjAttackMaxSpeed += 0.5f;
                    if (calCloneBroProjTargetDist > 500f)
                        calCloneBroProjAttackMaxSpeed += 0.55f;
                    if (calCloneBroProjTargetDist > 600f)
                        calCloneBroProjAttackMaxSpeed += 0.55f;
                    if (calCloneBroProjTargetDist > 700f)
                        calCloneBroProjAttackMaxSpeed += 0.6f;
                    if (calCloneBroProjTargetDist > 800f)
                        calCloneBroProjAttackMaxSpeed += 0.6f;
                }

                calCloneBroProjTargetDist = calCloneBroProjAttackMaxSpeed / calCloneBroProjTargetDist;
                calCloneBroProjTargetX *= calCloneBroProjTargetDist;
                calCloneBroProjTargetY *= calCloneBroProjTargetDist;

                if (npc.velocity.X < calCloneBroProjTargetX)
                {
                    npc.velocity.X += calCloneBroProjAttackAccel;
                    if (npc.velocity.X < 0f && calCloneBroProjTargetX > 0f)
                        npc.velocity.X += calCloneBroProjAttackAccel;
                }
                else if (npc.velocity.X > calCloneBroProjTargetX)
                {
                    npc.velocity.X -= calCloneBroProjAttackAccel;
                    if (npc.velocity.X > 0f && calCloneBroProjTargetX < 0f)
                        npc.velocity.X -= calCloneBroProjAttackAccel;
                }
                if (npc.velocity.Y < calCloneBroProjTargetY)
                {
                    npc.velocity.Y += calCloneBroProjAttackAccel;
                    if (npc.velocity.Y < 0f && calCloneBroProjTargetY > 0f)
                        npc.velocity.Y += calCloneBroProjAttackAccel;
                }
                else if (npc.velocity.Y > calCloneBroProjTargetY)
                {
                    npc.velocity.Y -= calCloneBroProjAttackAccel;
                    if (npc.velocity.Y > 0f && calCloneBroProjTargetY < 0f)
                        npc.velocity.Y -= calCloneBroProjAttackAccel;
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= (180f - (death ? 90f * (1f - lifeRatio) : 0f)))
                {
                    npc.ai[1] = 1f;
                    npc.ai[2] = 0f;
                    npc.target = 255;
                    npc.netUpdate = true;
                }

                bool fireDelay = npc.ai[2] > 120f || lifeRatio < 0.9f;
                if (Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height) && fireDelay)
                {
                    npc.localAI[2] += 1f;
                    if (npc.localAI[2] > 36f)
                    {
                        npc.localAI[2] = 0f;
                        SoundEngine.PlaySound(SoundID.Item34, npc.Center);
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        npc.localAI[1] += 1f;
                        if (revenge)
                            npc.localAI[1] += 0.5f;

                        if (npc.localAI[1] > 50f)
                        {
                            npc.localAI[1] = 0f;
                            float calCloneBroProjSpeed = death ? 14f : 12f;
                            calCloneBroProjSpeed += 3f * enrageScale;
                            int type = ModContent.ProjectileType<BrimstoneBall>();
                            int damage = npc.GetProjectileDamage(type);
                            calCloneBroProjLocation = new Vector2(npc.position.X + npc.width * 0.5f, npc.position.Y + npc.height * 0.5f);
                            calCloneBroProjTargetX = player.position.X + (player.width / 2) - calCloneBroProjLocation.X;
                            calCloneBroProjTargetY = player.position.Y + (player.height / 2) - calCloneBroProjLocation.Y;
                            calCloneBroProjTargetDist = (float)Math.Sqrt(calCloneBroProjTargetX * calCloneBroProjTargetX + calCloneBroProjTargetY * calCloneBroProjTargetY);
                            calCloneBroProjTargetDist = calCloneBroProjSpeed / calCloneBroProjTargetDist;
                            calCloneBroProjTargetX *= calCloneBroProjTargetDist;
                            calCloneBroProjTargetY *= calCloneBroProjTargetDist;
                            calCloneBroProjTargetY += npc.velocity.Y * 0.5f;
                            calCloneBroProjTargetX += npc.velocity.X * 0.5f;
                            calCloneBroProjLocation.X -= calCloneBroProjTargetX;
                            calCloneBroProjLocation.Y -= calCloneBroProjTargetY;
                            Projectile.NewProjectile(npc.GetSource_FromAI(), calCloneBroProjLocation.X, calCloneBroProjLocation.Y, calCloneBroProjTargetX, calCloneBroProjTargetY, type, damage, 0f, Main.myPlayer, 0f, 0f);
                        }
                    }
                }
            }
            else
            {
                if (npc.ai[1] == 1f)
                {
                    // Set damage
                    npc.damage = npc.defDamage;

                    SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                    npc.rotation = calCloneBroRotation;

                    float calCloneBroChargeSpeed = (NPC.AnyNPCs(ModContent.NPCType<Cataclysm>()) ? 12f : 16f) + (death ? 4f * (1f - lifeRatio) : 0f);
                    calCloneBroChargeSpeed += 4f * enrageScale;
                    if (expertMode)
                        calCloneBroChargeSpeed += 2f;
                    if (revenge)
                        calCloneBroChargeSpeed += 2f;
                    if (Main.getGoodWorld)
                        calCloneBroChargeSpeed *= 1.25f;

                    Vector2 calCloneBroChargeCenter = new Vector2(npc.position.X + npc.width * 0.5f, npc.position.Y + npc.height * 0.5f);
                    float calCloneBroChargeTargetXDist = player.position.X + (player.width / 2) - calCloneBroChargeCenter.X;
                    float calCloneBroChargeTargetYDist = player.position.Y + (player.height / 2) - calCloneBroChargeCenter.Y;
                    float calCloneBroChargeTargetDistance = (float)Math.Sqrt(calCloneBroChargeTargetXDist * calCloneBroChargeTargetXDist + calCloneBroChargeTargetYDist * calCloneBroChargeTargetYDist);
                    calCloneBroChargeTargetDistance = calCloneBroChargeSpeed / calCloneBroChargeTargetDistance;
                    npc.velocity.X = calCloneBroChargeTargetXDist * calCloneBroChargeTargetDistance;
                    npc.velocity.Y = calCloneBroChargeTargetYDist * calCloneBroChargeTargetDistance;
                    npc.ai[1] = 2f;

                    if (Main.zenithWorld)
                    {
                        SoundEngine.PlaySound(SupremeCalamitas.SupremeCalamitas.BrimstoneShotSound, npc.Center);

                        int type = ModContent.ProjectileType<BrimstoneBarrage>();
                        int damage = npc.GetProjectileDamage(ModContent.ProjectileType<BrimstoneBall>());
                        if (bossRush)
                            damage /= 2;

                        int totalProjectiles = bossRush ? 12 : death ? 10 : revenge ? 8 : expertMode ? 6 : 4;
                        float radians = MathHelper.TwoPi / totalProjectiles;
                        float velocity = 5f;
                        Vector2 spinningPoint = new Vector2(0f, -velocity);
                        float projectileVelocityToPass = velocity * 3f;
                        for (int k = 0; k < totalProjectiles; k++)
                        {
                            Vector2 velocity2 = spinningPoint.RotatedBy(radians * k);
                            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, velocity2, type, damage, 0f, Main.myPlayer, 0f, 0f, projectileVelocityToPass);
                        }

                        for (int i = 0; i < 6; i++)
                            Dust.NewDust(npc.position + npc.velocity, npc.width, npc.height, (int)CalamityDusts.Brimstone, 0f, 0f);
                    }
                    return;
                }

                if (npc.ai[1] == 2f)
                {
                    // Set damage
                    npc.damage = npc.defDamage;

                    npc.ai[2] += 1f + (death ? 0.5f * (1f - lifeRatio) : 0f);
                    if (expertMode)
                        npc.ai[2] += 0.25f;
                    if (revenge)
                        npc.ai[2] += 0.25f;

                    if (npc.ai[2] >= 60f) //50
                    {
                        // Avoid cheap bullshit
                        npc.damage = (int)Math.Round(npc.defDamage * 0.5);

                        npc.velocity.X *= 0.93f;
                        npc.velocity.Y *= 0.93f;

                        if (npc.velocity.X > -0.1 && npc.velocity.X < 0.1)
                            npc.velocity.X = 0f;
                        if (npc.velocity.Y > -0.1 && npc.velocity.Y < 0.1)
                            npc.velocity.Y = 0f;
                    }
                    else
                        npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) - MathHelper.PiOver2;

                    if (npc.ai[2] >= 90f) //80
                    {
                        npc.ai[3] += 1f;
                        npc.ai[2] = 0f;
                        npc.rotation = calCloneBroRotation;
                        if (npc.ai[3] >= 4f)
                        {
                            npc.ai[1] = 0f;
                            npc.ai[3] = 0f;
                            return;
                        }
                        npc.ai[1] = 1f;
                    }
                }
            }
        }
    }
}
