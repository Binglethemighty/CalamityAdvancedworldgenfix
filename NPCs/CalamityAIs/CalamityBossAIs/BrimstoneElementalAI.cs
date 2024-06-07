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
    public static class BrimstoneElementalAI
    {
        public static void VanillaBrimstoneElementalAI(NPC npc, Mod mod)
        {
            CalamityGlobalNPC calamityGlobalNPC = npc.Calamity();
            BrimstoneElemental.BrimstoneElemental brimmy = npc.ModNPC<BrimstoneElemental.BrimstoneElemental>();

            // Used for Brimling AI states
            CalamityGlobalNPC.brimstoneElemental = npc.whoAmI;

            // Emit light
            Lighting.AddLight((int)((npc.position.X + (npc.width / 2)) / 16f), (int)((npc.position.Y + (npc.height / 2)) / 16f), 1.2f, 0f, 0f);

            // Get a target
            if (npc.target < 0 || npc.target == Main.maxPlayers || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            Player player = Main.player[npc.target];

            bool despawnDistance = Vector2.Distance(player.Center, npc.Center) > CalamityGlobalNPC.CatchUpDistance350Tiles;

            if (!player.active || player.dead || despawnDistance)
            {
                npc.TargetClosest(false);
                player = Main.player[npc.target];
                if (!player.active || player.dead || despawnDistance)
                {
                    npc.rotation = npc.velocity.X * 0.04f;

                    if (npc.velocity.Y > 3f)
                        npc.velocity.Y = 3f;
                    npc.velocity.Y -= 0.1f;
                    if (npc.velocity.Y < -12f)
                        npc.velocity.Y = -12f;

                    if (npc.timeLeft > 60)
                        npc.timeLeft = 60;

                    if (npc.ai[0] != 0f)
                    {
                        npc.ai[0] = 0f;
                        npc.ai[1] = 0f;
                        npc.ai[2] = 0f;
                        npc.ai[3] = 0f;
                        npc.localAI[0] = 0f;
                        npc.localAI[1] = 0f;
                        npc.netUpdate = true;
                    }
                    return;
                }
            }
            else if (npc.timeLeft < 1800)
                npc.timeLeft = 1800;

            CalamityPlayer modPlayer = player.Calamity();

            // Reset defense
            npc.defense = npc.defDefense;
            calamityGlobalNPC.CurrentlyIncreasingDefenseOrDR = npc.ai[0] == 4f;

            // Percent life remaining
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Variables for buffing the AI
            bool bossRush = BossRushEvent.BossRushActive;
            bool expertMode = Main.expertMode || bossRush;
            bool masterMode = Main.masterMode || bossRush;
            bool revenge = CalamityWorld.revenge || bossRush;
            bool death = CalamityWorld.death || bossRush;

            bool phase2 = lifeRatio < 0.5f && revenge;
            bool phase3 = lifeRatio < 0.33f;

            // Enrage
            if ((!player.ZoneUnderworldHeight || !modPlayer.ZoneCalamity) && !bossRush)
            {
                if (calamityGlobalNPC.newAI[3] > 0f)
                    calamityGlobalNPC.newAI[3] -= 1f;
            }
            else
                calamityGlobalNPC.newAI[3] = CalamityGlobalNPC.biomeEnrageTimerMax;

            bool biomeEnraged = calamityGlobalNPC.newAI[3] <= 0f || bossRush;

            float enrageScale = bossRush ? 1f : 0f;
            if (biomeEnraged && (!player.ZoneUnderworldHeight || bossRush))
            {
                npc.Calamity().CurrentlyEnraged = !bossRush;
                enrageScale += 1f;
            }
            if (biomeEnraged && (!modPlayer.ZoneCalamity || bossRush))
            {
                npc.Calamity().CurrentlyEnraged = !bossRush;
                enrageScale += 1f;
            }

            npc.Calamity().DR = npc.ai[0] == 4f ? 0.6f : 0.15f;

            // Emit dust
            int dustAmt = (npc.ai[0] == 2f) ? 2 : 1;
            int size = (npc.ai[0] == 2f) ? 50 : 35;
            if (npc.ai[0] != 1f)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (Main.rand.Next(3) < dustAmt)
                    {
                        int dust = Dust.NewDust(npc.Center - new Vector2(size), size * 2, size * 2, DustID.LifeDrain, npc.velocity.X * 0.5f, npc.velocity.Y * 0.5f, 90, default, 1.5f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].velocity *= 0.2f;
                        Main.dust[dust].fadeIn = 1f;
                    }
                }
            }

            // Distance from destination where Brimmy stops moving
            float movementDistanceGateValue = 100f;

            // How fast Brimmy moves to the destination
            float baseVelocity = (death ? 6f : revenge ? 5.5f : expertMode ? 5f : 4.5f) * (npc.ai[0] == 5f ? 0.05f : npc.ai[0] == 3f ? 1.5f : 1f);
            baseVelocity += 3f * enrageScale;
            if (expertMode)
                baseVelocity += death ? 3f * (1f - lifeRatio) : 2f * (1f - lifeRatio);

            float baseAcceleration = (death ? 0.12f : 0.1f) * (npc.ai[0] == 5f ? 0.5f : npc.ai[0] == 3f ? 1.5f : 1f);
            baseAcceleration += 0.06f * enrageScale;
            if (expertMode)
                baseAcceleration += 0.03f * (1f - lifeRatio);

            // This is where Brimmy should be
            Vector2 destination = npc.ai[0] != 3f ? player.Center : new Vector2(player.Center.X, player.Center.Y - 300f);

            // How far Brimmy is from where she's supposed to be
            Vector2 distanceFromDestination = destination - npc.Center;

            // Movement
            if (npc.ai[0] != 4f)
                CalamityUtils.SmoothMovement(npc, movementDistanceGateValue, distanceFromDestination, baseVelocity, baseAcceleration, true);

            // Rotation and direction
            if (npc.ai[0] <= 2f || npc.ai[0] == 5f)
            {
                npc.rotation = npc.velocity.X * 0.04f;
                if (npc.ai[0] != 5 || (npc.ai[1] < 180f && npc.ai[0] == 5f))
                {
                    float playerLocation = npc.Center.X - player.Center.X;
                    npc.direction = playerLocation < 0f ? 1 : -1;
                    npc.spriteDirection = npc.direction;
                }
            }

            if (Main.zenithWorld) // in gfb, Brimmy channels the power of the other elementals.
            {
                int newMode;
                if (lifeRatio <= 0.8f && lifeRatio > 0.6f)
                {
                    newMode = 1; // Sand
                }
                else if (lifeRatio <= 0.6f && lifeRatio > 0.4f)
                {
                    newMode = 2; // Rare Sand
                }
                else if (lifeRatio <= 0.4f && lifeRatio > 0.2f)
                {
                    newMode = 3; // Cloud
                }
                else if (lifeRatio <= 0.2f)
                {
                    newMode = 4; // Water
                }
                else
                {
                    newMode = 0; // Brimstone, default
                }
                if (newMode != brimmy.currentMode)
                {
                    SoundEngine.PlaySound(SoundID.Item29, npc.Center);
                }
                brimmy.currentMode = newMode;
            }

            if (npc.ai[0] == -1f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int phase;
                    int random = phase2 ? 6 : 5;
                    do phase = Main.rand.Next(random);
                    while (phase == npc.ai[1] || (phase == 0 && phase3 && revenge) || phase == 1 || phase == 2 || (phase == 4 && npc.localAI[3] != 0f));

                    npc.ai[0] = phase;
                    npc.ai[1] = 0f;

                    // Cocoon phase cooldown
                    if (npc.localAI[3] > 0f)
                        npc.localAI[3] -= 1f;
                    else if (phase == 4)
                        npc.localAI[3] = 3f;

                    npc.netUpdate = true;

                    // Prevent netUpdate from being blocked by the spam counter.
                    // A phase switch sync is a critical operation that must be synced.
                    if (npc.netSpam >= 10)
                        npc.netSpam = 9;
                }
            }

            // Pick a location to teleport to
            else if (npc.ai[0] == 0f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                npc.chaseable = true;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.localAI[1] += 1f;
                    if (npc.justHit)
                        npc.localAI[1] += masterMode ? 7f : expertMode ? 5f : 3f;
                    if (npc.Distance(player.Center) < 160f)
                        npc.localAI[1] += masterMode ? 4f : expertMode ? 2f : 1f;

                    if (npc.localAI[1] >= (bossRush ? 60f : death ? 120f : 180f))
                    {
                        npc.TargetClosest();
                        npc.localAI[1] = 0f;
                        int timer = 0;
                        int playerPosX;
                        int playerPosY;
                        while (true)
                        {
                            timer++;
                            playerPosX = (int)player.Center.X / 16;
                            playerPosY = (int)player.Center.Y / 16;

                            int min = 12;
                            int max = 16;

                            if (Main.rand.NextBool())
                                playerPosX += Main.rand.Next(min, max);
                            else
                                playerPosX -= Main.rand.Next(min, max);

                            if (Main.rand.NextBool())
                                playerPosY += Main.rand.Next(min, max);
                            else
                                playerPosY -= Main.rand.Next(min, max);

                            if (!WorldGen.SolidTile(playerPosX, playerPosY))
                                break;

                            if (timer > 100)
                                return;
                        }
                        npc.ai[0] = 1f;
                        npc.ai[1] = playerPosX;
                        npc.ai[2] = playerPosY;
                        npc.netUpdate = true;
                    }
                }
            }

            // Teleport to location
            else if (npc.ai[0] == 1f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                npc.chaseable = true;
                Vector2 position = new Vector2(npc.ai[1] * 16f - (npc.width / 2), npc.ai[2] * 16f - (npc.height / 2));
                for (int m = 0; m < 5; m++)
                {
                    int dust = Dust.NewDust(position, npc.width, npc.height, DustID.LifeDrain, 0f, -1f, 90, default, 2f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].fadeIn = 1f;
                }
                npc.alpha += bossRush ? 15 : death ? 5 : revenge ? 4 : expertMode ? 3 : 2;
                if (npc.alpha >= 255)
                {
                    int spawnType = brimmy.currentMode == 3 ? NPCID.AngryNimbus : ModContent.NPCType<Brimling>();
                    int enemyCount = brimmy.currentMode == 3 ? 3 : 1; // 3 angry nimbi if cloud, otherwise 1 brimling
                    if (Main.netMode != NetmodeID.MultiplayerClient && NPC.CountNPCS(spawnType) < (death ? 1 : 2) && revenge && brimmy.currentMode != 2) // dont spawn anything if gfb rare sand
                    {
                        for (int i = 0; i < enemyCount; i++)
                            NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, spawnType);
                    }
                    SoundEngine.PlaySound(SoundID.Item8, npc.Center);
                    npc.alpha = 255;
                    npc.position = position;
                    for (int n = 0; n < 15; n++)
                    {
                        int warpDust = Dust.NewDust(npc.position, npc.width, npc.height, DustID.LifeDrain, 0f, -1f, 90, default, 3f);
                        Main.dust[warpDust].noGravity = true;
                    }
                    npc.ai[0] = 2f;
                    npc.netUpdate = true;
                }
            }

            // Either teleport again or go to next AI state
            else if (npc.ai[0] == 2f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                if (npc.alpha >= 255)
                {
                    if (Main.zenithWorld)
                    {
                        SoundEngine.PlaySound(SoundID.Item68, npc.Center);
                        int type = ModContent.ProjectileType<BrimstoneRay>();
                        int damage = npc.GetProjectileDamage(type);
                        Vector2 pos = npc.Center;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromAI(), pos, new Vector2(0, 1), type, damage, 0f, Main.myPlayer, 0f, npc.whoAmI);
                            Projectile.NewProjectile(npc.GetSource_FromAI(), pos, new Vector2(0, -1), type, damage, 0f, Main.myPlayer, 0f, npc.whoAmI);
                            Projectile.NewProjectile(npc.GetSource_FromAI(), pos, new Vector2(1, 0), type, damage, 0f, Main.myPlayer, 0f, npc.whoAmI);
                            Projectile.NewProjectile(npc.GetSource_FromAI(), pos, new Vector2(-1, 0), type, damage, 0f, Main.myPlayer, 0f, npc.whoAmI);
                        }
                        if (brimmy.currentMode >= 1 && brimmy.currentMode <= 3)
                        {
                            int tornadoType = brimmy.currentMode == 3 ? ModContent.ProjectileType<StormMarkHostile>() : ProjectileID.SandnadoHostileMark;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, tornadoType, damage, 0f, Main.myPlayer, 0f, 0f);
                            }
                        }
                        if (brimmy.currentMode == 2)
                        {
                            int healAmt = npc.lifeMax / 25;
                            if (healAmt > 0)
                            {
                                npc.life += healAmt;
                                npc.HealEffect(healAmt, true);
                                npc.netUpdate = true;
                            }
                        }
                    }
                }

                npc.alpha -= 50;
                if (npc.alpha <= 0)
                {
                    npc.chaseable = true;
                    npc.ai[3] += 1f;
                    npc.alpha = 0;
                    if (npc.ai[3] >= 2f || phase2 || Main.getGoodWorld)
                    {
                        npc.ai[0] = -1f;
                        npc.ai[1] = 0f;
                        npc.ai[2] = 0f;
                        npc.ai[3] = 0f;
                    }
                    else
                    {
                        npc.ai[0] = 0f;
                    }
                    npc.netUpdate = true;
                }
            }

            // Float above target and fire projectiles
            else if (npc.ai[0] == 3f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                npc.chaseable = true;
                npc.rotation = npc.velocity.X * 0.04f;

                float playerLocation = npc.Center.X - player.Center.X;
                npc.direction = playerLocation < 0f ? 1 : -1;
                npc.spriteDirection = npc.direction;

                npc.ai[1] += 1f;
                float divisor = expertMode ? (death ? 80f : revenge ? 45f : 50f) - (float)Math.Ceiling(10f * (1f - lifeRatio)) : 50f;
                divisor -= 3f * enrageScale;
                float divisor2 = divisor * 2f;

                if (npc.ai[1] % divisor == divisor - 1f)
                {
                    float velocity = (death ? 7f : revenge ? 6f : 5f) + (2f * enrageScale) + (expertMode ? 3f * (1f - lifeRatio) : 0f);
                    int type = ModContent.ProjectileType<BrimstoneHellfireball>();
                    int damage = npc.GetProjectileDamage(type);
                    if (brimmy.currentMode == 4)
                    {
                        type = ModContent.ProjectileType<FrostMist>();
                        SoundEngine.PlaySound(SoundID.Item30, player.Center);
                    }
                    Vector2 projectileVelocity = (player.Center - npc.Center).SafeNormalize(Vector2.UnitY) * velocity;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int proj = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + projectileVelocity.SafeNormalize(Vector2.UnitY) * 5f, projectileVelocity, type, damage, 0f, Main.myPlayer, player.position.X, player.position.Y);
                        Main.projectile[proj].timeLeft = 240;
                        if (CalamityWorld.LegendaryMode && CalamityWorld.revenge)
                            Main.projectile[proj].extraUpdates += 1;
                    }

                    if (npc.ai[1] % divisor2 == divisor2 - 1f)
                    {
                        velocity = (death ? 5f : 4f) + 2f * enrageScale;
                        type = ModContent.ProjectileType<BrimstoneBarrage>();
                        damage = npc.GetProjectileDamage(type);
                        if (brimmy.currentMode == 4)
                        {
                            type = ModContent.ProjectileType<WaterSpear>();
                            SoundEngine.PlaySound(SoundID.Item21, player.Center);
                        }
                        projectileVelocity = (player.Center - npc.Center).SafeNormalize(Vector2.UnitY) * velocity;
                        int numProj = death ? 8 : 4;
                        int spread = death ? 90 : 45;
                        if (Main.getGoodWorld)
                        {
                            numProj *= 3;
                            spread *= 2;
                        }

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float rotation = MathHelper.ToRadians(spread);
                            float projectileVelocityToPass = velocity * 3f;
                            for (int i = 0; i < numProj; i++)
                            {
                                Vector2 perturbedSpeed = projectileVelocity.RotatedBy(MathHelper.Lerp(-rotation, rotation, i / (float)(numProj - 1)));
                                int proj2 = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + perturbedSpeed.SafeNormalize(Vector2.UnitY) * 5f, perturbedSpeed, type, damage, 0f, Main.myPlayer, 1f, 0f, projectileVelocityToPass);
                                if (CalamityWorld.LegendaryMode && CalamityWorld.revenge)
                                    Main.projectile[proj2].extraUpdates += 1;
                            }
                        }
                    }
                }

                if (npc.ai[1] >= divisor * (death ? 5f : 10f))
                {
                    npc.TargetClosest();
                    npc.ai[0] = -1f;
                    npc.ai[1] = 3f;
                    npc.ai[2] = 0f;
                    npc.ai[3] = 0f;
                    npc.netUpdate = true;
                }
            }

            // Cocoon bullet hell
            else if (npc.ai[0] == 4f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                npc.defense = npc.defDefense * 4;

                npc.chaseable = false;
                npc.localAI[0] += 1f;
                if (Main.getGoodWorld)
                    npc.localAI[0] += 2f;
                if (expertMode)
                    npc.localAI[0] += 1f - lifeRatio;
                npc.localAI[0] += enrageScale;

                if (npc.localAI[0] >= 120f)
                {
                    npc.localAI[0] = 0f;

                    float projectileSpeed = death ? 9f : revenge ? 8f : 6f;
                    projectileSpeed += 2f * enrageScale;

                    Vector2 projectileVelocity = player.Center - npc.Center;

                    float radialOffset = 0.2f;
                    float diameter = 80f;

                    projectileVelocity = projectileVelocity.SafeNormalize(Vector2.UnitY) * projectileSpeed;

                    Vector2 velocity = projectileVelocity;
                    velocity = velocity.SafeNormalize(Vector2.UnitY);
                    velocity *= diameter;

                    int totalProjectiles = 6;
                    float offsetAngle = (float)Math.PI * radialOffset;
                    int type = ModContent.ProjectileType<BrimstoneHellblast>();
                    int damage = npc.GetProjectileDamage(type);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int j = 0; j < totalProjectiles; j++)
                        {
                            float radians = j - (totalProjectiles - 1f) / 2f;
                            Vector2 offset = velocity.RotatedBy(offsetAngle * radians);
                            int proj = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + offset, projectileVelocity, type, damage, 0f, Main.myPlayer, 1f, 0f);
                            Main.projectile[proj].timeLeft = 300;
                            Main.projectile[proj].tileCollide = false;
                            if (CalamityWorld.LegendaryMode && CalamityWorld.revenge)
                                Main.projectile[proj].extraUpdates += 1;
                        }
                    }

                    totalProjectiles = 12;
                    float radians2 = MathHelper.TwoPi / totalProjectiles;
                    type = ModContent.ProjectileType<BrimstoneBarrage>();
                    damage = npc.GetProjectileDamage(type);
                    if (brimmy.currentMode == 4)
                    {
                        type = ModContent.ProjectileType<SirenSong>();
                        SoundEngine.PlaySound(SoundID.Item26, player.Center);
                    }
                    double angleA = radians2 * 0.5;
                    double angleB = MathHelper.ToRadians(90f) - angleA;
                    float velocityX = (float)(projectileSpeed * Math.Sin(angleA) / Math.Sin(angleB));
                    Vector2 spinningPoint = npc.localAI[2] % 2f == 0f ? new Vector2(0f, -projectileSpeed) : new Vector2(-velocityX, -projectileSpeed);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float projectileVelocityToPass = projectileSpeed * 3f;
                        for (int k = 0; k < totalProjectiles; k++)
                        {
                            Vector2 vector255 = spinningPoint.RotatedBy(radians2 * k);
                            int proj = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + vector255.SafeNormalize(Vector2.UnitY) * 5f, vector255, type, damage, 0f, Main.myPlayer, 1f, 0f, projectileVelocityToPass);
                            if (CalamityWorld.LegendaryMode && CalamityWorld.revenge)
                                Main.projectile[proj].extraUpdates += 1;
                        }

                        if (death)
                        {
                            spinningPoint = npc.localAI[2] % 2f == 0f ? new Vector2(-velocityX, -projectileSpeed) : new Vector2(0f, -projectileSpeed);
                            for (int k = 0; k < totalProjectiles; k++)
                            {
                                Vector2 vector255 = spinningPoint.RotatedBy(radians2 * k);
                                int proj = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + vector255.SafeNormalize(Vector2.UnitY) * 5f, vector255 * 0.75f, type, damage, 0f, Main.myPlayer, 1f, 0f, projectileVelocityToPass);
                                if (CalamityWorld.LegendaryMode && CalamityWorld.revenge)
                                    Main.projectile[proj].extraUpdates += 1;
                            }
                        }
                    }

                    npc.localAI[2] += 1f;
                }

                npc.velocity *= 0.95f;
                npc.rotation = npc.velocity.X * 0.04f;
                float playerLocation = npc.Center.X - player.Center.X;
                npc.direction = playerLocation < 0f ? 1 : -1;
                npc.spriteDirection = npc.direction;

                npc.ai[1] += 1f;
                if (npc.ai[1] >= (death ? 240f : 300f))
                {
                    npc.TargetClosest();
                    npc.ai[0] = -1f;
                    npc.ai[1] = 4f;
                    npc.ai[2] = 0f;
                    npc.ai[3] = 0f;
                    npc.localAI[0] = 0f;
                    npc.netUpdate = true;
                }
            }

            // Laser beam attack
            else if (npc.ai[0] == 5f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                npc.chaseable = true;

                npc.defense = npc.defDefense * 2;

                Vector2 source = new Vector2(npc.Center.X + (npc.spriteDirection > 0 ? 34f : -34f), npc.Center.Y - 74f);
                Vector2 aimAt = player.Center + player.velocity * 20f;
                float aimResponsiveness = bossRush ? 0.05f : (npc.ai[2] == 1f || death) ? 0.1f : 0.25f;

                Vector2 aimVector = (aimAt - source).SafeNormalize(Vector2.UnitY);
                if (aimVector.HasNaNs())
                    aimVector = -Vector2.UnitY;
                aimVector = (Vector2.Lerp(aimVector, npc.velocity.SafeNormalize(Vector2.UnitY), aimResponsiveness)).SafeNormalize(Vector2.UnitY);
                aimVector *= 6f;

                Vector2 laserVelocity = aimVector.SafeNormalize(Vector2.UnitY);
                if (laserVelocity.HasNaNs())
                    laserVelocity = -Vector2.UnitY;

                calamityGlobalNPC.newAI[1] = laserVelocity.X;
                calamityGlobalNPC.newAI[2] = laserVelocity.Y;

                // Rev = 190 + 165 = 355
                // Death = 165

                npc.ai[1] += 1f;
                if (npc.ai[1] >= 240f)
                {
                    npc.TargetClosest();
                    npc.ai[2] += 1f;
                    npc.localAI[0] = 0f;
                    npc.localAI[1] = 0f;
                    if (npc.ai[2] >= (death ? 1f : 2f))
                    {
                        npc.ai[0] = -1f;
                        npc.ai[1] = 5f;
                        npc.ai[2] = 0f;
                        calamityGlobalNPC.newAI[0] = 0f;
                    }
                    else
                    {
                        npc.ai[1] = 0f;
                        calamityGlobalNPC.newAI[0] = 0f;
                    }
                }
                else if (npc.ai[1] >= 180f)
                {
                    npc.velocity *= 0.95f;
                    if (npc.ai[1] == 180f)
                    {
                        SoundEngine.PlaySound(SoundID.Item68, source);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 laserVelocity2 = new Vector2(npc.localAI[0], npc.localAI[1]);
                            laserVelocity2 = laserVelocity2.SafeNormalize(Vector2.UnitY);
                            int type = ModContent.ProjectileType<BrimstoneRay>();
                            int damage = npc.GetProjectileDamage(type);

                            Projectile.NewProjectile(npc.GetSource_FromAI(), source, laserVelocity2, type, damage, 0f, Main.myPlayer, 0f, npc.whoAmI);
                            if (Main.getGoodWorld)
                                Projectile.NewProjectile(npc.GetSource_FromAI(), source, -laserVelocity2, type, damage, 0f, Main.myPlayer, 0f, npc.whoAmI);

                            if (Main.zenithWorld)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromAI(), source, new Vector2(-laserVelocity2.X, laserVelocity2.Y), type, damage, 0f, Main.myPlayer, 0f, npc.whoAmI);
                                Projectile.NewProjectile(npc.GetSource_FromAI(), source, new Vector2(laserVelocity2.X, -laserVelocity2.Y), type, damage, 0f, Main.myPlayer, 0f, npc.whoAmI);
                            }
                        }
                    }
                }
                else
                {
                    float playSoundTimer = 30f;
                    if (npc.ai[1] < 150f)
                    {
                        switch ((int)npc.ai[2])
                        {
                            case 0:
                                npc.ai[1] += 0.5f;
                                break;
                            case 1:
                                npc.ai[1] += 1f;
                                playSoundTimer = 40f;
                                break;
                        }
                        if (death)
                        {
                            npc.ai[1] += 0.5f;
                            playSoundTimer += 10f;
                        }
                    }

                    if (npc.ai[1] % playSoundTimer == 0f)
                        SoundEngine.PlaySound(SoundID.Item20, npc.Center);

                    if (npc.ai[1] < 150f && calamityGlobalNPC.newAI[0] == 0f)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromAI(), source, laserVelocity, ModContent.ProjectileType<BrimstoneTargetRay>(), 0, 0f, Main.myPlayer, 0f, npc.whoAmI);
                            if (Main.getGoodWorld)
                                Projectile.NewProjectile(npc.GetSource_FromAI(), source, -laserVelocity, ModContent.ProjectileType<BrimstoneTargetRay>(), 0, 0f, Main.myPlayer, 0f, npc.whoAmI);

                            if (Main.zenithWorld)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromAI(), source, new Vector2(-laserVelocity.X, laserVelocity.Y), ModContent.ProjectileType<BrimstoneTargetRay>(), 0, 0f, Main.myPlayer, 0f, npc.whoAmI);
                                Projectile.NewProjectile(npc.GetSource_FromAI(), source, new Vector2(laserVelocity.X, -laserVelocity.Y), ModContent.ProjectileType<BrimstoneTargetRay>(), 0, 0f, Main.myPlayer, 0f, npc.whoAmI);
                            }
                        }

                        calamityGlobalNPC.newAI[0] = 1f;
                    }
                    else
                    {
                        if (npc.ai[1] == 150f)
                        {
                            npc.localAI[0] = laserVelocity.X;
                            npc.localAI[1] = laserVelocity.Y;

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromAI(), source.X, source.Y, npc.localAI[0], npc.localAI[1], ModContent.ProjectileType<BrimstoneTargetRay>(), 0, 0f, Main.myPlayer, 1f, npc.whoAmI);
                                if (Main.getGoodWorld)
                                    Projectile.NewProjectile(npc.GetSource_FromAI(), source.X, source.Y, -npc.localAI[0], -npc.localAI[1], ModContent.ProjectileType<BrimstoneTargetRay>(), 0, 0f, Main.myPlayer, 1f, npc.whoAmI);

                                if (Main.zenithWorld)
                                {
                                    Projectile.NewProjectile(npc.GetSource_FromAI(), source.X, source.Y, -npc.localAI[0], npc.localAI[1], ModContent.ProjectileType<BrimstoneTargetRay>(), 0, 0f, Main.myPlayer, 1f, npc.whoAmI);
                                    Projectile.NewProjectile(npc.GetSource_FromAI(), source.X, source.Y, npc.localAI[0], -npc.localAI[1], ModContent.ProjectileType<BrimstoneTargetRay>(), 0, 0f, Main.myPlayer, 1f, npc.whoAmI);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
