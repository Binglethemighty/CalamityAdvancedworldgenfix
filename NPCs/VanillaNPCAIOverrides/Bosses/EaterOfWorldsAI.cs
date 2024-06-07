using System;
using CalamityMod.Events;
using CalamityMod.Projectiles.Boss;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.NPCs.VanillaNPCAIOverrides.Bosses
{
    public static class EaterOfWorldsAI
    {
        private const int TotalMasterModeWorms = 4;
        public const float DRIncreaseTime = 600f;

        public static bool BuffedEaterofWorldsAI(NPC npc, Mod mod)
        {
            CalamityGlobalNPC calamityGlobalNPC = npc.Calamity();

            bool bossRush = BossRushEvent.BossRushActive;
            bool masterMode = Main.masterMode || bossRush;
            bool death = CalamityWorld.death || bossRush;

            // Causes it to split far more in death mode
            if ((((npc.ai[2] % 2f == 0f && npc.type == NPCID.EaterofWorldsBody) || npc.type == NPCID.EaterofWorldsHead) && death) || CalamityWorld.LegendaryMode)
            {
                calamityGlobalNPC.DR = 0.5f;
                npc.defense = npc.defDefense * 2;
            }

            if (CalamityWorld.LegendaryMode && npc.type == NPCID.EaterofWorldsHead)
                npc.reflectsProjectiles = true;

            // Get a target
            if (npc.target < 0 || npc.target == Main.maxPlayers || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            bool enrage = true;
            int targetTileX = (int)Main.player[npc.target].Center.X / 16;
            int targetTileY = (int)Main.player[npc.target].Center.Y / 16;

            Tile tile = Framing.GetTileSafely(targetTileX, targetTileY);
            if (tile.WallType == WallID.EbonstoneUnsafe)
                enrage = false;

            float enrageScale = bossRush ? 0.5f : 0f;
            if (((npc.position.Y / 16f) < Main.worldSurface && enrage) || bossRush)
            {
                calamityGlobalNPC.CurrentlyEnraged = !bossRush;
                enrageScale += 0.5f;
            }
            if (!Main.player[npc.target].ZoneCorrupt || bossRush)
            {
                calamityGlobalNPC.CurrentlyEnraged = !bossRush;
                enrageScale += 2f;
            }

            // Total body segments
            float totalSegments = GetEaterOfWorldsSegmentsCountRevDeath();

            // Count body segments remaining
            float segmentCount = NPC.CountNPCS(NPCID.EaterofWorldsBody);

            // Percent body segments remaining
            float lifeRatio = MathHelper.Clamp(segmentCount / totalSegments, 0f, 1f);

            // 10 seconds of resistance to prevent spawn killing
            if (calamityGlobalNPC.newAI[1] < DRIncreaseTime)
                calamityGlobalNPC.newAI[1] += 1f;

            // Phases

            // Cursed Flame phase
            bool phase2 = lifeRatio < 0.8f || masterMode;

            // Boost velocity by 20% phase
            bool phase3 = lifeRatio < 0.4f || masterMode;

            // Boost velocity by 50% phase
            bool phase4 = lifeRatio < (masterMode ? 0.5f : 0.2f);

            // Go fucking crazy in Master Mode
            bool phase5 = lifeRatio < 0.1f && masterMode;
            bool phase6 = lifeRatio < 0.05f && masterMode;

            // Fire projectiles
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Vile spit
                if (npc.type == NPCID.EaterofWorldsBody)
                {
                    int randomChanceLimit = (int)MathHelper.Lerp(masterMode ? 15f : 30f, 900f, lifeRatio);
                    if (Main.getGoodWorld)
                        randomChanceLimit = (int)(randomChanceLimit * 0.5f);

                    if (Main.rand.NextBool(randomChanceLimit))
                    {
                        npc.TargetClosest();
                        if (Collision.CanHitLine(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1) && Vector2.Distance(npc.Center, Main.player[npc.target].Center) > 320f)
                            NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X + npc.velocity.X), (int)(npc.Center.Y + npc.velocity.Y), NPCID.VileSpitEaterOfWorlds, 0, 0f, 1f);
                    }
                }

                // Cursed flames (shadowflames in death mode)
                else if (npc.type == NPCID.EaterofWorldsHead)
                {
                    if (Collision.CanHitLine(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
                        calamityGlobalNPC.newAI[0] += ((npc.justHit && masterMode) ? 10f : 1f);

                    float timer = enrageScale > 0f ? 120f : 180f;
                    float shootBoost = lifeRatio * 120f;
                    timer += shootBoost;

                    if (enrageScale >= 2f)
                        timer = 60f;

                    if (calamityGlobalNPC.newAI[0] >= timer && phase2)
                    {
                        if (Collision.CanHitLine(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1) &&
                            (Main.player[npc.target].Center - npc.Center).SafeNormalize(Vector2.UnitY).ToRotation().AngleTowards(npc.velocity.ToRotation(), MathHelper.PiOver4) == npc.velocity.ToRotation() &&
                            Vector2.Distance(npc.Center, Main.player[npc.target].Center) > 320f)
                        {
                            calamityGlobalNPC.newAI[0] = 0f;
                            Vector2 cursedFlameDirection = npc.Center;
                            float targetXDirection = Main.player[npc.target].Center.X - cursedFlameDirection.X;
                            float targetYDirection = Main.player[npc.target].Center.Y - cursedFlameDirection.Y;
                            float projSpeed = 7f + enrageScale * 2f;
                            float flameTargetDistance = (float)Math.Sqrt(targetXDirection * targetXDirection + targetYDirection * targetYDirection);
                            flameTargetDistance = projSpeed / flameTargetDistance;
                            targetXDirection *= flameTargetDistance;
                            targetYDirection *= flameTargetDistance;
                            targetYDirection += npc.velocity.Y * 0.5f;
                            targetXDirection += npc.velocity.X * 0.5f;
                            cursedFlameDirection.X -= targetXDirection;
                            cursedFlameDirection.Y -= targetYDirection;

                            int type = (death && phase3) ? ModContent.ProjectileType<ShadowflameFireball>() : ProjectileID.CursedFlameHostile;
                            Projectile.NewProjectile(npc.GetSource_FromAI(), cursedFlameDirection.X, cursedFlameDirection.Y, targetXDirection, targetYDirection, type, npc.GetProjectileDamage(type), 0f, Main.myPlayer);
                        }
                    }
                }
            }

            // Despawn
            if (Main.player[npc.target].dead)
            {
                if (npc.timeLeft > 300)
                    npc.timeLeft = 300;
            }

            // All functions that modify the active worm segments are here. This includes spawning the worm originally and splitting effects.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // If this segment is a head or a body without a next-segment defined, then it needs to spawn its own next segment.
                if ((npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsBody) && npc.ai[0] == 0f)
                {
                    int spawnX = (int)npc.position.X;
                    int spawnY = (int)npc.position.Y;

                    // A head sets the length variable (npc.ai[2]) and then sets its next segment to a freshly spawned body.
                    if (npc.type == NPCID.EaterofWorldsHead)
                    {
                        // Amount of segments to spawn.
                        int segmentSpawnAmount = (int)(masterMode ? (totalSegments / TotalMasterModeWorms) : totalSegments);

                        // Spawn additional worms of reduced length in Master Mode.
                        if (masterMode)
                        {
                            Vector2 additionalWormSpawnLocation = new Vector2(spawnX, spawnY);
                            int randomXLimit = 80;
                            int randomYLimit = 80;
                            for (int i = 1; i < TotalMasterModeWorms; i++)
                            {
                                additionalWormSpawnLocation += new Vector2((Main.rand.Next(randomXLimit + 1) + randomXLimit) * (Main.rand.NextBool() ? -1f : 1f), Main.rand.Next(randomYLimit + 1) + randomYLimit);
                                int wormHead = NPC.NewNPC(npc.GetSource_FromAI(), (int)additionalWormSpawnLocation.X, (int)additionalWormSpawnLocation.Y, NPCID.EaterofWorldsHead, npc.whoAmI + segmentSpawnAmount * i + 1);
                                Main.npc[wormHead].ai[2] = segmentSpawnAmount;
                                Main.npc[wormHead].ai[0] = NPC.NewNPC(Main.npc[wormHead].GetSource_FromAI(), (int)additionalWormSpawnLocation.X, (int)additionalWormSpawnLocation.Y, NPCID.EaterofWorldsBody, Main.npc[wormHead].whoAmI);
                                Main.npc[(int)Main.npc[wormHead].ai[0]].ai[1] = Main.npc[wormHead].whoAmI;
                                Main.npc[(int)Main.npc[wormHead].ai[0]].ai[2] = Main.npc[wormHead].ai[2] - 1f;
                                Main.npc[wormHead].netUpdate = true;
                            }
                        }

                        // Set head's "length beyond this point" to be the total length of the worm.
                        npc.ai[2] = segmentSpawnAmount;

                        // Body spawn
                        npc.ai[0] = NPC.NewNPC(npc.GetSource_FromAI(), spawnX, spawnY, NPCID.EaterofWorldsBody, npc.whoAmI);
                    }

                    // A body with a "length beyond this point" greater than zero just sets its next spawned segment to a freshly spawned body.
                    else if (npc.type == NPCID.EaterofWorldsBody && npc.ai[2] > 0f)
                        npc.ai[0] = NPC.NewNPC(npc.GetSource_FromAI(), spawnX, spawnY, NPCID.EaterofWorldsBody, npc.whoAmI);

                    // If the worm stops here ("length beyond this point" is zero), then spawn a tail instead.
                    else
                        npc.ai[0] = NPC.NewNPC(npc.GetSource_FromAI(), spawnX, spawnY, NPCID.EaterofWorldsTail, npc.whoAmI);

                    // Maintain the linked list of worm segments, and correctly set the "length beyond this point" of this segment.
                    Main.npc[(int)npc.ai[0]].ai[1] = npc.whoAmI;
                    Main.npc[(int)npc.ai[0]].ai[2] = npc.ai[2] - 1f;
                    npc.netUpdate = true;
                }

                // Helper function to destroy this Eater of Worlds worm segment.
                void DestroyThisSegment()
                {
                    npc.life = 0;
                    npc.HitEffect(0, 10.0);
                    npc.checkDead();
                }

                // If this segment's previous and next segments are both dead, make it explode instantly. Single segments cannot live.
                if (!Main.npc[(int)npc.ai[1]].active && !Main.npc[(int)npc.ai[0]].active)
                    DestroyThisSegment();

                // If this segment is a head and its next segment is dead, make it explode instantly. It's been decapitated.
                if (npc.type == NPCID.EaterofWorldsHead && !Main.npc[(int)npc.ai[0]].active)
                    DestroyThisSegment();

                // If this segment is a tail and its previous segment is dead, make it explode instantly. It's been chopped off.
                if (npc.type == NPCID.EaterofWorldsTail && !Main.npc[(int)npc.ai[1]].active)
                    DestroyThisSegment();

                // If this segment is a body and its previous segment is dead (or was rendered into a tail), transform into a head.
                if (npc.type == NPCID.EaterofWorldsBody && (!Main.npc[(int)npc.ai[1]].active || Main.npc[(int)npc.ai[1]].aiStyle != npc.aiStyle))
                {
                    npc.type = NPCID.EaterofWorldsHead;
                    float segmentLifeRatio = MathHelper.Lerp(0.5f, 1f, npc.life / (float)npc.lifeMax);
                    int whoAmI = npc.whoAmI;
                    float ai0Holdover = npc.ai[0];
                    float newAI1Holdover = calamityGlobalNPC.newAI[1];
                    int slowingDebuffResistTimer = calamityGlobalNPC.debuffResistanceTimer;

                    // Actually transform the body segment into a head segment.
                    npc.SetDefaultsKeepPlayerInteraction(npc.type);
                    npc.life = (int)(npc.lifeMax * segmentLifeRatio);
                    npc.whoAmI = whoAmI;
                    npc.ai[0] = ai0Holdover;
                    // Heads spawned mid fight by splitting do not get reset spawn invincibility.
                    CalamityGlobalNPC newCGN = npc.Calamity();
                    newCGN.newAI[1] = newAI1Holdover;
                    newCGN.debuffResistanceTimer = slowingDebuffResistTimer;
                    npc.TargetClosest();
                    npc.netUpdate = true;
                    npc.netSpam = 0;
                    npc.alpha = 0;
                }

                // If this segment is a body and its next segment is dead (or was rendered into a head), transform into a tail.
                if (npc.type == NPCID.EaterofWorldsBody && (!Main.npc[(int)npc.ai[0]].active || Main.npc[(int)npc.ai[0]].aiStyle != npc.aiStyle))
                {
                    npc.type = NPCID.EaterofWorldsTail;
                    float segmentLifeRatio = MathHelper.Lerp(0.5f, 1f, npc.life / (float)npc.lifeMax);
                    int whoAmI = npc.whoAmI;
                    float ai1Holdover = npc.ai[1];
                    int slowingDebuffResistTimer = calamityGlobalNPC.debuffResistanceTimer;

                    // Actually transform the body segment into a tail segment.
                    npc.SetDefaultsKeepPlayerInteraction(npc.type);
                    npc.life = (int)(npc.lifeMax * segmentLifeRatio);
                    npc.whoAmI = whoAmI;
                    npc.ai[1] = ai1Holdover;
                    npc.Calamity().debuffResistanceTimer = slowingDebuffResistTimer;
                    npc.TargetClosest();
                    npc.netUpdate = true;
                    npc.netSpam = 0;
                    npc.alpha = 0;
                }

                // If for any reason this segment was deleted, send info to clients so they also see it die.
                if (!npc.active && Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.DamageNPC, -1, -1, null, npc.whoAmI, -1f);
            }

            // Movement
            int tilePositionX = (int)(npc.position.X / 16f) - 1;
            int tileWidthPosX = (int)((npc.position.X + npc.width) / 16f) + 2;
            int tilePositionY = (int)(npc.position.Y / 16f) - 1;
            int tileWidthPosY = (int)((npc.position.Y + npc.height) / 16f) + 2;
            if (tilePositionX < 0)
                tilePositionX = 0;
            if (tileWidthPosX > Main.maxTilesX)
                tileWidthPosX = Main.maxTilesX;
            if (tilePositionY < 0)
                tilePositionY = 0;
            if (tileWidthPosY > Main.maxTilesY)
                tileWidthPosY = Main.maxTilesY;

            // Fly or not
            bool inTiles = false;
            if (!inTiles)
            {
                for (int i = tilePositionX; i < tileWidthPosX; i++)
                {
                    for (int j = tilePositionY; j < tileWidthPosY; j++)
                    {
                        if (Main.tile[i, j] != null && ((Main.tile[i, j].HasUnactuatedTile && (Main.tileSolid[Main.tile[i, j].TileType] || (Main.tileSolidTop[Main.tile[i, j].TileType] && Main.tile[i, j].TileFrameY == 0))) || Main.tile[i, j].LiquidAmount > 64))
                        {
                            Vector2 vector;
                            vector.X = i * 16;
                            vector.Y = j * 16;
                            if (npc.position.X + npc.width > vector.X && npc.position.X < vector.X + 16f && npc.position.Y + npc.height > vector.Y && npc.position.Y < vector.Y + 16f)
                            {
                                inTiles = true;
                                if (Main.rand.NextBool(100) && Main.tile[i, j].HasUnactuatedTile)
                                    WorldGen.KillTile(i, j, true, true, false);
                            }
                        }
                    }
                }
            }

            if (!inTiles && npc.type == NPCID.EaterofWorldsHead)
            {
                Rectangle rectangle = new Rectangle((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height);
                int noFlyZone = death ? 800 : 900;
                noFlyZone -= (int)(enrageScale * 200f);

                if (masterMode)
                    noFlyZone -= phase5 ? 400 : 200;

                if (noFlyZone < 100)
                    noFlyZone = 100;

                bool freeMoveAnyway = true;
                for (int k = 0; k < Main.maxPlayers; k++)
                {
                    if (Main.player[k].active)
                    {
                        Rectangle rectangle2 = new Rectangle((int)Main.player[k].position.X - noFlyZone, (int)Main.player[k].position.Y - noFlyZone, noFlyZone * 2, noFlyZone * 2);
                        if (rectangle.Intersects(rectangle2))
                        {
                            freeMoveAnyway = false;
                            break;
                        }
                    }
                }

                if (freeMoveAnyway)
                    inTiles = true;
            }

            // Velocity and acceleration
            float velocityScale = (death ? 4.8f : 2.4f) * enrageScale;
            float velocityBoost = velocityScale * (1f - lifeRatio);
            float accelerationScale = (death ? 0.06f : 0.03f) * enrageScale;
            float accelerationBoost = accelerationScale * (1f - lifeRatio);
            float segmentVelocity = 12f + velocityBoost;
            float segmentAcceleration = 0.15f + accelerationBoost;

            if (phase6)
            {
                segmentVelocity += (death ? 2.4f : 4f) * (enrageScale + 1f);
                segmentAcceleration += 0.2f * (enrageScale + 1f);
            }
            else if (phase5)
            {
                segmentVelocity += (death ? 2.2f : 3f) * (enrageScale + 1f);
                segmentAcceleration += 0.15f * (enrageScale + 1f);
            }
            else if (phase4)
            {
                segmentVelocity += 2f * (enrageScale + 1f);
                segmentAcceleration += 0.1f * (enrageScale + 1f);
            }
            else if (phase3)
            {
                segmentVelocity += 0.8f * (enrageScale + 1f);
                segmentAcceleration += 0.04f * (enrageScale + 1f);
            }

            if (masterMode)
            {
                segmentVelocity += (npc.justHit ? 8f : 2f);
                segmentAcceleration += (npc.justHit ? 0.16f : 0.04f);
            }

            if (Main.getGoodWorld)
            {
                segmentVelocity += 4f;
                segmentAcceleration += 0.05f;
            }

            Vector2 segmentDirection = npc.Center;
            Vector2 destination = Main.player[npc.target].Center + (phase6 ? Main.player[npc.target].velocity * 20f : Vector2.Zero);
            float targetPosX = destination.X;
            float targetPosY = destination.Y;

            targetPosX = (int)(targetPosX / 16f) * 16;
            targetPosY = (int)(targetPosY / 16f) * 16;
            segmentDirection.X = (int)(segmentDirection.X / 16f) * 16;
            segmentDirection.Y = (int)(segmentDirection.Y / 16f) * 16;
            targetPosX -= segmentDirection.X;
            targetPosY -= segmentDirection.Y;
            float targetDistance = (float)Math.Sqrt(targetPosX * targetPosX + targetPosY * targetPosY);

            // Does this worm segment have a "previous segment" defined?
            if (npc.ai[1] > 0f && npc.ai[1] < Main.npc.Length)
            {
                try
                {
                    segmentDirection = npc.Center;
                    targetPosX = Main.npc[(int)npc.ai[1]].Center.X - segmentDirection.X;
                    targetPosY = Main.npc[(int)npc.ai[1]].Center.Y - segmentDirection.Y;
                }
                catch
                {
                }

                npc.rotation = (float)Math.Atan2(targetPosY, targetPosX) + MathHelper.PiOver2;
                targetDistance = (float)Math.Sqrt(targetPosX * targetPosX + targetPosY * targetPosY);
                int npcWidth = npc.width;
                npcWidth = (int)(npcWidth * npc.scale);

                if (Main.getGoodWorld)
                    npcWidth = 62;

                targetDistance = (targetDistance - npcWidth) / targetDistance;
                targetPosX *= targetDistance;
                targetPosY *= targetDistance;
                npc.velocity = Vector2.Zero;
                npc.position.X += targetPosX;
                npc.position.Y += targetPosY;
            }

            // Otherwise this is a head. (Why does this not just check for head NPC type?)
            else
            {
                // Prevent new heads from being slowed when they spawn
                if (calamityGlobalNPC.newAI[2] < 3f)
                {
                    calamityGlobalNPC.newAI[2] += 1f;

                    // Set velocity for when a new head spawns
                    // Only set this if the head is far enough away from the player, to avoid unfair hits
                    if (npc.Distance(Main.player[npc.target].Center) > segmentVelocity * 20f)
                        npc.velocity = (Main.player[npc.target].Center - npc.Center).SafeNormalize(Vector2.UnitY) * (segmentVelocity * (death ? 0.75f : 0.5f));
                }

                if (!inTiles)
                {
                    npc.velocity.Y += death ? 0.1375f : 0.11f;
                    if (masterMode && npc.velocity.Y > 0f)
                        npc.velocity.Y += 0.07f;

                    if (npc.velocity.Y > segmentVelocity)
                        npc.velocity.Y = segmentVelocity;

                    // This bool exists to stop the strange wiggle behavior when worms are falling down
                    bool slowXVelocity = Math.Abs(npc.velocity.X) > segmentAcceleration;
                    if ((Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < segmentVelocity * 0.4)
                    {
                        if (npc.velocity.X < 0f)
                            npc.velocity.X -= segmentAcceleration * 1.1f;
                        else
                            npc.velocity.X += segmentAcceleration * 1.1f;
                    }
                    else if (npc.velocity.Y == segmentVelocity)
                    {
                        if (slowXVelocity)
                        {
                            if (npc.velocity.X < targetPosX)
                                npc.velocity.X += segmentAcceleration;
                            else if (npc.velocity.X > targetPosX)
                                npc.velocity.X -= segmentAcceleration;
                        }
                        else
                            npc.velocity.X = 0f;
                    }
                    else if (npc.velocity.Y > (death ? 5f : 4f))
                    {
                        if (slowXVelocity)
                        {
                            if (npc.velocity.X < 0f)
                                npc.velocity.X += segmentAcceleration * 0.9f;
                            else
                                npc.velocity.X -= segmentAcceleration * 0.9f;
                        }
                        else
                            npc.velocity.X = 0f;
                    }
                }
                else
                {
                    // Sound
                    if (npc.soundDelay == 0)
                    {
                        float soundDelay = targetDistance / 40f;
                        if (soundDelay < 10f)
                            soundDelay = 10f;
                        if (soundDelay > 20f)
                            soundDelay = 20f;

                        npc.soundDelay = (int)soundDelay;
                        SoundEngine.PlaySound(SoundID.WormDig, npc.Center);
                    }

                    targetDistance = (float)Math.Sqrt(targetPosX * targetPosX + targetPosY * targetPosY);
                    float absoluteTargetX = Math.Abs(targetPosX);
                    float absoluteTargetY = Math.Abs(targetPosY);
                    float timeToReachTarget = segmentVelocity / targetDistance;
                    targetPosX *= timeToReachTarget;
                    targetPosY *= timeToReachTarget;

                    // Despawn
                    bool shouldDespawn = npc.type == NPCID.EaterofWorldsHead && Main.player[npc.target].dead;
                    if (shouldDespawn && !bossRush)
                    {
                        bool everyoneDead = true;
                        for (int i = 0; i < Main.maxPlayers; i++)
                        {
                            if (Main.player[i].active && !Main.player[i].dead)
                            {
                                everyoneDead = false;
                                break;
                            }
                        }

                        if (everyoneDead)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient && (npc.position.Y / 16f) > (Main.rockLayer + Main.maxTilesY) / 2.0)
                            {
                                npc.active = false;
                                int segmentAmt = (int)npc.ai[0];

                                while (segmentAmt > 0 && segmentAmt < Main.maxNPCs && Main.npc[segmentAmt].active && Main.npc[segmentAmt].aiStyle == npc.aiStyle)
                                {
                                    int attachedSegments = (int)Main.npc[segmentAmt].ai[0];
                                    Main.npc[segmentAmt].active = false;
                                    npc.life = 0;

                                    if (Main.netMode == NetmodeID.Server)
                                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, segmentAmt, 0f, 0f, 0f, 0, 0, 0);

                                    segmentAmt = attachedSegments;
                                }

                                if (Main.netMode == NetmodeID.Server)
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI, 0f, 0f, 0f, 0, 0, 0);
                            }
                            targetPosX = 0f;
                            targetPosY = segmentVelocity;
                        }
                    }

                    if ((npc.velocity.X > 0f && targetPosX > 0f) || (npc.velocity.X < 0f && targetPosX < 0f) || (npc.velocity.Y > 0f && targetPosY > 0f) || (npc.velocity.Y < 0f && targetPosY < 0f))
                    {
                        if (npc.velocity.X < targetPosX)
                            npc.velocity.X += segmentAcceleration;
                        else if (npc.velocity.X > targetPosX)
                            npc.velocity.X -= segmentAcceleration;
                        if (npc.velocity.Y < targetPosY)
                            npc.velocity.Y += segmentAcceleration;
                        else if (npc.velocity.Y > targetPosY)
                            npc.velocity.Y -= segmentAcceleration;

                        if (Math.Abs(targetPosY) < segmentVelocity * 0.2 && ((npc.velocity.X > 0f && targetPosX < 0f) || (npc.velocity.X < 0f && targetPosX > 0f)))
                        {
                            if (npc.velocity.Y > 0f)
                                npc.velocity.Y += segmentAcceleration * 2f;
                            else
                                npc.velocity.Y -= segmentAcceleration * 2f;
                        }

                        if (Math.Abs(targetPosX) < segmentVelocity * 0.2 && ((npc.velocity.Y > 0f && targetPosY < 0f) || (npc.velocity.Y < 0f && targetPosY > 0f)))
                        {
                            if (npc.velocity.X > 0f)
                                npc.velocity.X += segmentAcceleration * 2f;
                            else
                                npc.velocity.X -= segmentAcceleration * 2f;
                        }
                    }
                    else if (absoluteTargetX > absoluteTargetY)
                    {
                        if (npc.velocity.X < targetPosX)
                            npc.velocity.X += segmentAcceleration * 1.1f;
                        else if (npc.velocity.X > targetPosX)
                            npc.velocity.X -= segmentAcceleration * 1.1f;

                        if ((Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < segmentVelocity * 0.5)
                        {
                            if (npc.velocity.Y > 0f)
                                npc.velocity.Y += segmentAcceleration;
                            else
                                npc.velocity.Y -= segmentAcceleration;
                        }
                    }
                    else
                    {
                        if (npc.velocity.Y < targetPosY)
                            npc.velocity.Y += segmentAcceleration * 1.1f;
                        else if (npc.velocity.Y > targetPosY)
                            npc.velocity.Y -= segmentAcceleration * 1.1f;

                        if ((Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < segmentVelocity * 0.5)
                        {
                            if (npc.velocity.X > 0f)
                                npc.velocity.X += segmentAcceleration;
                            else
                                npc.velocity.X -= segmentAcceleration;
                        }
                    }
                }

                if (death)
                {
                    int numHeads = NPC.CountNPCS(npc.type);
                    if (numHeads > 0)
                    {
                        // Limit this variable so that the following calculation never goes too low
                        numHeads--;
                        if (numHeads > 7)
                            numHeads = 7;

                        float pushDistanceLowerLimit = 14f - numHeads;
                        float pushDistanceUpperLimit = 140f - numHeads * 10f;
                        float pushDistance = MathHelper.Lerp(pushDistanceLowerLimit, pushDistanceUpperLimit, 1f - lifeRatio) * npc.scale;
                        float pushVelocity = 0.25f + enrageScale * 0.125f;
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            if (Main.npc[i].active)
                            {
                                if (i != npc.whoAmI && Main.npc[i].type == npc.type)
                                {
                                    if (Vector2.Distance(npc.Center, Main.npc[i].Center) < pushDistance)
                                    {
                                        if (npc.position.X < Main.npc[i].position.X)
                                            npc.velocity.X -= pushVelocity;
                                        else
                                            npc.velocity.X += pushVelocity;

                                        if (npc.position.Y < Main.npc[i].position.Y)
                                            npc.velocity.Y -= pushVelocity;
                                        else
                                            npc.velocity.Y += pushVelocity;
                                    }
                                }
                            }
                        }
                    }
                }

                npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + MathHelper.PiOver2;

                if (npc.type == NPCID.EaterofWorldsHead)
                {
                    if (inTiles)
                    {
                        if (npc.localAI[0] != 1f)
                            npc.netUpdate = true;

                        npc.localAI[0] = 1f;
                    }
                    else
                    {
                        if (npc.localAI[0] != 0f)
                            npc.netUpdate = true;

                        npc.localAI[0] = 0f;
                    }
                    if (((npc.velocity.X > 0f && npc.oldVelocity.X < 0f) || (npc.velocity.X < 0f && npc.oldVelocity.X > 0f) || (npc.velocity.Y > 0f && npc.oldVelocity.Y < 0f) || (npc.velocity.Y < 0f && npc.oldVelocity.Y > 0f)) && !npc.justHit)
                        npc.netUpdate = true;
                }
            }

            // Calculate contact damage based on velocity
            // This worm requires more velocity to deal damage with the body because it doesn't have spikes or metal bits or etc.
            float minimalContactDamageHeadVelocity = segmentVelocity * 0.25f;
            float minimalDamageHeadVelocity = segmentVelocity * 0.5f;
            float minimalContactDamageBodyVelocity = segmentVelocity * 0.4f;
            float minimalDamageBodyVelocity = segmentVelocity * 0.8f;
            if (npc.type == NPCID.EaterofWorldsHead)
            {
                if (npc.velocity.Length() <= minimalContactDamageHeadVelocity)
                {
                    npc.damage = (int)Math.Round(npc.defDamage * 0.5);
                }
                else
                {
                    float velocityDamageScalar = MathHelper.Clamp((npc.velocity.Length() - minimalContactDamageHeadVelocity) / minimalDamageHeadVelocity, 0f, 1f);
                    npc.damage = (int)MathHelper.Lerp((float)Math.Round(npc.defDamage * 0.5), npc.defDamage, velocityDamageScalar);
                }
            }
            else
            {
                float bodyAndTailVelocity = (npc.position - npc.oldPosition).Length();
                if (bodyAndTailVelocity <= minimalContactDamageBodyVelocity)
                {
                    npc.damage = 0;
                }
                else
                {
                    float velocityDamageScalar = MathHelper.Clamp((bodyAndTailVelocity - minimalContactDamageBodyVelocity) / minimalDamageBodyVelocity, 0f, 1f);
                    npc.damage = (int)MathHelper.Lerp(0f, npc.defDamage, velocityDamageScalar);
                }
            }

            if (npc.type == NPCID.EaterofWorldsHead || (npc.type != NPCID.EaterofWorldsHead && Main.npc[(int)npc.ai[1]].alpha >= 85))
            {
                if (npc.alpha > 0 && npc.life > 0)
                {
                    for (int dustIndex = 0; dustIndex < 2; dustIndex++)
                    {
                        int dust = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, DustID.Demonite, 0f, 0f, 100, default(Color), 2f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].noLight = true;
                    }
                }

                if ((npc.position - npc.oldPosition).Length() > 2f)
                {
                    npc.alpha -= 42;
                    if (npc.alpha < 0)
                        npc.alpha = 0;
                }
            }
            else if (npc.type > NPCID.EaterofWorldsHead && npc.alpha > 0)
            {
                npc.alpha -= 42;
                if (npc.alpha < 0)
                    npc.alpha = 0;
            }

            // Manually sync newAI because there is no GlobalNPC.SendExtraAI
            if (npc.active && npc.netUpdate && Main.netMode == NetmodeID.Server)
            {
                ModPacket packet = mod.GetPacket();
                packet.Write((byte)CalamityModMessageType.SyncCalamityNPCAIArray);
                packet.Write((byte)npc.whoAmI);
                packet.Write(calamityGlobalNPC.newAI[0]);
                packet.Write(calamityGlobalNPC.newAI[1]);
                packet.Write(calamityGlobalNPC.newAI[2]);
                packet.Write(calamityGlobalNPC.newAI[3]);
                packet.Send(-1, -1);
            }

            return false;
        }

        public static bool VanillaEaterofWorldsAI(NPC npc, Mod mod)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && Main.expertMode)
            {
                if (npc.type == NPCID.EaterofWorldsBody && ((double)(npc.position.Y / 16f) < Main.worldSurface || Main.getGoodWorld))
                {
                    int num7 = (int)(npc.Center.X / 16f);
                    int num8 = (int)(npc.Center.Y / 16f);
                    if (WorldGen.InWorld(num7, num8) && Main.tile[num7, num8].WallType == WallID.None)
                    {
                        int num9 = Main.masterMode ? 600 : 900;
                        if (Main.getGoodWorld)
                            num9 /= 2;

                        if (Main.rand.NextBool(num9))
                        {
                            npc.TargetClosest();
                            if (Collision.CanHitLine(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
                                NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X + npc.velocity.X), (int)(npc.Center.Y + npc.velocity.Y), NPCID.VileSpitEaterOfWorlds, 0, 0f, 1f);
                        }
                    }
                }
                else if (npc.type == NPCID.EaterofWorldsHead)
                {
                    int num10 = Main.masterMode ? 15 : 90;
                    num10 += (int)((float)npc.life / (float)npc.lifeMax * 300f);
                    if (Main.rand.NextBool(num10))
                    {
                        npc.TargetClosest();
                        if (Collision.CanHitLine(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
                            NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X + npc.velocity.X), (int)(npc.Center.Y + npc.velocity.Y), NPCID.VileSpitEaterOfWorlds, 0, 0f, 1f);
                    }
                }
            }

            // 10 seconds of resistance to prevent spawn killing
            if (npc.Calamity().newAI[1] < DRIncreaseTime)
                npc.Calamity().newAI[1] += 1f;

            npc.realLife = -1;

            if (npc.target < 0 || npc.target == Main.maxPlayers || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            if (Main.player[npc.target].dead)
                npc.EncourageDespawn(300);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if ((npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsBody) && npc.ai[0] == 0f)
                {
                    if (npc.type == NPCID.EaterofWorldsHead)
                    {
                        npc.ai[2] = GetEaterOfWorldsSegmentsCountVanilla();
                        npc.ai[0] = NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X), (int)(npc.position.Y + (float)npc.height), npc.type + 1, npc.whoAmI);
                        Main.npc[(int)npc.ai[0]].CopyInteractions(npc);
                    }
                    else if (npc.type == NPCID.EaterofWorldsBody && npc.ai[2] > 0f)
                    {
                        npc.ai[0] = NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X), (int)(npc.position.Y + (float)npc.height), npc.type, npc.whoAmI);
                        Main.npc[(int)npc.ai[0]].CopyInteractions(npc);
                    }
                    else
                    {
                        npc.ai[0] = NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X), (int)(npc.position.Y + (float)npc.height), npc.type + 1, npc.whoAmI);
                        Main.npc[(int)npc.ai[0]].CopyInteractions(npc);
                    }

                    Main.npc[(int)npc.ai[0]].ai[1] = npc.whoAmI;
                    Main.npc[(int)npc.ai[0]].ai[2] = npc.ai[2] - 1f;
                    npc.netUpdate = true;
                }

                if (!Main.npc[(int)npc.ai[1]].active && !Main.npc[(int)npc.ai[0]].active)
                {
                    npc.life = 0;
                    npc.HitEffect();
                    npc.checkDead();
                    npc.active = false;
                    NetMessage.SendData(MessageID.DamageNPC, -1, -1, null, npc.whoAmI, -1f);
                    return false;
                }

                if (npc.type == NPCID.EaterofWorldsHead && !Main.npc[(int)npc.ai[0]].active)
                {
                    npc.life = 0;
                    npc.HitEffect();
                    npc.checkDead();
                    npc.active = false;
                    NetMessage.SendData(MessageID.DamageNPC, -1, -1, null, npc.whoAmI, -1f);
                    return false;
                }

                if (npc.type == NPCID.EaterofWorldsTail && !Main.npc[(int)npc.ai[1]].active)
                {
                    npc.life = 0;
                    npc.HitEffect();
                    npc.checkDead();
                    npc.active = false;
                    NetMessage.SendData(MessageID.DamageNPC, -1, -1, null, npc.whoAmI, -1f);
                    return false;
                }

                if (npc.type == NPCID.EaterofWorldsBody && (!Main.npc[(int)npc.ai[1]].active || Main.npc[(int)npc.ai[1]].aiStyle != npc.aiStyle))
                {
                    npc.type = NPCID.EaterofWorldsHead;
                    int num38 = npc.whoAmI;
                    float num39 = (float)npc.life / (float)npc.lifeMax;
                    float num40 = npc.ai[0];
                    npc.SetDefaultsKeepPlayerInteraction(npc.type);
                    npc.life = (int)((float)npc.lifeMax * num39);
                    npc.ai[0] = num40;
                    npc.TargetClosest();
                    npc.netUpdate = true;
                    npc.whoAmI = num38;
                    npc.alpha = 0;
                }

                if (npc.type == NPCID.EaterofWorldsBody && (!Main.npc[(int)npc.ai[0]].active || Main.npc[(int)npc.ai[0]].aiStyle != npc.aiStyle))
                {
                    npc.type = NPCID.EaterofWorldsTail;
                    int num41 = npc.whoAmI;
                    float num42 = (float)npc.life / (float)npc.lifeMax;
                    float num43 = npc.ai[1];
                    npc.SetDefaultsKeepPlayerInteraction(npc.type);
                    npc.life = (int)((float)npc.lifeMax * num42);
                    npc.ai[1] = num43;
                    npc.TargetClosest();
                    npc.netUpdate = true;
                    npc.whoAmI = num41;
                    npc.alpha = 0;
                }

                if (!npc.active && Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.DamageNPC, -1, -1, null, npc.whoAmI, -1f);
            }

            int num44 = (int)(npc.position.X / 16f) - 1;
            int num45 = (int)((npc.position.X + (float)npc.width) / 16f) + 2;
            int num46 = (int)(npc.position.Y / 16f) - 1;
            int num47 = (int)((npc.position.Y + (float)npc.height) / 16f) + 2;
            if (num44 < 0)
                num44 = 0;

            if (num45 > Main.maxTilesX)
                num45 = Main.maxTilesX;

            if (num46 < 0)
                num46 = 0;

            if (num47 > Main.maxTilesY)
                num47 = Main.maxTilesY;

            bool flag2 = false;

            if (!flag2)
            {
                Vector2 vector2 = default(Vector2);
                for (int num48 = num44; num48 < num45; num48++)
                {
                    for (int num49 = num46; num49 < num47; num49++)
                    {
                        if (Main.tile[num48, num49] == null || ((!Main.tile[num48, num49].HasUnactuatedTile || (!Main.tileSolid[Main.tile[num48, num49].TileType] && (!Main.tileSolidTop[Main.tile[num48, num49].TileType] || Main.tile[num48, num49].TileFrameY != 0))) && Main.tile[num48, num49].LiquidAmount <= 64))
                            continue;

                        vector2.X = num48 * 16;
                        vector2.Y = num49 * 16;
                        if (npc.position.X + (float)npc.width > vector2.X && npc.position.X < vector2.X + 16f && npc.position.Y + (float)npc.height > vector2.Y && npc.position.Y < vector2.Y + 16f)
                        {
                            flag2 = true;
                            if (Main.rand.NextBool(100) && Main.tile[num48, num49].HasUnactuatedTile && Main.tileSolid[Main.tile[num48, num49].TileType])
                                WorldGen.KillTile(num48, num49, fail: true, effectOnly: true);
                        }
                    }
                }
            }

            if (!flag2 && npc.type == NPCID.EaterofWorldsHead)
            {
                Rectangle rectangle = new Rectangle((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height);
                int num50 = Main.masterMode ? 750 : 1000;
                bool flag3 = true;
                for (int num51 = 0; num51 < Main.maxPlayers; num51++)
                {
                    if (Main.player[num51].active)
                    {
                        Rectangle rectangle2 = new Rectangle((int)Main.player[num51].position.X - num50, (int)Main.player[num51].position.Y - num50, num50 * 2, num50 * 2);
                        if (rectangle.Intersects(rectangle2))
                        {
                            flag3 = false;
                            break;
                        }
                    }
                }

                if (flag3)
                    flag2 = true;
            }

            float num52 = 10f;
            float num53 = 0.07f;
            if (Main.expertMode)
            {
                num52 = Main.masterMode ? 15f : 12f;
                num53 = Main.masterMode ? 0.2f : 0.15f;
            }

            if (Main.getGoodWorld)
            {
                num52 += 4f;
                num53 += 0.05f;
            }

            Vector2 vector5 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
            float num55 = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2);
            float num56 = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2);
            num55 = (int)(num55 / 16f) * 16;
            num56 = (int)(num56 / 16f) * 16;
            vector5.X = (int)(vector5.X / 16f) * 16;
            vector5.Y = (int)(vector5.Y / 16f) * 16;
            num55 -= vector5.X;
            num56 -= vector5.Y;
            float num68 = (float)Math.Sqrt(num55 * num55 + num56 * num56);
            if (npc.ai[1] > 0f && npc.ai[1] < (float)Main.npc.Length)
            {
                try
                {
                    vector5 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                    num55 = Main.npc[(int)npc.ai[1]].position.X + (float)(Main.npc[(int)npc.ai[1]].width / 2) - vector5.X;
                    num56 = Main.npc[(int)npc.ai[1]].position.Y + (float)(Main.npc[(int)npc.ai[1]].height / 2) - vector5.Y;
                }
                catch
                {
                }

                npc.rotation = (float)Math.Atan2(num56, num55) + MathHelper.PiOver2;
                num68 = (float)Math.Sqrt(num55 * num55 + num56 * num56);
                int num69 = npc.width;
                num69 = (int)((float)num69 * npc.scale);

                if (Main.getGoodWorld)
                    num69 = 62;

                num68 = (num68 - (float)num69) / num68;
                num55 *= num68;
                num56 *= num68;
                npc.velocity = Vector2.Zero;
                npc.position.X += num55;
                npc.position.Y += num56;
            }
            else
            {
                if (!flag2)
                {
                    npc.velocity.Y += 0.11f;
                    if (Main.masterMode && npc.velocity.Y > 0f)
                        npc.velocity.Y += 0.07f;

                    if (npc.velocity.Y > num52)
                        npc.velocity.Y = num52;

                    // This bool exists to stop the strange wiggle behavior when worms are falling down
                    bool slowXVelocity = Math.Abs(npc.velocity.X) > num53;
                    if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)num52 * 0.4)
                    {
                        if (npc.velocity.X < 0f)
                            npc.velocity.X -= num53 * 1.1f;
                        else
                            npc.velocity.X += num53 * 1.1f;
                    }
                    else if (npc.velocity.Y == num52)
                    {
                        if (slowXVelocity)
                        {
                            if (npc.velocity.X < num55)
                                npc.velocity.X += num53;
                            else if (npc.velocity.X > num55)
                                npc.velocity.X -= num53;
                        }
                        else
                            npc.velocity.X = 0f;
                    }
                    else if (npc.velocity.Y > 4f)
                    {
                        if (slowXVelocity)
                        {
                            if (npc.velocity.X < 0f)
                                npc.velocity.X += num53 * 0.9f;
                            else
                                npc.velocity.X -= num53 * 0.9f;
                        }
                        else
                            npc.velocity.X = 0f;
                    }
                }
                else
                {
                    if (npc.soundDelay == 0)
                    {
                        float num70 = num68 / 40f;
                        if (num70 < 10f)
                            num70 = 10f;

                        if (num70 > 20f)
                            num70 = 20f;

                        npc.soundDelay = (int)num70;
                        SoundEngine.PlaySound(SoundID.WormDig, npc.Center);
                    }

                    num68 = (float)Math.Sqrt(num55 * num55 + num56 * num56);
                    float num71 = Math.Abs(num55);
                    float num72 = Math.Abs(num56);
                    float num73 = num52 / num68;
                    num55 *= num73;
                    num56 *= num73;
                    bool flag4 = false;
                    if (npc.type == NPCID.EaterofWorldsHead && ((!Main.player[npc.target].ZoneCorrupt && !Main.player[npc.target].ZoneCrimson) || Main.player[npc.target].dead))
                        flag4 = true;

                    if (flag4)
                    {
                        bool flag5 = true;
                        for (int num74 = 0; num74 < Main.maxPlayers; num74++)
                        {
                            if (Main.player[num74].active && !Main.player[num74].dead && Main.player[num74].ZoneCorrupt)
                                flag5 = false;
                        }

                        if (flag5)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient && (double)(npc.position.Y / 16f) > (Main.rockLayer + (double)Main.maxTilesY) / 2.0)
                            {
                                npc.active = false;
                                int num75 = (int)npc.ai[0];
                                while (num75 > 0 && num75 < Main.maxNPCs && Main.npc[num75].active && Main.npc[num75].aiStyle == npc.aiStyle)
                                {
                                    int num76 = (int)Main.npc[num75].ai[0];
                                    Main.npc[num75].active = false;
                                    npc.life = 0;
                                    if (Main.netMode == NetmodeID.Server)
                                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, num75);

                                    num75 = num76;
                                }

                                if (Main.netMode == NetmodeID.Server)
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                            }

                            num55 = 0f;
                            num56 = num52;
                        }
                    }

                    if ((npc.velocity.X > 0f && num55 > 0f) || (npc.velocity.X < 0f && num55 < 0f) || (npc.velocity.Y > 0f && num56 > 0f) || (npc.velocity.Y < 0f && num56 < 0f))
                    {
                        if (npc.velocity.X < num55)
                            npc.velocity.X += num53;
                        else if (npc.velocity.X > num55)
                            npc.velocity.X -= num53;

                        if (npc.velocity.Y < num56)
                            npc.velocity.Y += num53;
                        else if (npc.velocity.Y > num56)
                            npc.velocity.Y -= num53;

                        if ((double)Math.Abs(num56) < (double)num52 * 0.2 && ((npc.velocity.X > 0f && num55 < 0f) || (npc.velocity.X < 0f && num55 > 0f)))
                        {
                            if (npc.velocity.Y > 0f)
                                npc.velocity.Y += num53 * 2f;
                            else
                                npc.velocity.Y -= num53 * 2f;
                        }

                        if ((double)Math.Abs(num55) < (double)num52 * 0.2 && ((npc.velocity.Y > 0f && num56 < 0f) || (npc.velocity.Y < 0f && num56 > 0f)))
                        {
                            if (npc.velocity.X > 0f)
                                npc.velocity.X += num53 * 2f;
                            else
                                npc.velocity.X -= num53 * 2f;
                        }
                    }
                    else if (num71 > num72)
                    {
                        if (npc.velocity.X < num55)
                            npc.velocity.X += num53 * 1.1f;
                        else if (npc.velocity.X > num55)
                            npc.velocity.X -= num53 * 1.1f;

                        if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)num52 * 0.5)
                        {
                            if (npc.velocity.Y > 0f)
                                npc.velocity.Y += num53;
                            else
                                npc.velocity.Y -= num53;
                        }
                    }
                    else
                    {
                        if (npc.velocity.Y < num56)
                            npc.velocity.Y += num53 * 1.1f;
                        else if (npc.velocity.Y > num56)
                            npc.velocity.Y -= num53 * 1.1f;

                        if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)num52 * 0.5)
                        {
                            if (npc.velocity.X > 0f)
                                npc.velocity.X += num53;
                            else
                                npc.velocity.X -= num53;
                        }
                    }
                }

                npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + MathHelper.PiOver2;
                if (npc.type == NPCID.EaterofWorldsHead)
                {
                    if (flag2)
                    {
                        if (npc.localAI[0] != 1f)
                            npc.netUpdate = true;

                        npc.localAI[0] = 1f;
                    }
                    else
                    {
                        if (npc.localAI[0] != 0f)
                            npc.netUpdate = true;

                        npc.localAI[0] = 0f;
                    }

                    if (((npc.velocity.X > 0f && npc.oldVelocity.X < 0f) || (npc.velocity.X < 0f && npc.oldVelocity.X > 0f) || (npc.velocity.Y > 0f && npc.oldVelocity.Y < 0f) || (npc.velocity.Y < 0f && npc.oldVelocity.Y > 0f)) && !npc.justHit)
                        npc.netUpdate = true;
                }
            }

            // Calculate contact damage based on velocity
            // This worm requires more velocity to deal damage with the body because it doesn't have spikes or metal bits or etc.
            float minimalContactDamageHeadVelocity = num52 * 0.25f;
            float minimalDamageHeadVelocity = num52 * 0.5f;
            float minimalContactDamageBodyVelocity = num52 * 0.4f;
            float minimalDamageBodyVelocity = num52 * 0.8f;
            if (npc.type == NPCID.EaterofWorldsHead)
            {
                if (npc.velocity.Length() <= minimalContactDamageHeadVelocity)
                {
                    npc.damage = (int)Math.Round(npc.defDamage * 0.5);
                }
                else
                {
                    float velocityDamageScalar = MathHelper.Clamp((npc.velocity.Length() - minimalContactDamageHeadVelocity) / minimalDamageHeadVelocity, 0f, 1f);
                    npc.damage = (int)MathHelper.Lerp((float)Math.Round(npc.defDamage * 0.5), npc.defDamage, velocityDamageScalar);
                }
            }
            else
            {
                float bodyAndTailVelocity = (npc.position - npc.oldPosition).Length();
                if (bodyAndTailVelocity <= minimalContactDamageBodyVelocity)
                {
                    npc.damage = 0;
                }
                else
                {
                    float velocityDamageScalar = MathHelper.Clamp((bodyAndTailVelocity - minimalContactDamageBodyVelocity) / minimalDamageBodyVelocity, 0f, 1f);
                    npc.damage = (int)MathHelper.Lerp(0f, npc.defDamage, velocityDamageScalar);
                }
            }

            if (npc.type == NPCID.EaterofWorldsHead || (npc.type != NPCID.EaterofWorldsHead && Main.npc[(int)npc.ai[1]].alpha >= 85))
            {
                if (npc.alpha > 0 && npc.life > 0)
                {
                    for (int num80 = 0; num80 < 2; num80++)
                    {
                        int num81 = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, DustID.Demonite, 0f, 0f, 100, default(Color), 2f);
                        Main.dust[num81].noGravity = true;
                        Main.dust[num81].noLight = true;
                    }
                }

                if ((npc.position - npc.oldPosition).Length() > 2f)
                {
                    npc.alpha -= 42;
                    if (npc.alpha < 0)
                        npc.alpha = 0;
                }
            }
            else if (npc.type > NPCID.EaterofWorldsHead && npc.alpha > 0)
            {
                npc.alpha -= 42;
                if (npc.alpha < 0)
                    npc.alpha = 0;
            }

            return false;
        }

        public static int GetEaterOfWorldsSegmentsCountRevDeath()
        {
            return CalamityWorld.LegendaryMode ? 100 :
                (CalamityWorld.death || BossRushEvent.BossRushActive) ? ((Main.masterMode || BossRushEvent.BossRushActive) ? 60 : 57) :
                ((Main.masterMode || BossRushEvent.BossRushActive) ? 68 : 62);
        }

        public static int GetEaterOfWorldsSegmentsCountVanilla()
        {
            return (Main.masterMode || BossRushEvent.BossRushActive) ? 75 : Main.expertMode ? 70 : 65;
        }
    }
}
