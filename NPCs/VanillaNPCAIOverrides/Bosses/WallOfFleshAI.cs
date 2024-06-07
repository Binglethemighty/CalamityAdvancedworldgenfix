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
    public static class WallOfFleshAI
    {
        public const float LaserShootGateValue = 400f;
        public const float LaserShootTelegraphTime = LaserShootGateValue * 0.5f;
        public const float TotalLasersPerBarrage = 3f;
        public const float EnragedLaserFiringDuration = 300f;

        public static bool BuffedWallofFleshAI(NPC npc, Mod mod)
        {
            CalamityGlobalNPC calamityGlobalNPC = npc.Calamity();

            bool bossRush = BossRushEvent.BossRushActive;
            bool masterMode = Main.masterMode || bossRush;
            bool death = CalamityWorld.death || bossRush;

            // Despawn
            if (npc.position.X < 160f || npc.position.X > ((Main.maxTilesX - 10) * 16))
                npc.active = false;

            // Set Wall of Flesh variables
            if (npc.localAI[0] == 0f)
            {
                npc.localAI[0] = 1f;
                Main.wofDrawAreaBottom = -1;
                Main.wofDrawAreaTop = -1;
            }

            // Percent life remaining
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Clamp life ratio to prevent bad velocity math.
            lifeRatio = MathHelper.Clamp(lifeRatio, 0f, 1f);

            // Phases based on HP
            bool phase2 = lifeRatio < 0.66f;
            bool phase3 = lifeRatio < 0.33f;

            if (Main.getGoodWorld && Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(180))
            {
                if (NPC.CountNPCS(NPCID.FireImp) < 4)
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        int targetTileX = (int)(npc.Center.X / 16f);
                        int targetTileY = (int)(npc.Center.Y / 16f);
                        if (npc.target >= 0)
                        {
                            targetTileX = (int)(Main.player[npc.target].Center.X / 16f);
                            targetTileY = (int)(Main.player[npc.target].Center.Y / 16f);
                        }

                        targetTileX += Main.rand.Next(-50, 51);
                        for (targetTileY += Main.rand.Next(-50, 51); targetTileY < Main.maxTilesY - 10 && !WorldGen.SolidTile(targetTileX, targetTileY); targetTileY++)
                        {
                        }

                        targetTileY--;
                        if (!WorldGen.SolidTile(targetTileX, targetTileY))
                        {
                            int impSpawn = NPC.NewNPC(npc.GetSource_FromAI(), targetTileX * 16 + 8, targetTileY * 16, NPCID.FireImp);
                            if (Main.netMode == NetmodeID.Server && impSpawn < Main.maxNPCs)
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, impSpawn);

                            break;
                        }
                    }
                }
            }

            // Start leech spawning based on HP
            npc.ai[1] += 1f;
            if (npc.ai[2] == 0f)
            {
                if (masterMode)
                    npc.ai[1] += 2f;

                if (phase2)
                    npc.ai[1] += 1f;
                if (phase3)
                    npc.ai[1] += 1f;
                if (bossRush)
                    npc.ai[1] += 3f;
                if (CalamityWorld.LegendaryMode)
                    npc.ai[1] += 9f;

                if (npc.ai[1] > 2700f)
                    npc.ai[2] = 1f;
            }

            // Leech spawn
            if (npc.ai[2] > 0f && npc.ai[1] > 60f)
            {
                int leechAmt = phase3 ? 3 : 2;

                npc.ai[2] += 1f;
                npc.ai[1] = 0f;
                if (npc.ai[2] > leechAmt)
                    npc.ai[2] = 0f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (NPC.CountNPCS(NPCID.LeechHead) < 10)
                    {
                        int leechSpawn = NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X), (int)(npc.Center.Y + 20f), NPCID.LeechHead, 1);
                        int leechVelocity = masterMode ? 12 : 9;
                        Main.npc[leechSpawn].velocity.X = npc.direction * leechVelocity;
                    }

                    if (phase2 || masterMode)
                    {
                        // Get target vector
                        Vector2 projectileVelocity = (Main.player[npc.target].Center - npc.Center).SafeNormalize(Vector2.UnitY) * npc.velocity.Length();
                        Vector2 projectileSpawn = npc.Center + projectileVelocity.SafeNormalize(Vector2.UnitY) * 50f;

                        int damage = npc.GetProjectileDamage(ProjectileID.DemonSickle);
                        int proj = Projectile.NewProjectile(npc.GetSource_FromAI(), projectileSpawn, projectileVelocity, ProjectileID.DemonSickle, damage, 0f, Main.myPlayer, 0f, projectileVelocity.Length() * 3f);
                        Main.projectile[proj].timeLeft = 600;
                        Main.projectile[proj].tileCollide = false;

                        if (masterMode)
                        {
                            float fireballVelocity = npc.velocity.Length() + 3f;
                            projectileVelocity = projectileVelocity.SafeNormalize(Vector2.UnitY) * fireballVelocity;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                int type = ProjectileID.Fireball;
                                damage = npc.GetProjectileDamage(type);
                                int numProj = 3;
                                int spread = 15;
                                float rotation = MathHelper.ToRadians(spread);
                                fireballVelocity *= 0.1f;
                                for (int j = 0; j < numProj; j++)
                                {
                                    Vector2 randomVelocity = Main.rand.NextVector2CircularEdge(fireballVelocity, fireballVelocity);
                                    Vector2 perturbedSpeed = projectileVelocity.RotatedBy(MathHelper.Lerp(-rotation, rotation, j / (float)(numProj - 1))) + randomVelocity;
                                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + perturbedSpeed.SafeNormalize(Vector2.UnitY) * 50f, perturbedSpeed, type, damage, 0f, Main.myPlayer);
                                }
                            }
                        }
                    }
                }
            }

            // Play sound
            npc.localAI[3] += 1f;
            if (npc.localAI[3] >= (600 + Main.rand.Next(1000)))
            {
                npc.localAI[3] = -Main.rand.Next(200);
                SoundEngine.PlaySound(SoundID.NPCDeath10, npc.Center);
            }

            // Set whoAmI variable
            Main.wofNPCIndex = npc.whoAmI;

            // Set eye positions
            int currentEyeTileCenterX = (int)(npc.position.X / 16f);
            int currentEyeTileWidthX = (int)((npc.position.X + npc.width) / 16f);
            int currentEyeTileHeightY = (int)(npc.Center.Y / 16f);
            int eyeMovementTries = 0;
            int eyeMovementTileY = currentEyeTileHeightY + 7;
            while (eyeMovementTries < 15 && eyeMovementTileY > Main.UnderworldLayer)
            {
                eyeMovementTileY++;
                for (int eyeMovementTileX = currentEyeTileCenterX; eyeMovementTileX <= currentEyeTileWidthX; eyeMovementTileX++)
                {
                    try
                    {
                        if (WorldGen.SolidTile(eyeMovementTileX, eyeMovementTileY) || Main.tile[eyeMovementTileX, eyeMovementTileY].LiquidAmount > 0)
                            eyeMovementTries++;
                    }
                    catch
                    { eyeMovementTries += 15; }
                }
            }
            eyeMovementTileY += 4;
            if (Main.wofDrawAreaBottom == -1)
                Main.wofDrawAreaBottom = eyeMovementTileY * 16;
            else if (Main.wofDrawAreaBottom > eyeMovementTileY * 16)
            {
                Main.wofDrawAreaBottom--;
                if (Main.wofDrawAreaBottom < eyeMovementTileY * 16)
                    Main.wofDrawAreaBottom = eyeMovementTileY * 16;
            }
            else if (Main.wofDrawAreaBottom < eyeMovementTileY * 16)
            {
                Main.wofDrawAreaBottom++;
                if (Main.wofDrawAreaBottom > eyeMovementTileY * 16)
                    Main.wofDrawAreaBottom = eyeMovementTileY * 16;
            }

            eyeMovementTries = 0;
            eyeMovementTileY = currentEyeTileHeightY - 7;
            while (eyeMovementTries < 15 && eyeMovementTileY < Main.maxTilesY - 10)
            {
                eyeMovementTileY--;
                for (int i = currentEyeTileCenterX; i <= currentEyeTileWidthX; i++)
                {
                    try
                    {
                        if (WorldGen.SolidTile(i, eyeMovementTileY) || Main.tile[i, eyeMovementTileY].LiquidAmount > 0)
                            eyeMovementTries++;
                    }
                    catch
                    { eyeMovementTries += 15; }
                }
            }
            eyeMovementTileY -= 4;
            if (Main.wofDrawAreaTop == -1)
                Main.wofDrawAreaTop = eyeMovementTileY * 16;
            else if (Main.wofDrawAreaTop > eyeMovementTileY * 16)
            {
                Main.wofDrawAreaTop--;
                if (Main.wofDrawAreaTop < eyeMovementTileY * 16)
                    Main.wofDrawAreaTop = eyeMovementTileY * 16;
            }
            else if (Main.wofDrawAreaTop < eyeMovementTileY * 16)
            {
                Main.wofDrawAreaTop++;
                if (Main.wofDrawAreaTop > eyeMovementTileY * 16)
                    Main.wofDrawAreaTop = eyeMovementTileY * 16;
            }

            // Set Y position
            float mouthYPosition = (Main.wofDrawAreaBottom + Main.wofDrawAreaTop) / 2 - npc.height / 2;
            int worldBottomTileY = (Main.maxTilesY - 180) * 16;
            if (mouthYPosition < worldBottomTileY)
                mouthYPosition = worldBottomTileY;
            npc.position.Y = mouthYPosition;

            float targetPosition = Main.player[npc.target].Center.X;
            float npcPosition = npc.Center.X;

            // Speed up if target is too far, slow down if too close
            float distanceFromTarget;
            if (npc.velocity.X < 0f)
                distanceFromTarget = npcPosition - targetPosition;
            else
                distanceFromTarget = targetPosition - npcPosition;

            float halfAverageScreenWidth = 960f;
            float distanceBeforeSlowingDown = 400f;
            float timeBeforeEnrage = (masterMode ? 150f : 600f) - (death ? (masterMode ? 130f : 390f) * (1f - lifeRatio) : 0f);
            float speedMult = 1f;

            if (bossRush)
                timeBeforeEnrage *= 0.25f;

            if (calamityGlobalNPC.newAI[0] < timeBeforeEnrage)
            {
                if (distanceFromTarget > halfAverageScreenWidth)
                {
                    speedMult += (distanceFromTarget - halfAverageScreenWidth) * 0.001f;
                    calamityGlobalNPC.newAI[0] += 1f;

                    // Enrage after 10 seconds of target being off screen
                    if (calamityGlobalNPC.newAI[0] >= timeBeforeEnrage)
                    {
                        calamityGlobalNPC.newAI[1] = 1f;

                        // Tell eyes to fire different lasers
                        npc.ai[3] = 1f;

                        // Play roar sound on players nearby
                        if (Main.player[Main.myPlayer].active && !Main.player[Main.myPlayer].dead && Vector2.Distance(Main.player[Main.myPlayer].Center, npc.Center) < 2800f)
                            SoundEngine.PlaySound(SoundID.NPCDeath10 with { Pitch = SoundID.NPCDeath10.Pitch - 0.25f }, Main.player[Main.myPlayer].Center);
                    }
                }
                else if (distanceFromTarget < distanceBeforeSlowingDown)
                    speedMult += (distanceFromTarget - distanceBeforeSlowingDown) * 0.002f;

                if (distanceFromTarget < halfAverageScreenWidth)
                {
                    if (calamityGlobalNPC.newAI[0] > 0f)
                        calamityGlobalNPC.newAI[0] -= 1f;
                }

                speedMult = MathHelper.Clamp(speedMult, 0.75f, 2f);
            }

            // Enrage if target is off screen for too long
            if (calamityGlobalNPC.newAI[1] == 1f)
            {
                // Triple speed
                speedMult = 3.25f;

                // Return to normal if very close to target
                if (distanceFromTarget < distanceBeforeSlowingDown)
                {
                    calamityGlobalNPC.newAI[0] = 0f;
                    calamityGlobalNPC.newAI[1] = 0f;
                    npc.ai[3] = 0f;
                }
            }

            calamityGlobalNPC.CurrentlyEnraged = distanceFromTarget > halfAverageScreenWidth || npc.ai[3] == 1f;

            if (bossRush)
                speedMult += 0.2f;

            float masterModeVelocityBoost = 0f;
            if (masterMode)
            {
                float velocityBoostStartDistance = 480f;
                float velocityBoostMaxDistance = velocityBoostStartDistance * 2f;
                float distanceFromTargetX = Math.Abs(npc.Center.X - Main.player[npc.target].Center.X);
                float lerpAmount = MathHelper.Clamp((distanceFromTargetX - velocityBoostStartDistance) / velocityBoostMaxDistance, 0f, 1f);
                masterModeVelocityBoost = MathHelper.Lerp(0f, 8f, lerpAmount);
            }

            // NOTE: Max velocity is 8 in Expert Mode
            // NOTE: Max velocity is 9 in For The Worthy

            float velocityBoost = 4f * (1f - lifeRatio);
            float velocityX = (bossRush ? 7f : death ? 3.5f : 2f) + masterModeVelocityBoost + velocityBoost;
            velocityX *= speedMult;

            if (masterMode)
                velocityX *= 1.2f;

            if (Main.getGoodWorld)
            {
                velocityX *= 1.1f;
                velocityX += 0.1f;
            }

            // NOTE: Values below are based on Rev Mode only!
            // Max velocity without enrage is 12
            // Min velocity is 1.5
            // Max velocity with enrage is 18

            // Set X velocity
            if (npc.velocity.X == 0f)
            {
                npc.TargetClosest();
                if (Main.player[npc.target].dead)
                {
                    float wallVelocity = float.PositiveInfinity;
                    int wallDirection = 0;
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player player = Main.player[npc.target];
                        if (player.active)
                        {
                            float playerDist = npc.Distance(player.Center);
                            if (wallVelocity > playerDist)
                            {
                                wallVelocity = playerDist;
                                wallDirection = (npc.Center.X < player.Center.X) ? 1 : -1;
                            }
                        }
                    }

                    npc.direction = wallDirection;
                }

                npc.velocity.X = npc.direction;
            }

            if (npc.velocity.X < 0f)
            {
                npc.velocity.X = -velocityX;
                npc.direction = -1;
            }
            else
            {
                npc.velocity.X = velocityX;
                npc.direction = 1;
            }

            if (Main.player[npc.target].dead || !Main.player[npc.target].gross)
                npc.TargetClosest_WOF();

            if (Main.player[npc.target].dead)
            {
                npc.localAI[1] += 0.0055555557f;
                if (npc.localAI[1] >= 1f)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath10, npc.Center);
                    npc.life = 0;
                    npc.active = false;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NetMessage.SendData(MessageID.DamageNPC, -1, -1, null, npc.whoAmI, -1f);

                    return false;
                }
            }
            else
                npc.localAI[1] = MathHelper.Clamp(npc.localAI[1] - 1f / 30f, 0f, 1f);

            // Direction
            npc.spriteDirection = npc.direction;
            Vector2 mouthLocation = npc.Center;
            float mouthTargetX = Main.player[npc.target].Center.X - mouthLocation.X;
            float mouthTargetY = Main.player[npc.target].Center.Y - mouthLocation.Y;
            float mouthTargetDist = (float)Math.Sqrt(mouthTargetX * mouthTargetX + mouthTargetY * mouthTargetY);
            mouthTargetX *= mouthTargetDist;
            mouthTargetY *= mouthTargetDist;

            // Rotation based on direction
            if (npc.direction > 0)
            {
                if (Main.player[npc.target].Center.X > npc.Center.X)
                    npc.rotation = (float)Math.Atan2(-mouthTargetY, -mouthTargetX) + MathHelper.Pi;
                else
                    npc.rotation = 0f;
            }
            else if (Main.player[npc.target].Center.X < npc.Center.X)
                npc.rotation = (float)Math.Atan2(mouthTargetY, mouthTargetX) + MathHelper.Pi;
            else
                npc.rotation = 0f;

            // Expert hungry respawn over time
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Range of 2 to 11
                float spawnBoost = death ? 1f : (float)Math.Ceiling(lifeRatio * 10f);
                int chance = (int)(1f + spawnBoost);

                // Range of 4 to 121
                chance *= chance;

                // Range of 23 to 134
                chance = (chance * 19 + 400) / 20;

                // Range of 32 to 59
                if (chance < 60)
                    chance = (chance * 3 + 60) / 4;

                // Range of 64 to 268
                chance *= 2;

                if (bossRush)
                    chance /= 4;
                else if (masterMode)
                    chance /= 2;

                if (chance < 2)
                    chance = 2;

                if (Main.rand.NextBool(chance))
                {
                    int hungryAmt = 0;
                    float[] array = new float[10];
                    for (int j = 0; j < Main.maxNPCs; j++)
                    {
                        if (hungryAmt < 10 && Main.npc[j].active && Main.npc[j].type == NPCID.TheHungry)
                        {
                            array[hungryAmt] = Main.npc[j].ai[0];
                            hungryAmt++;
                        }
                    }

                    int maxValue = 1 + hungryAmt * 2;
                    if (masterMode)
                        maxValue /= 2;

                    if (maxValue < 2)
                        maxValue = 2;

                    if (hungryAmt < 10 && Main.rand.Next(maxValue) <= 1)
                    {
                        int spawnHungryControl = -1;
                        for (int k = 0; k < 1000; k++)
                        {
                            int randomHungrySpawnValue = Main.rand.Next(10);
                            float hungryArrayValue = randomHungrySpawnValue * 0.1f - 0.05f;
                            bool shouldRespawnHungry = true;
                            for (int i = 0; i < hungryAmt; i++)
                            {
                                if (hungryArrayValue == array[i])
                                {
                                    shouldRespawnHungry = false;
                                    break;
                                }
                            }
                            if (shouldRespawnHungry)
                            {
                                spawnHungryControl = randomHungrySpawnValue;
                                break;
                            }
                        }
                        if (spawnHungryControl >= 0)
                        {
                            int hungryRespawns = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.position.X, (int)mouthYPosition, NPCID.TheHungry, npc.whoAmI);
                            Main.npc[hungryRespawns].ai[0] = spawnHungryControl * 0.1f - 0.05f;
                        }
                    }
                }
            }

            // Spawn eyes and hungries
            if (npc.localAI[0] == 1f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                npc.localAI[0] = 2f;

                mouthYPosition = (Main.wofDrawAreaBottom + Main.wofDrawAreaTop) / 2;
                mouthYPosition = (mouthYPosition + Main.wofDrawAreaTop) / 2f;
                int eyeSpawn = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.position.X, (int)mouthYPosition, NPCID.WallofFleshEye, npc.whoAmI);
                Main.npc[eyeSpawn].ai[0] = 1f;
                if (masterMode)
                    Main.npc[eyeSpawn].ai[3] = 1f;

                mouthYPosition = (Main.wofDrawAreaBottom + Main.wofDrawAreaTop) / 2;
                mouthYPosition = (mouthYPosition + Main.wofDrawAreaBottom) / 2f;
                eyeSpawn = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.position.X, (int)mouthYPosition, NPCID.WallofFleshEye, npc.whoAmI);
                Main.npc[eyeSpawn].ai[0] = -1f;
                if (masterMode)
                    Main.npc[eyeSpawn].ai[3] = -1f;

                mouthYPosition = (Main.wofDrawAreaBottom + Main.wofDrawAreaTop) / 2;
                mouthYPosition = (mouthYPosition + Main.wofDrawAreaBottom) / 2f;

                int maxHungries = masterMode ? 15 : 11;
                for (int j = 0; j < maxHungries; j++)
                {
                    int hungrySpawn = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.position.X, (int)mouthYPosition, NPCID.TheHungry, npc.whoAmI);
                    Main.npc[hungrySpawn].ai[0] = j * 0.1f - 0.05f;
                }
            }

            return false;
        }

        public static bool BuffedHungryAI(NPC npc, Mod mod)
        {
            if (npc.justHit)
                npc.ai[1] = 10f;

            if (Main.wofNPCIndex < 0)
            {
                npc.active = false;
                return false;
            }

            npc.TargetClosest();
            float acceleration = Main.masterMode ? 0.15f : 0.12f;
            float distanceFromWall = 300f;
            npc.damage = npc.defDamage;
            npc.defense = npc.defDefense;
            if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.5)
            {
                npc.damage = Main.masterMode ? 180 : 120;
                npc.defense = 30;
                acceleration += Main.masterMode ? 0.1f : 0.08f;
            }
            else if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.75)
            {
                npc.damage = Main.masterMode ? 135 : 90;
                npc.defense = 20;
                acceleration += Main.masterMode ? 0.05f : 0.04f;
            }

            if (npc.whoAmI % 4 == 0)
                distanceFromWall *= 1.75f;

            if (npc.whoAmI % 4 == 1)
                distanceFromWall *= 1.5f;

            if (npc.whoAmI % 4 == 2)
                distanceFromWall *= 1.25f;

            if (npc.whoAmI % 3 == 0)
                distanceFromWall *= 1.5f;

            if (npc.whoAmI % 3 == 1)
                distanceFromWall *= 1.25f;

            distanceFromWall *= 0.75f;

            float num404 = Main.npc[Main.wofNPCIndex].Center.X;
            float y3 = Main.npc[Main.wofNPCIndex].position.Y;
            float num405 = Main.wofDrawAreaBottom - Main.wofDrawAreaTop;
            y3 = (float)Main.wofDrawAreaTop + num405 * npc.ai[0];
            npc.ai[2] += 1f;
            if (npc.ai[2] > 100f)
            {
                distanceFromWall = (int)(distanceFromWall * 1.3f);
                if (npc.ai[2] > 200f)
                    npc.ai[2] = 0f;
            }

            Vector2 vector40 = new Vector2(num404, y3);
            float num406 = Main.player[npc.target].Center.X - (float)(npc.width / 2) - vector40.X;
            float num407 = Main.player[npc.target].Center.Y - (float)(npc.height / 2) - vector40.Y;
            float num408 = (float)Math.Sqrt(num406 * num406 + num407 * num407);
            if (npc.ai[1] == 0f)
            {
                if (num408 > distanceFromWall)
                {
                    num408 = distanceFromWall / num408;
                    num406 *= num408;
                    num407 *= num408;
                }

                if (npc.position.X < num404 + num406)
                {
                    npc.velocity.X += acceleration;
                    if (npc.velocity.X < 0f && num406 > 0f)
                        npc.velocity.X += acceleration * 2.5f;
                }
                else if (npc.position.X > num404 + num406)
                {
                    npc.velocity.X -= acceleration;
                    if (npc.velocity.X > 0f && num406 < 0f)
                        npc.velocity.X -= acceleration * 2.5f;
                }

                if (npc.position.Y < y3 + num407)
                {
                    npc.velocity.Y += acceleration;
                    if (npc.velocity.Y < 0f && num407 > 0f)
                        npc.velocity.Y += acceleration * 2.5f;
                }
                else if (npc.position.Y > y3 + num407)
                {
                    npc.velocity.Y -= acceleration;
                    if (npc.velocity.Y > 0f && num407 < 0f)
                        npc.velocity.Y -= acceleration * 2.5f;
                }

                float maxVelocity = 4f;
                if (Main.wofNPCIndex >= 0)
                {
                    float velocityBoost = 1.5f;
                    float wallLifeRatio = Main.npc[Main.wofNPCIndex].life / (float)Main.npc[Main.wofNPCIndex].lifeMax;
                    if (wallLifeRatio < 0.75f)
                        velocityBoost += 0.7f;

                    if (wallLifeRatio < 0.5f)
                        velocityBoost += 0.7f;

                    if (wallLifeRatio < 0.25f)
                        velocityBoost += 0.9f;

                    if (wallLifeRatio < 0.1f)
                        velocityBoost += 0.9f;

                    velocityBoost *= Main.masterMode ? 1.5f : 1.25f;
                    velocityBoost += 0.3f;
                    maxVelocity += velocityBoost * 0.35f;
                    if (npc.Center.X < Main.npc[Main.wofNPCIndex].Center.X && Main.npc[Main.wofNPCIndex].velocity.X > 0f)
                        maxVelocity += 6f;

                    if (npc.Center.X > Main.npc[Main.wofNPCIndex].Center.X && Main.npc[Main.wofNPCIndex].velocity.X < 0f)
                        maxVelocity += 6f;
                }

                if (npc.velocity.X > maxVelocity)
                    npc.velocity.X = maxVelocity;

                if (npc.velocity.X < -maxVelocity)
                    npc.velocity.X = -maxVelocity;

                if (npc.velocity.Y > maxVelocity)
                    npc.velocity.Y = maxVelocity;

                if (npc.velocity.Y < -maxVelocity)
                    npc.velocity.Y = -maxVelocity;
            }
            else if (npc.ai[1] > 0f)
                npc.ai[1] -= 1f;
            else
                npc.ai[1] = 0f;

            if (num406 > 0f)
            {
                npc.spriteDirection = 1;
                npc.rotation = (float)Math.Atan2(num407, num406);
            }

            if (num406 < 0f)
            {
                npc.spriteDirection = -1;
                npc.rotation = (float)Math.Atan2(num407, num406) + MathHelper.Pi;
            }

            Lighting.AddLight(npc.Center, 0.3f, 0.2f, 0.1f);

            return false;
        }

        public static bool BuffedWallofFleshEyeAI(NPC npc, Mod mod)
        {
            CalamityGlobalNPC calamityGlobalNPC = npc.Calamity();

            bool bossRush = BossRushEvent.BossRushActive;
            bool masterMode = Main.masterMode || bossRush;
            bool death = CalamityWorld.death || bossRush;

            // Avoid cheap bullshit
            npc.damage = 0;

            // Despawn
            if (Main.wofNPCIndex < 0)
            {
                npc.active = false;
                return false;
            }

            npc.realLife = Main.wofNPCIndex;

            if (Main.npc[Main.wofNPCIndex].life > 0)
                npc.life = Main.npc[Main.wofNPCIndex].life;

            // Percent life remaining
            float lifeRatio = Main.npc[Main.wofNPCIndex].life / (float)Main.npc[Main.wofNPCIndex].lifeMax;

            // Get a target
            if (npc.target < 0 || npc.target == Main.maxPlayers || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.target = Main.npc[Main.wofNPCIndex].target;

            // Velocity, direction, and position
            bool shouldFireLasers = true;
            float phase2LifeRatio = 0.5f;
            bool masterModeDetach = lifeRatio < phase2LifeRatio && masterMode;
            if (!masterModeDetach)
            {
                npc.position.X = Main.npc[Main.wofNPCIndex].position.X;
                npc.direction = Main.npc[Main.wofNPCIndex].direction;
                npc.spriteDirection = npc.direction;

                float expectedPosition = (Main.wofDrawAreaBottom + Main.wofDrawAreaTop) / 2;
                if (npc.ai[0] > 0f)
                    expectedPosition = (expectedPosition + Main.wofDrawAreaTop) / 2f;
                else
                    expectedPosition = (expectedPosition + Main.wofDrawAreaBottom) / 2f;
                expectedPosition -= npc.height / 2;

                bool belowExpectedPosition = npc.position.Y > expectedPosition + 1f;
                bool aboveExpectedPosition = npc.position.Y < expectedPosition - 1f;
                if (belowExpectedPosition)
                {
                    float distanceBelowExpectedPosition = npc.position.Y - expectedPosition + 1f;
                    float movementVelocity = MathHelper.Clamp(distanceBelowExpectedPosition * 0.03125f, 1f, 5f);
                    npc.velocity.Y = -movementVelocity;
                }
                else if (aboveExpectedPosition)
                {
                    float distanceAboveExpectedPosition = expectedPosition - 1f - npc.position.Y;
                    float movementVelocity = MathHelper.Clamp(distanceAboveExpectedPosition * 0.03125f, 1f, 5f);
                    npc.velocity.Y = movementVelocity;
                }
                else
                {
                    npc.velocity.Y = 0f;
                    npc.position.Y = expectedPosition;
                }
            }
            else
            {
                float distanceAboveTarget = 240f * npc.ai[0];
                float distanceAwayFromTargetX = 560f;
                float distanceAwayFromTargetXLeeway = 40f;
                float distanceAwayFromTargetY = Main.player[npc.target].Center.Y - npc.Center.Y;
                float distanceAwayFromTargetYLeeway = 40f;
                float absoluteDistanceX = Math.Abs(Main.player[npc.target].Center.X - npc.Center.X);
                bool tooFarX = absoluteDistanceX > distanceAwayFromTargetX + distanceAwayFromTargetXLeeway || absoluteDistanceX < distanceAwayFromTargetX - distanceAwayFromTargetXLeeway;
                bool tooFarY = distanceAwayFromTargetY > distanceAboveTarget + distanceAwayFromTargetYLeeway || distanceAwayFromTargetY < distanceAboveTarget - distanceAwayFromTargetYLeeway;
                bool tooFar = tooFarX || tooFarY;
                Vector2 hoverDestination = Main.player[npc.target].Center - Vector2.UnitY * distanceAboveTarget + Vector2.UnitX * distanceAwayFromTargetX * npc.ai[3];
                if (tooFar)
                {
                    Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 16f;
                    npc.SimpleFlyMovement(idealVelocity, 0.36f);
                }

                if (npc.Distance(Main.player[npc.target].Center) < distanceAwayFromTargetX || npc.Distance(hoverDestination) > 120f)
                    shouldFireLasers = false;

                float playerLocation = npc.Center.X - Main.player[npc.target].Center.X;
                npc.direction = playerLocation < 0f ? 1 : -1;
                npc.spriteDirection = npc.direction;

                if (npc.ai[1] == 0f)
                {
                    npc.ai[1] = 1f;
                    SoundEngine.PlaySound(SoundID.NPCDeath12, npc.Center);
                    for (int i = 0; i < 100; i++)
                    {
                        int dust = Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, npc.velocity.X, npc.velocity.Y);
                        Main.dust[dust].scale = Main.rand.NextFloat(1.5f, 4f);
                        Main.dust[dust].velocity *= Main.rand.NextFloat(0.5f, 1.5f);
                    }
                }

                // 50% chance to change Y position
                float eyePositionRandomChangeGateValue = MathHelper.Lerp(death ? 180f : 240f, death ? 480f : 720f, lifeRatio / phase2LifeRatio);
                if (npc.ai[2] >= eyePositionRandomChangeGateValue)
                {
                    npc.ai[2] = 0f;
                    npc.ai[0] = Main.rand.NextBool() ? 1f : -1f;
                    npc.netUpdate = true;
                }
                npc.ai[2] += 1f;
            }

            Vector2 eyeLocation = npc.Center;
            float predictionAmount = MathHelper.Lerp(0f, 20f, (float)Math.Sqrt(1f - lifeRatio));
            Vector2 lookAt = Main.player[npc.target].Center + (bossRush ? (Main.player[npc.target].velocity * predictionAmount) : Vector2.Zero);
            float eyeTargetX = lookAt.X - eyeLocation.X;
            float eyeTargetY = lookAt.Y - eyeLocation.Y;
            float wallVelocity = (float)Math.Sqrt(eyeTargetX * eyeTargetX + eyeTargetY * eyeTargetY);
            eyeTargetX *= wallVelocity;
            eyeTargetY *= wallVelocity;

            // Rotation based on direction and whether to fire lasers or not
            if (npc.direction > 0)
            {
                if (Main.player[npc.target].Center.X > npc.Center.X)
                {
                    npc.rotation = (float)Math.Atan2(-eyeTargetY, -eyeTargetX) + MathHelper.Pi;
                }
                else
                {
                    npc.rotation = 0f;
                    if (!masterModeDetach)
                        shouldFireLasers = false;
                }
            }
            else if (Main.player[npc.target].Center.X < npc.Center.X)
            {
                npc.rotation = (float)Math.Atan2(eyeTargetY, eyeTargetX) + MathHelper.Pi;
            }
            else
            {
                npc.rotation = 0f;
                if (!masterModeDetach)
                    shouldFireLasers = false;
            }

            // Fire lasers
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                bool charging = Main.npc[Main.wofNPCIndex].ai[3] == 1f;

                // Set up enraged laser firing timer
                float enragedLaserTimer = EnragedLaserFiringDuration;
                if (charging)
                    npc.localAI[3] = enragedLaserTimer;

                bool fireEnragedLasers = npc.localAI[3] > 0f && npc.localAI[3] < enragedLaserTimer;

                // Decrement the enraged laser timer
                if (npc.localAI[3] > 0f)
                {
                    npc.localAI[3] -= 1f;

                    // Stop firing normal lasers when enrage ends
                    if (npc.localAI[3] == 0f)
                        npc.localAI[1] = 0f;
                }

                float shootBoost = fireEnragedLasers ? (death ? 5f : 4f) : (death ? 3f : 3f * (1f - lifeRatio));
                npc.localAI[1] += 1f + shootBoost;

                bool canHit = Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height);

                if (npc.localAI[2] == 0f)
                {
                    if (npc.localAI[1] > LaserShootGateValue)
                    {
                        npc.localAI[2] = 1f;
                        npc.localAI[1] = 0f;
                    }
                }
                else if (npc.localAI[1] > 45f && (canHit || masterModeDetach) && !charging)
                {
                    npc.localAI[1] = 0f;
                    npc.localAI[2] += 1f;
                    if (npc.localAI[2] >= TotalLasersPerBarrage + 1f)
                    {
                        npc.localAI[2] = 0f;
                        npc.TargetClosest();
                    }

                    if (shouldFireLasers)
                    {
                        float velocity = (fireEnragedLasers ? 3f : 4f) + shootBoost;
                        int projectileType = ProjectileID.EyeLaser;
                        int damage = npc.GetProjectileDamage(projectileType);

                        Vector2 projectileVelocity = (lookAt - npc.Center).SafeNormalize(Vector2.UnitY) * velocity;
                        Vector2 projectileSpawn = npc.Center + projectileVelocity.SafeNormalize(Vector2.UnitY) * 150f;

                        int proj = Projectile.NewProjectile(npc.GetSource_FromAI(), projectileSpawn, projectileVelocity, projectileType, damage, 0f, Main.myPlayer, 1f, 0f);
                        Main.projectile[proj].timeLeft = 900;

                        if (!canHit)
                            Main.projectile[proj].tileCollide = false;
                    }
                }
            }

            return false;
        }

        public static bool VanillaWallofFleshAI(NPC npc, Mod mod)
        {
            if (npc.position.X < 160f || npc.position.X > (float)((Main.maxTilesX - 10) * 16))
                npc.active = false;

            if (npc.localAI[0] == 0f)
            {
                npc.localAI[0] = 1f;
                Main.wofDrawAreaBottom = -1;
                Main.wofDrawAreaTop = -1;
            }

            if (Main.getGoodWorld && Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(180) && NPC.CountNPCS(NPCID.FireImp) < 4)
            {
                int num349 = 1;
                for (int num350 = 0; num350 < num349; num350++)
                {
                    int num351 = 1000;
                    for (int num352 = 0; num352 < num351; num352++)
                    {
                        int num353 = (int)(npc.Center.X / 16f);
                        int num354 = (int)(npc.Center.Y / 16f);
                        if (npc.target >= 0)
                        {
                            num353 = (int)(Main.player[npc.target].Center.X / 16f);
                            num354 = (int)(Main.player[npc.target].Center.Y / 16f);
                        }

                        num353 += Main.rand.Next(-50, 51);
                        for (num354 += Main.rand.Next(-50, 51); num354 < Main.maxTilesY - 10 && !WorldGen.SolidTile(num353, num354); num354++)
                        {
                        }

                        num354--;
                        if (!WorldGen.SolidTile(num353, num354))
                        {
                            int num355 = NPC.NewNPC(npc.GetSource_FromAI(), num353 * 16 + 8, num354 * 16, 24);
                            if (Main.netMode == NetmodeID.Server && num355 < Main.maxNPCs)
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, num355);

                            break;
                        }
                    }
                }
            }

            npc.ai[1] += 1f;
            if (npc.ai[2] == 0f)
            {
                if (Main.masterMode)
                    npc.ai[1] += 2f;

                if ((double)npc.life < (double)npc.lifeMax * 0.5)
                    npc.ai[1] += 1f;

                if ((double)npc.life < (double)npc.lifeMax * 0.2)
                    npc.ai[1] += 1f;

                if (npc.ai[1] > 2700f)
                    npc.ai[2] = 1f;
            }

            int num356 = Main.masterMode ? 30 : 60;
            if (npc.ai[2] > 0f && npc.ai[1] > (float)num356)
            {
                int num357 = 3;
                if ((double)npc.life < (double)npc.lifeMax * 0.3)
                    num357++;

                npc.ai[2] += 1f;
                npc.ai[1] = 0f;
                if (npc.ai[2] > (float)num357)
                    npc.ai[2] = 0f;

                if (Main.netMode != NetmodeID.MultiplayerClient && NPC.CountNPCS(NPCID.LeechHead) < 10)
                {
                    int num358 = NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X), (int)(npc.Center.Y + 20f), NPCID.LeechHead, 1);
                    int leechVelocity = Main.masterMode ? 12 : 8;
                    Main.npc[num358].velocity.X = npc.direction * leechVelocity;
                }
            }

            npc.localAI[3] += 1f;
            if (npc.localAI[3] >= (float)(600 + Main.rand.Next(1000)))
            {
                npc.localAI[3] = -Main.rand.Next(200);
                SoundEngine.PlaySound(SoundID.NPCDeath10, npc.Center);
            }

            int num359 = Main.UnderworldLayer + 10;
            int num360 = num359 + 70;
            Main.wofNPCIndex = npc.whoAmI;
            int num361 = (int)(npc.position.X / 16f);
            int num362 = (int)((npc.position.X + (float)npc.width) / 16f);
            int num363 = (int)(npc.Center.Y / 16f);
            int num364 = 0;
            int num365 = num363 + 7;
            while (num364 < 15 && num365 > Main.UnderworldLayer)
            {
                num365++;
                if (num365 > Main.maxTilesY - 10)
                {
                    num365 = Main.maxTilesY - 10;
                    break;
                }

                if (num365 < num359)
                    continue;

                for (int num366 = num361; num366 <= num362; num366++)
                {
                    try
                    {
                        if (WorldGen.InWorld(num366, num365, 2) && (WorldGen.SolidTile(num366, num365) || Main.tile[num366, num365].LiquidAmount > 0))
                            num364++;
                    }
                    catch
                    {
                        num364 += 15;
                    }
                }
            }

            num365 += 4;
            if (Main.wofDrawAreaBottom == -1)
            {
                Main.wofDrawAreaBottom = num365 * 16;
            }
            else if (Main.wofDrawAreaBottom > num365 * 16)
            {
                Main.wofDrawAreaBottom--;
                if (Main.wofDrawAreaBottom < num365 * 16)
                    Main.wofDrawAreaBottom = num365 * 16;
            }
            else if (Main.wofDrawAreaBottom < num365 * 16)
            {
                Main.wofDrawAreaBottom++;
                if (Main.wofDrawAreaBottom > num365 * 16)
                    Main.wofDrawAreaBottom = num365 * 16;
            }

            num364 = 0;
            num365 = num363 - 7;
            while (num364 < 15 && num365 < Main.maxTilesY - 10)
            {
                num365--;
                if (num365 <= 10)
                {
                    num365 = 10;
                    break;
                }

                if (num365 > num360)
                    continue;

                if (num365 < num359)
                {
                    num365 = num359;
                    break;
                }

                for (int num367 = num361; num367 <= num362; num367++)
                {
                    try
                    {
                        if (WorldGen.InWorld(num367, num365, 2) && (WorldGen.SolidTile(num367, num365) || Main.tile[num367, num365].LiquidAmount > 0))
                            num364++;
                    }
                    catch
                    {
                        num364 += 15;
                    }
                }
            }

            num365 -= 4;
            if (Main.wofDrawAreaTop == -1)
            {
                Main.wofDrawAreaTop = num365 * 16;
            }
            else if (Main.wofDrawAreaTop > num365 * 16)
            {
                Main.wofDrawAreaTop--;
                if (Main.wofDrawAreaTop < num365 * 16)
                    Main.wofDrawAreaTop = num365 * 16;
            }
            else if (Main.wofDrawAreaTop < num365 * 16)
            {
                Main.wofDrawAreaTop++;
                if (Main.wofDrawAreaTop > num365 * 16)
                    Main.wofDrawAreaTop = num365 * 16;
            }

            Main.wofDrawAreaTop = (int)MathHelper.Clamp(Main.wofDrawAreaTop, (float)num359 * 16f, (float)num360 * 16f);
            Main.wofDrawAreaBottom = (int)MathHelper.Clamp(Main.wofDrawAreaBottom, (float)num359 * 16f, (float)num360 * 16f);
            if (Main.wofDrawAreaTop > Main.wofDrawAreaBottom - 160)
                Main.wofDrawAreaTop = Main.wofDrawAreaBottom - 160;
            else if (Main.wofDrawAreaBottom < Main.wofDrawAreaTop + 160)
                Main.wofDrawAreaBottom = Main.wofDrawAreaTop + 160;

            float num368 = (Main.wofDrawAreaBottom + Main.wofDrawAreaTop) / 2 - npc.height / 2;
            if (npc.position.Y > num368 + 1f)
                npc.velocity.Y = -1f;
            else if (npc.position.Y < num368 - 1f)
                npc.velocity.Y = 1f;

            npc.velocity.Y = 0f;
            npc.position.Y = num368;

            float masterModeVelocityBoost = 0f;
            if (Main.masterMode)
            {
                float velocityBoostStartDistance = 480f;
                float velocityBoostMaxDistance = velocityBoostStartDistance * 2f;
                float distanceFromTargetX = Math.Abs(npc.Center.X - Main.player[npc.target].Center.X);
                float lerpAmount = MathHelper.Clamp((distanceFromTargetX - velocityBoostStartDistance) / velocityBoostMaxDistance, 0f, 1f);
                masterModeVelocityBoost = MathHelper.Lerp(0f, 8f, lerpAmount);
            }

            float num369 = (Main.expertMode ? 3.5f : 2.5f) + masterModeVelocityBoost;
            if (!Main.expertMode)
            {
                // 4.7 is the max in classic
                if ((double)npc.life < (double)npc.lifeMax * 0.75)
                    num369 += 0.4f;

                if ((double)npc.life < (double)npc.lifeMax * 0.5)
                    num369 += 0.5f;

                if ((double)npc.life < (double)npc.lifeMax * 0.25)
                    num369 += 0.6f;

                if ((double)npc.life < (double)npc.lifeMax * 0.1)
                    num369 += 0.7f;
            }
            else
            {
                // 6.5 is the max in expert
                if ((double)npc.life < (double)npc.lifeMax * 0.8)
                    num369 += 0.3f;

                if ((double)npc.life < (double)npc.lifeMax * 0.6)
                    num369 += 0.3f;

                if ((double)npc.life < (double)npc.lifeMax * 0.4)
                    num369 += 0.5f;

                if ((double)npc.life < (double)npc.lifeMax * 0.2)
                    num369 += 0.5f;

                if ((double)npc.life < (double)npc.lifeMax * 0.1)
                    num369 += 0.7f;

                if ((double)npc.life < (double)npc.lifeMax * 0.05)
                    num369 += 0.7f;

                // 8.3 is the max in master
                if (Main.masterMode)
                {
                    if ((double)npc.life < (double)npc.lifeMax * 0.025)
                        num369 += 0.9f;

                    if ((double)npc.life < (double)npc.lifeMax * 0.01)
                        num369 += 0.9f;
                }
            }

            if (Main.getGoodWorld)
            {
                num369 *= 1.1f;
                num369 += 0.2f;
            }

            if (npc.velocity.X == 0f)
            {
                npc.TargetClosest();
                if (Main.player[npc.target].dead)
                {
                    float num370 = float.PositiveInfinity;
                    int num371 = 0;
                    for (int num372 = 0; num372 < Main.maxPlayers; num372++)
                    {
                        Player player = Main.player[npc.target];
                        if (player.active)
                        {
                            float num373 = npc.Distance(player.Center);
                            if (num370 > num373)
                            {
                                num370 = num373;
                                num371 = ((npc.Center.X < player.Center.X) ? 1 : (-1));
                            }
                        }
                    }

                    npc.direction = num371;
                }

                npc.velocity.X = npc.direction;
            }

            if (npc.velocity.X < 0f)
            {
                npc.velocity.X = -num369;
                npc.direction = -1;
            }
            else
            {
                npc.velocity.X = num369;
                npc.direction = 1;
            }

            if (Main.player[npc.target].dead || !Main.player[npc.target].gross)
                npc.TargetClosest_WOF();

            if (Main.player[npc.target].dead)
            {
                npc.localAI[1] += 1f / 180f;
                if (npc.localAI[1] >= 1f)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath10, npc.Center);
                    npc.life = 0;
                    npc.active = false;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NetMessage.SendData(MessageID.DamageNPC, -1, -1, null, npc.whoAmI, -1f);

                    return false;
                }
            }
            else
                npc.localAI[1] = MathHelper.Clamp(npc.localAI[1] - 1f / 30f, 0f, 1f);

            npc.spriteDirection = npc.direction;
            Vector2 vector38 = npc.Center;
            float num374 = Main.player[npc.target].Center.X - vector38.X;
            float num375 = Main.player[npc.target].Center.Y - vector38.Y;
            float num376 = (float)Math.Sqrt(num374 * num374 + num375 * num375);
            float num377 = num376;
            num374 *= num376;
            num375 *= num376;
            if (npc.direction > 0)
            {
                if (Main.player[npc.target].Center.X > npc.Center.X)
                    npc.rotation = (float)Math.Atan2(0f - num375, 0f - num374) + MathHelper.Pi;
                else
                    npc.rotation = 0f;
            }
            else if (Main.player[npc.target].Center.X < npc.Center.X)
                npc.rotation = (float)Math.Atan2(num375, num374) + MathHelper.Pi;
            else
                npc.rotation = 0f;

            if (Main.expertMode && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int num378 = (int)(1f + (float)npc.life / (float)npc.lifeMax * 10f);
                num378 *= num378;
                if (num378 < 400)
                    num378 = (num378 * 19 + 400) / 20;

                if (num378 < 60)
                    num378 = (num378 * 3 + 60) / 4;

                if (num378 < 20)
                    num378 = (num378 + 20) / 2;

                num378 = (int)((double)num378 * (Main.masterMode ? 0.5 : 0.7));
                if (num378 < 2)
                    num378 = 2;

                if (Main.rand.NextBool(num378))
                {
                    int num379 = 0;
                    float[] array = new float[10];
                    for (int num380 = 0; num380 < Main.maxNPCs; num380++)
                    {
                        if (num379 < 10 && Main.npc[num380].active && Main.npc[num380].type == NPCID.TheHungry)
                        {
                            array[num379] = Main.npc[num380].ai[0];
                            num379++;
                        }
                    }

                    int maxValue = 1 + num379 * 2;
                    if (Main.masterMode)
                        maxValue /= 2;
                    if (maxValue < 2)
                        maxValue = 2;

                    if (num379 < 10 && Main.rand.Next(maxValue) <= 1)
                    {
                        int num381 = -1;
                        for (int num382 = 0; num382 < 1000; num382++)
                        {
                            int num383 = Main.rand.Next(10);
                            float num384 = (float)num383 * 0.1f - 0.05f;
                            bool flag26 = true;
                            for (int num385 = 0; num385 < num379; num385++)
                            {
                                if (num384 == array[num385])
                                {
                                    flag26 = false;
                                    break;
                                }
                            }

                            if (flag26)
                            {
                                num381 = num383;
                                break;
                            }
                        }

                        if (num381 >= 0)
                            NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.position.X, (int)num368, NPCID.TheHungry, npc.whoAmI, (float)num381 * 0.1f - 0.05f);
                    }
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && npc.localAI[0] == 1f)
            {
                npc.localAI[0] = 2f;
                float num386 = (npc.Center.Y + (float)Main.wofDrawAreaTop) / 2f;
                int num387 = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.position.X, (int)num386, NPCID.WallofFleshEye, npc.whoAmI, 1f);
                float num388 = (npc.Center.Y + (float)Main.wofDrawAreaBottom) / 2f;
                num387 = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.position.X, (int)num388, NPCID.WallofFleshEye, npc.whoAmI, -1f);
                float num389 = (npc.Center.Y + (float)Main.wofDrawAreaBottom) / 2f;
                int maxHungries = Main.masterMode ? 17 : 11;
                for (int num390 = 0; num390 < maxHungries; num390++)
                    num387 = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.position.X, (int)num389, NPCID.TheHungry, npc.whoAmI, (float)num390 * 0.1f - 0.05f);
            }

            return false;
        }

        public static bool VanillaWallofFleshEyeAI(NPC npc, Mod mod)
        {
            if (Main.wofNPCIndex < 0)
            {
                npc.active = false;
                return false;
            }

            npc.realLife = Main.wofNPCIndex;
            if (Main.npc[Main.wofNPCIndex].life > 0)
                npc.life = Main.npc[Main.wofNPCIndex].life;

            // Get a target
            if (npc.target < 0 || npc.target == Main.maxPlayers || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.target = Main.npc[Main.wofNPCIndex].target;

            // Avoid cheap bullshit
            npc.damage = 0;

            npc.position.X = Main.npc[Main.wofNPCIndex].position.X;
            npc.direction = Main.npc[Main.wofNPCIndex].direction;
            npc.spriteDirection = npc.direction;
            float num391 = (Main.wofDrawAreaBottom + Main.wofDrawAreaTop) / 2;
            num391 = ((!(npc.ai[0] > 0f)) ? ((num391 + (float)Main.wofDrawAreaBottom) / 2f) : ((num391 + (float)Main.wofDrawAreaTop) / 2f));
            num391 -= (float)(npc.height / 2);
            if (npc.position.Y > num391 + 1f)
            {
                npc.velocity.Y = -1f;
            }
            else if (npc.position.Y < num391 - 1f)
            {
                npc.velocity.Y = 1f;
            }
            else
            {
                npc.velocity.Y = 0f;
                npc.position.Y = num391;
            }

            if (npc.velocity.Y > 5f)
                npc.velocity.Y = 5f;

            if (npc.velocity.Y < -5f)
                npc.velocity.Y = -5f;

            Vector2 vector39 = npc.Center;
            float num392 = Main.player[npc.target].Center.X - vector39.X;
            float num393 = Main.player[npc.target].Center.Y - vector39.Y;
            float num394 = (float)Math.Sqrt(num392 * num392 + num393 * num393);
            float num395 = num394;
            num392 *= num394;
            num393 *= num394;
            bool flag27 = true;
            if (npc.direction > 0)
            {
                if (Main.player[npc.target].Center.X > npc.Center.X)
                {
                    npc.rotation = (float)Math.Atan2(0f - num393, 0f - num392) + MathHelper.Pi;
                }
                else
                {
                    npc.rotation = 0f;
                    flag27 = false;
                }
            }
            else if (Main.player[npc.target].Center.X < npc.Center.X)
            {
                npc.rotation = (float)Math.Atan2(num393, num392) + MathHelper.Pi;
            }
            else
            {
                npc.rotation = 0f;
                flag27 = false;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return false;

            int num396 = 4;
            npc.localAI[1] += 1f;
            if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.75)
            {
                npc.localAI[1] += 1f;
                num396++;
            }

            if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.5)
            {
                npc.localAI[1] += 1f;
                num396++;
            }

            if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.25)
            {
                npc.localAI[1] += 1f;
                num396 += 2;
            }

            if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.1)
            {
                npc.localAI[1] += 2f;
                num396 += 3;
            }

            if (Main.expertMode)
            {
                npc.localAI[1] += 0.5f;
                num396++;
                if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.1)
                {
                    npc.localAI[1] += 2f;
                    num396 += 3;
                }
            }

            if (npc.localAI[2] == 0f)
            {
                if (npc.localAI[1] > 600f)
                {
                    npc.localAI[2] = 1f;
                    npc.localAI[1] = 0f;
                }
            }
            else
            {
                if (!(npc.localAI[1] > 45f) || !Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                    return false;

                npc.localAI[1] = 0f;
                npc.localAI[2] += 1f;
                if (npc.localAI[2] >= (float)num396)
                {
                    npc.localAI[2] = 0f;
                    npc.TargetClosest();
                }

                if (flag27)
                {
                    float num397 = 4f;
                    int type = ProjectileID.EyeLaser;
                    if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.5)
                        num397 += 1f;
                    if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.25)
                        num397 += 1f;
                    if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.1)
                        num397 += 1f;

                    vector39 = npc.Center;
                    num392 = Main.player[npc.target].Center.X - vector39.X;
                    num393 = Main.player[npc.target].Center.Y - vector39.Y;
                    num394 = (float)Math.Sqrt(num392 * num392 + num393 * num393);
                    num394 = num397 / num394;
                    num392 *= num394;
                    num393 *= num394;
                    Vector2 projectileVelocity = new Vector2(num392, num393);
                    vector39 += projectileVelocity.SafeNormalize(Vector2.UnitY) * 150f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), vector39, projectileVelocity, type, npc.GetProjectileDamage(type), 0f, Main.myPlayer, 1f, 0f);
                }
            }

            return false;
        }
    }
}
