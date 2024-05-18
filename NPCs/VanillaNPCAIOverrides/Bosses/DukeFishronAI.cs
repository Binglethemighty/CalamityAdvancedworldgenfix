using System;
using CalamityMod.Events;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.NPCs.VanillaNPCAIOverrides.Bosses
{
    public static class DukeFishronAI
    {
        public static bool BuffedDukeFishronAI(NPC npc, Mod mod)
        {
            CalamityGlobalNPC calamityGlobalNPC = npc.Calamity();

            // Percent life remaining
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Variables
            bool bossRush = BossRushEvent.BossRushActive;
            bool masterMode = Main.masterMode || bossRush;
            bool death = CalamityWorld.death || bossRush;
            bool masterModeSurprise = lifeRatio >= 0.9f && masterMode;
            bool phase2 = lifeRatio < (masterMode ? 0.6f : 0.7f);
            bool phase3 = lifeRatio < (masterMode ? 0.3f : 0.4f);
            bool phase4 = lifeRatio < (masterMode ? 0.1f : 0.2f);
            bool phase2AI = npc.ai[0] > 4f;
            bool phase3AI = npc.ai[0] > 9f;
            bool charging = npc.ai[3] < 10f;

            // Adjust stats
            int setDamage = npc.defDamage;
            if (phase3AI)
            {
                setDamage = (int)Math.Round(setDamage * 1.32);
                npc.defense = 0;
            }
            else if (phase2AI)
            {
                setDamage = (int)Math.Round(setDamage * 1.44);
                npc.defense = (int)Math.Round(npc.defDefense * 0.8);
            }
            else
                npc.defense = npc.defDefense;

            int idlePhaseTimer = 30;
            float idlePhaseAcceleration = 0.55f;
            float idlePhaseVelocity = 8.5f;
            if (phase3AI)
            {
                idlePhaseAcceleration = 0.7f;
                idlePhaseVelocity = 12f;
            }
            else if (phase2AI & charging)
            {
                idlePhaseAcceleration = 0.6f;
                idlePhaseVelocity = 10f;
            }

            if (Main.getGoodWorld)
            {
                idlePhaseAcceleration *= 1.15f;
                idlePhaseVelocity *= 1.15f;
            }

            int chargeTime = 28;
            float chargeVelocity = 17f;
            if (phase3AI)
            {
                chargeTime = 25;
                chargeVelocity = 27f;
            }
            else if (charging & phase2AI)
            {
                chargeTime = 27;
                chargeVelocity = 21f;
            }

            if (death)
            {
                idlePhaseTimer = 28;
                idlePhaseAcceleration *= 1.05f;
                idlePhaseVelocity *= 1.08f;
                chargeTime -= 1;
                chargeVelocity *= 1.13f;
            }

            if (Main.getGoodWorld)
                chargeVelocity *= 1.15f;

            int bubbleBelchPhaseTimer = death ? 60 : 80;
            int bubbleBelchPhaseDivisor = death ? 3 : 4;
            float bubbleBelchPhaseAcceleration = death ? 0.35f : 0.3f;
            float bubbleBelchPhaseVelocity = death ? 5.5f : 5f;

            if (Main.getGoodWorld)
            {
                bubbleBelchPhaseAcceleration *= 1.5f;
                bubbleBelchPhaseVelocity *= 1.5f;
            }

            int sharknadoPhaseTimer = 90;

            int phaseTransitionTimer = 180;

            int teleportPhaseTimer = 30;

            int bubbleSpinPhaseTimer = bossRush ? 45 : death ? 90 : 120;
            int bubbleSpinPhaseDivisor = death ? 3 : 4;
            float bubbleSpinBubbleVelocity = death ? 8f : 7f;
            float bubbleSpinPhaseVelocity = 20f;
            float bubbleSpinPhaseRotation = MathHelper.TwoPi / (bubbleSpinPhaseTimer / 2);

            if (Main.getGoodWorld)
                bubbleSpinBubbleVelocity *= 1.5f;

            int spawnEffectPhaseTimer = 75;

            Player player = Main.player[npc.target];

            // Get target
            if (npc.target < 0 || npc.target == Main.maxPlayers || player.dead || !player.active)
            {
                npc.TargetClosest();
                player = Main.player[npc.target];
                npc.netUpdate = true;
            }

            // Despawn safety, make sure to target another player if the current player target is too far away
            if (Vector2.Distance(player.Center, npc.Center) > CalamityGlobalNPC.CatchUpDistance200Tiles)
                npc.TargetClosest();

            // Despawn
            if (player.dead || Vector2.Distance(player.Center, npc.Center) > CalamityGlobalNPC.CatchUpDistance350Tiles)
            {
                npc.TargetClosest();

                npc.velocity.Y -= 0.4f;

                if (npc.timeLeft > 10)
                    npc.timeLeft = 10;

                if (npc.ai[0] > 4f)
                    npc.ai[0] = 5f;
                else
                    npc.ai[0] = 0f;

                npc.ai[2] = 0f;
            }

            // Enrage variable
            bool enrage = !bossRush &&
                (player.position.Y < 800f || player.position.Y > Main.worldSurface * 16.0 ||
                (player.position.X > 6400f && player.position.X < (Main.maxTilesX * 16 - 6400)));

            calamityGlobalNPC.CurrentlyEnraged = !bossRush && enrage;

            // Make him always able to take damage
            npc.dontTakeDamage = false;

            // Increased DR during phase transitions
            calamityGlobalNPC.DR = (npc.ai[0] == -1f || npc.ai[0] == 4f || npc.ai[0] == 9f) ? (bossRush ? 0.99f : 0.625f) : 0.15f;
            calamityGlobalNPC.CurrentlyIncreasingDefenseOrDR = npc.ai[0] == -1f || npc.ai[0] == 4f || npc.ai[0] == 9f;

            // Enrage
            if (enrage || bossRush)
            {
                bubbleBelchPhaseTimer = 20;
                bubbleBelchPhaseDivisor = 1;
                bubbleBelchPhaseAcceleration = 0.65f;
                bubbleBelchPhaseVelocity = 10f;
                idlePhaseTimer = 20;
                idlePhaseAcceleration = 1f;
                idlePhaseVelocity = 15f;
                chargeTime = 24;
                chargeVelocity += 5f;
                bubbleSpinPhaseDivisor = 1;
                bubbleSpinBubbleVelocity = 15f;

                if (!bossRush)
                {
                    setDamage *= 2;
                    npc.defense = npc.defDefense * 3;
                }
            }

            if (masterMode)
            {
                idlePhaseTimer -= 6;
                idlePhaseAcceleration *= 1.2f;
                idlePhaseVelocity *= 1.2f;
                chargeTime -= 4;
                chargeVelocity += 3f;
            }

            if (CalamityWorld.LegendaryMode)
                chargeTime += Main.rand.Next(5, 66);

            // Spawn cthulhunadoes in phase 3
            if (phase3AI && ((!phase4 && !masterModeSurprise) || Main.getGoodWorld))
            {
                calamityGlobalNPC.newAI[0] += 1f;
                float timeGateValue = 600f;
                if (calamityGlobalNPC.newAI[0] >= timeGateValue)
                {
                    calamityGlobalNPC.newAI[0] = 0f;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ProjectileID.SharknadoBolt, 0, 0f, Main.myPlayer, 1f, npc.target + 1, (enrage || masterMode) ? 1 : 0);

                    npc.netUpdate = true;
                }
            }

            // Set variables for spawn effects
            if (npc.localAI[0] == 0f)
            {
                npc.localAI[0] = 1f;
                npc.alpha = 255;
                npc.rotation = 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.ai[0] = masterModeSurprise ? 9f : -1f;
                    npc.netUpdate = true;
                }
            }

            // Rotation
            float rateOfRotation = 0.04f;
            if (npc.ai[0] == 1f || npc.ai[0] == 6f || npc.ai[0] == 7f)
                rateOfRotation = 0f;
            if (npc.ai[0] == 3f || npc.ai[0] == 4f || npc.ai[0] == 8f)
                rateOfRotation = 0.01f;

            Vector2 rotationVector = player.Center - npc.Center;
            if (!player.dead && bossRush && phase4)
            {
                // Rotate to show direction of predictive charge
                if (npc.ai[0] == 10f)
                {
                    rateOfRotation = 0.1f;
                    rotationVector = Vector2.Normalize(player.Center + player.velocity * 20f - npc.Center) * chargeVelocity;
                }
            }

            float rotationSpeed = (float)Math.Atan2(rotationVector.Y, rotationVector.X);
            if (npc.spriteDirection == 1)
                rotationSpeed += MathHelper.Pi;
            if (rotationSpeed < 0f)
                rotationSpeed += MathHelper.TwoPi;
            if (rotationSpeed > MathHelper.TwoPi)
                rotationSpeed -= MathHelper.TwoPi;
            if (npc.ai[0] == -1f || npc.ai[0] == 3f || npc.ai[0] == 4f || npc.ai[0] == 8f)
                rotationSpeed = 0f;

            if (rateOfRotation != 0f)
                npc.rotation = npc.rotation.AngleTowards(rotationSpeed, rateOfRotation);

            // Alpha adjustments
            if (npc.ai[0] != -1f && npc.ai[0] < 9f)
            {
                if (Collision.SolidCollision(npc.position, npc.width, npc.height))
                    npc.alpha += 15;
                else
                    npc.alpha -= 15;

                if (npc.alpha < 0)
                    npc.alpha = 0;
                if (npc.alpha > 150)
                    npc.alpha = 150;
            }

            // Spawn effects
            if (npc.ai[0] == -1f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                // Velocity
                npc.velocity *= 0.98f;

                // Direction
                int faceDirection = Math.Sign(player.Center.X - npc.Center.X);
                if (faceDirection != 0)
                {
                    npc.direction = faceDirection;
                    npc.spriteDirection = -npc.direction;
                }

                // Alpha
                if (npc.ai[2] > 20f)
                {
                    npc.velocity.Y = -2f;

                    npc.alpha -= 5;
                    if (Collision.SolidCollision(npc.position, npc.width, npc.height))
                        npc.alpha += 15;
                    if (npc.alpha < 0)
                        npc.alpha = 0;
                    if (npc.alpha > 150)
                        npc.alpha = 150;
                }

                // Spawn dust and play sound
                if (npc.ai[2] == sharknadoPhaseTimer - 30)
                {
                    int dustAmt = 36;
                    for (int i = 0; i < dustAmt; i++)
                    {
                        Vector2 dust = (Vector2.Normalize(npc.velocity) * new Vector2(npc.width / 2f, npc.height) * 0.75f * 0.5f).RotatedBy((i - (dustAmt / 2 - 1)) * MathHelper.TwoPi / dustAmt) + npc.Center;
                        Vector2 sharknadoDustDirection = dust - npc.Center;
                        int sharknadoDust = Dust.NewDust(dust + sharknadoDustDirection, 0, 0, DustID.DungeonWater, sharknadoDustDirection.X * 2f, sharknadoDustDirection.Y * 2f, 100, default, 1.4f);
                        Main.dust[sharknadoDust].noGravity = true;
                        Main.dust[sharknadoDust].noLight = true;
                        Main.dust[sharknadoDust].velocity = Vector2.Normalize(sharknadoDustDirection) * 3f;
                    }

                    SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= spawnEffectPhaseTimer)
                {
                    npc.ai[0] = 0f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.netUpdate = true;
                }
            }

            // Phase 1
            else if (npc.ai[0] == 0f && !player.dead)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                // Velocity
                if (npc.ai[1] == 0f)
                    npc.ai[1] = 300 * Math.Sign((npc.Center - player.Center).X);

                Vector2 idlePhaseDirection = Vector2.Normalize(player.Center + new Vector2(npc.ai[1], -200f) - npc.Center - npc.velocity) * idlePhaseVelocity;
                npc.SimpleFlyMovement(idlePhaseDirection, idlePhaseAcceleration);

                // Rotation and direction
                int playerFaceDirection = Math.Sign(player.Center.X - npc.Center.X);
                if (playerFaceDirection != 0)
                {
                    if (npc.ai[2] == 0f && playerFaceDirection != npc.direction)
                        npc.rotation += MathHelper.Pi;

                    npc.direction = playerFaceDirection;

                    if (npc.spriteDirection != -npc.direction)
                        npc.rotation += MathHelper.Pi;

                    npc.spriteDirection = -npc.direction;
                }

                // Phase switch
                npc.ai[2] += 1f;
                if (npc.ai[2] >= idlePhaseTimer || CalamityWorld.LegendaryMode)
                {
                    int attackPicker = 0;
                    switch ((int)npc.ai[3])
                    {
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                            attackPicker = 1;
                            break;
                        case 10:
                            npc.ai[3] = 1f;
                            attackPicker = 2;
                            break;
                        case 11:
                            npc.ai[3] = 0f;
                            attackPicker = 3;
                            break;
                    }

                    if (enrage && attackPicker == 2)
                        attackPicker = 3;

                    if (phase2)
                        attackPicker = 4;

                    // Set velocity for charge
                    if (attackPicker == 1)
                    {
                        npc.ai[0] = 1f;
                        npc.ai[1] = 0f;
                        npc.ai[2] = 0f;

                        // Velocity
                        npc.velocity = Vector2.Normalize(player.Center - npc.Center) * chargeVelocity;
                        npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X);

                        // Direction
                        if (playerFaceDirection != 0)
                        {
                            npc.direction = playerFaceDirection;

                            if (npc.spriteDirection == 1)
                                npc.rotation += MathHelper.Pi;

                            npc.spriteDirection = -npc.direction;
                        }
                    }

                    // Bubbles
                    else if (attackPicker == 2)
                    {
                        npc.ai[0] = 2f;
                        npc.ai[1] = 0f;
                        npc.ai[2] = 0f;
                    }

                    // Spawn sharknadoes
                    else if (attackPicker == 3)
                    {
                        npc.ai[0] = 3f;
                        npc.ai[1] = 0f;
                        npc.ai[2] = 0f;
                        if (enrage)
                            npc.ai[2] = sharknadoPhaseTimer - 40;
                        else if (masterMode)
                            npc.ai[2] = sharknadoPhaseTimer - 60;
                    }

                    // Go to phase 2
                    else if (attackPicker == 4)
                    {
                        npc.ai[0] = 4f;
                        npc.ai[1] = 0f;
                        npc.ai[2] = 0f;
                    }

                    npc.netUpdate = true;
                }
            }

            // Charge
            else if (npc.ai[0] == 1f)
            {
                // Set damage
                npc.damage = setDamage;

                // Accelerate
                npc.velocity *= 1.01f;

                // Spawn dust
                int chargeDustAmt = 7;
                for (int j = 0; j < chargeDustAmt; j++)
                {
                    Vector2 arg_E1C_0 = (Vector2.Normalize(npc.velocity) * new Vector2((npc.width + 50) / 2f, npc.height) * 0.75f).RotatedBy((j - (chargeDustAmt / 2 - 1)) * MathHelper.Pi / chargeDustAmt) + npc.Center;
                    Vector2 chargeDustDirection = ((float)(Main.rand.NextDouble() * MathHelper.Pi) - MathHelper.PiOver2).ToRotationVector2() * Main.rand.Next(3, 8);
                    int chargeDust = Dust.NewDust(arg_E1C_0 + chargeDustDirection, 0, 0, DustID.DungeonWater, chargeDustDirection.X * 2f, chargeDustDirection.Y * 2f, 100, default, 1.4f);
                    Main.dust[chargeDust].noGravity = true;
                    Main.dust[chargeDust].noLight = true;
                    Main.dust[chargeDust].velocity /= 4f;
                    Main.dust[chargeDust].velocity -= npc.velocity;
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= chargeTime)
                {
                    npc.ai[0] = 0f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.ai[3] += 2f;
                    npc.TargetClosest();
                    npc.netUpdate = true;
                }
            }

            // Bubble belch
            else if (npc.ai[0] == 2f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                // Velocity
                if (npc.ai[1] == 0f)
                    npc.ai[1] = 300 * Math.Sign((npc.Center - player.Center).X);

                Vector2 bubbleAttackDirection = Vector2.Normalize(player.Center + new Vector2(npc.ai[1], -200f) - npc.Center - npc.velocity) * bubbleBelchPhaseVelocity;
                npc.SimpleFlyMovement(bubbleAttackDirection, bubbleBelchPhaseAcceleration);

                // Play sounds and spawn bubbles
                if (npc.ai[2] == 0f)
                    SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);

                if (npc.ai[2] % bubbleBelchPhaseDivisor == 0f)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath19, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 bubbleSpawnDirection = Vector2.Normalize(player.Center - npc.Center) * (npc.width + 20) / 2f + npc.Center;
                        NPC.NewNPC(npc.GetSource_FromAI(), (int)bubbleSpawnDirection.X, (int)bubbleSpawnDirection.Y + 45, NPCID.DetonatingBubble);
                    }
                }

                // Direction
                int bubbleSpriteFaceDirection = Math.Sign(player.Center.X - npc.Center.X);
                if (bubbleSpriteFaceDirection != 0)
                {
                    npc.direction = bubbleSpriteFaceDirection;
                    if (npc.spriteDirection != -npc.direction)
                        npc.rotation += MathHelper.Pi;
                    npc.spriteDirection = -npc.direction;
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= bubbleBelchPhaseTimer)
                {
                    npc.ai[0] = 0f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.TargetClosest();
                    npc.netUpdate = true;
                }
            }

            // Sharknado spawn
            else if (npc.ai[0] == 3f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                // Velocity
                npc.velocity *= 0.98f;
                npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, 0f, 0.02f);

                // Play sound and spawn sharknadoes
                if (npc.ai[2] == (sharknadoPhaseTimer - 30))
                    SoundEngine.PlaySound(SoundID.Zombie9, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[2] == sharknadoPhaseTimer - 30)
                {
                    Vector2 sharknadoSpawnerDirection = npc.rotation.ToRotationVector2() * (Vector2.UnitX * npc.direction) * (npc.width + 20) / 2f + npc.Center;
                    bool normal = Main.rand.NextBool();
                    float velocityY = normal ? 8f : -4f;
                    float ai1 = normal ? 0f : -1f;

                    Projectile.NewProjectile(npc.GetSource_FromAI(), sharknadoSpawnerDirection.X, sharknadoSpawnerDirection.Y, npc.direction * 3, velocityY, ProjectileID.SharknadoBolt, 0, 0f, Main.myPlayer, 0f, ai1);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), sharknadoSpawnerDirection.X, sharknadoSpawnerDirection.Y, -(float)npc.direction * 3, velocityY, ProjectileID.SharknadoBolt, 0, 0f, Main.myPlayer, 0f, ai1);

                    velocityY = normal ? -4f : 8f;
                    ai1 = normal ? -1f : 0f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), sharknadoSpawnerDirection.X, sharknadoSpawnerDirection.Y, 0f, velocityY, ProjectileID.SharknadoBolt, 0, 0f, Main.myPlayer, 0f, ai1);
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= sharknadoPhaseTimer)
                {
                    npc.ai[0] = 0f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.TargetClosest();
                    npc.netUpdate = true;
                }
            }

            // Transition to phase 2
            else if (npc.ai[0] == 4f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                // Velocity
                npc.velocity *= 0.98f;
                npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, 0f, 0.02f);

                // Sound
                if (npc.ai[2] == phaseTransitionTimer - 60)
                    SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);

                npc.ai[2] += 1f;
                if (npc.ai[2] >= phaseTransitionTimer)
                {
                    npc.ai[0] = 5f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.ai[3] = 0f;
                    npc.TargetClosest();
                    npc.netUpdate = true;
                }
            }

            // Phase 2
            else if (npc.ai[0] == 5f && !player.dead)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                // Velocity
                if (npc.ai[1] == 0f)
                    npc.ai[1] = 300 * Math.Sign((npc.Center - player.Center).X);

                Vector2 phase2IdleDirection = Vector2.Normalize(player.Center + new Vector2(npc.ai[1], -200f) - npc.Center - npc.velocity) * idlePhaseVelocity;
                npc.SimpleFlyMovement(phase2IdleDirection, idlePhaseAcceleration);

                // Direction and rotation
                int phase2SpriteFaceDirection = Math.Sign(player.Center.X - npc.Center.X);
                if (phase2SpriteFaceDirection != 0)
                {
                    if (npc.ai[2] == 0f && phase2SpriteFaceDirection != npc.direction)
                        npc.rotation += MathHelper.Pi;

                    npc.direction = phase2SpriteFaceDirection;

                    if (npc.spriteDirection != -npc.direction)
                        npc.rotation += MathHelper.Pi;

                    npc.spriteDirection = -npc.direction;
                }

                // Phase switch
                npc.ai[2] += 1f;
                if (npc.ai[2] >= idlePhaseTimer || CalamityWorld.LegendaryMode)
                {
                    int phase2AttackPicker = 0;
                    switch ((int)npc.ai[3])
                    {
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                            phase2AttackPicker = 1;
                            break;
                        case 6:
                            npc.ai[3] = 1f;
                            phase2AttackPicker = 2;
                            break;
                        case 7:
                            npc.ai[3] = 0f;
                            phase2AttackPicker = 3;
                            break;
                    }

                    if (enrage && phase2AttackPicker == 2)
                        phase2AttackPicker = 3;

                    if (phase3)
                        phase2AttackPicker = 4;

                    // Set velocity for charge
                    if (phase2AttackPicker == 1)
                    {
                        npc.ai[0] = 6f;
                        npc.ai[1] = 0f;
                        npc.ai[2] = 0f;

                        // Velocity and rotation
                        npc.velocity = Vector2.Normalize(player.Center - npc.Center) * chargeVelocity;
                        npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X);

                        // Direction
                        if (phase2SpriteFaceDirection != 0)
                        {
                            npc.direction = phase2SpriteFaceDirection;

                            if (npc.spriteDirection == 1)
                                npc.rotation += MathHelper.Pi;

                            npc.spriteDirection = -npc.direction;
                        }
                    }

                    // Set velocity for spin
                    else if (phase2AttackPicker == 2)
                    {
                        // Velocity and rotation
                        npc.velocity = Vector2.Normalize(player.Center - npc.Center) * bubbleSpinPhaseVelocity;
                        npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X);

                        // Direction
                        if (phase2SpriteFaceDirection != 0)
                        {
                            npc.direction = phase2SpriteFaceDirection;

                            if (npc.spriteDirection == 1)
                                npc.rotation += MathHelper.Pi;

                            npc.spriteDirection = -npc.direction;
                        }

                        npc.ai[0] = 7f;
                        npc.ai[1] = 0f;
                        npc.ai[2] = 0f;
                    }

                    // Spawn cthulhunado
                    else if (phase2AttackPicker == 3)
                    {
                        npc.ai[0] = 8f;
                        npc.ai[1] = 0f;
                        npc.ai[2] = 0f;
                    }

                    // Go to next phase
                    else if (phase2AttackPicker == 4)
                    {
                        npc.ai[0] = 9f;
                        npc.ai[1] = 0f;
                        npc.ai[2] = 0f;
                    }

                    npc.netUpdate = true;
                }
            }

            // Charge
            else if (npc.ai[0] == 6f)
            {
                // Set damage
                npc.damage = setDamage;

                // Accelerate
                npc.velocity *= 1.01f;

                // Spawn dust
                int phase2ChargeDustAmt = 7;
                for (int k = 0; k < phase2ChargeDustAmt; k++)
                {
                    Vector2 arg_1A97_0 = (Vector2.Normalize(npc.velocity) * new Vector2((npc.width + 50) / 2f, npc.height) * 0.75f).RotatedBy((k - (phase2ChargeDustAmt / 2 - 1)) * MathHelper.Pi / phase2ChargeDustAmt) + npc.Center;
                    Vector2 phase2ChargeDustDirection = ((float)(Main.rand.NextDouble() * MathHelper.Pi) - MathHelper.PiOver2).ToRotationVector2() * Main.rand.Next(3, 8);
                    int phase2ChargeDust = Dust.NewDust(arg_1A97_0 + phase2ChargeDustDirection, 0, 0, DustID.DungeonWater, phase2ChargeDustDirection.X * 2f, phase2ChargeDustDirection.Y * 2f, 100, default, 1.4f);
                    Main.dust[phase2ChargeDust].noGravity = true;
                    Main.dust[phase2ChargeDust].noLight = true;
                    Main.dust[phase2ChargeDust].velocity /= 4f;
                    Main.dust[phase2ChargeDust].velocity -= npc.velocity;
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= chargeTime)
                {
                    npc.ai[0] = 5f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.ai[3] += 2f;
                    npc.TargetClosest();
                    npc.netUpdate = true;
                }
            }

            // Bubble spin
            else if (npc.ai[0] == 7f)
            {
                // Set damage
                npc.damage = 0;

                // Play sounds and spawn bubbles
                if (npc.ai[2] == 0f)
                    SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);

                if (npc.ai[2] % bubbleSpinPhaseDivisor == 0f)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath19, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 phase2BubbleSharkronDirection = Vector2.Normalize(npc.velocity) * (npc.width + 20) / 2f + npc.Center;
                        int phase2Bubbles = NPC.NewNPC(npc.GetSource_FromAI(), (int)phase2BubbleSharkronDirection.X, (int)phase2BubbleSharkronDirection.Y + 45, NPCID.DetonatingBubble);
                        Main.npc[phase2Bubbles].target = npc.target;
                        Main.npc[phase2Bubbles].velocity = Vector2.Normalize(npc.velocity).RotatedBy(MathHelper.PiOver2 * npc.direction) * bubbleSpinBubbleVelocity * (CalamityWorld.LegendaryMode ? (Main.rand.NextFloat() + 0.5f) : 1f);
                        Main.npc[phase2Bubbles].netUpdate = true;
                        Main.npc[phase2Bubbles].ai[3] = Main.rand.Next(80, 121) / 100f;

                        if (npc.ai[2] % (bubbleSpinPhaseDivisor * 5) == 0f)
                        {
                            int phase2BubbleSharkrons = NPC.NewNPC(npc.GetSource_FromAI(), (int)phase2BubbleSharkronDirection.X, (int)phase2BubbleSharkronDirection.Y + 45, NPCID.Sharkron2);
                            Main.npc[phase2BubbleSharkrons].ai[1] = 89f;
                        }
                    }
                }

                // Velocity and rotation
                npc.velocity = npc.velocity.RotatedBy(-(double)bubbleSpinPhaseRotation * (float)npc.direction);
                npc.rotation -= bubbleSpinPhaseRotation * npc.direction;

                npc.ai[2] += 1f;
                if (npc.ai[2] >= bubbleSpinPhaseTimer)
                {
                    npc.ai[0] = 5f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.TargetClosest();
                    npc.netUpdate = true;
                }
            }

            // Spawn cthulhunado
            else if (npc.ai[0] == 8f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                // Velocity
                npc.velocity *= 0.98f;
                npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, 0f, 0.02f);

                // Play sound and spawn cthulhunado
                if (npc.ai[2] == sharknadoPhaseTimer - 30)
                    SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[2] == sharknadoPhaseTimer - 30)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ProjectileID.SharknadoBolt, 0, 0f, Main.myPlayer, 1f, npc.target + 1, (enrage || masterMode) ? 1 : 0);

                npc.ai[2] += 1f;
                if (npc.ai[2] >= sharknadoPhaseTimer)
                {
                    npc.ai[0] = 5f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.TargetClosest();
                    npc.netUpdate = true;
                }
            }

            // Transition to phase 3
            else if (npc.ai[0] == 9f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                // Alpha adjustments
                if (npc.ai[2] < phaseTransitionTimer - 90)
                {
                    if (Collision.SolidCollision(npc.position, npc.width, npc.height))
                        npc.alpha += 15;
                    else
                        npc.alpha -= 15;

                    if (npc.alpha < 0)
                        npc.alpha = 0;
                    if (npc.alpha > 150)
                        npc.alpha = 150;
                }
                else if (npc.alpha < 255)
                {
                    npc.alpha += 4;
                    if (npc.alpha > 255)
                        npc.alpha = 255;
                }

                // Velocity
                npc.velocity *= 0.98f;
                npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, 0f, 0.02f);

                // Play sound
                if (npc.ai[2] == phaseTransitionTimer - 60)
                    SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);

                npc.ai[2] += 1f;
                if (npc.ai[2] >= phaseTransitionTimer)
                {
                    npc.ai[0] = 10f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.ai[3] = 0f;
                    npc.TargetClosest();
                    npc.netUpdate = true;
                }
            }

            // Phase 3
            else if (npc.ai[0] == 10f && !player.dead)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                // Alpha
                if (npc.alpha < 255)
                {
                    npc.alpha += 25;
                    if (npc.alpha > 255)
                        npc.alpha = 255;
                }

                // Teleport location
                if (npc.ai[1] == 0f)
                    npc.ai[1] = 360 * Math.Sign((npc.Center - player.Center).X);

                Vector2 desiredVelocity = Vector2.Normalize(player.Center + new Vector2(npc.ai[1], -200f) - npc.Center - npc.velocity) * idlePhaseVelocity;
                npc.SimpleFlyMovement(desiredVelocity, idlePhaseAcceleration);

                // Rotation and direction
                int phase3SpriteFaceDirection = Math.Sign(player.Center.X - npc.Center.X);
                if (phase3SpriteFaceDirection != 0)
                {
                    if (npc.ai[2] == 0f && phase3SpriteFaceDirection != npc.direction)
                    {
                        npc.rotation += MathHelper.Pi;
                        for (int l = 0; l < npc.oldPos.Length; l++)
                            npc.oldPos[l] = Vector2.Zero;
                    }

                    npc.direction = phase3SpriteFaceDirection;

                    if (npc.spriteDirection != -npc.direction)
                        npc.rotation += MathHelper.Pi;

                    npc.spriteDirection = -npc.direction;
                }

                // Phase switch
                npc.ai[2] += 1f;
                if (npc.ai[2] >= idlePhaseTimer || CalamityWorld.LegendaryMode)
                {
                    int phase3AttackPicker = 0;
                    if (phase4)
                    {
                        switch ((int)npc.ai[3])
                        {
                            case 0:
                            case 1:
                            case 2:
                            case 4:
                            case 5:
                            case 6:
                            case 7:
                                phase3AttackPicker = 1;
                                break;
                            case 3:
                            case 8:
                                phase3AttackPicker = 2;
                                break;
                        }

                        if (death)
                            phase3AttackPicker = 1;
                    }
                    else
                    {
                        switch ((int)npc.ai[3])
                        {
                            case 0:
                            case 2:
                            case 3:
                            case 5:
                            case 6:
                            case 7:
                                phase3AttackPicker = 1;
                                break;
                            case 1:
                            case 4:
                            case 8:
                                phase3AttackPicker = 2;
                                break;
                        }
                    }

                    // Go back to normalcy after dropping below 90% HP
                    if (!masterModeSurprise && !phase3)
                        phase3AttackPicker = 3;

                    // Set velocity for charge
                    if (phase3AttackPicker == 1)
                    {
                        npc.ai[0] = 11f;
                        npc.ai[1] = 0f;
                        npc.ai[2] = 0f;

                        // Velocity and rotation
                        npc.velocity = Vector2.Normalize(player.Center + (bossRush && phase4 ? player.velocity * 20f : Vector2.Zero) - npc.Center) * chargeVelocity;
                        npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X);

                        // Direction
                        if (phase3SpriteFaceDirection != 0)
                        {
                            npc.direction = phase3SpriteFaceDirection;

                            if (npc.spriteDirection == 1)
                                npc.rotation += MathHelper.Pi;

                            npc.spriteDirection = -npc.direction;
                        }
                    }

                    // Pause
                    else if (phase3AttackPicker == 2)
                    {
                        npc.ai[0] = 12f;
                        npc.ai[1] = 0f;
                        npc.ai[2] = 0f;
                    }

                    // Go to next phase
                    else if (phase3AttackPicker == 3)
                    {
                        npc.ai[0] = -1f;
                        npc.ai[1] = 0f;
                        npc.ai[2] = 0f;
                    }

                    npc.netUpdate = true;
                }
            }

            // Charge
            else if (npc.ai[0] == 11f)
            {
                // Set damage
                npc.damage = setDamage;

                // Accelerate
                npc.velocity *= 1.01f;

                // Alpha
                npc.alpha -= 25;
                if (npc.alpha < 0)
                    npc.alpha = 0;

                // Spawn dust
                int phase3ChargeDustAmt = 7;
                for (int m = 0; m < phase3ChargeDustAmt; m++)
                {
                    Vector2 arg_2444_0 = (Vector2.Normalize(npc.velocity) * new Vector2((npc.width + 50) / 2f, npc.height) * 0.75f).RotatedBy((m - (phase3ChargeDustAmt / 2 - 1)) * MathHelper.Pi / phase3ChargeDustAmt) + npc.Center;
                    Vector2 phase3ChargeDustDirection = ((float)(Main.rand.NextDouble() * MathHelper.Pi) - MathHelper.PiOver2).ToRotationVector2() * Main.rand.Next(3, 8);
                    int phase3ChargeDust = Dust.NewDust(arg_2444_0 + phase3ChargeDustDirection, 0, 0, DustID.DungeonWater, phase3ChargeDustDirection.X * 2f, phase3ChargeDustDirection.Y * 2f, 100, default, 1.4f);
                    Main.dust[phase3ChargeDust].noGravity = true;
                    Main.dust[phase3ChargeDust].noLight = true;
                    Main.dust[phase3ChargeDust].velocity /= 4f;
                    Main.dust[phase3ChargeDust].velocity -= npc.velocity;
                }

                // Spawn bubbles during charge in Master Mode (these bubbles have special behavior that makes them float upward, doing no damage, before returning to their normal behavior)
                if (masterMode && phase4)
                {
                    if (npc.ai[2] % (bubbleBelchPhaseDivisor * 2) == 0f)
                    {
                        SoundEngine.PlaySound(SoundID.NPCDeath19, npc.Center);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 bubbleSpawnDirection = Vector2.Normalize(player.Center - npc.Center) * (npc.width + 20) / 2f + npc.Center;
                            NPC.NewNPC(npc.GetSource_FromAI(), (int)bubbleSpawnDirection.X, (int)bubbleSpawnDirection.Y + 45, NPCID.DetonatingBubble, 0, 0f, -60f);
                        }
                    }
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= chargeTime)
                {
                    npc.ai[0] = 10f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;

                    if (!phase4 || !death)
                        npc.ai[3] += 1f;

                    npc.TargetClosest();
                    npc.netUpdate = true;
                }
            }

            // Pause before teleport
            else if (npc.ai[0] == 12f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                // Alpha
                if (npc.alpha < 255)
                {
                    npc.alpha += 17;
                    if (npc.alpha > 255)
                        npc.alpha = 255;
                }

                // Velocity
                npc.velocity *= 0.98f;
                npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, 0f, 0.02f);

                // Play sound
                if (npc.ai[2] == teleportPhaseTimer / 2)
                    SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[2] == teleportPhaseTimer / 2)
                {
                    // Teleport location
                    if (npc.ai[1] == 0f)
                        npc.ai[1] = 300 * Math.Sign((npc.Center - player.Center).X);

                    // Rotation and direction
                    Vector2 center = player.Center + new Vector2(-npc.ai[1], -200f);
                    npc.Center = center;
                    int phase3PlayerDirection = Math.Sign(player.Center.X - npc.Center.X);
                    if (phase3PlayerDirection != 0)
                    {
                        if (npc.ai[2] == 0f && phase3PlayerDirection != npc.direction)
                        {
                            npc.rotation += MathHelper.Pi;
                            for (int n = 0; n < npc.oldPos.Length; n++)
                                npc.oldPos[n] = Vector2.Zero;
                        }

                        npc.direction = phase3PlayerDirection;

                        if (npc.spriteDirection != -npc.direction)
                            npc.rotation += MathHelper.Pi;

                        npc.spriteDirection = -npc.direction;
                    }
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= teleportPhaseTimer)
                {
                    npc.ai[0] = 10f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;

                    npc.ai[3] += 1f;
                    if (npc.ai[3] >= 9f)
                        npc.ai[3] = 0f;

                    npc.netUpdate = true;
                }
            }

            return false;
        }

        public static bool BuffedDetonatingBubbleAI(NPC npc, Mod mod)
        {
            bool driftUpward = npc.ai[1] < 0f;
            npc.damage = driftUpward ? 0 : npc.defDamage;

            if (driftUpward)
            {
                npc.ai[1] += 1f;

                if (npc.velocity.Y > -2f)
                    npc.velocity.Y -= 0.04f;
                
                return false;
            }

            if (npc.target == Main.maxPlayers)
            {
                npc.TargetClosest();
                npc.ai[3] = (float)Main.rand.Next(100, 151) / 100f;
                float startingVelocity = (float)Main.rand.Next(250, 351) / 15f;
                npc.velocity = (Main.player[npc.target].Center - npc.Center + new Vector2(Main.rand.Next(-100, 101), Main.rand.Next(-100, 101))).SafeNormalize(Vector2.UnitY) * startingVelocity;
                npc.netUpdate = true;
            }

            bool pop = npc.ai[0] == 1f;

            Vector2 velocityVector = (Main.player[npc.target].Center - npc.Center).SafeNormalize(Vector2.UnitY);
            float inertia = 30f;
            float velocity = 25f;
            npc.velocity = (npc.velocity * inertia + velocityVector * velocity) / (inertia + 1f);
            
            npc.scale = npc.ai[3];

            npc.alpha -= 30;
            if (npc.alpha < 50)
                npc.alpha = 50;
            npc.alpha = 50;

            float inertia2 = inertia + 10f;
            npc.velocity.X = (npc.velocity.X * inertia2 + (float)Main.rand.Next(-10, 11) * 0.1f) / (inertia2 + 1f);
            npc.velocity.Y = (npc.velocity.Y * inertia2 + -0.25f + (float)Main.rand.Next(-10, 11) * 0.2f) / (inertia2 + 1f);
            if (npc.velocity.Y > 0f)
                npc.velocity.Y -= 0.04f;

            // Push Bubbles away from each other.
            float spreadOutStrength = (CalamityWorld.death || BossRushEvent.BossRushActive) ? -0.1f : -0.08f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (i != npc.whoAmI && Main.npc[i].active && Main.npc[i].type == npc.type)
                {
                    Vector2 otherBubbleDist = Main.npc[i].Center - npc.Center;
                    if (otherBubbleDist.Length() < (npc.width + npc.height))
                    {
                        otherBubbleDist = otherBubbleDist.SafeNormalize(Vector2.UnitY);
                        otherBubbleDist *= spreadOutStrength;
                        npc.velocity += otherBubbleDist;
                        Main.npc[i].velocity -= otherBubbleDist;
                    }
                }
            }

            if (npc.ai[0] == 0f)
            {
                int size = 40;
                Rectangle rect = npc.getRect();
                rect.X -= size + npc.width / 2;
                rect.Y -= size + npc.height / 2;
                rect.Width += size * 2;
                rect.Height += size * 2;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (player.active && !player.dead && rect.Intersects(player.getRect()))
                    {
                        npc.ai[0] = 1f;
                        npc.ai[1] = 4f;
                        npc.netUpdate = true;
                        break;
                    }
                }
            }

            if (npc.ai[0] == 0f)
            {
                npc.ai[1] += 1f;
                float timeBeforePopping = 300f;
                if (npc.ai[1] >= timeBeforePopping)
                {
                    npc.ai[0] = 1f;
                    npc.ai[1] = 4f;
                }
            }

            if (pop)
            {
                npc.ai[1] -= 1f;
                if (npc.ai[1] <= 0f)
                {
                    npc.life = 0;
                    npc.HitEffect();
                    npc.active = false;
                    return false;
                }
            }

            if (pop)
            {
                npc.position = npc.Center;
                npc.width = npc.height = 100;
                npc.position = new Vector2(npc.position.X - (float)(npc.width / 2), npc.position.Y - (float)(npc.height / 2));
                npc.EncourageDespawn(3);
            }

            return false;
        }

        public static bool VanillaDukeFishronAI(NPC npc, Mod mod)
        {
            bool expertMode = Main.expertMode;
            bool masterMode = Main.masterMode;

            double damageScale = expertMode ? 1.2 : 1D;
            float phase2LifeRatio = masterMode ? 0.6f : 0.5f;
            float phase3LifeRatio = masterMode ? 0.25f : 0.15f;
            bool flag = npc.life <= npc.lifeMax * phase2LifeRatio;
            bool flag2 = expertMode && npc.life <= npc.lifeMax * phase3LifeRatio;
            bool flag3 = npc.ai[0] > 4f;
            bool flag4 = npc.ai[0] > 9f;
            bool flag5 = npc.ai[3] < 10f;

            int setDamage = npc.defDamage;
            if (flag4)
            {
                setDamage = (int)Math.Round(setDamage * 1.1 * damageScale);
                npc.defense = 0;
            }
            else if (flag3)
            {
                setDamage = (int)Math.Round(setDamage * 1.2 * damageScale);
                npc.defense = (int)Math.Round(npc.defDefense * 0.8);
            }
            else
                npc.defense = npc.defDefense;

            int num2 = (masterMode ? 30 : expertMode ? 40 : 60);
            float num3 = (masterMode ? 0.65f : expertMode ? 0.55f : 0.45f);
            float num4 = (masterMode ? 9.5f : expertMode ? 8.5f : 7.5f);
            if (flag4)
            {
                num3 = masterMode ? 0.8f : 0.7f;
                num4 = masterMode ? 13f : 12f;
                num2 = masterMode ? 20 : 30;
            }
            else if (flag3 && flag5)
            {
                num3 = (masterMode ? 0.7f : expertMode ? 0.6f : 0.5f);
                num4 = (masterMode ? 12f : expertMode ? 10f : 8f);
                num2 = (masterMode ? 35 : expertMode ? 40 : 20);
            }
            else if (flag5 && !flag3 && !flag4)
                num2 = masterMode ? 25 : 30;

            int num5 = (masterMode ? 25 : expertMode ? 28 : 30);
            float num6 = (masterMode ? 18f : expertMode ? 17f : 16f);
            if (flag4)
            {
                num5 = masterMode ? 20 : 25;
                num6 = masterMode ? 29f : 27f;
            }
            else if (flag5 && flag3)
            {
                num5 = (masterMode ? 24 : expertMode ? 27 : 30);
                if (expertMode)
                    num6 = masterMode ? 24f : 21f;
            }

            int num7 = masterMode ? 40 : 80;
            int num8 = masterMode ? 2 : 4;
            float num9 = masterMode ? 0.5f : 0.3f;
            float num10 = masterMode ? 8f : 5f;
            int num11 = masterMode ? 60 : 90;
            int num12 = 180;
            int num13 = 180;
            int num14 = 30;
            int num15 = masterMode ? 60 : 120;
            int num16 = masterMode ? 2 : 4;
            float num17 = masterMode ? 9f : 6f;
            float num18 = 20f;
            float num19 = MathHelper.TwoPi / (float)(num15 / 2);
            int num20 = masterMode ? 50 : 75;
            Vector2 center = npc.Center;
            Player player = Main.player[npc.target];
            if (npc.target < 0 || npc.target == Main.maxPlayers || player.dead || !player.active || Vector2.Distance(player.Center, center) > 5600f)
            {
                npc.TargetClosest();
                player = Main.player[npc.target];
                npc.netUpdate = true;
            }

            if (player.dead || Vector2.Distance(player.Center, center) > 5600f)
            {
                npc.velocity.Y -= 0.4f;
                npc.EncourageDespawn(10);
                if (npc.ai[0] > 4f)
                    npc.ai[0] = 5f;
                else
                    npc.ai[0] = 0f;

                npc.ai[2] = 0f;
            }

            bool enrage = player.position.Y < 800f || (double)player.position.Y > Main.worldSurface * 16.0 || (player.position.X > 6400f && player.position.X < (float)(Main.maxTilesX * 16 - 6400));

            npc.Calamity().CurrentlyEnraged = !BossRushEvent.BossRushActive && enrage;

            // Increased DR during phase transitions
            npc.Calamity().DR = (npc.ai[0] == -1f || npc.ai[0] == 4f || npc.ai[0] == 9f) ? (BossRushEvent.BossRushActive ? 0.99f : 0.625f) : 0.15f;
            npc.Calamity().CurrentlyIncreasingDefenseOrDR = npc.ai[0] == -1f || npc.ai[0] == 4f || npc.ai[0] == 9f;

            if (enrage)
            {
                num2 = masterMode ? 5 : 10;
                setDamage *= 2;
                npc.defense = npc.defDefense * 2;
                num6 += (masterMode ? 12f : 6f);
            }

            if (npc.localAI[0] == 0f)
            {
                npc.localAI[0] = 1f;
                npc.alpha = 255;
                npc.rotation = 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.ai[0] = -1f;
                    npc.netUpdate = true;
                }
            }

            float num21 = (float)Math.Atan2(player.Center.Y - center.Y, player.Center.X - center.X);
            if (npc.spriteDirection == 1)
                num21 += (float)Math.PI;

            if (num21 < 0f)
                num21 += MathHelper.TwoPi;

            if (num21 > MathHelper.TwoPi)
                num21 -= MathHelper.TwoPi;

            if (npc.ai[0] == -1f)
                num21 = 0f;

            if (npc.ai[0] == 3f)
                num21 = 0f;

            if (npc.ai[0] == 4f)
                num21 = 0f;

            if (npc.ai[0] == 8f)
                num21 = 0f;

            float num22 = 0.04f;
            if (npc.ai[0] == 1f || npc.ai[0] == 6f)
                num22 = 0f;

            if (npc.ai[0] == 7f)
                num22 = 0f;

            if (npc.ai[0] == 3f)
                num22 = 0.01f;

            if (npc.ai[0] == 4f)
                num22 = 0.01f;

            if (npc.ai[0] == 8f)
                num22 = 0.01f;

            if (npc.rotation < num21)
            {
                if ((double)(num21 - npc.rotation) > Math.PI)
                    npc.rotation -= num22;
                else
                    npc.rotation += num22;
            }

            if (npc.rotation > num21)
            {
                if ((double)(npc.rotation - num21) > Math.PI)
                    npc.rotation += num22;
                else
                    npc.rotation -= num22;
            }

            if (npc.rotation > num21 - num22 && npc.rotation < num21 + num22)
                npc.rotation = num21;

            if (npc.rotation < 0f)
                npc.rotation += MathHelper.TwoPi;

            if (npc.rotation > MathHelper.TwoPi)
                npc.rotation -= MathHelper.TwoPi;

            if (npc.rotation > num21 - num22 && npc.rotation < num21 + num22)
                npc.rotation = num21;

            if (npc.ai[0] != -1f && npc.ai[0] < 9f)
            {
                if (Collision.SolidCollision(npc.position, npc.width, npc.height))
                    npc.alpha += 15;
                else
                    npc.alpha -= 15;

                if (npc.alpha < 0)
                    npc.alpha = 0;

                if (npc.alpha > 150)
                    npc.alpha = 150;
            }

            if (npc.ai[0] == -1f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                npc.velocity *= 0.98f;
                int num23 = Math.Sign(player.Center.X - center.X);
                if (num23 != 0)
                {
                    npc.direction = num23;
                    npc.spriteDirection = -npc.direction;
                }

                if (npc.ai[2] > 20f)
                {
                    npc.velocity.Y = -2f;
                    npc.alpha -= 5;
                    if (Collision.SolidCollision(npc.position, npc.width, npc.height))
                        npc.alpha += 15;

                    if (npc.alpha < 0)
                        npc.alpha = 0;

                    if (npc.alpha > 150)
                        npc.alpha = 150;
                }

                if (npc.ai[2] == (float)(num11 - 30))
                {
                    int num24 = 36;
                    for (int i = 0; i < num24; i++)
                    {
                        Vector2 vector = (Vector2.Normalize(npc.velocity) * new Vector2((float)npc.width / 2f, npc.height) * 0.75f * 0.5f).RotatedBy((float)(i - (num24 / 2 - 1)) * MathHelper.TwoPi / (float)num24) + npc.Center;
                        Vector2 vector2 = vector - npc.Center;
                        int num25 = Dust.NewDust(vector + vector2, 0, 0, DustID.DungeonWater, vector2.X * 2f, vector2.Y * 2f, 100, default(Color), 1.4f);
                        Main.dust[num25].noGravity = true;
                        Main.dust[num25].noLight = true;
                        Main.dust[num25].velocity = Vector2.Normalize(vector2) * 3f;
                    }

                    SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= (float)num20)
                {
                    npc.ai[0] = 0f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.netUpdate = true;
                }
            }
            else if (npc.ai[0] == 0f && !player.dead)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                if (npc.ai[1] == 0f)
                    npc.ai[1] = 300 * Math.Sign((center - player.Center).X);

                Vector2 vector3 = Vector2.Normalize(player.Center + new Vector2(npc.ai[1], -200f) - center - npc.velocity) * num4;
                if (npc.velocity.X < vector3.X)
                {
                    npc.velocity.X += num3;
                    if (npc.velocity.X < 0f && vector3.X > 0f)
                        npc.velocity.X += num3;
                }
                else if (npc.velocity.X > vector3.X)
                {
                    npc.velocity.X -= num3;
                    if (npc.velocity.X > 0f && vector3.X < 0f)
                        npc.velocity.X -= num3;
                }

                if (npc.velocity.Y < vector3.Y)
                {
                    npc.velocity.Y += num3;
                    if (npc.velocity.Y < 0f && vector3.Y > 0f)
                        npc.velocity.Y += num3;
                }
                else if (npc.velocity.Y > vector3.Y)
                {
                    npc.velocity.Y -= num3;
                    if (npc.velocity.Y > 0f && vector3.Y < 0f)
                        npc.velocity.Y -= num3;
                }

                int num26 = Math.Sign(player.Center.X - center.X);
                if (num26 != 0)
                {
                    if (npc.ai[2] == 0f && num26 != npc.direction)
                        npc.rotation += (float)Math.PI;

                    npc.direction = num26;
                    if (npc.spriteDirection != -npc.direction)
                        npc.rotation += (float)Math.PI;

                    npc.spriteDirection = -npc.direction;
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= (float)num2)
                {
                    int num27 = 0;
                    switch ((int)npc.ai[3])
                    {
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                            num27 = 1;
                            break;
                        case 10:
                            npc.ai[3] = 1f;
                            num27 = 2;
                            break;
                        case 11:
                            npc.ai[3] = 0f;
                            num27 = 3;
                            break;
                    }

                    if (enrage && num27 == 2)
                        num27 = 3;

                    if (flag)
                        num27 = 4;

                    switch (num27)
                    {
                        case 1:
                            npc.ai[0] = 1f;
                            npc.ai[1] = 0f;
                            npc.ai[2] = 0f;
                            npc.velocity = Vector2.Normalize(player.Center - center) * num6;
                            npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X);
                            if (num26 != 0)
                            {
                                npc.direction = num26;
                                if (npc.spriteDirection == 1)
                                    npc.rotation += (float)Math.PI;

                                npc.spriteDirection = -npc.direction;
                            }
                            break;

                        case 2:
                            npc.ai[0] = 2f;
                            npc.ai[1] = 0f;
                            npc.ai[2] = 0f;
                            break;

                        case 3:
                            npc.ai[0] = 3f;
                            npc.ai[1] = 0f;
                            npc.ai[2] = 0f;
                            if (enrage)
                                npc.ai[2] = num11 - 40;
                            break;

                        case 4:
                            npc.ai[0] = 4f;
                            npc.ai[1] = 0f;
                            npc.ai[2] = 0f;
                            break;
                    }

                    npc.netUpdate = true;
                }
            }
            else if (npc.ai[0] == 1f)
            {
                // Set damage
                npc.damage = setDamage;

                int num28 = 7;
                for (int j = 0; j < num28; j++)
                {
                    Vector2 vector4 = (Vector2.Normalize(npc.velocity) * new Vector2((float)(npc.width + 50) / 2f, npc.height) * 0.75f).RotatedBy((double)(j - (num28 / 2 - 1)) * Math.PI / (double)(float)num28) + center;
                    Vector2 vector5 = ((float)(Main.rand.NextDouble() * MathHelper.Pi) - MathHelper.PiOver2).ToRotationVector2() * Main.rand.Next(3, 8);
                    int num29 = Dust.NewDust(vector4 + vector5, 0, 0, DustID.DungeonWater, vector5.X * 2f, vector5.Y * 2f, 100, default(Color), 1.4f);
                    Main.dust[num29].noGravity = true;
                    Main.dust[num29].noLight = true;
                    Main.dust[num29].velocity /= 4f;
                    Main.dust[num29].velocity -= npc.velocity;
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= (float)num5)
                {
                    npc.ai[0] = 0f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.ai[3] += 2f;
                    npc.netUpdate = true;
                }
            }
            else if (npc.ai[0] == 2f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                if (npc.ai[1] == 0f)
                    npc.ai[1] = 300 * Math.Sign((center - player.Center).X);

                Vector2 vector6 = Vector2.Normalize(player.Center + new Vector2(npc.ai[1], -200f) - center - npc.velocity) * num10;
                if (npc.velocity.X < vector6.X)
                {
                    npc.velocity.X += num9;
                    if (npc.velocity.X < 0f && vector6.X > 0f)
                        npc.velocity.X += num9;
                }
                else if (npc.velocity.X > vector6.X)
                {
                    npc.velocity.X -= num9;
                    if (npc.velocity.X > 0f && vector6.X < 0f)
                        npc.velocity.X -= num9;
                }

                if (npc.velocity.Y < vector6.Y)
                {
                    npc.velocity.Y += num9;
                    if (npc.velocity.Y < 0f && vector6.Y > 0f)
                        npc.velocity.Y += num9;
                }
                else if (npc.velocity.Y > vector6.Y)
                {
                    npc.velocity.Y -= num9;
                    if (npc.velocity.Y > 0f && vector6.Y < 0f)
                        npc.velocity.Y -= num9;
                }

                if (npc.ai[2] == 0f)
                    SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);

                if (npc.ai[2] % (float)num8 == 0f)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath19, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 vector7 = Vector2.Normalize(player.Center - center) * (npc.width + 20) / 2f + center;
                        NPC.NewNPC(npc.GetSource_FromAI(), (int)vector7.X, (int)vector7.Y + 45, NPCID.DetonatingBubble);
                    }
                }

                int num30 = Math.Sign(player.Center.X - center.X);
                if (num30 != 0)
                {
                    npc.direction = num30;
                    if (npc.spriteDirection != -npc.direction)
                        npc.rotation += (float)Math.PI;

                    npc.spriteDirection = -npc.direction;
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= (float)num7)
                {
                    npc.ai[0] = 0f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.netUpdate = true;
                }
            }
            else if (npc.ai[0] == 3f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                npc.velocity *= 0.98f;
                npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, 0f, 0.02f);
                if (npc.ai[2] == (float)(num11 - 30))
                    SoundEngine.PlaySound(SoundID.Zombie9, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[2] == (float)(num11 - 30))
                {
                    Vector2 vector8 = npc.rotation.ToRotationVector2() * (Vector2.UnitX * npc.direction) * (npc.width + 20) / 2f + center;
                    Vector2 sharknadoBoltVelocity = new Vector2(npc.direction * 2, masterMode ? 12f : 8f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), vector8, sharknadoBoltVelocity, ProjectileID.SharknadoBolt, 0, 0f, Main.myPlayer);
                    sharknadoBoltVelocity = new Vector2(npc.direction * -2, masterMode ? 12f : 8f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), vector8, sharknadoBoltVelocity, ProjectileID.SharknadoBolt, 0, 0f, Main.myPlayer);
                    if (masterMode)
                    {
                        sharknadoBoltVelocity = new Vector2(npc.direction * 2, masterMode ? -12f : -8f);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), vector8, sharknadoBoltVelocity, ProjectileID.SharknadoBolt, 0, 0f, Main.myPlayer, 0f, -1f);
                        sharknadoBoltVelocity = new Vector2(npc.direction * -2, masterMode ? -12f : -8f);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), vector8, sharknadoBoltVelocity, ProjectileID.SharknadoBolt, 0, 0f, Main.myPlayer, 0f, -1f);
                    }
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= (float)num11)
                {
                    npc.ai[0] = 0f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.netUpdate = true;
                }
            }
            else if (npc.ai[0] == 4f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                npc.velocity *= 0.98f;
                npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, 0f, 0.02f);
                if (npc.ai[2] == (float)(num12 - 60))
                    SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);

                npc.ai[2] += 1f;
                if (npc.ai[2] >= (float)num12)
                {
                    npc.ai[0] = 5f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.ai[3] = 0f;
                    npc.netUpdate = true;
                }
            }
            else if (npc.ai[0] == 5f && !player.dead)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                if (npc.ai[1] == 0f)
                    npc.ai[1] = 300 * Math.Sign((center - player.Center).X);

                Vector2 vector9 = Vector2.Normalize(player.Center + new Vector2(npc.ai[1], -200f) - center - npc.velocity) * num4;
                if (npc.velocity.X < vector9.X)
                {
                    npc.velocity.X += num3;
                    if (npc.velocity.X < 0f && vector9.X > 0f)
                        npc.velocity.X += num3;
                }
                else if (npc.velocity.X > vector9.X)
                {
                    npc.velocity.X -= num3;
                    if (npc.velocity.X > 0f && vector9.X < 0f)
                        npc.velocity.X -= num3;
                }

                if (npc.velocity.Y < vector9.Y)
                {
                    npc.velocity.Y += num3;
                    if (npc.velocity.Y < 0f && vector9.Y > 0f)
                        npc.velocity.Y += num3;
                }
                else if (npc.velocity.Y > vector9.Y)
                {
                    npc.velocity.Y -= num3;
                    if (npc.velocity.Y > 0f && vector9.Y < 0f)
                        npc.velocity.Y -= num3;
                }

                int num31 = Math.Sign(player.Center.X - center.X);
                if (num31 != 0)
                {
                    if (npc.ai[2] == 0f && num31 != npc.direction)
                        npc.rotation += (float)Math.PI;

                    npc.direction = num31;
                    if (npc.spriteDirection != -npc.direction)
                        npc.rotation += (float)Math.PI;

                    npc.spriteDirection = -npc.direction;
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= (float)num2)
                {
                    int num32 = 0;
                    switch ((int)npc.ai[3])
                    {
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                            num32 = 1;
                            break;
                        case 6:
                            npc.ai[3] = 1f;
                            num32 = 2;
                            break;
                        case 7:
                            npc.ai[3] = 0f;
                            num32 = 3;
                            break;
                    }

                    if (flag2)
                        num32 = 4;

                    if (enrage && num32 == 2)
                        num32 = 3;

                    switch (num32)
                    {
                        case 1:
                            npc.ai[0] = 6f;
                            npc.ai[1] = 0f;
                            npc.ai[2] = 0f;
                            npc.velocity = Vector2.Normalize(player.Center - center) * num6;
                            npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X);
                            if (num31 != 0)
                            {
                                npc.direction = num31;
                                if (npc.spriteDirection == 1)
                                    npc.rotation += (float)Math.PI;

                                npc.spriteDirection = -npc.direction;
                            }
                            break;

                        case 2:
                            npc.velocity = Vector2.Normalize(player.Center - center) * num18;
                            npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X);
                            if (num31 != 0)
                            {
                                npc.direction = num31;
                                if (npc.spriteDirection == 1)
                                    npc.rotation += (float)Math.PI;

                                npc.spriteDirection = -npc.direction;
                            }
                            npc.ai[0] = 7f;
                            npc.ai[1] = 0f;
                            npc.ai[2] = 0f;
                            break;

                        case 3:
                            npc.ai[0] = 8f;
                            npc.ai[1] = 0f;
                            npc.ai[2] = 0f;
                            break;

                        case 4:
                            npc.ai[0] = 9f;
                            npc.ai[1] = 0f;
                            npc.ai[2] = 0f;
                            break;
                    }

                    npc.netUpdate = true;
                }
            }
            else if (npc.ai[0] == 6f)
            {
                // Set damage
                npc.damage = setDamage;

                int num33 = 7;
                for (int k = 0; k < num33; k++)
                {
                    Vector2 vector10 = (Vector2.Normalize(npc.velocity) * new Vector2((float)(npc.width + 50) / 2f, npc.height) * 0.75f).RotatedBy((double)(k - (num33 / 2 - 1)) * Math.PI / (double)(float)num33) + center;
                    Vector2 vector11 = ((float)(Main.rand.NextDouble() * MathHelper.Pi) - MathHelper.PiOver2).ToRotationVector2() * Main.rand.Next(3, 8);
                    int num34 = Dust.NewDust(vector10 + vector11, 0, 0, DustID.DungeonWater, vector11.X * 2f, vector11.Y * 2f, 100, default(Color), 1.4f);
                    Main.dust[num34].noGravity = true;
                    Main.dust[num34].noLight = true;
                    Main.dust[num34].velocity /= 4f;
                    Main.dust[num34].velocity -= npc.velocity;
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= (float)num5)
                {
                    npc.ai[0] = 5f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.ai[3] += 2f;
                    npc.netUpdate = true;
                }
            }
            else if (npc.ai[0] == 7f)
            {
                // Set damage
                npc.damage = 0;

                if (npc.ai[2] == 0f)
                    SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);

                if (npc.ai[2] % (float)num16 == 0f)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath19, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 vector12 = Vector2.Normalize(npc.velocity) * (npc.width + 20) / 2f + center;
                        int num35 = NPC.NewNPC(npc.GetSource_FromAI(), (int)vector12.X, (int)vector12.Y + 45, NPCID.DetonatingBubble);
                        Main.npc[num35].target = npc.target;
                        Main.npc[num35].velocity = Vector2.Normalize(npc.velocity).RotatedBy(MathHelper.PiOver2 * (float)npc.direction) * num17;
                        Main.npc[num35].netUpdate = true;
                        Main.npc[num35].ai[3] = (float)Main.rand.Next(80, 121) / 100f;
                    }
                }

                npc.velocity = npc.velocity.RotatedBy((0f - num19) * (float)npc.direction);
                npc.rotation -= num19 * (float)npc.direction;
                npc.ai[2] += 1f;
                if (npc.ai[2] >= (float)num15)
                {
                    npc.ai[0] = 5f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.netUpdate = true;
                }
            }
            else if (npc.ai[0] == 8f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                npc.velocity *= 0.98f;
                npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, 0f, 0.02f);
                if (npc.ai[2] == (float)(num11 - 30))
                    SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[2] == (float)(num11 - 30))
                    Projectile.NewProjectile(npc.GetSource_FromAI(), center, Vector2.Zero, ProjectileID.SharknadoBolt, 0, 0f, Main.myPlayer, 1f, npc.target + 1, (enrage || masterMode) ? 1 : 0);

                npc.ai[2] += 1f;
                if (npc.ai[2] >= (float)num11)
                {
                    npc.ai[0] = 5f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.netUpdate = true;
                }
            }
            else if (npc.ai[0] == 9f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                if (npc.ai[2] < (float)(num13 - 90))
                {
                    if (Collision.SolidCollision(npc.position, npc.width, npc.height))
                        npc.alpha += 15;
                    else
                        npc.alpha -= 15;

                    if (npc.alpha < 0)
                        npc.alpha = 0;

                    if (npc.alpha > 150)
                        npc.alpha = 150;
                }
                else if (npc.alpha < 255)
                {
                    npc.alpha += 4;
                    if (npc.alpha > 255)
                        npc.alpha = 255;
                }

                npc.velocity *= 0.98f;
                npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, 0f, 0.02f);
                if (npc.ai[2] == (float)(num13 - 60))
                    SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);

                npc.ai[2] += 1f;
                if (npc.ai[2] >= (float)num13)
                {
                    npc.ai[0] = 10f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.ai[3] = 0f;
                    npc.netUpdate = true;
                }
            }
            else if (npc.ai[0] == 10f && !player.dead)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                if (npc.alpha < 255)
                {
                    npc.alpha += 25;
                    if (npc.alpha > 255)
                        npc.alpha = 255;
                }

                if (npc.ai[1] == 0f)
                    npc.ai[1] = 360 * Math.Sign((center - player.Center).X);

                Vector2 desiredVelocity = Vector2.Normalize(player.Center + new Vector2(npc.ai[1], -200f) - center - npc.velocity) * num4;
                npc.SimpleFlyMovement(desiredVelocity, num3);
                int num36 = Math.Sign(player.Center.X - center.X);
                if (num36 != 0)
                {
                    if (npc.ai[2] == 0f && num36 != npc.direction)
                    {
                        npc.rotation += (float)Math.PI;
                        for (int l = 0; l < npc.oldPos.Length; l++)
                            npc.oldPos[l] = Vector2.Zero;
                    }

                    npc.direction = num36;
                    if (npc.spriteDirection != -npc.direction)
                        npc.rotation += (float)Math.PI;

                    npc.spriteDirection = -npc.direction;
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= (float)num2)
                {
                    int num37 = 0;
                    switch ((int)npc.ai[3])
                    {
                        case 0:
                        case 2:
                        case 3:
                        case 5:
                        case 6:
                        case 7:
                            num37 = 1;
                            break;
                        case 1:
                        case 4:
                        case 8:
                            num37 = 2;
                            break;
                    }

                    switch (num37)
                    {
                        case 1:
                            npc.ai[0] = 11f;
                            npc.ai[1] = 0f;
                            npc.ai[2] = 0f;
                            npc.velocity = Vector2.Normalize(player.Center - center) * num6;
                            npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X);
                            if (num36 != 0)
                            {
                                npc.direction = num36;
                                if (npc.spriteDirection == 1)
                                    npc.rotation += (float)Math.PI;

                                npc.spriteDirection = -npc.direction;
                            }
                            break;

                        case 2:
                            npc.ai[0] = 12f;
                            npc.ai[1] = 0f;
                            npc.ai[2] = 0f;
                            break;

                        case 3:
                            npc.ai[0] = 13f;
                            npc.ai[1] = 0f;
                            npc.ai[2] = 0f;
                            break;
                    }

                    npc.netUpdate = true;
                }
            }
            else if (npc.ai[0] == 11f)
            {
                // Set damage
                npc.damage = setDamage;

                npc.alpha -= 25;
                if (npc.alpha < 0)
                    npc.alpha = 0;

                int num38 = 7;
                for (int m = 0; m < num38; m++)
                {
                    Vector2 vector13 = (Vector2.Normalize(npc.velocity) * new Vector2((float)(npc.width + 50) / 2f, npc.height) * 0.75f).RotatedBy((double)(m - (num38 / 2 - 1)) * Math.PI / (double)(float)num38) + center;
                    Vector2 vector14 = ((float)(Main.rand.NextDouble() * MathHelper.Pi) - MathHelper.PiOver2).ToRotationVector2() * Main.rand.Next(3, 8);
                    int num39 = Dust.NewDust(vector13 + vector14, 0, 0, DustID.DungeonWater, vector14.X * 2f, vector14.Y * 2f, 100, default(Color), 1.4f);
                    Main.dust[num39].noGravity = true;
                    Main.dust[num39].noLight = true;
                    Main.dust[num39].velocity /= 4f;
                    Main.dust[num39].velocity -= npc.velocity;
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= (float)num5)
                {
                    npc.ai[0] = 10f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.ai[3] += 1f;
                    npc.netUpdate = true;
                }
            }
            else if (npc.ai[0] == 12f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                if (npc.alpha < 255)
                {
                    npc.alpha += 17;
                    if (npc.alpha > 255)
                        npc.alpha = 255;
                }

                npc.velocity *= 0.98f;
                npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, 0f, 0.02f);
                if (npc.ai[2] == (float)(num14 / 2))
                    SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[2] == (float)(num14 / 2))
                {
                    if (npc.ai[1] == 0f)
                        npc.ai[1] = 300 * Math.Sign((center - player.Center).X);

                    Vector2 vector15 = player.Center + new Vector2(0f - npc.ai[1], -200f);
                    Vector2 vector17 = (npc.Center = vector15);
                    center = vector17;
                    int num40 = Math.Sign(player.Center.X - center.X);
                    if (num40 != 0)
                    {
                        if (npc.ai[2] == 0f && num40 != npc.direction)
                        {
                            npc.rotation += (float)Math.PI;
                            for (int n = 0; n < npc.oldPos.Length; n++)
                                npc.oldPos[n] = Vector2.Zero;
                        }

                        npc.direction = num40;
                        if (npc.spriteDirection != -npc.direction)
                            npc.rotation += (float)Math.PI;

                        npc.spriteDirection = -npc.direction;
                    }
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= (float)num14)
                {
                    npc.ai[0] = 10f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.ai[3] += 1f;
                    if (npc.ai[3] >= 9f)
                        npc.ai[3] = 0f;

                    npc.netUpdate = true;
                }
            }
            else if (npc.ai[0] == 13f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                if (npc.ai[2] == 0f)
                    SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);

                npc.velocity = npc.velocity.RotatedBy((0f - num19) * (float)npc.direction);
                npc.rotation -= num19 * (float)npc.direction;
                npc.ai[2] += 1f;
                if (npc.ai[2] >= (float)num15)
                {
                    npc.ai[0] = 10f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.ai[3] += 1f;
                    npc.netUpdate = true;
                }
            }

            // Make him always able to take damage
            npc.dontTakeDamage = false;

            return false;
        }
    }
}
