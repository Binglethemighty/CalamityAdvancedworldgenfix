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
    public static class SkeletronAI
    {
        public const float ChargeGateValue = 600f;
        public const float ChargeTelegraphTime = 120f;
        public const float HandSlapGateValue = 300f;
        public const float HandSlapTelegraphTime = 120f;
        public const float HandSwipeDistance = 960f; // 60 tiles
        public const float HandSwipeDistance_Master = 1280f; // 80 tiles

        public static bool BuffedSkeletronAI(NPC npc, Mod mod)
        {
            CalamityGlobalNPC calamityGlobalNPC = npc.Calamity();

            bool bossRush = BossRushEvent.BossRushActive;
            bool masterMode = Main.masterMode || bossRush;
            bool death = CalamityWorld.death || bossRush;

            // Percent life remaining
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Phases
            float phase2LifeRatio = masterMode ? 1f : 0.85f;
            float phase3LifeRatio = masterMode ? 0.9f : 0.7f;
            float respawnHandsLifeRatio = 0.5f;
            float phase4LifeRatio = masterMode ? 0.4f : 0.3f;
            float useSkullSpreadsAfterChargeLifeRatio = masterMode ? 0.3f : 0.2f;
            float phase5LifeRatio = masterMode ? 0.2f : 0.1f;

            // Begin firing spreads of skulls phase
            bool phase2 = lifeRatio < phase2LifeRatio;

            // Begin using a more dangerous charge attack phase
            bool phase3 = lifeRatio < phase3LifeRatio;

            // Spawn a new set of hands, fire skulls at the end of each charge and fire skulls from hands at the end of each slap phase
            bool respawnHands = lifeRatio < respawnHandsLifeRatio;

            // Fire giant cursed skull projectiles (yes, these curse you if you get hit) during charge attack and hands fire skulls phase
            bool phase4 = lifeRatio < phase4LifeRatio;

            // Self-explanatory
            bool useSkullSpreadsAfterCharge = lifeRatio < useSkullSpreadsAfterChargeLifeRatio;

            // Rapid teleport and charge, stop using idle phase
            bool phase5 = lifeRatio < phase5LifeRatio;

            // Set defense
            npc.defense = npc.defDefense;

            npc.reflectsProjectiles = false;

            // Get a target
            if (npc.target < 0 || npc.target == Main.maxPlayers || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            // Despawn safety, make sure to target another player if the current player target is too far away
            if (Vector2.Distance(Main.player[npc.target].Center, npc.Center) > CalamityGlobalNPC.CatchUpDistance200Tiles)
                npc.TargetClosest();

            // Spawn hands
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (npc.ai[0] == 0f)
                {
                    npc.ai[0] = 1f;
                    SpawnHands();
                    npc.netUpdate = true;
                }

                // Respawn hands
                if (respawnHands && calamityGlobalNPC.newAI[0] == 0f && Vector2.Distance(Main.player[npc.target].Center, npc.Center) > 160f)
                {
                    calamityGlobalNPC.newAI[0] = 1f;
                    SoundEngine.PlaySound(SoundID.ForceRoar with { Pitch = SoundID.ForceRoar.Pitch - 0.25f }, npc.Center);
                    SpawnHands();

                    npc.netUpdate = true;
                    npc.SyncExtraAI();
                }

                void SpawnHands()
                {
                    int skeletronHand = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.SkeletronHand, npc.whoAmI);
                    Main.npc[skeletronHand].ai[0] = masterMode ? -1.3f : -1f;
                    Main.npc[skeletronHand].ai[1] = npc.whoAmI;
                    Main.npc[skeletronHand].target = npc.target;
                    Main.npc[skeletronHand].netUpdate = true;

                    skeletronHand = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.SkeletronHand, npc.whoAmI);
                    Main.npc[skeletronHand].ai[0] = masterMode ? 1.3f : 1f;
                    Main.npc[skeletronHand].ai[1] = npc.whoAmI;
                    Main.npc[skeletronHand].ai[3] = 150f;
                    Main.npc[skeletronHand].Calamity().newAI[2] = 150f;
                    Main.npc[skeletronHand].target = npc.target;
                    Main.npc[skeletronHand].netUpdate = true;

                    // Spawn two additional hands with different attack timings
                    if (death)
                    {
                        skeletronHand = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.SkeletronHand, npc.whoAmI);
                        Main.npc[skeletronHand].ai[0] = masterMode ? -1.3f : -1f;
                        Main.npc[skeletronHand].Calamity().newAI[0] = -1f;
                        Main.npc[skeletronHand].ai[1] = npc.whoAmI;
                        Main.npc[skeletronHand].ai[3] = respawnHands ? -75f : 0f;
                        Main.npc[skeletronHand].Calamity().newAI[2] = respawnHands ? -75f : 0f;
                        Main.npc[skeletronHand].target = npc.target;
                        Main.npc[skeletronHand].netUpdate = true;

                        skeletronHand = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.SkeletronHand, npc.whoAmI);
                        Main.npc[skeletronHand].ai[0] = masterMode ? 1.3f : 1f;
                        Main.npc[skeletronHand].Calamity().newAI[0] = -1f;
                        Main.npc[skeletronHand].ai[1] = npc.whoAmI;
                        Main.npc[skeletronHand].ai[3] = respawnHands ? 75f : 150f;
                        Main.npc[skeletronHand].Calamity().newAI[2] = respawnHands ? 75f : 150f;
                        Main.npc[skeletronHand].target = npc.target;
                        Main.npc[skeletronHand].netUpdate = true;
                    }
                }
            }

            // Distance from target
            float distance = Vector2.Distance(Main.player[npc.target].Center, npc.Center);

            // Despawn
            if (npc.ai[1] != 3f)
            {
                int despawnDistanceInTiles = 500;
                if (Main.player[npc.target].dead || Math.Abs(npc.Center.X - Main.player[npc.target].Center.X) / 16f > despawnDistanceInTiles)
                {
                    npc.TargetClosest();
                    if (Main.player[npc.target].dead || Math.Abs(npc.Center.X - Main.player[npc.target].Center.X) / 16f > despawnDistanceInTiles)
                        npc.ai[1] = 3f;
                }
                else if (npc.timeLeft < 1800)
                    npc.timeLeft = 1800;
            }

            // Daytime enrage
            if (Main.IsItDay() && !bossRush && npc.ai[1] != 3f && npc.ai[1] != 2f)
            {
                npc.ai[1] = 2f;
                SoundEngine.PlaySound(SoundID.ForceRoar, npc.Center);
            }

            // Hand count
            int numHandsAlive = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == NPCID.SkeletronHand)
                    numHandsAlive++;
            }

            // Hand variables
            bool handsDead = numHandsAlive == 0;
            int numProj = Main.getGoodWorld ? 22 : death ? 5 : 3;
            float spread = MathHelper.ToRadians(Main.getGoodWorld ? 180 : 60);
            float headSpinVelocityMult = bossRush ? (phase3 ? 18f : 9f) : (phase3 ? 13.5f : 4.5f);

            switch (numHandsAlive)
            {
                case 0:
                    numProj = Main.getGoodWorld ? 36 : death ? 9 : 7;
                    spread = MathHelper.ToRadians(Main.getGoodWorld ? 180 : death ? 90 : 82);
                    headSpinVelocityMult = bossRush ? (phase3 ? 18f : 12f) : (phase3 ? 13.5f : 6f);
                    break;

                case 1:
                    numProj = Main.getGoodWorld ? 27 : death ? 7 : 5;
                    spread = MathHelper.ToRadians(Main.getGoodWorld ? 150 : death ? 76 : 68);
                    headSpinVelocityMult = bossRush ? (phase3 ? 15f : 10f) : (phase3 ? 12.75f : 5f);
                    break;

                case 2:
                    numProj = Main.getGoodWorld ? 18 : death ? 6 : 4;
                    spread = MathHelper.ToRadians(Main.getGoodWorld ? 140 : death ? 70 : 62);
                    headSpinVelocityMult = bossRush ? (phase3 ? 13.5f : 9f) : (phase3 ? 12f : 4.5f);
                    break;

                case 3:
                    numProj = Main.getGoodWorld ? 15 : death ? 5 : 3;
                    spread = MathHelper.ToRadians(Main.getGoodWorld ? 130 : death ? 64 : 56);
                    headSpinVelocityMult = bossRush ? (phase3 ? 12f : 8f) : (phase3 ? 11.25f : 4f);
                    break;

                case 4:
                    numProj = Main.getGoodWorld ? 12 : death ? 4 : 3;
                    spread = MathHelper.ToRadians(Main.getGoodWorld ? 120 : 56);
                    headSpinVelocityMult = bossRush ? (phase3 ? 10.5f : 7f) : (phase3 ? 10.5f : 3.5f);
                    break;
            }

            // Reduce the amount of skulls per spread in later phases due to near-constant teleporting
            if (!masterMode && numProj > 3)
            {
                if (phase4)
                    numProj--;
                if (phase5 && numProj > 3)
                    numProj--;
            }

            if (death)
                headSpinVelocityMult *= 1.2f;

            // Hand DR, scale DR up if the hands are still alive as Skeletron's HP lowers
            npc.chaseable = handsDead;
            float minDR = 0.05f;
            float maxDR = 0.9999f;
            calamityGlobalNPC.DR = !handsDead ? (float)Math.Sqrt(MathHelper.Lerp(minDR, maxDR, respawnHands ? (respawnHandsLifeRatio - lifeRatio) / respawnHandsLifeRatio : 2f - lifeRatio / respawnHandsLifeRatio)) : minDR;
            calamityGlobalNPC.unbreakableDR = !handsDead;
            calamityGlobalNPC.CurrentlyIncreasingDefenseOrDR = !handsDead;

            // Value to start teleport dust
            int teleportGateValue = phase5 ? 180 : 300;

            // Bool to disable skull firing after charging if teleport was recent or is about to happen
            bool disableSkullsAfterCharge = npc.ai[3] <= 60f || npc.ai[3] > teleportGateValue + 60f;

            // Teleport while not despawning
            if (npc.ai[1] != 3f)
            {
                int dustType = DustID.GemDiamond;

                // Post-teleport
                if (npc.ai[3] == -60f)
                {
                    npc.ai[3] = 0f;

                    SoundEngine.PlaySound(SoundID.Item66, npc.Center);

                    // Fire skulls after teleport
                    if (Collision.CanHit(npc.Center, 1, 1, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float skullSpeed = death ? 6f : 5f;
                            int type = ProjectileID.Skull;
                            int damage = npc.GetProjectileDamage(type);

                            float boltTargetXDist = Main.player[npc.target].Center.X - npc.Center.X;
                            float boltTargetYDist = Main.player[npc.target].Center.Y - npc.Center.Y;
                            float boltTargetDistance = (float)Math.Sqrt(boltTargetXDist * boltTargetXDist + boltTargetYDist * boltTargetYDist);

                            boltTargetDistance = skullSpeed / boltTargetDistance;
                            boltTargetXDist *= boltTargetDistance;
                            boltTargetYDist *= boltTargetDistance;
                            Vector2 center = npc.Center;
                            center.X += boltTargetXDist * 5f;
                            center.Y += boltTargetYDist * 5f;

                            float baseSpeed = (float)Math.Sqrt(boltTargetXDist * boltTargetXDist + boltTargetYDist * boltTargetYDist);
                            double startAngle = Math.Atan2(boltTargetXDist, boltTargetYDist) - spread / 2;
                            double deltaAngle = spread / numProj;
                            double offsetAngle;

                            // Inverse parabolic projectile spreads
                            bool evenNumberOfProjectiles = numProj % 2 == 0;
                            int centralProjectile = evenNumberOfProjectiles ? numProj / 2 : (numProj - 1) / 2;
                            int otherCentralProjectile = evenNumberOfProjectiles ? centralProjectile - 1 : -1;
                            float centralScalingAmount = (float)Math.Floor(numProj / (double)centralProjectile) * 0.75f;
                            float amountToAdd = evenNumberOfProjectiles ? 0.5f : 0f;
                            float originalBaseSpeed = baseSpeed;
                            float minVelocityMultiplier = 0.5f;
                            for (int i = 0; i < numProj; i++)
                            {
                                float velocityScalar = (evenNumberOfProjectiles && (i == centralProjectile || i == otherCentralProjectile)) ? minVelocityMultiplier : MathHelper.Lerp(minVelocityMultiplier, centralScalingAmount, Math.Abs((i + amountToAdd) - centralProjectile) / (float)centralProjectile);
                                baseSpeed = originalBaseSpeed;
                                baseSpeed *= velocityScalar;
                                offsetAngle = startAngle + deltaAngle * i;
                                int proj = Projectile.NewProjectile(npc.GetSource_FromAI(), center.X, center.Y, baseSpeed * (float)Math.Sin(offsetAngle), baseSpeed * (float)Math.Cos(offsetAngle), type, damage, 0f, Main.myPlayer, -2f);
                                Main.projectile[proj].timeLeft = 600;
                            }

                            npc.netUpdate = true;
                        }
                    }

                    // Teleport dust
                    for (int m = 0; m < 30; m++)
                    {
                        int teleportDust = Dust.NewDust(npc.position, npc.width, npc.height, dustType, 0f, 0f, 100, default, 3f);
                        Main.dust[teleportDust].noGravity = true;
                        Main.dust[teleportDust].velocity.X *= 2f;
                    }
                }

                // Teleport after a certain time
                // If hands are dead: 7 seconds
                // If hands are not dead: 14 seconds
                // If hands are dead in phase 2: 4.7 seconds
                npc.ai[3] += 1f + (((phase2 && handsDead) || bossRush || phase4) ? 0.5f : 0f) - ((handsDead || bossRush) ? 0f : 0.5f);

                // Dust to show teleport
                int ai3 = (int)npc.ai[3]; // 0 to 30, and -60
                bool emitDust = false;

                if (ai3 >= teleportGateValue && calamityGlobalNPC.newAI[2] == 0f && calamityGlobalNPC.newAI[3] == 0f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 skullFaceDirection = npc.Center + new Vector2(npc.direction * 20, 6f);
                        Vector2 skullTargetDirection = Main.player[npc.target].Center - skullFaceDirection;
                        Point skullTileCoords = npc.Center.ToTileCoordinates();
                        Point targetTileCoords = Main.player[npc.target].Center.ToTileCoordinates();
                        int randomTeleportOffset = 20 - (int)Math.Ceiling(MathHelper.Lerp(0f, 10f, 1f - lifeRatio));
                        int skullPositionOffset = 4;
                        int targetPositionOffset = randomTeleportOffset - 4;
                        int teleportTries = 0;

                        bool targetTooFar = false;
                        if (skullTargetDirection.Length() > 2000f)
                            targetTooFar = true;

                        while (!targetTooFar && teleportTries < 100)
                        {
                            teleportTries++;
                            int teleportTileX = Main.rand.Next(targetTileCoords.X - randomTeleportOffset, targetTileCoords.X + randomTeleportOffset + 1);
                            int teleportTileY = Main.rand.Next(targetTileCoords.Y - randomTeleportOffset, targetTileCoords.Y + randomTeleportOffset + 1);
                            if ((teleportTileY < targetTileCoords.Y - targetPositionOffset || teleportTileY > targetTileCoords.Y + targetPositionOffset || teleportTileX < targetTileCoords.X - targetPositionOffset || teleportTileX > targetTileCoords.X + targetPositionOffset) && (teleportTileY < skullTileCoords.Y - skullPositionOffset || teleportTileY > skullTileCoords.Y + skullPositionOffset || teleportTileX < skullTileCoords.X - skullPositionOffset || teleportTileX > skullTileCoords.X + skullPositionOffset) && !Main.tile[teleportTileX, teleportTileY].HasUnactuatedTile)
                            {
                                // New location params
                                calamityGlobalNPC.newAI[2] = teleportTileX * 16 - npc.width / 2;
                                calamityGlobalNPC.newAI[3] = teleportTileY * 16 - npc.height;
                                npc.SyncExtraAI();
                                break;
                            }
                        }
                    }
                }

                if (calamityGlobalNPC.newAI[2] != 0f && calamityGlobalNPC.newAI[3] != 0f)
                {
                    for (int m = 0; m < 5; m++)
                    {
                        Vector2 position = new Vector2(calamityGlobalNPC.newAI[2], calamityGlobalNPC.newAI[3]);
                        int teleportDust = Dust.NewDust(position, npc.width, npc.height, dustType, 0f, 0f, 100, default, 2f);
                        Main.dust[teleportDust].noGravity = true;
                    }
                }

                if (ai3 >= teleportGateValue + 90)
                {
                    emitDust = true;
                }
                else if (ai3 >= teleportGateValue + 30)
                {
                    if (Main.rand.Next(teleportGateValue + 10, ai3 + 1) >= teleportGateValue + 25)
                        emitDust = true;
                }

                if (emitDust)
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, dustType, 0f, 0f, 100, default, 1.5f);
                    Main.dust[dust].noGravity = true;
                }

                // Teleport
                if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[3] >= teleportGateValue + 120)
                {
                    // Teleport dust
                    for (int m = 0; m < 30; m++)
                    {
                        int teleportDust = Dust.NewDust(npc.position, npc.width, npc.height, dustType, 0f, 0f, 100, default, 3f);
                        Main.dust[teleportDust].noGravity = true;
                        Main.dust[teleportDust].velocity.X *= 2f;
                    }

                    // New location
                    npc.Center = new Vector2(calamityGlobalNPC.newAI[2], calamityGlobalNPC.newAI[3]);

                    // Do not set velocity to zero during charge attacks
                    if (npc.ai[1] != 1f)
                        npc.velocity = Vector2.Zero;

                    npc.ai[3] = -60f;
                    calamityGlobalNPC.newAI[2] = calamityGlobalNPC.newAI[3] = 0f;
                    npc.SyncExtraAI();
                    npc.netUpdate = true;
                }
            }

            // Skull shooting
            if ((handsDead || masterMode) && npc.ai[1] == 0f && !phase4)
            {
                float skullProjFrequency = bossRush ? 10f : phase2 ? (48f - (death ? 17.5f * (1f - lifeRatio) : 0f)) : 60f;
                if (Main.getGoodWorld)
                    skullProjFrequency *= 0.8f;
                skullProjFrequency = (float)Math.Ceiling(skullProjFrequency);

                if (Main.netMode != NetmodeID.MultiplayerClient && calamityGlobalNPC.newAI[1] % skullProjFrequency == 0f && calamityGlobalNPC.newAI[1] > 45f)
                {
                    Vector2 skullFiringPos = npc.Center;
                    float skullProjTargetX = Main.player[npc.target].Center.X - skullFiringPos.X;
                    float skullProjTargetY = Main.player[npc.target].Center.Y - skullFiringPos.Y;
                    if (Collision.CanHit(skullFiringPos, 1, 1, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                    {
                        float skullProjSpeed = phase2 ? (5f + (death ? 3f * (1f - lifeRatio) : 0f)) : 4f;
                        int spread2 = bossRush ? 100 : 50;
                        Vector2 skullProjDirection = new Vector2(skullProjTargetX + Main.rand.Next(-spread2, spread2 + 1) * 0.01f, skullProjTargetY + Main.rand.Next(-spread2, spread2 + 1) * 0.01f).SafeNormalize(Vector2.UnitY);
                        skullProjDirection *= skullProjSpeed;
                        skullProjDirection += npc.velocity;
                        skullFiringPos += skullProjDirection * 5f;

                        int type = ProjectileID.Skull;
                        int damage = npc.GetProjectileDamage(type);

                        int skullProjectile = Projectile.NewProjectile(npc.GetSource_FromAI(), skullFiringPos, skullProjDirection, type, damage, 0f, Main.myPlayer, -1f);
                        Main.projectile[skullProjectile].timeLeft = 600;
                        if (masterMode && handsDead)
                        {
                            skullProjDirection = new Vector2(skullProjTargetX, skullProjTargetY).SafeNormalize(Vector2.UnitY);
                            skullProjDirection *= skullProjSpeed * 2f;
                            int skullProjectile2 = Projectile.NewProjectile(npc.GetSource_FromAI(), skullFiringPos, skullProjDirection, type, damage, 0f, Main.myPlayer, -2f);
                            Main.projectile[skullProjectile2].timeLeft = 600;
                        }

                        npc.netUpdate = true;
                    }
                }
            }

            // Float above target
            if (npc.ai[1] == 0f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                calamityGlobalNPC.newAI[1] += 1f;
                float chargePhaseChangeRateBoost = phase5 ? (death ? 24f : 14f) : phase4 ? (death ? 8f : 6f) : (((masterMode && death) ? 6f : 4f) * ((1f - lifeRatio) / (1f - phase4LifeRatio)));
                if (!handsDead)
                    chargePhaseChangeRateBoost *= 0.5f;

                float chargePhaseChangeRate = chargePhaseChangeRateBoost + 1f;
                npc.ai[2] += chargePhaseChangeRate;
                npc.localAI[1] += chargePhaseChangeRate;
                float chargePhaseGateValue = ChargeGateValue;
                if (npc.localAI[1] > chargePhaseGateValue)
                    npc.localAI[1] = chargePhaseGateValue;

                float forcedMoveAwayTime = death ? 30f : 45f;
                if (masterMode)
                    forcedMoveAwayTime *= 0.5f;

                float canChargeDistance = phase3 ? 640f : 320f; // 20 tile distance, 40 tile distance in phase 3
                bool hasMovedForcedDistance = npc.localAI[2] >= forcedMoveAwayTime;
                bool canCharge = Vector2.Distance(Main.player[npc.target].Center, npc.Center) >= canChargeDistance;
                bool charge = npc.ai[2] >= chargePhaseGateValue && canCharge;
                bool forceCharge = npc.ai[2] > chargePhaseGateValue + 120f * chargePhaseChangeRate;
                if (charge || forceCharge)
                {
                    npc.localAI[2] += 1f;
                    if (hasMovedForcedDistance || !phase3)
                    {
                        npc.ai[2] = 0f;
                        npc.ai[1] = 1f;
                        npc.localAI[1] = chargePhaseGateValue;
                        npc.localAI[2] = 0f;
                        calamityGlobalNPC.newAI[1] = 0f;

                        npc.TargetClosest();
                        npc.SyncExtraAI();
                        npc.SyncVanillaLocalAI();
                        npc.netUpdate = true;
                    }
                }

                float headYAcceleration = (Main.getGoodWorld ? 0.07f : masterMode ? 0.06f : 0.04f) + (death ? 0.04f * (1f - lifeRatio) : 0f);
                float headYTopSpeed = headYAcceleration * 100f;
                float headXAcceleration = (Main.getGoodWorld ? 0.21f : masterMode ? 0.16f : 0.08f) + (death ? 0.08f * (1f - lifeRatio) : 0f);
                float headXTopSpeed = headXAcceleration * 100f;
                float deceleration = Main.getGoodWorld ? 0.83f : masterMode ? 0.86f : 0.89f;

                if (bossRush)
                {
                    headYAcceleration *= 1.25f;
                    headXAcceleration *= 1.25f;
                }

                float moveAwayGateValue = chargePhaseGateValue - (5f + chargePhaseChangeRate);
                bool moveAwayBeforeCharge = npc.ai[2] >= moveAwayGateValue;
                if (moveAwayBeforeCharge)
                {
                    if (!canCharge || !hasMovedForcedDistance)
                    {
                        float maxAcceleration = headXAcceleration + headYAcceleration + (npc.ai[2] - moveAwayGateValue) * (phase5 ? 0.006f : 0.004f);
                        float maxAccelerationCap = (headXAcceleration + headYAcceleration) * 5f;
                        if (maxAcceleration > maxAccelerationCap)
                            maxAcceleration = maxAccelerationCap;

                        npc.velocity += (Main.player[npc.target].Center - npc.Center).SafeNormalize(Vector2.UnitY) * -maxAcceleration;

                        float maxVelocity = headSpinVelocityMult + (npc.ai[2] - moveAwayGateValue) * (phase5 ? 0.12f : 0.08f);
                        float maxVelocityCap = headSpinVelocityMult * 2.5f;
                        if (maxVelocity > maxVelocityCap)
                            maxVelocity = maxVelocityCap;

                        if (npc.velocity.Length() > maxVelocity)
                        {
                            npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY);
                            npc.velocity *= maxVelocity;
                        }
                    }

                    // New charge attack
                    if (phase3)
                    {
                        npc.rotation += npc.direction * 0.3f;

                        if (npc.localAI[0] == 0f)
                        {
                            npc.localAI[0] = 1f;
                            npc.SyncVanillaLocalAI();

                            SoundEngine.PlaySound(SoundID.ForceRoar, npc.Center);
                        }
                    }
                    else
                        npc.rotation = npc.velocity.X / 15f;

                    return false;
                }

                npc.rotation = npc.velocity.X / 15f;

                if (npc.Top.Y > Main.player[npc.target].Top.Y - 250f)
                {
                    if (npc.velocity.Y > 0f)
                        npc.velocity.Y *= 0.98f;
                    npc.velocity.Y -= headYAcceleration;
                    if (npc.velocity.Y > headYTopSpeed)
                        npc.velocity.Y = headYTopSpeed;
                }
                else if (npc.Top.Y < Main.player[npc.target].Top.Y - 250f)
                {
                    if (npc.velocity.Y < 0f)
                        npc.velocity.Y *= 0.98f;
                    npc.velocity.Y += headYAcceleration;
                    if (npc.velocity.Y < -headYTopSpeed)
                        npc.velocity.Y = -headYTopSpeed;
                }

                if (npc.Center.X > Main.player[npc.target].Center.X)
                {
                    if (npc.velocity.X > 0f)
                        npc.velocity.X *= 0.98f;
                    npc.velocity.X -= headXAcceleration;
                    if (npc.velocity.X > headXTopSpeed)
                        npc.velocity.X = headXTopSpeed;
                }

                if (npc.Center.X < Main.player[npc.target].Center.X)
                {
                    if (npc.velocity.X < 0f)
                        npc.velocity.X *= 0.98f;
                    npc.velocity.X += headXAcceleration;
                    if (npc.velocity.X < -headXTopSpeed)
                        npc.velocity.X = -headXTopSpeed;
                }
            }

            // Spin charge
            else if (npc.ai[1] == 1f)
            {
                // Set damage
                npc.damage = npc.defDamage;

                if (Main.getGoodWorld)
                {
                    npc.reflectsProjectiles = true;
                    if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[2] == 0f)
                    {
                        if (NPC.CountNPCS(NPCID.DarkCaster) < 6)
                        {
                            for (int i = 0; i < 1000; i++)
                            {
                                int headYAcceleration = (int)(npc.Center.X / 16f) + Main.rand.Next(-50, 51);
                                int headYTopSpeed;
                                for (headYTopSpeed = (int)(npc.Center.Y / 16f) + Main.rand.Next(-50, 51); headYTopSpeed < Main.maxTilesY - 10 && !WorldGen.SolidTile(headYAcceleration, headYTopSpeed); headYTopSpeed++)
                                {
                                }

                                headYTopSpeed--;
                                if (!WorldGen.SolidTile(headYAcceleration, headYTopSpeed))
                                {
                                    int headXAcceleration = NPC.NewNPC(npc.GetSource_FromAI(), headYAcceleration * 16 + 8, headYTopSpeed * 16, NPCID.DarkCaster);
                                    if (Main.netMode == NetmodeID.Server && headXAcceleration < Main.maxNPCs)
                                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, headXAcceleration);

                                    break;
                                }
                            }
                        }

                        if (CalamityWorld.LegendaryMode)
                        {
                            if (!NPC.AnyNPCs(NPCID.DiabolistWhite))
                            {
                                for (int i = 0; i < 1000; i++)
                                {
                                    int headYAcceleration = (int)(npc.Center.X / 16f) + Main.rand.Next(-50, 51);
                                    int headYTopSpeed;
                                    for (headYTopSpeed = (int)(npc.Center.Y / 16f) + Main.rand.Next(-50, 51); headYTopSpeed < Main.maxTilesY - 10 && !WorldGen.SolidTile(headYAcceleration, headYTopSpeed); headYTopSpeed++)
                                    {
                                    }

                                    headYTopSpeed--;
                                    if (!WorldGen.SolidTile(headYAcceleration, headYTopSpeed))
                                    {
                                        int headXAcceleration = NPC.NewNPC(npc.GetSource_FromAI(), headYAcceleration * 16 + 8, headYTopSpeed * 16, NPCID.DiabolistWhite);
                                        if (Main.netMode == NetmodeID.Server && headXAcceleration < Main.maxNPCs)
                                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, headXAcceleration);

                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                npc.defense -= 10;

                float phaseChangeRateBoost = phase3 ? 0f : 1f - (lifeRatio - phase3LifeRatio) / (1f - phase3LifeRatio);
                npc.ai[2] += 1f + phaseChangeRateBoost;

                calamityGlobalNPC.newAI[1] += 1f;
                if (calamityGlobalNPC.newAI[1] == 2f)
                    SoundEngine.PlaySound(phase3 ? SoundID.ForceRoarPitched : SoundID.ForceRoar, npc.Center);

                // Shoot shadowflames (giant cursed skull projectiles) while charging in phase 4
                if (phase4 && Collision.CanHit(npc.Center, 1, 1, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                {
                    float shadowFlameGateValue = 20f;
                    int shadowFlameLimit = death ? 3 : 2;
                    if (calamityGlobalNPC.newAI[1] % shadowFlameGateValue == 0f && calamityGlobalNPC.newAI[1] < shadowFlameGateValue * shadowFlameLimit)
                    {
                        // Spawn projectiles
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (Vector2.Distance(Main.player[npc.target].Center, npc.Center) > 240f)
                            {
                                float shadowFlameProjectileSpeed = death ? 5f : 4f;
                                if (masterMode)
                                    shadowFlameProjectileSpeed *= 1.2f;

                                Vector2 initialProjectileVelocity = npc.Center.DirectionTo(Main.player[npc.target].Center) * shadowFlameProjectileSpeed;
                                int type = ProjectileID.Shadowflames;
                                int damage = npc.GetProjectileDamage(type);
                                int shadowFlameProjectile = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, initialProjectileVelocity, type, damage, 0f, Main.myPlayer, 0f, 1f);
                                Main.projectile[shadowFlameProjectile].timeLeft = 600;
                            }
                        }
                    }
                }

                // Reset telegraph timer to create color fade
                if (npc.localAI[1] > 0f)
                {
                    npc.localAI[1] -= 2f;
                    if (npc.localAI[1] <= 0f)
                    {
                        npc.localAI[1] = 0f;
                        npc.SyncVanillaLocalAI();
                    }
                }

                bool dontGoMach10 = false;
                float dashPhaseTime = masterMode ? 210f : 300f;
                if (npc.ai[2] >= dashPhaseTime)
                {
                    if (Main.getGoodWorld)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient && NPC.CountNPCS(NPCID.DarkCaster) < 6)
                        {
                            for (int j = 0; j < 1000; j++)
                            {
                                int headYAcceleration = (int)(npc.Center.X / 16f) + Main.rand.Next(-50, 51);
                                int headYTopSpeed;
                                for (headYTopSpeed = (int)(npc.Center.Y / 16f) + Main.rand.Next(-50, 51); headYTopSpeed < Main.maxTilesY - 10 && !WorldGen.SolidTile(headYAcceleration, headYTopSpeed); headYTopSpeed++)
                                {
                                }

                                headYTopSpeed--;
                                if (!WorldGen.SolidTile(headYAcceleration, headYTopSpeed))
                                {
                                    int headXAcceleration = NPC.NewNPC(npc.GetSource_FromAI(), headYAcceleration * 16 + 8, headYTopSpeed * 16, NPCID.DarkCaster);
                                    if (Main.netMode == NetmodeID.Server && headXAcceleration < Main.maxNPCs)
                                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, headXAcceleration);

                                    break;
                                }
                            }
                        }
                    }

                    if (useSkullSpreadsAfterCharge && !disableSkullsAfterCharge && Collision.CanHit(npc.Center, 1, 1, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                    {
                        // Spawn projectiles
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int chargeSkullAmt = death ? 5 : 3;
                            int chargeSkullSpread = death ? 80 : 60;
                            float rotation = MathHelper.ToRadians(chargeSkullSpread);
                            float skullProjSpeed = phase5 ? (6f + (death ? 2f * ((phase5LifeRatio - lifeRatio) / phase5LifeRatio) : 0f)) : 4f;
                            Vector2 initialProjectileVelocity = npc.Center.DirectionTo(Main.player[npc.target].Center) * skullProjSpeed;
                            int type = ProjectileID.Skull;
                            int damage = npc.GetProjectileDamage(type);
                            for (int k = 0; k < chargeSkullAmt + 1; k++)
                            {
                                Vector2 perturbedSpeed = initialProjectileVelocity.RotatedBy(MathHelper.Lerp(-rotation, rotation, k / (float)(chargeSkullAmt - 1)));
                                int proj = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center - perturbedSpeed.SafeNormalize(Vector2.UnitY) * 5f, perturbedSpeed, type, damage, 0f, Main.myPlayer, -1f);
                                Main.projectile[proj].timeLeft = 600;
                                if (masterMode)
                                {
                                    int proj2 = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center - perturbedSpeed.SafeNormalize(Vector2.UnitY) * 5f, perturbedSpeed, type, damage, 0f, Main.myPlayer, -2f);
                                    Main.projectile[proj2].timeLeft = 600;
                                }
                            }
                        }
                    }

                    npc.ai[2] = 0f;
                    npc.ai[1] = 0f;
                    npc.localAI[0] = 0f;
                    npc.localAI[1] = 0f;
                    calamityGlobalNPC.newAI[1] = 0f;

                    npc.TargetClosest();
                    npc.SyncVanillaLocalAI();
                    npc.SyncExtraAI();
                    npc.netUpdate = true;

                    dontGoMach10 = true;
                }

                npc.rotation += npc.direction * 0.3f;

                Vector2 headSpinPos = npc.Center;
                float headSpinTargetX = Main.player[npc.target].Center.X - headSpinPos.X;
                float headSpinTargetY = Main.player[npc.target].Center.Y - headSpinPos.Y;
                float headSpinTargetDist = (float)Math.Sqrt(headSpinTargetX * headSpinTargetX + headSpinTargetY * headSpinTargetY);

                // Increase speed while charging
                npc.damage = (int)Math.Round(npc.defDamage * 1.3);

                if (!phase3)
                {
                    float velocityBoost = MathHelper.Lerp(0f, 3f, (1f - lifeRatio) / (1f - phase3LifeRatio));
                    if (handsDead || bossRush)
                        headSpinVelocityMult += velocityBoost;
                }

                float altDashStopDistance = death ? (masterMode ? 320f : 360f) : (masterMode ? 360f : 400f);
                float headSpeedIncreaseDist = phase3 ? altDashStopDistance : 160f;
                if (headSpinTargetDist > headSpeedIncreaseDist)
                {
                    float velocityMult = phase3 ? 0.00075f : 0.0015f;
                    float baseDistanceVelocityMult = 1f + MathHelper.Clamp((headSpinTargetDist - headSpeedIncreaseDist) * 0.0015f, 0.05f, masterMode ? 2f : 1.5f);
                    headSpinVelocityMult *= baseDistanceVelocityMult;
                }

                if (Main.getGoodWorld)
                    headSpinVelocityMult *= 1.3f;

                headSpinTargetDist = headSpinVelocityMult / headSpinTargetDist;
                Vector2 headSpinVelocity = new Vector2(headSpinTargetX, headSpinTargetY) * headSpinTargetDist;

                if (!dontGoMach10)
                {
                    if (phase3)
                    {
                        // Dash directly towards the target until within 15 tiles of the target, and then continue in the same direction
                        float altDashPhaseTime = dashPhaseTime * (death ? 0.9f : 0.85f);
                        if (npc.ai[2] < altDashPhaseTime)
                        {
                            if (npc.Center.Distance(Main.player[npc.target].Center) > altDashStopDistance || npc.ai[2] == 1f + phaseChangeRateBoost)
                                npc.velocity = headSpinVelocity.SafeNormalize(Vector2.UnitY) * headSpinVelocityMult + npc.Center.DirectionTo(Main.player[npc.target].Center + (bossRush ? Main.player[npc.target].velocity * 20f : Vector2.Zero)) * 2f;
                            else
                                npc.ai[2] = altDashPhaseTime;
                        }
                    }
                    else
                        npc.velocity = headSpinVelocity;
                }
            }

            // Daytime enrage
            else if (npc.ai[1] == 2f)
            {
                npc.damage = 1000;
                calamityGlobalNPC.DR = 0.9999f;
                calamityGlobalNPC.unbreakableDR = true;

                calamityGlobalNPC.CurrentlyEnraged = true;
                calamityGlobalNPC.CurrentlyIncreasingDefenseOrDR = true;

                npc.rotation += npc.direction * 0.3f;
                Vector2 enrageSpinPos = npc.Center;
                float enrageSpinTargetX = Main.player[npc.target].Center.X - enrageSpinPos.X;
                float enrageSpinTargetY = Main.player[npc.target].Center.Y - enrageSpinPos.Y;
                float enrageSpinTargetDist = (float)Math.Sqrt(enrageSpinTargetX * enrageSpinTargetX + enrageSpinTargetY * enrageSpinTargetY);
                enrageSpinTargetDist = 8f / enrageSpinTargetDist;
                npc.velocity.X = enrageSpinTargetX * enrageSpinTargetDist;
                npc.velocity.Y = enrageSpinTargetY * enrageSpinTargetDist;
            }

            // Despawn
            else if (npc.ai[1] == 3f)
            {
                // Disable teleports
                if (npc.ai[3] != 0f || calamityGlobalNPC.newAI[2] != 0f || calamityGlobalNPC.newAI[3] != 0f)
                {
                    npc.ai[3] = 0f;
                    calamityGlobalNPC.newAI[2] = 0f;
                    calamityGlobalNPC.newAI[3] = 0f;
                    npc.SyncExtraAI();
                    npc.netUpdate = true;
                }

                // Avoid cheap bullshit
                npc.damage = 0;

                npc.velocity.Y += 0.1f;
                if (npc.velocity.Y < 0f)
                    npc.velocity.Y *= 0.95f;
                npc.velocity.X *= 0.95f;
                if (npc.timeLeft > 50)
                    npc.timeLeft = 50;
            }

            // Emit dust
            if (npc.ai[1] != 2f && npc.ai[1] != 3f && numHandsAlive != 0)
            {
                int idleDust = Dust.NewDust(new Vector2(npc.Center.X - 15f - npc.velocity.X * 5f, npc.position.Y + npc.height - 2f), 30, 10, DustID.Blood, -npc.velocity.X * 0.2f, 3f, 0, default, 2f);
                Main.dust[idleDust].noGravity = true;
                Main.dust[idleDust].velocity.X = Main.dust[idleDust].velocity.X * 1.3f;
                Main.dust[idleDust].velocity.X = Main.dust[idleDust].velocity.X + npc.velocity.X * 0.4f;
                Main.dust[idleDust].velocity.Y = Main.dust[idleDust].velocity.Y + (2f + npc.velocity.Y);
                for (int j = 0; j < 2; j++)
                {
                    idleDust = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y + 120f), npc.width, 60, DustID.Blood, npc.velocity.X, npc.velocity.Y, 0, default, 2f);
                    Main.dust[idleDust].noGravity = true;
                    Main.dust[idleDust].velocity -= npc.velocity;
                    Main.dust[idleDust].velocity.Y = Main.dust[idleDust].velocity.Y + 5f;
                }
            }

            return false;
        }

        public static bool BuffedSkeletronHandAI(NPC npc, Mod mod)
        {
            CalamityGlobalNPC calamityGlobalNPC = npc.Calamity();

            bool bossRush = BossRushEvent.BossRushActive;
            bool masterMode = Main.masterMode || bossRush;
            bool death = CalamityWorld.death || bossRush;

            // Get a target
            if (npc.target < 0 || npc.target == Main.maxPlayers || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            // Despawn safety, make sure to target another player if the current player target is too far away
            if (Vector2.Distance(Main.player[npc.target].Center, npc.Center) > CalamityGlobalNPC.CatchUpDistance200Tiles)
                npc.TargetClosest();

            float yMultiplier = 1f;
            if (calamityGlobalNPC.newAI[0] != 0f)
                yMultiplier = calamityGlobalNPC.newAI[0];
            if (masterMode)
                yMultiplier *= 1.3f;

            // Inflict 0 damage for 3 seconds after spawning
            if (calamityGlobalNPC.newAI[1] < 180f)
            {
                calamityGlobalNPC.newAI[1] += 1f;
                if (calamityGlobalNPC.newAI[1] % 15f == 0f)
                    npc.SyncExtraAI();

                npc.damage = 0;
            }
            else
                npc.damage = npc.defDamage;

            npc.spriteDirection = -(int)npc.ai[0];

            if (Main.npc[(int)npc.ai[1]].ai[3] == -60f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Teleport dust
                    for (int m = 0; m < 10; m++)
                    {
                        int teleportDust = Dust.NewDust(npc.position, npc.width, npc.height, DustID.GemDiamond, 0f, 0f, 200, default, 3f);
                        Main.dust[teleportDust].noGravity = true;
                        Main.dust[teleportDust].velocity.X *= 2f;
                    }

                    // New location
                    npc.Center = Main.npc[(int)npc.ai[1]].Center;
                    npc.velocity = Vector2.Zero;
                    npc.netUpdate = true;
                }
            }

            float skeletronLifeRatio = 1f;
            if (!Main.npc[(int)npc.ai[1]].active || Main.npc[(int)npc.ai[1]].aiStyle != NPCAIStyleID.SkeletronHead)
            {
                npc.ai[2] += 10f;
                if (npc.ai[2] > 50f || Main.netMode != NetmodeID.Server)
                {
                    npc.life = -1;
                    npc.HitEffect(0, 10.0);
                    npc.active = false;
                }
            }
            else
                skeletronLifeRatio = Main.npc[(int)npc.ai[1]].life / (float)Main.npc[(int)npc.ai[1]].lifeMax;

            // This bool exists for fairness so the hands don't slap when Skeletron is in phase 3 and getting ready to do the new charge
            bool cancelSlap = Main.npc[(int)npc.ai[1]].ai[2] >= ChargeGateValue;

            // Fire skulls from hands at the end of each slap phase (master mode only)
            bool phase2 = skeletronLifeRatio < 0.5f;

            // Attack far more often if still alive
            bool phase3 = skeletronLifeRatio < 0.3f;

            float velocityMultiplier = MathHelper.Lerp(death ? 0.6f : 0.7f, 1f, skeletronLifeRatio);
            float velocityIncrement = MathHelper.Lerp(0.2f, death ? 0.4f : 0.3f, 1f - skeletronLifeRatio);
            float handSwipeVelocity = MathHelper.Lerp(16f, death ? 24f : 20f, 1f - skeletronLifeRatio);
            float deceleration = Main.getGoodWorld ? 0.78f : masterMode ? 0.82f : 0.86f;

            if (masterMode)
            {
                velocityMultiplier *= 0.75f;
                velocityIncrement *= 1.5f;
                handSwipeVelocity *= 1.35f;
                deceleration *= 0.75f;
            }

            float handSwipeDistance = masterMode ? HandSwipeDistance_Master : HandSwipeDistance;
            float handSwipeDuration = handSwipeDistance / handSwipeVelocity;
            float slapGateValue = HandSlapGateValue;

            float slapTimerIncrement = MathHelper.Lerp(masterMode ? 1.5f : 1f, masterMode ? 3f : 2f, 1f - skeletronLifeRatio);
            if (phase3)
                slapTimerIncrement *= (death ? 2.5f : 2f);
            else if (phase2)
                slapTimerIncrement *= (death ? 2f : 1.5f);

            if (npc.ai[2] == 0f || npc.ai[2] == 3f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                if (Main.npc[(int)npc.ai[1]].ai[1] == 3f && npc.timeLeft > 10)
                    npc.timeLeft = 10;

                if (Main.npc[(int)npc.ai[1]].ai[1] != 0f || cancelSlap)
                {
                    deceleration *= 0.75f;
                    velocityIncrement *= 1.5f;

                    float maxX = velocityIncrement * 100f * velocityMultiplier;
                    float maxY = velocityIncrement * 100f * velocityMultiplier;

                    if (npc.Top.Y > Main.npc[(int)npc.ai[1]].Top.Y - 100f * yMultiplier)
                    {
                        if (npc.velocity.Y > 0f)
                            npc.velocity.Y *= deceleration;
                        npc.velocity.Y -= velocityIncrement;
                        if (npc.velocity.Y > maxY)
                            npc.velocity.Y = maxY;
                    }
                    else if (npc.Top.Y < Main.npc[(int)npc.ai[1]].Top.Y - 100f * yMultiplier)
                    {
                        if (npc.velocity.Y < 0f)
                            npc.velocity.Y *= deceleration;
                        npc.velocity.Y += velocityIncrement;
                        if (npc.velocity.Y < -maxY)
                            npc.velocity.Y = -maxY;
                    }

                    if (npc.Center.X > Main.npc[(int)npc.ai[1]].Center.X - 120f * npc.ai[0])
                    {
                        if (npc.velocity.X > 0f)
                            npc.velocity.X *= deceleration;
                        npc.velocity.X -= velocityIncrement;
                        if (npc.velocity.X > maxX)
                            npc.velocity.X = maxX;
                    }

                    if (npc.Center.X < Main.npc[(int)npc.ai[1]].Center.X - 120f * npc.ai[0])
                    {
                        if (npc.velocity.X < 0f)
                            npc.velocity.X *= deceleration;
                        npc.velocity.X += velocityIncrement;
                        if (npc.velocity.X < -maxX)
                            npc.velocity.X = -maxX;
                    }
                }
                else
                {
                    if (calamityGlobalNPC.newAI[3] == 1f)
                    {
                        calamityGlobalNPC.newAI[2] += slapTimerIncrement;
                        npc.ai[3] += slapTimerIncrement;
                        if (npc.ai[3] >= slapGateValue)
                        {
                            npc.ai[2] += 1f;
                            npc.ai[3] = calamityGlobalNPC.newAI[2] = slapGateValue;
                            calamityGlobalNPC.newAI[3] = 0f;
                            npc.netUpdate = true;
                            npc.SyncExtraAI();
                        }
                    }
                    else
                    {
                        calamityGlobalNPC.newAI[2] -= slapTimerIncrement * 2f;
                        if (calamityGlobalNPC.newAI[2] <= 0f)
                        {
                            calamityGlobalNPC.newAI[2] = 0f;
                            calamityGlobalNPC.newAI[3] = 1f;
                            npc.SyncExtraAI();
                        }
                    }

                    float maxX = velocityIncrement * 100f * velocityMultiplier;
                    float maxY = velocityIncrement * 100f * velocityMultiplier;

                    if (npc.Top.Y > Main.npc[(int)npc.ai[1]].Top.Y + 230f * yMultiplier)
                    {
                        if (npc.velocity.Y > 0f)
                            npc.velocity.Y *= deceleration;
                        npc.velocity.Y -= velocityIncrement;
                        if (npc.velocity.Y > maxY)
                            npc.velocity.Y = maxY;
                    }
                    else if (npc.Top.Y < Main.npc[(int)npc.ai[1]].Top.Y + 230f * yMultiplier)
                    {
                        if (npc.velocity.Y < 0f)
                            npc.velocity.Y *= deceleration;
                        npc.velocity.Y += velocityIncrement;
                        if (npc.velocity.Y < -maxY)
                            npc.velocity.Y = -maxY;
                    }

                    if (npc.Center.X > Main.npc[(int)npc.ai[1]].Center.X - 200f * npc.ai[0])
                    {
                        if (npc.velocity.X > 0f)
                            npc.velocity.X *= deceleration;
                        npc.velocity.X -= velocityIncrement;
                        if (npc.velocity.X > maxX)
                            npc.velocity.X = maxX;
                    }

                    if (npc.Center.X < Main.npc[(int)npc.ai[1]].Center.X - 200f * npc.ai[0])
                    {
                        if (npc.velocity.X < 0f)
                            npc.velocity.X *= deceleration;
                        npc.velocity.X += velocityIncrement;
                        if (npc.velocity.X < -maxX)
                            npc.velocity.X = -maxX;
                    }
                }

                Vector2 handCurrentPos = npc.Center;
                float handIdleXPos = Main.npc[(int)npc.ai[1]].Center.X - 200f * npc.ai[0] - handCurrentPos.X;
                float handIdleYPos = Main.npc[(int)npc.ai[1]].Top.Y + 230f - handCurrentPos.Y;
                float handIdleDist = (float)Math.Sqrt(handIdleXPos * handIdleXPos + handIdleYPos * handIdleYPos);
                npc.rotation = (float)Math.Atan2(handIdleYPos, handIdleXPos) + MathHelper.PiOver2;

                return false;
            }

            if (npc.ai[2] == 1f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                Vector2 handCurrentPosition = npc.Center;
                float handDrawbackXPos = Main.npc[(int)npc.ai[1]].Center.X - 200f * npc.ai[0] - handCurrentPosition.X;
                float handDrawbackYPos = Main.npc[(int)npc.ai[1]].Top.Y + 230f - handCurrentPosition.Y;
                float handDrawbackDist = (float)Math.Sqrt(handDrawbackXPos * handDrawbackXPos + handDrawbackYPos * handDrawbackYPos);
                npc.rotation = (float)Math.Atan2(handDrawbackYPos, handDrawbackXPos) + MathHelper.PiOver2;
                npc.velocity.X *= 0.95f;
                npc.velocity.Y -= velocityIncrement;

                if (npc.velocity.Y < -14f)
                    npc.velocity.Y = -14f;
                else if (npc.velocity.Y > 10f)
                    npc.velocity.Y = 10f;

                if (npc.Top.Y < Main.npc[(int)npc.ai[1]].Top.Y - 200f)
                {
                    // Set damage
                    npc.damage = npc.defDamage;

                    npc.TargetClosest();
                    npc.ai[2] = 2f;
                    npc.ai[3] = 0f;
                    npc.velocity = (Main.player[npc.target].Center - npc.Center).SafeNormalize(Vector2.UnitY) * handSwipeVelocity;
                    npc.netUpdate = true;
                }
            }
            else if (npc.ai[2] == 2f)
            {
                // Set damage
                npc.damage = npc.defDamage;

                npc.ai[3] += 1f;
                if (npc.ai[3] >= handSwipeDuration || Vector2.Distance(Main.npc[(int)npc.ai[1]].Center, npc.Center) > handSwipeDistance || cancelSlap)
                {
                    npc.ai[2] = 3f;
                    npc.ai[3] = 0f;
                    npc.netUpdate = true;

                    // Spawn projectiles
                    if (masterMode && Collision.CanHit(npc.Center, 1, 1, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height) && !cancelSlap)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (phase2 && Vector2.Distance(Main.player[npc.target].Center, npc.Center) > 160f)
                            {
                                float skullProjSpeed = handSwipeVelocity * (phase3 ? 0.6f : 0.2f);
                                Vector2 initialProjectileVelocity = npc.Center.DirectionTo(Main.player[npc.target].Center) * skullProjSpeed;
                                int type = ProjectileID.Skull;
                                int damage = npc.GetProjectileDamage(type);
                                int skullProjectile = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, initialProjectileVelocity, type, damage, 0f, Main.myPlayer, -(phase3 ? 2f : 1f));
                                Main.projectile[skullProjectile].timeLeft = 600;
                            }
                        }
                    }
                }
            }
            else if (npc.ai[2] == 4f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                Vector2 handStrikeCurrentPos = npc.Center;
                float handStrikeXPos = Main.npc[(int)npc.ai[1]].Center.X - 200f * npc.ai[0] - handStrikeCurrentPos.X;
                float handStrikeYPos = Main.npc[(int)npc.ai[1]].Top.Y + 230f - handStrikeCurrentPos.Y;
                float handStrikeDist = (float)Math.Sqrt(handStrikeXPos * handStrikeXPos + handStrikeYPos * handStrikeYPos);
                npc.rotation = (float)Math.Atan2(handStrikeYPos, handStrikeXPos) + MathHelper.PiOver2;
                npc.velocity.Y *= 0.95f;
                npc.velocity.X += velocityIncrement * -npc.ai[0];

                if (npc.velocity.X < -10f)
                    npc.velocity.X = -10f;
                else if (npc.velocity.X > 14f)
                    npc.velocity.X = 14f;

                if (npc.Center.X < Main.npc[(int)npc.ai[1]].Center.X - 500f || npc.Center.X > Main.npc[(int)npc.ai[1]].Center.X + 500f)
                {
                    // Set damage
                    npc.damage = npc.defDamage;

                    npc.TargetClosest();
                    npc.ai[2] = 5f;
                    npc.ai[3] = 0f;
                    npc.velocity = (Main.player[npc.target].Center - npc.Center).SafeNormalize(Vector2.UnitY) * handSwipeVelocity;
                    npc.netUpdate = true;
                }
            }
            else if (npc.ai[2] == 5f)
            {
                // Set damage
                npc.damage = npc.defDamage;

                npc.ai[3] += 1f;
                if (npc.ai[3] >= handSwipeDuration || Vector2.Distance(Main.npc[(int)npc.ai[1]].Center, npc.Center) > handSwipeDistance || cancelSlap)
                {
                    npc.ai[2] = 0f;
                    npc.ai[3] = 0f;
                    npc.netUpdate = true;

                    // Spawn projectiles
                    if (masterMode && Collision.CanHit(npc.Center, 1, 1, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height) && !cancelSlap)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (phase2 && Vector2.Distance(Main.player[npc.target].Center, npc.Center) > 160f)
                            {
                                float skullProjSpeed = handSwipeVelocity * (phase3 ? 0.6f : 0.2f);
                                Vector2 initialProjectileVelocity = npc.Center.DirectionTo(Main.player[npc.target].Center) * skullProjSpeed;
                                int type = ProjectileID.Skull;
                                int damage = npc.GetProjectileDamage(type);
                                int skullProjectile = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, initialProjectileVelocity, type, damage, 0f, Main.myPlayer, -(phase3 ? 2f : 1f));
                                Main.projectile[skullProjectile].timeLeft = 600;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public static void RevengeanceDungeonGuardianAI(NPC npc)
        {
            Player target = Main.player[npc.target];
            if (npc.ai[1] != 3f)
            {
                Vector2 targetVector = target.Center - npc.Center;
                float targetDist = targetVector.Length();
                targetDist = 12f / targetDist;
                npc.velocity.X = targetVector.X * targetDist;
                npc.velocity.Y = targetVector.Y * targetDist;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (npc.localAI[1]++ % 60f == 59f)
                    {
                        Vector2 source = npc.Center;
                        if (Collision.CanHit(source, 1, 1, target.Center, target.width, target.height))
                        {
                            float speed = 5f;
                            float xDist = target.Center.X - source.X + Main.rand.Next(-20, 21);
                            float yDist = target.Center.Y - source.Y + Main.rand.Next(-20, 21);
                            Vector2 velocity = new Vector2(xDist, yDist);
                            float distTarget = velocity.Length();
                            distTarget = speed / distTarget;
                            velocity.X *= distTarget;
                            velocity.Y *= distTarget;
                            Vector2 offset = new Vector2(velocity.X * 1f + Main.rand.Next(-50, 51) * 0.01f, velocity.Y * 1f + Main.rand.Next(-50, 51) * 0.01f).SafeNormalize(Vector2.UnitY);
                            offset *= speed;
                            offset += npc.velocity;
                            velocity.X = offset.X;
                            velocity.Y = offset.Y;
                            int damage = 2500;
                            int projType = ProjectileID.Skull;
                            source += offset * 5f;
                            int skull = Projectile.NewProjectile(npc.GetSource_FromAI(), source, velocity, projType, damage, 0f, Main.myPlayer, -1f);
                            Main.projectile[skull].timeLeft = 600;
                            Main.projectile[skull].tileCollide = false;
                        }
                    }
                }
            }
        }

        public static bool VanillaSkeletronAI(NPC npc, Mod mod)
        {
            npc.reflectsProjectiles = false;
            npc.defense = npc.defDefense;
            if (npc.ai[0] == 0f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                npc.TargetClosest();
                npc.ai[0] = 1f;
                int num148 = NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X), (int)npc.Center.Y, NPCID.SkeletronHand, npc.whoAmI);
                Main.npc[num148].ai[0] = -1f;
                Main.npc[num148].ai[1] = npc.whoAmI;
                Main.npc[num148].target = npc.target;
                Main.npc[num148].netUpdate = true;
                num148 = NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X), (int)npc.Center.Y, NPCID.SkeletronHand, npc.whoAmI);
                Main.npc[num148].ai[0] = 1f;
                Main.npc[num148].ai[1] = npc.whoAmI;
                Main.npc[num148].ai[3] = 150f;
                Main.npc[num148].target = npc.target;
                Main.npc[num148].netUpdate = true;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient && npc.localAI[0] == 0f)
            {
                npc.localAI[0] = 1f;
                SoundEngine.PlaySound(SoundID.ForceRoar, npc.Center);
            }

            if (Main.player[npc.target].dead || Math.Abs(npc.position.X - Main.player[npc.target].position.X) > 2000f || Math.Abs(npc.position.Y - Main.player[npc.target].position.Y) > 2000f)
            {
                npc.TargetClosest();
                if (Main.player[npc.target].dead || Math.Abs(npc.position.X - Main.player[npc.target].position.X) > 2000f || Math.Abs(npc.position.Y - Main.player[npc.target].position.Y) > 2000f)
                    npc.ai[1] = 3f;
            }

            if (Main.IsItDay() && npc.ai[1] != 3f && npc.ai[1] != 2f)
            {
                npc.ai[1] = 2f;
                SoundEngine.PlaySound(SoundID.ForceRoar, npc.Center);
            }

            int numHands = 0;
            int maxHands = 2;
            if (Main.expertMode)
            {
                for (int num150 = 0; num150 < Main.maxNPCs; num150++)
                {
                    if (Main.npc[num150].active && Main.npc[num150].type == NPCID.SkeletronHand)
                        numHands++;
                }

                npc.defense += numHands * (Main.masterMode ? 15 : 10);
                npc.Calamity().CurrentlyIncreasingDefenseOrDR = numHands > 0;
                npc.chaseable = numHands == 0;
                if ((numHands < maxHands || (double)npc.life < (double)npc.lifeMax * 0.75 || Main.masterMode) && npc.ai[1] == 0f)
                {
                    float num151 = 80f;
                    if (numHands == 0 || Main.masterMode)
                        num151 /= 2f;

                    if (Main.getGoodWorld)
                        num151 *= 0.8f;

                    if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[2] % num151 == 0f)
                    {
                        Vector2 center3 = npc.Center;
                        float num152 = Main.player[npc.target].Center.X - center3.X;
                        float num153 = Main.player[npc.target].Center.Y - center3.Y;
                        float num154 = (float)Math.Sqrt(num152 * num152 + num153 * num153);
                        if (Collision.CanHit(center3, 1, 1, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                        {
                            float num155 = 3f;
                            if (numHands == 0 || Main.masterMode)
                                num155 += 2f;

                            float num156 = Main.player[npc.target].Center.X - center3.X + (float)Main.rand.Next(-5, 6);
                            float num157 = Main.player[npc.target].Center.Y - center3.Y + (float)Main.rand.Next(-5, 6);
                            float num158 = (float)Math.Sqrt(num156 * num156 + num157 * num157);
                            num158 = num155 / num158;
                            num156 *= num158;
                            num157 *= num158;
                            Vector2 vector19 = new Vector2(num156 * 1f + (float)Main.rand.Next(-50, 51) * 0.01f, num157 * 1f + (float)Main.rand.Next(-50, 51) * 0.01f).SafeNormalize(Vector2.UnitY);
                            vector19 *= num155;
                            vector19 += npc.velocity;
                            num156 = vector19.X;
                            num157 = vector19.Y;
                            int type = ProjectileID.Skull;
                            center3 += vector19 * 5f;
                            int num160 = Projectile.NewProjectile(npc.GetSource_FromAI(), center3.X, center3.Y, num156, num157, type, npc.GetProjectileDamage(type), 0f, Main.myPlayer, -1f);
                            Main.projectile[num160].timeLeft = 300;
                        }
                    }
                }
            }

            if (npc.ai[1] == 0f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                npc.ai[2] += 1f;
                float chargeGateValue = Main.masterMode ? (600f - (maxHands - numHands) * 200f) : 800f;
                if (npc.ai[2] >= chargeGateValue)
                {
                    npc.ai[2] = 0f;
                    npc.ai[1] = 1f;
                    npc.TargetClosest();
                    npc.netUpdate = true;
                }

                npc.rotation = npc.velocity.X / 15f;
                float accelerationY = Main.getGoodWorld ? 0.05f : Main.masterMode ? 0.04f : Main.expertMode ? 0.03f : 0.02f;
                float maxVelocityY = accelerationY * 100f;
                float accelerationX = Main.getGoodWorld ? 0.093f : Main.masterMode ? 0.088f : Main.expertMode ? 0.076f : 0.064f;
                float maxVelocityX = accelerationX * 100f;
                float deceleration = Main.getGoodWorld ? 0.83f : Main.masterMode ? 0.86f : Main.expertMode ? 0.89f : 0.92f;

                if (npc.position.Y > Main.player[npc.target].position.Y - 250f)
                {
                    if (npc.velocity.Y > 0f)
                        npc.velocity.Y *= deceleration;

                    npc.velocity.Y -= accelerationY;
                    if (npc.velocity.Y > maxVelocityY)
                        npc.velocity.Y = maxVelocityY;
                }
                else if (npc.position.Y < Main.player[npc.target].position.Y - 250f)
                {
                    if (npc.velocity.Y < 0f)
                        npc.velocity.Y *= deceleration;

                    npc.velocity.Y += accelerationY;
                    if (npc.velocity.Y < -maxVelocityY)
                        npc.velocity.Y = -maxVelocityY;
                }

                if (npc.Center.X > Main.player[npc.target].Center.X)
                {
                    if (npc.velocity.X > 0f)
                        npc.velocity.X *= deceleration;

                    npc.velocity.X -= accelerationX;
                    if (npc.velocity.X > maxVelocityX)
                        npc.velocity.X = maxVelocityX;
                }

                if (npc.Center.X < Main.player[npc.target].Center.X)
                {
                    if (npc.velocity.X < 0f)
                        npc.velocity.X *= deceleration;

                    npc.velocity.X += accelerationX;
                    if (npc.velocity.X < 0f - maxVelocityX)
                        npc.velocity.X = 0f - maxVelocityX;
                }
            }
            else if (npc.ai[1] == 1f)
            {
                if (Main.getGoodWorld)
                {
                    if (numHands > 0)
                    {
                        npc.reflectsProjectiles = true;
                    }
                    else if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[2] % 200f == 0f && NPC.CountNPCS(NPCID.DarkCaster) < 6)
                    {
                        int num165 = 1;
                        for (int num166 = 0; num166 < num165; num166++)
                        {
                            int num167 = 1000;
                            for (int num168 = 0; num168 < num167; num168++)
                            {
                                int num169 = (int)(npc.Center.X / 16f) + Main.rand.Next(-50, 51);
                                int num170;
                                for (num170 = (int)(npc.Center.Y / 16f) + Main.rand.Next(-50, 51); num170 < Main.maxTilesY - 10 && !WorldGen.SolidTile(num169, num170); num170++)
                                {
                                }

                                num170--;
                                if (!WorldGen.SolidTile(num169, num170))
                                {
                                    int num171 = NPC.NewNPC(npc.GetSource_FromAI(), num169 * 16 + 8, num170 * 16, NPCID.DarkCaster);
                                    if (Main.netMode == NetmodeID.Server && num171 < Main.maxNPCs)
                                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, num171);

                                    break;
                                }
                            }
                        }
                    }
                }

                npc.defense -= 10;
                npc.ai[2] += 1f;
                if (npc.ai[2] == 2f)
                    SoundEngine.PlaySound(SoundID.ForceRoar, npc.Center);

                float idlePhaseGateValue = Main.masterMode ? (300f - (maxHands - numHands) * 100f) : 400f;
                if (npc.ai[2] >= idlePhaseGateValue)
                {
                    npc.ai[2] = 0f;
                    npc.ai[1] = 0f;
                }

                npc.rotation += (float)npc.direction * 0.3f;
                Vector2 vector20 = npc.Center;
                float num172 = Main.player[npc.target].Center.X - vector20.X;
                float num173 = Main.player[npc.target].Center.Y - vector20.Y;
                float num174 = (float)Math.Sqrt(num172 * num172 + num173 * num173);
                float num175 = 3f;
                npc.damage = (int)Math.Round(npc.defDamage * 1.3);
                if (Main.expertMode)
                {
                    num175 = Main.masterMode ? (5f - numHands * 0.5f) : 3.5f;
                    if (num174 > 150f)
                        num175 *= 1.05f;

                    if (num174 > 200f)
                        num175 *= 1.1f;

                    if (num174 > 250f)
                        num175 *= 1.1f;

                    if (num174 > 300f)
                        num175 *= 1.1f;

                    if (num174 > 350f)
                        num175 *= 1.1f;

                    if (num174 > 400f)
                        num175 *= 1.1f;

                    if (num174 > 450f)
                        num175 *= 1.1f;

                    if (num174 > 500f)
                        num175 *= 1.1f;

                    if (num174 > 550f)
                        num175 *= 1.1f;

                    if (num174 > 600f)
                        num175 *= 1.1f;

                    switch (numHands)
                    {
                        case 0:
                            num175 *= 1.1f;
                            break;
                        case 1:
                            num175 *= 1.05f;
                            break;
                    }
                }

                if (Main.getGoodWorld)
                    num175 *= 1.3f;

                num174 = num175 / num174;
                npc.velocity.X = num172 * num174;
                npc.velocity.Y = num173 * num174;
            }
            else if (npc.ai[1] == 2f)
            {
                npc.damage = 1000;
                npc.defense = 9999;

                npc.Calamity().CurrentlyEnraged = true;
                npc.Calamity().CurrentlyIncreasingDefenseOrDR = true;

                npc.rotation += (float)npc.direction * 0.3f;
                Vector2 vector21 = npc.Center;
                float num176 = Main.player[npc.target].Center.X - vector21.X;
                float num177 = Main.player[npc.target].Center.Y - vector21.Y;
                float num178 = (float)Math.Sqrt(num176 * num176 + num177 * num177);
                num178 = 8f / num178;
                npc.velocity.X = num176 * num178;
                npc.velocity.Y = num177 * num178;
            }
            else if (npc.ai[1] == 3f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                npc.velocity.Y += 0.1f;
                if (npc.velocity.Y < 0f)
                    npc.velocity.Y *= 0.95f;

                npc.velocity.X *= 0.95f;
                npc.EncourageDespawn(50);
            }

            if (npc.ai[1] != 2f && npc.ai[1] != 3f && (numHands != 0 || !Main.expertMode))
            {
                int num179 = Dust.NewDust(new Vector2(npc.Center.X - 15f - npc.velocity.X * 5f, npc.position.Y + (float)npc.height - 2f), 30, 10, DustID.Blood, (0f - npc.velocity.X) * 0.2f, 3f, 0, default(Color), 2f);
                Main.dust[num179].noGravity = true;
                Main.dust[num179].velocity.X *= 1.3f;
                Main.dust[num179].velocity.X += npc.velocity.X * 0.4f;
                Main.dust[num179].velocity.Y += 2f + npc.velocity.Y;
                for (int num180 = 0; num180 < 2; num180++)
                {
                    num179 = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y + 120f), npc.width, 60, DustID.Blood, npc.velocity.X, npc.velocity.Y, 0, default(Color), 2f);
                    Main.dust[num179].noGravity = true;
                    Dust dust = Main.dust[num179];
                    dust.velocity -= npc.velocity;
                    Main.dust[num179].velocity.Y += 5f;
                }
            }

            return false;
        }

        public static bool VanillaSkeletronHandAI(NPC npc, Mod mod)
        {
            npc.spriteDirection = -(int)npc.ai[0];
            if (!Main.npc[(int)npc.ai[1]].active || Main.npc[(int)npc.ai[1]].aiStyle != NPCAIStyleID.SkeletronHead)
            {
                npc.ai[2] += 10f;
                if (npc.ai[2] > 50f || Main.netMode != NetmodeID.Server)
                {
                    npc.life = -1;
                    npc.HitEffect();
                    npc.active = false;
                }
            }

            if (npc.ai[2] == 0f || npc.ai[2] == 3f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                if (Main.npc[(int)npc.ai[1]].ai[1] == 3f)
                    npc.EncourageDespawn(10);

                if (Main.npc[(int)npc.ai[1]].ai[1] != 0f)
                {
                    float accelerationY = Main.getGoodWorld ? 0.1f : Main.masterMode ? 0.09f : Main.expertMode ? 0.08f : 0.07f;
                    float maxVelocityY = accelerationY * 100f;
                    float accelerationX = Main.getGoodWorld ? 0.16f : Main.masterMode ? 0.14f : Main.expertMode ? 0.12f : 0.1f;
                    float maxVelocityX = accelerationX * 100f;
                    float deceleration = Main.getGoodWorld ? 0.81f : Main.masterMode ? 0.84f : Main.expertMode ? 0.87f : 0.9f;

                    if (npc.position.Y > Main.npc[(int)npc.ai[1]].position.Y - 100f)
                    {
                        if (npc.velocity.Y > 0f)
                            npc.velocity.Y *= deceleration;

                        npc.velocity.Y -= accelerationY;
                        if (npc.velocity.Y > maxVelocityY)
                            npc.velocity.Y = maxVelocityY;
                    }
                    else if (npc.position.Y < Main.npc[(int)npc.ai[1]].position.Y - 100f)
                    {
                        if (npc.velocity.Y < 0f)
                            npc.velocity.Y *= deceleration;

                        npc.velocity.Y += accelerationY;
                        if (npc.velocity.Y < -maxVelocityY)
                            npc.velocity.Y = -maxVelocityY;
                    }

                    if (npc.Center.X > Main.npc[(int)npc.ai[1]].Center.X - 120f * npc.ai[0])
                    {
                        if (npc.velocity.X > 0f)
                            npc.velocity.X *= deceleration;

                        npc.velocity.X -= accelerationX;
                        if (npc.velocity.X > maxVelocityX)
                            npc.velocity.X = maxVelocityX;
                    }

                    if (npc.Center.X < Main.npc[(int)npc.ai[1]].Center.X - 120f * npc.ai[0])
                    {
                        if (npc.velocity.X < 0f)
                            npc.velocity.X *= deceleration;

                        npc.velocity.X += accelerationX;
                        if (npc.velocity.X < -maxVelocityX)
                            npc.velocity.X = -maxVelocityX;
                    }
                }
                else
                {
                    npc.ai[3] += 1f;
                    if (Main.expertMode)
                        npc.ai[3] += 0.5f;
                    if (Main.masterMode)
                        npc.ai[3] += 0.5f;

                    if (npc.ai[3] >= 300f)
                    {
                        npc.ai[2] += 1f;
                        npc.ai[3] = 0f;
                        npc.netUpdate = true;
                    }

                    float accelerationY = Main.getGoodWorld ? 0.07f : Main.masterMode ? 0.06f : Main.expertMode ? 0.05f : 0.04f;
                    float maxVelocityY = accelerationY * 100f;
                    float accelerationX = Main.getGoodWorld ? 0.13f : Main.masterMode ? 0.11f : Main.expertMode ? 0.09f : 0.07f;
                    float maxVelocityX = accelerationX * 100f;
                    float deceleration = Main.getGoodWorld ? 0.88f : Main.masterMode ? 0.9f : Main.expertMode ? 0.92f : 0.94f;

                    if (Main.expertMode)
                    {
                        if (npc.position.Y > Main.npc[(int)npc.ai[1]].position.Y + 230f)
                        {
                            if (npc.velocity.Y > 0f)
                                npc.velocity.Y *= deceleration;

                            npc.velocity.Y -= accelerationY;
                            if (npc.velocity.Y > maxVelocityY)
                                npc.velocity.Y = maxVelocityY;
                        }
                        else if (npc.position.Y < Main.npc[(int)npc.ai[1]].position.Y + 230f)
                        {
                            if (npc.velocity.Y < 0f)
                                npc.velocity.Y *= deceleration;

                            npc.velocity.Y += accelerationY;
                            if (npc.velocity.Y < -maxVelocityY)
                                npc.velocity.Y = -maxVelocityY;
                        }

                        if (npc.Center.X > Main.npc[(int)npc.ai[1]].Center.X - 200f * npc.ai[0])
                        {
                            if (npc.velocity.X > 0f)
                                npc.velocity.X *= deceleration;

                            npc.velocity.X -= accelerationX;
                            if (npc.velocity.X > maxVelocityX)
                                npc.velocity.X = maxVelocityX;
                        }

                        if (npc.Center.X < Main.npc[(int)npc.ai[1]].Center.X - 200f * npc.ai[0])
                        {
                            if (npc.velocity.X < 0f)
                                npc.velocity.X *= deceleration;

                            npc.velocity.X += accelerationX;
                            if (npc.velocity.X < -maxVelocityX)
                                npc.velocity.X = -maxVelocityX;
                        }
                    }

                    if (npc.position.Y > Main.npc[(int)npc.ai[1]].position.Y + 230f)
                    {
                        if (npc.velocity.Y > 0f)
                            npc.velocity.Y *= deceleration;

                        npc.velocity.Y -= accelerationY;
                        if (npc.velocity.Y > maxVelocityY)
                            npc.velocity.Y = maxVelocityY;
                    }
                    else if (npc.position.Y < Main.npc[(int)npc.ai[1]].position.Y + 230f)
                    {
                        if (npc.velocity.Y < 0f)
                            npc.velocity.Y *= deceleration;

                        npc.velocity.Y += accelerationY;
                        if (npc.velocity.Y < -maxVelocityY)
                            npc.velocity.Y = -maxVelocityY;
                    }

                    if (npc.Center.X > Main.npc[(int)npc.ai[1]].Center.X - 200f * npc.ai[0])
                    {
                        if (npc.velocity.X > 0f)
                            npc.velocity.X *= deceleration;

                        npc.velocity.X -= accelerationX;
                        if (npc.velocity.X > maxVelocityX)
                            npc.velocity.X = maxVelocityX;
                    }

                    if (npc.Center.X < Main.npc[(int)npc.ai[1]].Center.X - 200f * npc.ai[0])
                    {
                        if (npc.velocity.X < 0f)
                            npc.velocity.X *= deceleration;

                        npc.velocity.X += accelerationX;
                        if (npc.velocity.X < -maxVelocityX)
                            npc.velocity.X = -maxVelocityX;
                    }
                }

                Vector2 vector22 = npc.Center;
                float num181 = Main.npc[(int)npc.ai[1]].Center.X - 200f * npc.ai[0] - vector22.X;
                float num182 = Main.npc[(int)npc.ai[1]].position.Y + 230f - vector22.Y;
                float num183 = (float)Math.Sqrt(num181 * num181 + num182 * num182);
                npc.rotation = (float)Math.Atan2(num182, num181) + MathHelper.PiOver2;
            }
            else if (npc.ai[2] == 1f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                Vector2 vector23 = npc.Center;
                float num184 = Main.npc[(int)npc.ai[1]].Center.X - 200f * npc.ai[0] - vector23.X;
                float num185 = Main.npc[(int)npc.ai[1]].position.Y + 230f - vector23.Y;
                float num186 = (float)Math.Sqrt(num184 * num184 + num185 * num185);
                npc.rotation = (float)Math.Atan2(num185, num184) + MathHelper.PiOver2;
                npc.velocity.X *= 0.95f;
                npc.velocity.Y -= 0.1f;
                if (Main.expertMode)
                {
                    npc.velocity.Y -= (Main.masterMode ? 0.09f : 0.06f);
                    if (npc.velocity.Y < -13f)
                        npc.velocity.Y = -13f;
                }
                else if (npc.velocity.Y < -8f)
                    npc.velocity.Y = -8f;

                if (npc.position.Y < Main.npc[(int)npc.ai[1]].position.Y - 200f)
                {
                    // Set damage
                    npc.damage = npc.defDamage;

                    npc.TargetClosest();
                    npc.ai[2] = 2f;
                    vector23 = npc.Center;
                    num184 = Main.player[npc.target].Center.X - vector23.X;
                    num185 = Main.player[npc.target].Center.Y - vector23.Y;
                    num186 = (float)Math.Sqrt(num184 * num184 + num185 * num185);
                    num186 = ((!Main.expertMode) ? (18f / num186) : ((Main.masterMode ? 24f : 21f) / num186));
                    npc.velocity.X = num184 * num186;
                    npc.velocity.Y = num185 * num186;
                    npc.netUpdate = true;
                }
            }
            else if (npc.ai[2] == 2f)
            {
                // Set damage
                npc.damage = npc.defDamage;

                if (npc.position.Y > Main.player[npc.target].position.Y || npc.velocity.Y < 0f)
                {
                    // Avoid cheap bullshit
                    npc.damage = 0;

                    npc.ai[2] = 3f;
                }
            }
            else if (npc.ai[2] == 4f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                Vector2 vector24 = npc.Center;
                float num187 = Main.npc[(int)npc.ai[1]].Center.X - 200f * npc.ai[0] - vector24.X;
                float num188 = Main.npc[(int)npc.ai[1]].position.Y + 230f - vector24.Y;
                float num189 = (float)Math.Sqrt(num187 * num187 + num188 * num188);
                npc.rotation = (float)Math.Atan2(num188, num187) + MathHelper.PiOver2;
                npc.velocity.Y *= 0.95f;
                npc.velocity.X += 0.1f * (0f - npc.ai[0]);
                if (Main.expertMode)
                {
                    npc.velocity.X += (Main.masterMode ? 0.1f : 0.07f) * (0f - npc.ai[0]);
                    if (npc.velocity.X < -12f)
                        npc.velocity.X = -12f;
                    else if (npc.velocity.X > 12f)
                        npc.velocity.X = 12f;
                }
                else if (npc.velocity.X < -8f)
                    npc.velocity.X = -8f;
                else if (npc.velocity.X > 8f)
                    npc.velocity.X = 8f;

                if (npc.Center.X < Main.npc[(int)npc.ai[1]].Center.X - 500f || npc.Center.X > Main.npc[(int)npc.ai[1]].Center.X + 500f)
                {
                    // Set damage
                    npc.damage = npc.defDamage;

                    npc.TargetClosest();
                    npc.ai[2] = 5f;
                    vector24 = npc.Center;
                    num187 = Main.player[npc.target].Center.X - vector24.X;
                    num188 = Main.player[npc.target].Center.Y - vector24.Y;
                    num189 = (float)Math.Sqrt(num187 * num187 + num188 * num188);
                    num189 = ((!Main.expertMode) ? (17f / num189) : ((Main.masterMode ? 25f : 22f) / num189));
                    npc.velocity.X = num187 * num189;
                    npc.velocity.Y = num188 * num189;
                    npc.netUpdate = true;
                }
            }
            else if (npc.ai[2] == 5f && ((npc.velocity.X > 0f && npc.Center.X > Main.player[npc.target].Center.X) || (npc.velocity.X < 0f && npc.Center.X < Main.player[npc.target].Center.X)))
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                npc.ai[2] = 0f;
            }

            return false;
        }
    }
}
