using System;
using CalamityMod.Events;
using CalamityMod.NPCs.NormalNPCs;
using CalamityMod.UI;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.NPCs.VanillaNPCAIOverrides.Bosses
{
    public static class KingSlimeAI
    {
        public static bool BuffedKingSlimeAI(NPC npc, Mod mod)
        {
            // Percent life remaining
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float lifeRatio2 = lifeRatio;

            // Variables
            float teleportScale = 1f;
            bool teleporting = false;
            bool teleported = false;
            npc.aiAction = 0;
            float teleportScaleSpeed = 2f;
            if (Main.getGoodWorld)
            {
                teleportScaleSpeed -= 1f - lifeRatio;
                teleportScale *= teleportScaleSpeed;
            }

            bool bossRush = BossRushEvent.BossRushActive;
            bool masterMode = Main.masterMode || bossRush;
            bool death = CalamityWorld.death || bossRush;

            // Get a target
            if (npc.target < 0 || npc.target == Main.maxPlayers || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            // Phases based on life percentage

            // Higher velocity jumps phase
            bool phase2 = lifeRatio < 0.75f;

            // Spawn Emerald Crystal phase
            bool spawnGreenCrystal = lifeRatio < 0.7f;

            // Spawn Ruby Crystal phase
            bool phase3 = lifeRatio < 0.5f;

            // Spawn Sapphire Crystal phase
            bool spawnBlueCrystal = lifeRatio < 0.3f;

            // Check if the crystals are alive
            bool crystalAlive = true;
            bool blueCrystalAlive = false;
            bool greenCrystalAlive = true;

            if (spawnGreenCrystal)
            {
                if (masterMode)
                    greenCrystalAlive = NPC.AnyNPCs(ModContent.NPCType<KingSlimeJewelEmerald>());
            }

            if (phase3)
                crystalAlive = NPC.AnyNPCs(ModContent.NPCType<KingSlimeJewelRuby>());

            if (spawnBlueCrystal)
            {
                if (masterMode)
                    blueCrystalAlive = NPC.AnyNPCs(ModContent.NPCType<KingSlimeJewelSapphire>());
            }

            // Sapphire Crystal buffs
            int setDamage = npc.defDamage;
            npc.defense = npc.defDefense;
            if (blueCrystalAlive)
            {
                setDamage = (int)Math.Round(setDamage * 1.5);
                npc.defense *= 2;
            }

            // Dust color when the blue crystal is alive
            Color dustColor = Color.Lerp(new Color(0, 0, 150, npc.alpha), new Color(125, 125, 255, npc.alpha), (float)Math.Sin(Main.GlobalTimeWrappedHourly) / 2f + 0.5f);

            // Master Mode Crystal spawning
            if (masterMode)
            {
                if (spawnGreenCrystal && npc.Calamity().newAI[0] == 0f)
                {
                    npc.Calamity().newAI[0] = 1f;
                    npc.SyncExtraAI();
                    Vector2 vector = npc.Center + new Vector2(-40f, -(float)npc.height / 2) * npc.scale;
                    int totalDustPerCrystalSpawn = 20;
                    for (int i = 0; i < totalDustPerCrystalSpawn; i++)
                    {
                        int emeraldDust = Dust.NewDust(vector, npc.width / 2, npc.height / 2, DustID.GemEmerald, 0f, 0f, 100, default, 2f);
                        Main.dust[emeraldDust].velocity *= 2f;
                        Main.dust[emeraldDust].noGravity = true;
                        if (Main.rand.NextBool())
                        {
                            Main.dust[emeraldDust].scale = 0.5f;
                            Main.dust[emeraldDust].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                        }
                    }

                    SoundEngine.PlaySound(SoundID.Item38, vector);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NPC.NewNPC(npc.GetSource_FromAI(), (int)vector.X, (int)vector.Y, ModContent.NPCType<KingSlimeJewelEmerald>());
                }

                if (phase3 && npc.Calamity().newAI[0] == 1f)
                {
                    npc.Calamity().newAI[0] = 2f;
                    npc.SyncExtraAI();
                    Vector2 vector = npc.Center + new Vector2(-40f, -(float)npc.height / 2) * npc.scale;
                    int totalDustPerCrystalSpawn = 20;
                    for (int i = 0; i < totalDustPerCrystalSpawn; i++)
                    {
                        int rubyDust = Dust.NewDust(vector, npc.width / 2, npc.height / 2, DustID.GemRuby, 0f, 0f, 100, default, 2f);
                        Main.dust[rubyDust].velocity *= 2f;
                        Main.dust[rubyDust].noGravity = true;
                        if (Main.rand.NextBool())
                        {
                            Main.dust[rubyDust].scale = 0.5f;
                            Main.dust[rubyDust].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                        }
                    }

                    SoundEngine.PlaySound(SoundID.Item38, vector);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NPC.NewNPC(npc.GetSource_FromAI(), (int)vector.X, (int)vector.Y, ModContent.NPCType<KingSlimeJewelRuby>());
                }

                if (spawnBlueCrystal && npc.Calamity().newAI[0] == 2f)
                {
                    npc.Calamity().newAI[0] = 3f;
                    npc.SyncExtraAI();
                    Vector2 vector = npc.Center + new Vector2(-40f, -(float)npc.height / 2) * npc.scale;
                    int totalDustPerCrystalSpawn = 20;
                    for (int i = 0; i < totalDustPerCrystalSpawn; i++)
                    {
                        int sapphireDust = Dust.NewDust(vector, npc.width / 2, npc.height / 2, DustID.GemSapphire, 0f, 0f, 100, default, 2f);
                        Main.dust[sapphireDust].velocity *= 2f;
                        Main.dust[sapphireDust].noGravity = true;
                        if (Main.rand.NextBool())
                        {
                            Main.dust[sapphireDust].scale = 0.5f;
                            Main.dust[sapphireDust].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                        }
                    }

                    SoundEngine.PlaySound(SoundID.Item38, vector);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NPC.NewNPC(npc.GetSource_FromAI(), (int)vector.X, (int)vector.Y, ModContent.NPCType<KingSlimeJewelSapphire>());
                }
            }
            else
            {
                // Spawn crystal in phase 2
                if (phase3 && npc.Calamity().newAI[0] == 0f)
                {
                    npc.Calamity().newAI[0] = 1f;
                    npc.SyncExtraAI();
                    Vector2 vector = npc.Center + new Vector2(-40f, -(float)npc.height / 2) * npc.scale;
                    int totalDustPerCrystalSpawn = 20;
                    for (int i = 0; i < totalDustPerCrystalSpawn; i++)
                    {
                        int rubyDust = Dust.NewDust(vector, npc.width / 2, npc.height / 2, DustID.GemRuby, 0f, 0f, 100, default, 2f);
                        Main.dust[rubyDust].velocity *= 2f;
                        Main.dust[rubyDust].noGravity = true;
                        if (Main.rand.NextBool())
                        {
                            Main.dust[rubyDust].scale = 0.5f;
                            Main.dust[rubyDust].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                        }
                    }

                    SoundEngine.PlaySound(SoundID.Item38, vector);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NPC.NewNPC(npc.GetSource_FromAI(), (int)vector.X, (int)vector.Y, ModContent.NPCType<KingSlimeJewelRuby>());
                }
            }

            // Set up health value for spawning slimes
            if (npc.ai[3] == 0f && npc.life > 0)
                npc.ai[3] = npc.lifeMax;

            // Spawn with attack delay
            if (npc.localAI[3] == 0f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                npc.ai[0] = -100f;
                npc.localAI[3] = 1f;
                npc.netUpdate = true;
            }

            // Despawn
            int despawnDistance = 500;
            if (Main.player[npc.target].dead || Math.Abs(npc.Center.X - Main.player[npc.target].Center.X) / 16f > despawnDistance)
            {
                npc.TargetClosest();
                if (Main.player[npc.target].dead || Math.Abs(npc.Center.X - Main.player[npc.target].Center.X) / 16f > despawnDistance)
                {
                    if (npc.timeLeft > 10)
                        npc.timeLeft = 10;

                    if (Main.player[npc.target].Center.X < npc.Center.X)
                        npc.direction = 1;
                    else
                        npc.direction = -1;
                }
            }

            // Faster fall
            if (npc.velocity.Y > 0f)
            {
                float fallSpeedBonus = (bossRush ? 0.1f : death ? 0.05f : 0f) + (!crystalAlive ? 0.1f : 0f) + (masterMode ? 0.1f : 0f);
                npc.velocity.Y += fallSpeedBonus;
            }

            // Activate teleport
            float teleportGateValue = 480f;
            if (!Main.player[npc.target].dead && npc.ai[2] >= teleportGateValue && npc.ai[1] < 5f && npc.velocity.Y == 0f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                npc.ai[2] = 0f;
                npc.ai[0] = 0f;
                npc.ai[1] = 5f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    GetPlaceToTeleportTo(npc);
            }

            if (!Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0) || Math.Abs(npc.Top.Y - Main.player[npc.target].Bottom.Y) > 160f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    npc.localAI[0] += 1f;
            }
            else if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                npc.localAI[0] -= 1f;

                if (npc.localAI[0] < 0f)
                    npc.localAI[0] = 0f;
            }

            if (npc.timeLeft < 10 && (npc.ai[0] != 0f || npc.ai[1] != 0f))
            {
                npc.ai[0] = 0f;
                npc.ai[1] = 0f;
                npc.netUpdate = true;
                teleporting = false;
            }

            // Get closer to activating teleport
            if (npc.ai[2] < teleportGateValue)
            {
                if (!Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0) || Math.Abs(npc.Top.Y - Main.player[npc.target].Bottom.Y) > (masterMode ? 160f : 320f))
                    npc.ai[2] += death ? 3f : 2f;
                else
                    npc.ai[2] += 1f;
            }

            // Teleport
            if (npc.ai[1] == 5f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                teleporting = true;
                npc.aiAction = 1;
                
                float teleportRate = crystalAlive ? 1f : 2f;
                if (masterMode)
                    teleportRate *= 2f;

                npc.ai[0] += teleportRate;
                teleportScale = MathHelper.Clamp((60f - npc.ai[0]) / 60f, 0f, 1f);
                teleportScale = 0.5f + teleportScale * 0.5f;
                if (Main.getGoodWorld)
                    teleportScale *= teleportScaleSpeed;

                if (npc.ai[0] >= 60f)
                    teleported = true;

                if (npc.ai[0] == 60f && Main.netMode != NetmodeID.Server)
                    Gore.NewGore(npc.GetSource_FromAI(), npc.Center + new Vector2(-40f, -(float)npc.height / 2), npc.velocity, 734, 1f);

                if (npc.ai[0] >= 60f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.Bottom = new Vector2(npc.localAI[1], npc.localAI[2]);
                    npc.ai[1] = 6f;
                    npc.ai[0] = 0f;
                    npc.netUpdate = true;
                }

                if (Main.netMode == NetmodeID.MultiplayerClient && npc.ai[0] >= 120f)
                {
                    npc.ai[1] = 6f;
                    npc.ai[0] = 0f;
                }

                if (!teleported)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        int slimeDust = Dust.NewDust(npc.position + Vector2.UnitX * -20f, npc.width + 40, npc.height, blueCrystalAlive ? DustID.GemSapphire : DustID.TintableDust, npc.velocity.X, npc.velocity.Y, blueCrystalAlive ? 100 : 150, blueCrystalAlive ? dustColor : new Color(78, 136, 255, 80), 2f);
                        Main.dust[slimeDust].noGravity = true;
                        Main.dust[slimeDust].velocity *= blueCrystalAlive ? 0f : 0.5f;
                    }
                }
            }

            // Post-teleport
            else if (npc.ai[1] == 6f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                teleporting = true;
                npc.aiAction = 0;

                float teleportRate = crystalAlive ? 1f : 2f;
                if (masterMode)
                    teleportRate *= 2f;

                if (npc.ai[0] == 0f)
                {
                    // Move Blue Crystal
                    if (blueCrystalAlive)
                    {
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            NPC blueCrystal = Main.npc[i];
                            if (blueCrystal.active && blueCrystal.type == ModContent.NPCType<KingSlimeJewelSapphire>())
                            {
                                blueCrystal.position.X = npc.position.X;
                                blueCrystal.position.Y = npc.position.Y - 200f;

                                for (int dusty = 0; dusty < 10; dusty++)
                                {
                                    Vector2 dustVel = Main.rand.NextVector2CircularEdge(5f, 5f);
                                    int sapphire = Dust.NewDust(blueCrystal.Center, blueCrystal.width, blueCrystal.height, DustID.GemSapphire, 0f, 0f, 100, default, 2f);
                                    Main.dust[sapphire].velocity = dustVel * Main.rand.NextFloat(1f, 2f);
                                    Main.dust[sapphire].noGravity = true;
                                    if (Main.rand.NextBool())
                                    {
                                        Main.dust[sapphire].scale = 0.5f;
                                        Main.dust[sapphire].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                                    }
                                }

                                break;
                            }
                        }
                    }
                }

                npc.ai[0] += teleportRate;
                teleportScale = MathHelper.Clamp(npc.ai[0] / 30f, 0f, 1f);
                teleportScale = 0.5f + teleportScale * 0.5f;
                if (Main.getGoodWorld)
                    teleportScale *= teleportScaleSpeed;

                if (npc.ai[0] >= 30f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.ai[1] = 0f;
                    npc.ai[0] = -15f;
                    npc.netUpdate = true;
                    npc.TargetClosest();
                }

                if (Main.netMode == NetmodeID.MultiplayerClient && npc.ai[0] >= 60f)
                {
                    npc.ai[1] = 0f;
                    npc.ai[0] = -15f;
                    npc.TargetClosest();
                }

                for (int j = 0; j < 10; j++)
                {
                    int slimyDust = Dust.NewDust(npc.position + Vector2.UnitX * -20f, npc.width + 40, npc.height, blueCrystalAlive ? DustID.GemSapphire : DustID.TintableDust, npc.velocity.X, npc.velocity.Y, blueCrystalAlive ? 100 : 150, blueCrystalAlive ? dustColor : new Color(78, 136, 255, 80), 2f);
                    Main.dust[slimyDust].noGravity = true;
                    Main.dust[slimyDust].velocity *= blueCrystalAlive ? 0f : 2f;
                }
            }

            npc.noTileCollide = false;

            // Jump
            if (npc.velocity.Y == 0f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                npc.velocity.X *= 0.8f;
                if (npc.velocity.X > -0.1f && npc.velocity.X < 0.1f)
                    npc.velocity.X = 0f;

                if (!teleporting)
                {
                    npc.ai[0] += (bossRush ? 15f : MathHelper.Lerp(1f, 8f, 1f - lifeRatio));
                    if (npc.ai[0] >= 0f)
                    {
                        // Set damage
                        npc.damage = setDamage;

                        npc.netUpdate = true;
                        npc.TargetClosest();

                        float distanceBelowTarget = npc.position.Y - (Main.player[npc.target].position.Y + 80f);
                        float speedMult = 1f;
                        if (distanceBelowTarget > 0f)
                            speedMult += distanceBelowTarget * 0.002f;

                        if (speedMult > 2f)
                            speedMult = 2f;

                        bool deathModeRapidHops = death && lifeRatio < 0.3f;
                        if (deathModeRapidHops)
                            npc.ai[1] = 2f;

                        float bossRushJumpSpeedMult = 1.5f;

                        // Jump type
                        if (npc.ai[1] == 3f)
                        {
                            npc.velocity.Y = -10f * speedMult;
                            npc.velocity.X += (phase2 ? (death ? 5.5f : 4.5f) : 3.5f) * npc.direction;
                            npc.ai[0] = -100f;
                            npc.ai[1] = 0f;
                        }
                        else if (npc.ai[1] == 2f)
                        {
                            npc.velocity.Y = -6f * speedMult;
                            npc.velocity.X += (phase2 ? (deathModeRapidHops ? 8f : death ? 6.5f : 5.5f) : 4.5f) * npc.direction;
                            npc.ai[0] = -60f;

                            // Use the quick forward jump over and over while at low HP in death mode
                            if (!deathModeRapidHops)
                                npc.ai[1] += 1f;
                        }
                        else
                        {
                            npc.velocity.Y = -8f * speedMult;
                            npc.velocity.X += (phase2 ? (death ? 6f : 5f) : 4f) * npc.direction;
                            npc.ai[0] = -60f;
                            npc.ai[1] += 1f;
                        }

                        if (!greenCrystalAlive)
                        {
                            npc.velocity.X *= 1.2f;
                            npc.velocity.Y *= 0.6f;
                        }

                        if (masterMode)
                            npc.velocity.X *= 1.4f;

                        if (bossRush)
                            npc.velocity.X *= bossRushJumpSpeedMult;

                        npc.noTileCollide = true;
                    }
                    else if (npc.ai[0] >= -30f)
                        npc.aiAction = 1;
                }
            }

            // Change jump velocity
            else if (npc.target < Main.maxPlayers)
            {
                float jumpVelocityLimit = crystalAlive ? 3f : 4.5f;
                if (masterMode)
                    jumpVelocityLimit += 3f;
                if (Main.getGoodWorld)
                    jumpVelocityLimit = 8f;

                if ((npc.direction == 1 && npc.velocity.X < jumpVelocityLimit) || (npc.direction == -1 && npc.velocity.X > -jumpVelocityLimit))
                {
                    if ((npc.direction == -1 && npc.velocity.X < 0.1) || (npc.direction == 1 && npc.velocity.X > -0.1))
                    {
                        npc.velocity.X += (bossRush ? 0.4f : death ? 0.25f : 0.2f) * npc.direction;
                        if (masterMode)
                            npc.velocity.X += 0.3f * npc.direction;
                    }
                    else
                    {
                        npc.velocity.X *= bossRush ? 0.9f : death ? 0.92f : 0.93f;
                        if (masterMode)
                            npc.velocity.X *= 0.9f;
                    }
                }

                if (!Main.player[npc.target].dead)
                {
                    if (npc.velocity.Y > 0f && npc.Bottom.Y > Main.player[npc.target].Top.Y)
                        npc.noTileCollide = false;
                    else if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].Center, 1, 1) && !Collision.SolidCollision(npc.position, npc.width, npc.height))
                        npc.noTileCollide = false;
                    else
                        npc.noTileCollide = true;
                }
            }

            int idleSlimeDust = Dust.NewDust(npc.position, npc.width, npc.height, blueCrystalAlive ? DustID.GemSapphire : DustID.TintableDust, npc.velocity.X, npc.velocity.Y, blueCrystalAlive ? 100 : 255, blueCrystalAlive ? dustColor : new Color(0, 80, 255, 80), npc.scale * 1.2f);
            Main.dust[idleSlimeDust].noGravity = true;
            Main.dust[idleSlimeDust].velocity *= blueCrystalAlive ? 0f : 0.5f;

            if (npc.life <= 0)
                return false;

            // Adjust size based on HP
            float maxScale = death ? (Main.getGoodWorld ? 6f : 3f) : (Main.getGoodWorld ? 3f : 1.25f);
            float minScale = death ? 0.5f : 0.75f;
            float maxScaledValue = maxScale - minScale;

            // Inversed scale in FTW
            if (Main.getGoodWorld)
                lifeRatio = (maxScaledValue - lifeRatio * maxScaledValue) + minScale;
            else
                lifeRatio = lifeRatio * maxScaledValue + minScale;

            lifeRatio *= teleportScale;
            if (lifeRatio != npc.scale)
            {
                npc.position.X += npc.width / 2;
                npc.position.Y += npc.height;
                npc.scale = lifeRatio;
                npc.width = (int)(98f * npc.scale);
                npc.height = (int)(92f * npc.scale);
                npc.position.X -= npc.width / 2;
                npc.position.Y -= npc.height;
            }

            // Slime spawning
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int slimeSpawnThreshold = (int)(npc.lifeMax * 0.03);
                if (npc.life + slimeSpawnThreshold < npc.ai[3])
                {
                    npc.ai[3] = npc.life;
                    int slimeAmt = Main.rand.Next(1, 3);
                    for (int i = 0; i < slimeAmt; i++)
                    {
                        float minLowerLimit = death ? 5f : 0f;
                        float maxLowerLimit = death ? 7f : 2f;
                        int minTypeChoice = (int)MathHelper.Lerp(minLowerLimit, 7f, 1f - lifeRatio2);
                        int maxTypeChoice = (int)MathHelper.Lerp(maxLowerLimit, 9f, 1f - lifeRatio2);

                        int npcType;
                        switch (Main.rand.Next(minTypeChoice, maxTypeChoice + 1))
                        {
                            default:
                                npcType = NPCID.SlimeSpiked;
                                break;
                            case 0:
                                npcType = NPCID.GreenSlime;
                                break;
                            case 1:
                                npcType = Main.raining ? NPCID.UmbrellaSlime : NPCID.BlueSlime;
                                break;
                            case 2:
                                npcType = NPCID.IceSlime;
                                break;
                            case 3:
                                npcType = NPCID.RedSlime;
                                break;
                            case 4:
                                npcType = NPCID.PurpleSlime;
                                break;
                            case 5:
                                npcType = NPCID.YellowSlime;
                                break;
                            case 6:
                                npcType = NPCID.SlimeSpiked;
                                break;
                            case 7:
                                npcType = NPCID.SpikedIceSlime;
                                break;
                            case 8:
                                npcType = NPCID.SpikedJungleSlime;
                                break;
                        }

                        if (((Main.raining && Main.hardMode) || bossRush) && Main.rand.NextBool(50))
                            npcType = NPCID.RainbowSlime;

                        if (masterMode)
                            npcType = Main.rand.NextBool() ? NPCID.SpikedIceSlime : NPCID.SpikedJungleSlime;

                        if (Main.rand.NextBool(100))
                            npcType = NPCID.Pinky;

                        if (CalamityWorld.LegendaryMode)
                            npcType = NPCID.RainbowSlime;

                        int spawnZoneWidth = npc.width - 32;
                        int spawnZoneHeight = npc.height - 32;
                        int x = (int)(npc.position.X + Main.rand.Next(spawnZoneWidth));
                        int y = (int)(npc.position.Y + Main.rand.Next(spawnZoneHeight));
                        int slimeSpawns = NPC.NewNPC(npc.GetSource_FromAI(), x, y, npcType);
                        Main.npc[slimeSpawns].SetDefaults(npcType);
                        Main.npc[slimeSpawns].velocity.X = Main.rand.Next(-15, 16) * 0.1f;
                        Main.npc[slimeSpawns].velocity.Y = Main.rand.Next(-30, 31) * 0.1f;
                        Main.npc[slimeSpawns].ai[0] = -1000 * Main.rand.Next(3);
                        Main.npc[slimeSpawns].ai[1] = 0f;

                        if (Main.netMode == NetmodeID.Server && slimeSpawns < Main.maxNPCs)
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, slimeSpawns);
                    }
                }
            }
            return false;
        }

        // If you think for a fucking second that I'm going to refactor this...
        public static bool VanillaKingSlimeAI(NPC npc, Mod mod)
        {
            float num236 = 1f;
            float num237 = 1f;
            bool flag6 = false;
            bool flag7 = false;
            bool flag8 = false;
            float num238 = 2f;
            if (Main.getGoodWorld)
            {
                num238 -= 1f - (float)npc.life / (float)npc.lifeMax;
                num237 *= num238;
            }

            npc.aiAction = 0;
            if (npc.ai[3] == 0f && npc.life > 0)
                npc.ai[3] = npc.lifeMax;

            if (npc.localAI[3] == 0f)
            {
                npc.localAI[3] = 1f;
                flag6 = true;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.ai[0] = -100f;
                    npc.TargetClosest();
                    npc.netUpdate = true;
                }
            }

            int num239 = 3000;
            if (Main.player[npc.target].dead || Vector2.Distance(npc.Center, Main.player[npc.target].Center) > (float)num239)
            {
                npc.TargetClosest();
                if (Main.player[npc.target].dead || Vector2.Distance(npc.Center, Main.player[npc.target].Center) > (float)num239)
                {
                    npc.EncourageDespawn(10);
                    if (Main.player[npc.target].Center.X < npc.Center.X)
                        npc.direction = 1;
                    else
                        npc.direction = -1;

                    if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[1] != 5f)
                    {
                        npc.netUpdate = true;
                        npc.ai[2] = 0f;
                        npc.ai[0] = 0f;
                        npc.ai[1] = 5f;
                        npc.localAI[1] = Main.maxTilesX * 16;
                        npc.localAI[2] = Main.maxTilesY * 16;
                    }
                }
            }

            if (!Main.player[npc.target].dead && npc.timeLeft > 10 && npc.ai[2] >= 300f && npc.ai[1] < 5f && npc.velocity.Y == 0f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                npc.ai[2] = 0f;
                npc.ai[0] = 0f;
                npc.ai[1] = 5f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.TargetClosest(false);
                    Point point3 = npc.Center.ToTileCoordinates();
                    Point point4 = Main.player[npc.target].Center.ToTileCoordinates();
                    Vector2 vector30 = Main.player[npc.target].Center - npc.Center;
                    int num240 = 10;
                    int num241 = 0;
                    int num242 = 7;
                    int num243 = 0;
                    bool flag9 = false;
                    if (npc.localAI[0] >= 360f || vector30.Length() > 2000f)
                    {
                        if (npc.localAI[0] >= 360f)
                            npc.localAI[0] = 360f;

                        flag9 = true;
                        num243 = 100;
                    }

                    while (!flag9 && num243 < 100)
                    {
                        num243++;
                        int num244 = Main.rand.Next(point4.X - num240, point4.X + num240 + 1);
                        int num245 = Main.rand.Next(point4.Y - num240, point4.Y + 1);
                        if ((num245 >= point4.Y - num242 && num245 <= point4.Y + num242 && num244 >= point4.X - num242 && num244 <= point4.X + num242) || (num245 >= point3.Y - num241 && num245 <= point3.Y + num241 && num244 >= point3.X - num241 && num244 <= point3.X + num241) || Main.tile[num244, num245].HasUnactuatedTile)
                            continue;

                        int num246 = num245;
                        int num247 = 0;
                        if (Main.tile[num244, num246].HasUnactuatedTile && Main.tileSolid[Main.tile[num244, num246].TileType] && !Main.tileSolidTop[Main.tile[num244, num246].TileType])
                        {
                            num247 = 1;
                        }
                        else
                        {
                            for (; num247 < 150 && num246 + num247 < Main.maxTilesY; num247++)
                            {
                                int num248 = num246 + num247;
                                if (Main.tile[num244, num248].HasUnactuatedTile && Main.tileSolid[Main.tile[num244, num248].TileType] && !Main.tileSolidTop[Main.tile[num244, num248].TileType])
                                {
                                    num247--;
                                    break;
                                }
                            }
                        }

                        num245 += num247;
                        bool flag10 = true;
                        if (flag10 && Main.tile[num244, num245].LiquidType == LiquidID.Lava)
                            flag10 = false;

                        if (flag10 && !Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                            flag10 = false;

                        if (flag10)
                        {
                            npc.localAI[1] = num244 * 16 + 8;
                            npc.localAI[2] = num245 * 16 + 16;
                            flag9 = true;
                            break;
                        }
                    }

                    if (num243 >= 100)
                    {
                        Vector2 bottom = Main.player[Player.FindClosest(npc.position, npc.width, npc.height)].Bottom;
                        npc.localAI[1] = bottom.X;
                        npc.localAI[2] = bottom.Y;
                    }
                }
            }

            if (!Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0) || Math.Abs(npc.Top.Y - Main.player[npc.target].Bottom.Y) > 160f)
            {
                npc.ai[2]++;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    npc.localAI[0]++;
            }
            else if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                npc.localAI[0]--;
                if (npc.localAI[0] < 0f)
                    npc.localAI[0] = 0f;
            }

            if (npc.timeLeft < 10 && (npc.ai[0] != 0f || npc.ai[1] != 0f))
            {
                npc.ai[0] = 0f;
                npc.ai[1] = 0f;
                npc.netUpdate = true;
                flag7 = false;
            }

            Dust dust;
            if (npc.ai[1] == 5f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                flag7 = true;
                npc.aiAction = 1;
                npc.ai[0]++;
                num236 = MathHelper.Clamp((60f - npc.ai[0]) / 60f, 0f, 1f);
                num236 = 0.5f + num236 * 0.5f;
                if (npc.ai[0] >= 60f)
                    flag8 = true;

                if (npc.ai[0] == 60f)
                    Gore.NewGore(npc.GetSource_FromAI(), npc.Center + new Vector2(-40f, -npc.height / 2), npc.velocity, 734);

                if (npc.ai[0] >= 60f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.Bottom = new Vector2(npc.localAI[1], npc.localAI[2]);
                    npc.ai[1] = 6f;
                    npc.ai[0] = 0f;
                    npc.netUpdate = true;
                }

                if (Main.netMode == NetmodeID.MultiplayerClient && npc.ai[0] >= 120f)
                {
                    npc.ai[1] = 6f;
                    npc.ai[0] = 0f;
                }

                if (!flag8)
                {
                    for (int num249 = 0; num249 < 10; num249++)
                    {
                        int num250 = Dust.NewDust(npc.position + Vector2.UnitX * -20f, npc.width + 40, npc.height, DustID.TintableDust, npc.velocity.X, npc.velocity.Y, 150, new Color(78, 136, 255, 80), 2f);
                        Main.dust[num250].noGravity = true;
                        dust = Main.dust[num250];
                        dust.velocity *= 0.5f;
                    }
                }
            }
            else if (npc.ai[1] == 6f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                flag7 = true;
                npc.aiAction = 0;
                npc.ai[0]++;
                num236 = MathHelper.Clamp(npc.ai[0] / 30f, 0f, 1f);
                num236 = 0.5f + num236 * 0.5f;
                if (npc.ai[0] >= 30f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.ai[1] = 0f;
                    npc.ai[0] = -15f;
                    npc.netUpdate = true;
                    npc.TargetClosest();
                }

                if (Main.netMode == NetmodeID.MultiplayerClient && npc.ai[0] >= 60f)
                {
                    npc.ai[1] = 0f;
                    npc.ai[0] = -15f;
                    npc.TargetClosest();
                }

                for (int num251 = 0; num251 < 10; num251++)
                {
                    int num252 = Dust.NewDust(npc.position + Vector2.UnitX * -20f, npc.width + 40, npc.height, DustID.TintableDust, npc.velocity.X, npc.velocity.Y, 150, new Color(78, 136, 255, 80), 2f);
                    Main.dust[num252].noGravity = true;
                    dust = Main.dust[num252];
                    dust.velocity *= 2f;
                }
            }

            npc.dontTakeDamage = (npc.hide = flag8);
            if (npc.velocity.Y == 0f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                npc.velocity.X *= 0.8f;
                if ((double)npc.velocity.X > -0.1 && (double)npc.velocity.X < 0.1)
                    npc.velocity.X = 0f;

                if (!flag7)
                {
                    npc.ai[0] += (MathHelper.Lerp(1f, 13f, 1f - npc.life / (float)npc.lifeMax));
                    if (npc.ai[0] >= 0f)
                    {
                        // Set damage
                        npc.damage = npc.defDamage;

                        npc.netUpdate = true;
                        npc.TargetClosest();

                        if (npc.ai[1] == 3f)
                        {
                            npc.velocity.Y = -13f;
                            npc.velocity.X += (Main.masterMode ? 5.25f : 3.5f) * (float)npc.direction;
                            npc.ai[0] = -(Main.masterMode ? 140f : 180f);
                            npc.ai[1] = 0f;
                        }
                        else if (npc.ai[1] == 2f)
                        {
                            npc.velocity.Y = -6f;
                            npc.velocity.X += (Main.masterMode ? 6.75f : 4.5f) * (float)npc.direction;
                            npc.ai[0] = -(Main.masterMode ? 80f : 100f);
                            npc.ai[1] += 1f;
                        }
                        else
                        {
                            npc.velocity.Y = -8f;
                            npc.velocity.X += (Main.masterMode ? 6f : 4f) * (float)npc.direction;
                            npc.ai[0] = -(Main.masterMode ? 80f : 100f);
                            npc.ai[1] += 1f;
                        }
                    }
                    else if (npc.ai[0] >= -30f)
                    {
                        npc.aiAction = 1;
                    }
                }
            }
            else if (npc.target < Main.maxPlayers)
            {
                float num253 = Main.masterMode ? 4.5f : 3f;
                if (Main.getGoodWorld)
                    num253 = 6f;

                if ((npc.direction == 1 && npc.velocity.X < num253) || (npc.direction == -1 && npc.velocity.X > 0f - num253))
                {
                    if ((npc.direction == -1 && (double)npc.velocity.X < 0.1) || (npc.direction == 1 && (double)npc.velocity.X > -0.1))
                        npc.velocity.X += (Main.masterMode ? 0.3f : 0.2f) * (float)npc.direction;
                    else
                        npc.velocity.X *= (Main.masterMode ? 0.86f : 0.93f);
                }
            }

            int num254 = Dust.NewDust(npc.position, npc.width, npc.height, DustID.TintableDust, npc.velocity.X, npc.velocity.Y, 255, new Color(0, 80, 255, 80), npc.scale * 1.2f);
            Main.dust[num254].noGravity = true;
            dust = Main.dust[num254];
            dust.velocity *= 0.5f;
            if (npc.life <= 0)
                return false;

            float num255 = (float)npc.life / (float)npc.lifeMax;
            num255 = num255 * 0.5f + 0.75f;
            num255 *= num236;
            num255 *= num237;
            if (num255 != npc.scale || flag6)
            {
                npc.position.X += npc.width / 2;
                npc.position.Y += npc.height;
                npc.scale = num255;
                npc.width = (int)(98f * npc.scale);
                npc.height = (int)(92f * npc.scale);
                npc.position.X -= npc.width / 2;
                npc.position.Y -= npc.height;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return false;

            int num256 = (int)((double)npc.lifeMax * 0.05);
            if (!((float)(npc.life + num256) < npc.ai[3]))
                return false;

            npc.ai[3] = npc.life;
            int num257 = Main.rand.Next(1, 4);
            for (int num258 = 0; num258 < num257; num258++)
            {
                int x = (int)(npc.position.X + (float)Main.rand.Next(npc.width - 32));
                int y = (int)(npc.position.Y + (float)Main.rand.Next(npc.height - 32));
                int num259 = 1;

                int chanceForSpikedSlime = Main.masterMode ? 2 : 4;
                if (Main.expertMode && Main.rand.NextBool(chanceForSpikedSlime))
                    num259 = NPCID.SlimeSpiked;

                int num260 = NPC.NewNPC(npc.GetSource_FromAI(), x, y, num259);
                Main.npc[num260].SetDefaults(num259);
                Main.npc[num260].velocity.X = (float)Main.rand.Next(-15, 16) * 0.1f;
                Main.npc[num260].velocity.Y = (float)Main.rand.Next(-30, 1) * 0.1f;
                Main.npc[num260].ai[0] = -1000 * Main.rand.Next(3);
                Main.npc[num260].ai[1] = 0f;
                if (Main.netMode == NetmodeID.Server && num260 < Main.maxNPCs)
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, num260);
            }

            return false;
        }

        public static void GetPlaceToTeleportTo(NPC npc)
        {
            npc.TargetClosest(false);
            float distanceAhead = 800f;
            Vector2 randomDefault = Main.rand.NextBool() ? Vector2.UnitX : -Vector2.UnitX;
            Vector2 vectorAimedAheadOfTarget = Main.player[npc.target].Center + new Vector2((float)Math.Round(Main.player[npc.target].velocity.X), 0f).SafeNormalize(randomDefault) * distanceAhead;
            Point predictiveTeleportPoint = vectorAimedAheadOfTarget.ToTileCoordinates();
            if (predictiveTeleportPoint.X < 10)
                predictiveTeleportPoint.X = 10;
            if (predictiveTeleportPoint.X > Main.maxTilesX - 10)
                predictiveTeleportPoint.X = Main.maxTilesX - 10;
            if (predictiveTeleportPoint.Y < 10)
                predictiveTeleportPoint.Y = 10;
            if (predictiveTeleportPoint.Y > Main.maxTilesY - 10)
                predictiveTeleportPoint.Y = Main.maxTilesY - 10;

            int randomPredictiveTeleportOffset = 5;
            int teleportTries = 0;
            while (teleportTries < 100)
            {
                teleportTries++;
                int teleportTileX = Main.rand.Next(predictiveTeleportPoint.X - randomPredictiveTeleportOffset, predictiveTeleportPoint.X + randomPredictiveTeleportOffset + 1);
                int teleportTileY = Main.rand.Next(predictiveTeleportPoint.Y - randomPredictiveTeleportOffset, predictiveTeleportPoint.Y);

                if (!Main.tile[teleportTileX, teleportTileY].HasUnactuatedTile)
                {
                    bool canTeleportToTile = true;
                    if (canTeleportToTile && Main.tile[teleportTileX, teleportTileY].LiquidType == LiquidID.Lava)
                        canTeleportToTile = false;
                    if (canTeleportToTile && !Collision.CanHitLine(npc.Center, 0, 0, predictiveTeleportPoint.ToVector2() * 16, 0, 0))
                        canTeleportToTile = false;

                    if (canTeleportToTile)
                    {
                        npc.localAI[1] = teleportTileX * 16 + 8;
                        npc.localAI[2] = teleportTileY * 16 + 16;
                        break;
                    }
                    else
                    {
                        predictiveTeleportPoint.X += predictiveTeleportPoint.X < 0f ? 1 : -1;
                        if (predictiveTeleportPoint.X < 10)
                            predictiveTeleportPoint.X = 10;
                        if (predictiveTeleportPoint.X > Main.maxTilesX - 10)
                            predictiveTeleportPoint.X = Main.maxTilesX - 10;
                    }
                }
                else
                {
                    predictiveTeleportPoint.X += predictiveTeleportPoint.X < 0f ? 1 : -1;
                    if (predictiveTeleportPoint.X < 10)
                        predictiveTeleportPoint.X = 10;
                    if (predictiveTeleportPoint.X > Main.maxTilesX - 10)
                        predictiveTeleportPoint.X = Main.maxTilesX - 10;
                }
            }

            // Default teleport if the above conditions aren't met in 100 iterations
            if (teleportTries >= 100)
            {
                Vector2 bottom = Main.player[Player.FindClosest(npc.position, npc.width, npc.height)].Bottom;
                npc.localAI[1] = bottom.X;
                npc.localAI[2] = bottom.Y;
            }
        }
    }
}
