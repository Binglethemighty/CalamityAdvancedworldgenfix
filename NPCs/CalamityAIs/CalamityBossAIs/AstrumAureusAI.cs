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
using CalamityMod.NPCs.VanillaNPCAIOverrides.RegularEnemies;
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
    public static class AstrumAureusAI
    {
        public static void VanillaAstrumAureusAI(NPC npc, Mod mod)
        {
            CalamityGlobalNPC calamityGlobalNPC = npc.Calamity();

            CalamityGlobalNPC.astrumAureus = npc.whoAmI;

            // Variables
            bool bossRush = BossRushEvent.BossRushActive;
            bool expertMode = Main.expertMode || bossRush;
            bool masterMode = Main.masterMode || bossRush;
            bool revenge = CalamityWorld.revenge || bossRush;
            bool death = CalamityWorld.death || bossRush;

            // Percent life remaining
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Phases
            bool phase2 = lifeRatio < (revenge ? 0.85f : expertMode ? 0.8f : 0.75f);
            bool phase3 = lifeRatio < (revenge ? 0.7f : expertMode ? 0.6f : 0.5f);
            bool phase4 = lifeRatio < (revenge ? 0.5f : 0.4f) && expertMode;
            bool phase5 = lifeRatio < 0.3f && revenge;

            // Exhaustion
            bool exhausted = npc.ai[2] >= (phase3 ? 2f : 1f);
            calamityGlobalNPC.DR = exhausted ? 0.25f : 0.5f;
            npc.defense = exhausted ? npc.defDefense / 2 : npc.defDefense;

            // Get a target
            if (npc.target < 0 || npc.target == Main.maxPlayers || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            // Despawn safety, make sure to target another player if the current player target is too far away
            if (Vector2.Distance(Main.player[npc.target].Center, npc.Center) > CalamityGlobalNPC.CatchUpDistance200Tiles)
                npc.TargetClosest();

            Player player = Main.player[npc.target];

            // Don't fire projectiles and don't increment phase timers for 4 seconds after the teleport phase to avoid cheap bullshit
            float noProjectileOrPhaseIncrementTime = 240f;

            bool dontAttack = npc.localAI[3] > 0f;
            if (dontAttack)
            {
                npc.localAI[3] -= 1f;
                if (npc.justHit)
                    npc.localAI[3] -= masterMode ? 7f : expertMode ? 5f : 3f;
                if (npc.Distance(player.Center) < 240f)
                    npc.localAI[3] -= masterMode ? 4f : expertMode ? 2f : 1f;
            }

            float enrageScale = bossRush ? 1f : 0f;
            if ((Main.IsItDay() && !player.Calamity().ZoneAstral) || bossRush)
            {
                npc.Calamity().CurrentlyEnraged = !bossRush;
                enrageScale += 1f;
            }

            float astralFlameBarrageTimerIncrement = 1f;
            if (expertMode)
                astralFlameBarrageTimerIncrement += death ? (float)Math.Round(3f * (1f - lifeRatio)) : (float)Math.Round(2f * (1f - lifeRatio));

            float walkingVelocity = (CalamityWorld.LegendaryMode && CalamityWorld.revenge) ? 6f : 5f;
            walkingVelocity += 3f * enrageScale;
            if (phase5)
                walkingVelocity += 2f;
            if (expertMode)
                walkingVelocity += 1.5f * (1f - lifeRatio);
            if (revenge)
                walkingVelocity += Math.Abs(npc.Center.X - player.Center.X) * 0.0025f;
            if (Main.getGoodWorld)
                walkingVelocity *= 1.15f;

            float walkingProjectileVelocity = walkingVelocity * 0.8f;

            // Direction
            npc.spriteDirection = (npc.direction > 0) ? 1 : -1;

            // Used to reduce Aureus' fall speed
            bool reduceFallSpeed = npc.velocity.Y > 0f && Collision.SolidCollision(npc.position + Vector2.UnitY * 1.1f * npc.velocity.Y, npc.width, npc.height) && npc.ai[0] == 4f;

            // Despawn
            bool despawnDistance = Vector2.Distance(player.Center, npc.Center) > CalamityGlobalNPC.CatchUpDistance350Tiles;
            if (!player.active || player.dead || despawnDistance)
            {
                npc.TargetClosest(false);
                player = Main.player[npc.target];
                if (!player.active || player.dead || despawnDistance)
                {
                    npc.noTileCollide = true;

                    if (npc.velocity.Y < -3f)
                        npc.velocity.Y = -3f;
                    npc.velocity.Y += 0.1f;
                    if (npc.velocity.Y > 12f)
                        npc.velocity.Y = 12f;

                    if (npc.timeLeft > 60)
                        npc.timeLeft = 60;

                    if (npc.ai[0] != 0f)
                    {
                        npc.ai[0] = 0f;
                        npc.ai[1] = 0f;
                        npc.ai[2] = 0f;
                        npc.ai[3] = 0f;
                        npc.localAI[2] = 0f;
                        npc.localAI[3] = 0f;
                        calamityGlobalNPC.newAI[0] = 0f;
                        calamityGlobalNPC.newAI[1] = 0f;
                        npc.netUpdate = true;
                    }
                    return;
                }
            }
            else if (npc.timeLeft < 1800)
                npc.timeLeft = 1800;

            bool geldonPhase1 = lifeRatio > 0.6f && lifeRatio <= 0.7f;
            bool geldonPhase2 = lifeRatio <= 0.1f;
            if (Main.zenithWorld && (geldonPhase1 || geldonPhase2))
            {
                AstrumAureus.AstrumAureus astrumAureus = npc.ModNPC<AstrumAureus.AstrumAureus>();
                astrumAureus.slimeProjCounter++;
                if (astrumAureus.slimeProjCounter % 180 == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item33, npc.Center); // Intentionally keeping the old laser sound in GFB

                    if (astrumAureus.slimePhase == 1)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int type = ModContent.ProjectileType<AstralFlame>();
                            int damage = npc.GetProjectileDamage(type);
                            int totalProjectiles = bossRush ? 14 : death ? 12 : revenge ? 10 : expertMode ? 8 : 6;
                            float radians = MathHelper.TwoPi / totalProjectiles;
                            float velocity = 10f;
                            Vector2 spinningPoint = new Vector2(0f, -velocity);
                            for (int k = 0; k < totalProjectiles; k++)
                            {
                                Vector2 velocity2 = spinningPoint.RotatedBy(radians * k);
                                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, velocity2, type, damage, 0f, Main.myPlayer, 0f, 1f);
                            }
                        }
                        astrumAureus.slimePhase = 0;
                    }
                    else
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int type = ModContent.ProjectileType<AstralLaser>();
                            int damage = npc.GetProjectileDamage(type);
                            float aureusLaserSpeed = 7f;
                            float aureusLaserTargetX = player.Center.X - npc.Center.X;
                            float aureusLaserTargetY = player.Center.Y - npc.Center.Y;
                            float aureusLaserTargetDist = (float)Math.Sqrt(aureusLaserTargetX * aureusLaserTargetX + aureusLaserTargetY * aureusLaserTargetY);
                            aureusLaserTargetDist = aureusLaserSpeed / aureusLaserTargetDist;
                            aureusLaserTargetX *= aureusLaserTargetDist;
                            aureusLaserTargetY *= aureusLaserTargetDist;
                            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center.X, npc.Center.Y, aureusLaserTargetX, aureusLaserTargetY, type, damage, 0f, Main.myPlayer);
                            for (int i = 0; i < 4; i++)
                            {
                                Vector2 offset = new Vector2(Main.rand.Next(-6, 7), Main.rand.Next(-6, 7));
                                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center.X, npc.Center.Y, aureusLaserTargetX + offset.X, aureusLaserTargetY + offset.Y, type, damage, 0f, Main.myPlayer);
                            }
                        }
                        astrumAureus.slimePhase = 1;
                    }
                }
                RevengeanceAndDeathAI.BuffedMimicAI(npc, mod);
                npc.noGravity = false;
                npc.noTileCollide = false;
                return;
            }
            else
            {
                npc.noGravity = true;
            }

            // Emit light when not Idle
            if (npc.ai[0] != 1f)
                Lighting.AddLight((int)((npc.position.X + (npc.width / 2)) / 16f), (int)((npc.position.Y + (npc.height / 2)) / 16f), 1.3f, 0.5f, 0f);

            // Fire projectiles while walking, teleporting, or falling
            if (npc.ai[0] == 2f || npc.ai[0] >= 5f)
            {
                if (!dontAttack)
                    npc.localAI[0] += npc.ai[0] == 2f ? 1f : astralFlameBarrageTimerIncrement;

                float astralFlameBarrageGateValue = phase4 ? 30f : 60f;
                if (npc.localAI[0] >= astralFlameBarrageGateValue)
                {
                    // Fire astral flames while teleporting
                    if (npc.ai[0] >= 5f && npc.ai[0] != 7)
                    {
                        npc.localAI[0] = 0f;
                        SoundEngine.PlaySound(AstrumAureus.AstrumAureus.FlameCrystalSound, npc.Center);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float velocity = death ? (8f + npc.localAI[2] * 0.025f) : 7f;
                            int type = ModContent.ProjectileType<AstralFlame>();
                            int damage = npc.GetProjectileDamage(type);
                            float spreadLimit = (phase4 ? 100f : 50f) + enrageScale * 50f;
                            float randomSpread = (Main.rand.NextFloat() - 0.5f) * spreadLimit;
                            Vector2 spawnVector = new Vector2(npc.Center.X, npc.Center.Y - 80f * npc.scale);
                            Vector2 destination = new Vector2(spawnVector.X + randomSpread, spawnVector.Y - 100f * npc.scale);
                            Projectile.NewProjectile(npc.GetSource_FromAI(), spawnVector, Vector2.Normalize(destination - spawnVector) * velocity, type, damage, 0f, Main.myPlayer);
                        }
                    }
                }

                float laserBarrageGateValue = phase5 ? 160f : phase3 ? 120f : phase2 ? 80f : 60f;
                if (npc.localAI[0] >= laserBarrageGateValue)
                {
                    // Fire astral lasers while walking
                    if (npc.ai[0] == 2f)
                    {
                        npc.localAI[0] = 0f;

                        SoundEngine.PlaySound(AstrumAureus.AstrumAureus.LaserSound, npc.Center);

                        if (calamityGlobalNPC.newAI[2] == 0f)
                        {
                            calamityGlobalNPC.newAI[2] = 1f;

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                int maxProjectiles = !phase2 ? (bossRush ? 5 : 3) : (bossRush ? 7 : 5);
                                int spread = !phase2 ? (bossRush ? 11 : 8) : (bossRush ? 12 : 10);

                                int type = ModContent.ProjectileType<AstralLaser>();
                                int damage = npc.GetProjectileDamage(type);
                                Vector2 projectileVelocity = Vector2.Normalize(player.Center - npc.Center) * walkingProjectileVelocity;
                                float rotation = MathHelper.ToRadians(spread);
                                for (int i = 0; i < maxProjectiles; i++)
                                {
                                    Vector2 perturbedSpeed = projectileVelocity.RotatedBy(MathHelper.Lerp(-rotation, rotation, i / (float)(maxProjectiles - 1)));
                                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, perturbedSpeed, type, damage, 0f, Main.myPlayer, 0f, walkingProjectileVelocity * 2f);
                                }

                                if (phase3)
                                {
                                    float flameVelocity = walkingProjectileVelocity;
                                    maxProjectiles = 2;
                                    spread = 45;

                                    type = ModContent.ProjectileType<AstralFlame>();
                                    damage = npc.GetProjectileDamage(type);
                                    projectileVelocity = Vector2.Normalize(player.Center - npc.Center) * flameVelocity;
                                    rotation = MathHelper.ToRadians(spread);
                                    for (int i = 0; i < maxProjectiles; i++)
                                    {
                                        Vector2 perturbedSpeed = projectileVelocity.RotatedBy(MathHelper.Lerp(-rotation, rotation, i / (float)(maxProjectiles - 1)));
                                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, perturbedSpeed, type, damage, 0f, Main.myPlayer);
                                    }
                                }
                            }
                        }
                        else
                        {
                            calamityGlobalNPC.newAI[2] = 0f;

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                int maxProjectiles = !phase3 ? (bossRush ? 13 : death ? 11 : 9) : (bossRush ? 19 : death ? 17 : 15);
                                int spread = !phase3 ? (bossRush ? 20 : death ? 18 : 16) : (bossRush ? 24 : death ? 22 : 20);

                                int type = ModContent.ProjectileType<AstralLaser>();
                                int damage = npc.GetProjectileDamage(type);
                                int centralLaser = maxProjectiles / 2;
                                int[] lasersToNotFire = new int[6] { centralLaser - 3, centralLaser - 2, centralLaser - 1, centralLaser + 1, centralLaser + 2, centralLaser + 3 };
                                Vector2 projectileVelocity = Vector2.Normalize(player.Center - npc.Center) * walkingProjectileVelocity;
                                float rotation = MathHelper.ToRadians(spread);
                                for (int i = 0; i < maxProjectiles; i++)
                                {
                                    if (i != lasersToNotFire[0] && i != lasersToNotFire[1] && i != lasersToNotFire[2] && i != lasersToNotFire[3] && i != lasersToNotFire[4] && i != lasersToNotFire[5])
                                    {
                                        Vector2 perturbedSpeed = projectileVelocity.RotatedBy(MathHelper.Lerp(-rotation, rotation, i / (float)(maxProjectiles - 1)));
                                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, perturbedSpeed, type, damage, 0f, Main.myPlayer, 0f, walkingProjectileVelocity * 2f);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
                npc.localAI[0] = 0f;

            // Start up
            if (npc.ai[0] == 0f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                npc.ai[0] = 1f;
                npc.netUpdate = true;
                CustomGravity();
            }

            // Idle
            else if (npc.ai[0] == 1f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                // Slow down
                npc.velocity.X *= 0.8f;

                // Stay vulnerable for 3 seconds
                npc.ai[1] += 1f;
                if (npc.justHit)
                    npc.ai[1] += masterMode ? 7f : expertMode ? 5f : 3f;
                if (npc.Distance(player.Center) < 240f)
                    npc.ai[1] += masterMode ? 4f : expertMode ? 2f : 1f;

                if (npc.ai[1] >= 180f || bossRush)
                {
                    // Set AI to random state and reset other AI arrays
                    npc.TargetClosest();
                    switch (Main.rand.Next(phase3 ? 3 : 2))
                    {
                        case 0:
                            npc.ai[0] = 2f;
                            break;

                        case 1:
                            npc.ai[0] = 3f;
                            break;

                        case 2:
                            npc.ai[0] = 5f;
                            break;

                        default:
                            break;
                    }
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;

                    // Stop colliding with tiles if entering walking phase
                    npc.noTileCollide = npc.ai[0] == 2f;

                    npc.netUpdate = true;
                }
                else
                    CustomGravity();
            }

            // Walk
            else if (npc.ai[0] == 2f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                // Set walking direction
                if (Math.Abs(npc.Center.X - player.Center.X) < 200f * npc.scale)
                {
                    npc.velocity.X *= 0.8f;
                    if (npc.velocity.X > -0.1 && npc.velocity.X < 0.1)
                        npc.velocity.X = 0f;
                }
                else
                {
                    float playerLocation = npc.Center.X - player.Center.X;
                    npc.direction = playerLocation < 0 ? 1 : -1;

                    if (npc.direction > 0)
                        npc.velocity.X = (npc.velocity.X * 20f + walkingVelocity) / 21f;
                    if (npc.direction < 0)
                        npc.velocity.X = (npc.velocity.X * 20f - walkingVelocity) / 21f;
                }

                if (Collision.CanHit(npc.position, npc.width, npc.height, player.Center, 1, 1) && !Collision.SolidCollision(npc.position, npc.width, npc.height) && player.position.Y <= npc.position.Y + npc.height && !npc.collideX)
                {
                    CustomGravity();
                    npc.noTileCollide = false;
                }
                else
                {
                    npc.noTileCollide = true;

                    // Walk through tiles if colliding with tiles and player is out of reach
                    int aureusHitboxWidth = 80;
                    int aureusHitboxHeight = 20;
                    Vector2 aureusHitboxTileCollideSize = new Vector2(npc.Center.X - (aureusHitboxWidth / 2), npc.position.Y + npc.height - aureusHitboxHeight);

                    bool nearPlayerWalkingThroughTiles = false;
                    if (npc.position.X < player.position.X && npc.position.X + npc.width > player.position.X + player.width && npc.position.Y + npc.height < player.position.Y + player.height - 16f)
                        nearPlayerWalkingThroughTiles = true;

                    if (nearPlayerWalkingThroughTiles)
                    {
                        npc.velocity.Y += 0.5f;
                    }
                    else if (Collision.SolidCollision(aureusHitboxTileCollideSize, aureusHitboxWidth, aureusHitboxHeight))
                    {
                        if (npc.velocity.Y > 0f)
                            npc.velocity.Y = 0f;

                        if (npc.velocity.Y > -0.2)
                            npc.velocity.Y -= 0.025f;
                        else
                            npc.velocity.Y -= 0.2f;

                        if (npc.velocity.Y < -4f)
                            npc.velocity.Y = -4f;
                    }
                    else
                    {
                        if (npc.velocity.Y < 0f)
                            npc.velocity.Y = 0f;

                        if (npc.velocity.Y < 0.1)
                            npc.velocity.Y += 0.025f;
                        else
                            npc.velocity.Y += 0.5f;
                    }
                }

                // Walk for a maximum of 6 seconds
                if (!dontAttack)
                {
                    npc.ai[1] += 1f;
                    if (npc.Distance(player.Center) < 240f)
                        npc.ai[1] += masterMode ? 4f : expertMode ? 2f : 1f;
                }

                if (npc.ai[1] >= ((bossRush ? 270f : 360f) - (death ? 90f * (1f - lifeRatio) : 0f)))
                {
                    // Collide with tiles again
                    npc.noTileCollide = false;

                    // Set AI to next phase (Jump) and reset other AI
                    npc.TargetClosest();
                    npc.ai[0] = exhausted ? 1f : 3f;
                    npc.ai[1] = 0f;
                    npc.ai[2] += 1f;
                    npc.netUpdate = true;
                }

                // Limit downward velocity
                if (npc.velocity.Y > 10f)
                    npc.velocity.Y = 10f;
            }

            // Jump
            else if (npc.ai[0] == 3f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                npc.noTileCollide = false;

                if (npc.velocity.Y == 0f)
                {
                    // Slow down
                    npc.velocity.X *= 0.8f;

                    // Half second delay before jumping
                    if (!dontAttack)
                        npc.ai[1] += 1f;

                    if (npc.ai[1] >= 30f)
                    {
                        npc.ai[1] = -20f;
                    }
                    else if (npc.ai[1] == -1f)
                    {
                        // Set damage
                        npc.damage = npc.defDamage;

                        // Set jump velocity, reset and set AI to next phase (Stomp)
                        float distanceFromPlayerOnXAxis = npc.Center.X - player.Center.X;
                        npc.direction = distanceFromPlayerOnXAxis < 0 ? 1 : -1;
                        calamityGlobalNPC.newAI[3] = npc.direction;

                        // The limit for how much Aureus can multiply its jump velocity
                        float speedMultLimit = 1f;

                        // Maxes out when the player is a full 2000 pixels away from or above Aureus
                        float multiplier = 1f / 2000f;

                        // Increase Aureus jump velocity X if it's far enough away from the player
                        float distanceAwayFromTarget = Math.Abs(distanceFromPlayerOnXAxis);
                        float distanceGateValue = 400f;
                        bool increaseJumpVelocityX = distanceAwayFromTarget > distanceGateValue && expertMode;
                        if (increaseJumpVelocityX)
                        {
                            calamityGlobalNPC.newAI[0] = (distanceAwayFromTarget - distanceGateValue) * multiplier;
                            if (calamityGlobalNPC.newAI[0] > speedMultLimit)
                                calamityGlobalNPC.newAI[0] = speedMultLimit;
                        }

                        // Increase Aureus jump velocity Y if it's far enough above the player
                        float distanceBelowTarget = npc.position.Y - (player.position.Y + 80f);
                        bool increaseJumpVelocityY = distanceBelowTarget > 0f && revenge;
                        if (increaseJumpVelocityY)
                        {
                            calamityGlobalNPC.newAI[1] = distanceBelowTarget * multiplier;
                            if (calamityGlobalNPC.newAI[1] > speedMultLimit)
                                calamityGlobalNPC.newAI[1] = speedMultLimit;
                        }

                        float velocity = (CalamityWorld.LegendaryMode && CalamityWorld.revenge) ? 27f : 20f;
                        velocity += 6f * enrageScale;
                        if (expertMode)
                            velocity += death ? 6f * (1f - lifeRatio) : 4f * (1f - lifeRatio);
                        if (Main.getGoodWorld)
                            velocity *= 1.15f;

                        npc.velocity = (new Vector2(player.Center.X, player.Center.Y - 500f) - npc.Center).SafeNormalize(Vector2.Zero) * velocity;
                        npc.velocity *= new Vector2(calamityGlobalNPC.newAI[0] + 1f, calamityGlobalNPC.newAI[1] + 1f);

                        npc.noTileCollide = true;

                        npc.ai[0] = 4f;
                        npc.ai[1] = 0f;

                        SoundEngine.PlaySound(AstrumAureus.AstrumAureus.JumpSound, npc.Center);

                        npc.netUpdate = true;
                    }
                }

                // Don't run custom gravity when starting a jump
                if (npc.ai[0] != 4f)
                    CustomGravity();
            }

            // Stomp
            else if (npc.ai[0] == 4f)
            {
                if (npc.velocity.Y == 0f)
                {
                    // Avoid cheap bullshit
                    npc.damage = 0;

                    // Play stomp sound. Gotta specify the filepath to avoid confusion between the namespace and npc
                    SoundStyle soundToPlay = Main.zenithWorld ? NPCs.ExoMechs.Ares.AresGaussNuke.NukeExplosionSound : NPCs.AstrumAureus.AstrumAureus.StompSound;
                    SoundEngine.PlaySound(soundToPlay, npc.Center);

                    if (Main.zenithWorld)
                    {
                        float screenShakePower = 16 * Utils.GetLerpValue(1300f, 0f, npc.Distance(Main.LocalPlayer.Center), true);
                        if (Main.LocalPlayer.Calamity().GeneralScreenShakePower < screenShakePower)
                            Main.LocalPlayer.Calamity().GeneralScreenShakePower = screenShakePower;
                    }

                    // Stomp and jump again, if stomped twice then reset and set AI to next phase (Teleport or Idle)
                    npc.TargetClosest();
                    npc.localAI[1] += 1f;
                    float maxStompAmt = phase5 ? 5f : phase3 ? 2f : 3f;
                    if (npc.localAI[1] >= maxStompAmt)
                    {
                        npc.ai[0] = exhausted ? 1f : (phase3 ? 5f : 2f);
                        npc.localAI[1] = 0f;
                        npc.ai[2] += 1f;
                        npc.ai[3] = 0f;
                        npc.noTileCollide = false;
                        npc.netUpdate = true;
                    }
                    else
                    {
                        float playerLocation = npc.Center.X - player.Center.X;
                        npc.direction = playerLocation < 0 ? 1 : -1;
                        npc.ai[0] = 3f;
                        npc.ai[3] = 0f;
                        npc.netUpdate = true;
                    }

                    calamityGlobalNPC.newAI[0] = 0f;
                    calamityGlobalNPC.newAI[1] = 0f;
                    calamityGlobalNPC.newAI[3] = 0f;

                    // Spawn dust for visual effect
                    for (int i = (int)npc.position.X - 20; i < (int)npc.position.X + npc.width + 40; i += 20)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            int stompDust = Dust.NewDust(new Vector2(npc.position.X - 20f, npc.position.Y + npc.height), npc.width + 20, 4, ModContent.DustType<AstralOrange>(), 0f, 0f, 100, default, 1.5f);
                            Main.dust[stompDust].velocity *= 0.2f;
                        }
                    }

                    // Fire lasers or flames on stomp
                    SoundEngine.PlaySound(AstrumAureus.AstrumAureus.LaserSound, npc.Center);

                    if (Main.zenithWorld)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int type = Main.rand.NextBool() ? ModContent.ProjectileType<AstralLaser>() : ModContent.ProjectileType<AstralFlame>();
                            int damage = npc.GetProjectileDamage(type);
                            int totalProjectiles = bossRush ? 14 : death ? 12 : revenge ? 10 : expertMode ? 8 : 6;
                            float radians = MathHelper.TwoPi / totalProjectiles;
                            float velocity = 10f;
                            Vector2 spinningPoint = new Vector2(0f, -velocity);
                            for (int k = 0; k < totalProjectiles; k++)
                            {
                                Vector2 velocity2 = spinningPoint.RotatedBy(radians * k);
                                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, velocity2, type, damage, 0f, Main.myPlayer, 0f, 1f);
                            }
                        }
                    }
                    else if (calamityGlobalNPC.newAI[2] == 0f && phase2)
                    {
                        calamityGlobalNPC.newAI[2] = 1f;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float flameVelocity = 6f;
                            int maxProjectiles = bossRush ? 4 : death ? 3 : 2;
                            int spread = bossRush ? 36 : death ? 28 : 20;

                            int type = ModContent.ProjectileType<AstralFlame>();
                            int damage = npc.GetProjectileDamage(type);
                            Vector2 spawnVector = new Vector2(npc.Center.X, npc.Center.Y - 80f * npc.scale);
                            Vector2 destination = new Vector2(spawnVector.X, spawnVector.Y + 100f * npc.scale);
                            Vector2 projectileVelocity = Vector2.Normalize(destination - spawnVector) * flameVelocity;
                            float rotation = MathHelper.ToRadians(spread);
                            for (int i = 0; i < maxProjectiles; i++)
                            {
                                Vector2 perturbedSpeed = projectileVelocity.RotatedBy(MathHelper.Lerp(-rotation, rotation, i / (float)(maxProjectiles - 1)));
                                Projectile.NewProjectile(npc.GetSource_FromAI(), spawnVector + Vector2.Normalize(perturbedSpeed) * 100f, perturbedSpeed, type, damage, 0f, Main.myPlayer);
                            }
                        }
                    }
                    else
                    {
                        calamityGlobalNPC.newAI[2] = 0f;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float laserVelocity = (CalamityWorld.LegendaryMode && CalamityWorld.revenge) ? 7f : death ? 6f : 5f;
                            int maxProjectiles = !phase3 ? (bossRush ? 13 : death ? 11 : 9) : (bossRush ? 17 : death ? 15 : 13);
                            int spread = !phase3 ? (bossRush ? 20 : death ? 18 : 16) : (bossRush ? 24 : death ? 22 : 20);

                            int type = ModContent.ProjectileType<AstralLaser>();
                            int damage = npc.GetProjectileDamage(type);
                            int[] lasersToNotFire = new int[4] { 1, 3, maxProjectiles - 2, maxProjectiles - 4 };
                            Vector2 projectileVelocity = Vector2.Normalize(player.Center - npc.Center) * laserVelocity;
                            float rotation = MathHelper.ToRadians(spread);
                            for (int i = 0; i < maxProjectiles; i++)
                            {
                                if (i != lasersToNotFire[0] && i != lasersToNotFire[1] && i != lasersToNotFire[2] && i != lasersToNotFire[3])
                                {
                                    Vector2 perturbedSpeed = projectileVelocity.RotatedBy(MathHelper.Lerp(-rotation, rotation, i / (float)(maxProjectiles - 1)));
                                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, perturbedSpeed, type, damage, 0f, Main.myPlayer, 0f, laserVelocity * 2f);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Set damage
                    npc.damage = npc.defDamage;

                    // Set velocities while falling, this happens before the stomp
                    // Fall through
                    if (!player.dead)
                    {
                        if ((player.position.Y > npc.Bottom.Y && npc.velocity.Y > 0f) || (player.position.Y < npc.Bottom.Y && npc.velocity.Y < 0f))
                            npc.noTileCollide = true;
                        else if ((npc.velocity.Y > 0f && npc.Bottom.Y > Main.player[npc.target].Top.Y) || (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].Center, 1, 1) && !Collision.SolidCollision(npc.position, npc.width, npc.height)))
                            npc.noTileCollide = false;
                    }

                    if (npc.position.X < player.position.X && npc.position.X + npc.width > player.position.X + player.width)
                    {
                        // Make sure Aureus falls quickly when directly on top of or below the player
                        if (npc.ai[3] < 30f)
                            npc.ai[3] = 30f;

                        npc.velocity.X *= 0.8f;

                        if (npc.Bottom.Y < player.position.Y)
                        {
                            // Make sure Aureus falls rather quickly
                            if (npc.velocity.Y < -3f)
                                npc.velocity.Y = -3f;

                            float fallSpeed = 1.2f;
                            fallSpeed += 0.36f * enrageScale;
                            if (expertMode)
                                fallSpeed += death ? 0.36f * (1f - lifeRatio) : 0.24f * (1f - lifeRatio);
                            if (CalamityWorld.LegendaryMode && CalamityWorld.revenge)
                                fallSpeed += 0.5f;

                            if (calamityGlobalNPC.newAI[1] > 0f)
                                fallSpeed *= calamityGlobalNPC.newAI[1] + 1f;

                            npc.velocity.Y += fallSpeed;
                        }
                    }
                    else
                    {
                        // Push Aureus towards the player on the X axis if he's not directly on top of or below the player
                        float velocityXChange = 0.2f + Math.Abs(npc.Center.X - player.Center.X) * 0.0001f;
                        velocityXChange += 0.1f * enrageScale;

                        if (calamityGlobalNPC.newAI[0] > 0f)
                            velocityXChange *= calamityGlobalNPC.newAI[0] + 1f;

                        if (npc.direction < 0)
                            npc.velocity.X -= velocityXChange;
                        else if (npc.direction > 0)
                            npc.velocity.X += velocityXChange;

                        float velocityXCap = 12f;
                        velocityXCap += 3.6f * enrageScale;
                        if (expertMode)
                            velocityXCap += death ? 3.6f * (1f - lifeRatio) : 2.4f * (1f - lifeRatio);
                        if (CalamityWorld.LegendaryMode && CalamityWorld.revenge)
                            velocityXCap += 5f;

                        if (calamityGlobalNPC.newAI[0] > 0f)
                            velocityXCap *= calamityGlobalNPC.newAI[0] + 1f;

                        float playerLocation = npc.Center.X - player.Center.X;
                        int directionRelativeToTarget = playerLocation < 0 ? 1 : -1;
                        bool slowDown = directionRelativeToTarget != calamityGlobalNPC.newAI[3];

                        if (slowDown)
                            velocityXCap *= 0.333f;

                        if (npc.velocity.X < -velocityXCap)
                            npc.velocity.X = -velocityXCap;
                        if (npc.velocity.X > velocityXCap)
                            npc.velocity.X = velocityXCap;
                    }

                    // Don't start falling quickly until half a second has passed
                    npc.ai[3] += 1f;
                    if (npc.ai[3] > 30f)
                        CustomGravity();
                }
            }

            // Teleport
            else if (npc.ai[0] == 5f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                // Slow down
                npc.velocity.X *= 0.8f;

                // Start teleport
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.localAI[1] += 1f;
                    if (death)
                        npc.localAI[2] += 1.25f;

                    if (phase4)
                    {
                        npc.localAI[1] += 1f;
                        if (death)
                            npc.localAI[2] += 1.25f;
                    }

                    if (npc.localAI[1] >= (bossRush ? 60f : death ? 180f : 240f))
                    {
                        // Reset localAI and find a teleport destination
                        npc.TargetClosest();
                        npc.localAI[1] = 0f;

                        Vector2 vectorAimedAheadOfTarget = Main.player[npc.target].Center + new Vector2((float)Math.Round(Main.player[npc.target].velocity.X), 0f).SafeNormalize(Vector2.Zero) * 1000f;
                        Point point4 = vectorAimedAheadOfTarget.ToTileCoordinates();
                        int teleportTries = 0;
                        while (teleportTries < 100)
                        {
                            teleportTries++;
                            int teleportTileX = Main.rand.Next(point4.X - 5, point4.X + 6);
                            int teleportTileY = Main.rand.Next(point4.Y - 5, point4.Y);

                            if (!Main.tile[teleportTileX, teleportTileY].HasUnactuatedTile)
                            {
                                npc.ai[1] = teleportTileX * 16 + 8;
                                npc.ai[3] = teleportTileY * 16 + 16;
                                break;
                            }
                        }

                        // Default teleport if the above conditions aren't met in 100 iterations
                        if (teleportTries >= 100)
                        {
                            Vector2 bottom = Main.player[Player.FindClosest(npc.position, npc.width, npc.height)].Bottom;
                            npc.ai[1] = bottom.X;
                            npc.ai[3] = bottom.Y;
                        }

                        // Set AI to next phase (Mid-teleport)
                        npc.ai[0] = 6f;
                        npc.netUpdate = true;
                    }
                }

                CustomGravity();
            }

            // Mid-teleport
            else if (npc.ai[0] == 6f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                if (death)
                    npc.localAI[2] += 1.25f;

                if (phase4)
                {
                    if (death)
                        npc.localAI[2] += 1.25f;
                }

                // Turn invisible
                npc.alpha += 10;
                if (npc.alpha >= 255)
                {
                    // Set position to teleport destination
                    npc.Bottom = new Vector2(npc.ai[1], npc.ai[3]);

                    // Reset alpha and set AI to next phase (End of teleport)
                    npc.alpha = 255;
                    npc.ai[0] = 7f;
                    npc.localAI[2] = 0f;
                    npc.netUpdate = true;
                }

                // Play sound for cool effect
                if (npc.soundDelay == 0)
                {
                    npc.soundDelay = 15;
                    SoundEngine.PlaySound(AstrumAureus.AstrumAureus.TeleportSound, npc.Center);
                }

                // Emit dust to make the teleport pretty
                for (int i = 0; i < 10; i++)
                {
                    int teleportDust = Dust.NewDust(npc.position, npc.width, npc.height, ModContent.DustType<AstralOrange>(), npc.velocity.X, npc.velocity.Y, 255, default, 2f);
                    Main.dust[teleportDust].noGravity = true;
                    Main.dust[teleportDust].velocity *= 0.5f;
                }

                CustomGravity();
            }

            // End of teleport
            else if (npc.ai[0] == 7f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                // Turn visible
                npc.alpha -= 10;
                if (npc.alpha <= 0)
                {
                    // Spawn Aureus Spawns
                    bool spawnFlag = expertMode;
                    if (NPC.CountNPCS(ModContent.NPCType<AureusSpawn>()) >= 2)
                        spawnFlag = false;

                    if (spawnFlag && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int aureusSpawn = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)(npc.Center.Y - 25f * npc.scale), ModContent.NPCType<AureusSpawn>());
                        Main.npc[aureusSpawn].velocity.Y = -10f;
                        Main.npc[aureusSpawn].netUpdate = true;
                        if (revenge)
                        {
                            aureusSpawn = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)(npc.Center.Y - 25f * npc.scale), ModContent.NPCType<AureusSpawn>());
                            Main.npc[aureusSpawn].velocity.Y = -15f;
                            Main.npc[aureusSpawn].netUpdate = true;
                        }

                        if (death)
                        {
                            int damageAmt = npc.lifeMax / 50;
                            npc.life -= damageAmt;
                            if (npc.life < 1)
                                npc.life = 1;

                            npc.HealEffect(-damageAmt, true);
                        }
                    }

                    // Reset alpha and set AI to next phase (Idle)
                    npc.alpha = 0;
                    npc.ai[0] = exhausted ? 1f : 2f;
                    npc.ai[1] = 0f;
                    npc.ai[2] += 1f;
                    npc.localAI[3] = noProjectileOrPhaseIncrementTime;

                    // Stop colliding with tiles if entering walking phase
                    npc.noTileCollide = npc.ai[0] == 2f;

                    npc.netUpdate = true;
                }

                // Play sound at teleport destination for cool effect
                if (npc.soundDelay == 0)
                {
                    npc.soundDelay = 15;
                    SoundEngine.PlaySound(SoundID.Item109, npc.Center);
                }

                // Emit dust to make the teleport pretty
                for (int i = 0; i < 10; i++)
                {
                    int teleportDust = Dust.NewDust(npc.position, npc.width, npc.height, ModContent.DustType<AstralOrange>(), npc.velocity.X, npc.velocity.Y, 255, default, 2f);
                    Main.dust[teleportDust].noGravity = true;
                    Main.dust[teleportDust].velocity *= 0.5f;
                }

                CustomGravity();
            }

            void CustomGravity()
            {
                float gravity = 0.36f + 0.12f * enrageScale;
                float maxFallSpeed = reduceFallSpeed ? 12f : 12f + 4f * enrageScale;

                if (calamityGlobalNPC.newAI[1] > 0f && !reduceFallSpeed)
                    maxFallSpeed *= calamityGlobalNPC.newAI[1] + 1f;

                if (Main.getGoodWorld && !reduceFallSpeed)
                {
                    gravity *= 1.15f;
                    maxFallSpeed *= 1.15f;
                }

                npc.velocity.Y += gravity;
                if (npc.velocity.Y > maxFallSpeed)
                    npc.velocity.Y = maxFallSpeed;
            }
        }
    }
}
