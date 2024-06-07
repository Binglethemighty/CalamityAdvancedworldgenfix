using System;
using CalamityMod.Events;
using CalamityMod.NPCs.NormalNPCs;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.NPCs.VanillaNPCAIOverrides.Bosses
{
    public static class EyeOfCthulhuAI
    {
        private const float ProjectileOffset = 50f;

        public static bool BuffedEyeofCthulhuAI(NPC npc, Mod mod)
        {
            CalamityGlobalNPC calamityGlobalNPC = npc.Calamity();

            // Percent life remaining
            float lifeRatio = npc.life / (float)npc.lifeMax;

            bool bossRush = BossRushEvent.BossRushActive;
            bool masterMode = Main.masterMode || bossRush;
            bool death = CalamityWorld.death || bossRush;

            // Phases
            float phase2LifeRatio = masterMode ? 0.75f : 0.6f;
            float phase3LifeRatio = masterMode ? 0.4f : 0.3f;
            float finalPhaseRevLifeRatio = masterMode ? 0.2f : 0.15f;
            float penultimatePhaseDeathLifeRatio = masterMode ? 0.3f : 0.2f;
            float finalPhaseDeathLifeRatio = masterMode ? 0.15f : 0.1f;
            bool phase2 = lifeRatio < phase2LifeRatio;
            bool phase3 = lifeRatio < phase3LifeRatio;
            bool finalPhaseRev = lifeRatio < finalPhaseRevLifeRatio;
            bool penultimatePhaseDeath = lifeRatio < penultimatePhaseDeathLifeRatio;
            bool finalPhaseDeath = lifeRatio < finalPhaseDeathLifeRatio;

            float lineUpDist = death ? 15f : 20f;

            // Servant and projectile velocity, the projectile velocity is multiplied by 2
            float servantAndProjectileVelocity = (death ? 8f : 6f) + (masterMode ? 2f : 0f);

            float enrageScale = bossRush ? 1f : masterMode ? 0.5f : 0f;
            if (Main.IsItDay() || bossRush)
            {
                npc.Calamity().CurrentlyEnraged = !bossRush;
                enrageScale += 2f;
            }

            npc.reflectsProjectiles = false;

            // Get a target
            if (npc.target < 0 || npc.target == Main.maxPlayers || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            bool dead = Main.player[npc.target].dead;
            float targetXDistance = npc.Center.X - Main.player[npc.target].position.X - (Main.player[npc.target].width / 2);
            float targetYDistance = npc.position.Y + npc.height - 59f - Main.player[npc.target].position.Y - (Main.player[npc.target].height / 2);
            float eyeRotation = (float)Math.Atan2(targetYDistance, targetXDistance) + MathHelper.PiOver2;

            if (eyeRotation < 0f)
                eyeRotation += MathHelper.TwoPi;
            else if (eyeRotation > MathHelper.TwoPi)
                eyeRotation -= MathHelper.TwoPi;

            float eyeRotationAcceleration = 0f;
            if (npc.ai[0] == 0f && npc.ai[1] == 0f)
                eyeRotationAcceleration = 0.04f;
            if (npc.ai[0] == 0f && npc.ai[1] == 2f && npc.ai[2] > 40f)
                eyeRotationAcceleration = 0.1f;
            if (npc.ai[0] == 3f && npc.ai[1] == 0f)
                eyeRotationAcceleration = 0.1f;
            if (npc.ai[0] == 3f && npc.ai[1] == 2f && npc.ai[2] > 40f)
                eyeRotationAcceleration = 0.16f;
            if (npc.ai[0] == 3f && npc.ai[1] == 4f && npc.ai[2] > lineUpDist)
                eyeRotationAcceleration = 0.3f;
            if (npc.ai[0] == 3f && npc.ai[1] == 5f)
                eyeRotationAcceleration = 0.1f;

            if (npc.rotation < eyeRotation)
            {
                if ((eyeRotation - npc.rotation) > MathHelper.Pi)
                    npc.rotation -= eyeRotationAcceleration;
                else
                    npc.rotation += eyeRotationAcceleration;
            }
            else if (npc.rotation > eyeRotation)
            {
                if ((npc.rotation - eyeRotation) > MathHelper.Pi)
                    npc.rotation += eyeRotationAcceleration;
                else
                    npc.rotation -= eyeRotationAcceleration;
            }

            if (npc.rotation > eyeRotation - eyeRotationAcceleration && npc.rotation < eyeRotation + eyeRotationAcceleration)
                npc.rotation = eyeRotation;
            if (npc.rotation < 0f)
                npc.rotation += MathHelper.TwoPi;
            else if (npc.rotation > MathHelper.TwoPi)
                npc.rotation -= MathHelper.TwoPi;
            if (npc.rotation > eyeRotation - eyeRotationAcceleration && npc.rotation < eyeRotation + eyeRotationAcceleration)
                npc.rotation = eyeRotation;

            if (Main.rand.NextBool(5))
            {
                int randomBlood = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y + npc.height * 0.25f), npc.width, (int)(npc.height * 0.5f), DustID.Blood, npc.velocity.X, 2f, 0, default, 1f);
                Dust dust = Main.dust[randomBlood];
                dust.velocity.X *= 0.5f;
                dust.velocity.Y *= 0.1f;
            }

            bool shootProjectile = Collision.CanHitLine(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1) &&
                npc.SafeDirectionTo(Main.player[npc.target].Center).AngleBetween((npc.rotation + MathHelper.PiOver2).ToRotationVector2()) < MathHelper.ToRadians(18f) &&
                Vector2.Distance(npc.Center, Main.player[npc.target].Center) > 240f;

            bool charge = Vector2.Distance(Main.player[npc.target].Center, npc.Center) >= 320f; // 20 tile distance

            if (dead)
            {
                npc.velocity.Y -= 0.04f;

                if (npc.timeLeft > 10)
                    npc.timeLeft = 10;
            }

            else if (npc.ai[0] == 0f)
            {
                if (npc.ai[1] == 0f)
                {
                    // Avoid cheap bullshit
                    npc.damage = 0;

                    float hoverSpeed = 7f;
                    float hoverAcceleration = 0.15f;
                    hoverSpeed += 5f * enrageScale;
                    hoverAcceleration += 0.1f * enrageScale;

                    if (death)
                    {
                        hoverSpeed += 7f * (1f - lifeRatio);
                        hoverAcceleration += 0.15f * (1f - lifeRatio);
                    }

                    if (Main.getGoodWorld)
                    {
                        hoverSpeed += 3f;
                        hoverAcceleration += 0.08f;
                    }

                    float attackSwitchTimer = (masterMode ? 120f : 180f) - (death ? 180f * (1f - lifeRatio) : 0f);
                    bool timeToCharge = npc.ai[2] >= attackSwitchTimer;
                    Vector2 hoverDestination = Main.player[npc.target].Center - Vector2.UnitY * 400f;
                    Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * (hoverSpeed + (timeToCharge ? ((npc.ai[2] - attackSwitchTimer) * 0.01f) : 0f));
                    npc.SimpleFlyMovement(idealVelocity, hoverAcceleration + (timeToCharge ? ((npc.ai[2] - attackSwitchTimer) * 0.001f) : 0f));

                    npc.ai[2] += 1f;
                    if (timeToCharge && charge)
                    {
                        npc.ai[1] = 1f;
                        npc.ai[2] = 0f;
                        npc.ai[3] = 0f;
                        npc.TargetClosest();
                        npc.netUpdate = true;
                    }
                    else if (npc.WithinRange(hoverDestination, 900f))
                    {
                        if (!Main.player[npc.target].dead)
                            npc.ai[3] += 1f;

                        float servantSpawnGateValue = death ? (masterMode ? 10f : 20f) : (masterMode ? 25f : 40f);
                        if (Main.getGoodWorld)
                            servantSpawnGateValue *= 0.8f;

                        if (npc.ai[3] >= servantSpawnGateValue && shootProjectile)
                        {
                            npc.ai[3] = 0f;
                            npc.rotation = eyeRotation;

                            Vector2 servantSpawnVelocity = npc.SafeDirectionTo(Main.player[npc.target].Center) * servantAndProjectileVelocity;
                            Vector2 servantSpawnCenter = npc.Center + servantSpawnVelocity.SafeNormalize(Vector2.UnitY) * ProjectileOffset;
                            int maxServants = 4;
                            bool spawnServant = NPC.CountNPCS(NPCID.ServantofCthulhu) < maxServants;
                            if (spawnServant)
                                SoundEngine.PlaySound(SoundID.NPCHit1, servantSpawnCenter);

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                if (spawnServant)
                                {
                                    int eye = NPC.NewNPC(npc.GetSource_FromAI(), (int)servantSpawnCenter.X, (int)servantSpawnCenter.Y, NPCID.ServantofCthulhu, 0, 0f, 0f, enrageScale);
                                    Main.npc[eye].velocity = servantSpawnVelocity;

                                    if (Main.netMode == NetmodeID.Server && eye < Main.maxNPCs)
                                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, eye);
                                }
                                else
                                {
                                    int projType = ProjectileID.BloodNautilusShot;
                                    int projDamage = npc.GetProjectileDamage(projType);
                                    int proj = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + servantSpawnVelocity.SafeNormalize(Vector2.UnitY) * ProjectileOffset, servantSpawnVelocity * 2f, projType, projDamage, 0f, Main.myPlayer);
                                    Main.projectile[proj].timeLeft = 600;
                                }
                            }

                            if (spawnServant)
                            {
                                for (int m = 0; m < 10; m++)
                                    Dust.NewDust(servantSpawnCenter, 20, 20, DustID.Blood, servantSpawnVelocity.X * 0.4f, servantSpawnVelocity.Y * 0.4f, 0, default, 1f);
                            }
                        }
                    }
                }
                else if (npc.ai[1] == 1f)
                {
                    // Set damage
                    npc.damage = npc.defDamage;

                    npc.rotation = eyeRotation;
                    float additionalVelocityPerCharge = 2f;
                    float chargeSpeed = 8f + npc.ai[3] * additionalVelocityPerCharge;
                    chargeSpeed += 5f * enrageScale;
                    if (death)
                        chargeSpeed += 10f * (1f - lifeRatio);
                    if (Main.getGoodWorld)
                        chargeSpeed += 4f;

                    npc.velocity = npc.SafeDirectionTo(Main.player[npc.target].Center) * chargeSpeed;

                    npc.ai[1] = 2f;
                    npc.netUpdate = true;

                    if (npc.netSpam > 10)
                        npc.netSpam = 10;
                }
                else if (npc.ai[1] == 2f)
                {
                    // Set damage
                    npc.damage = npc.defDamage;

                    int chargeDelay = masterMode ? 70 : 90;
                    if (death)
                        chargeDelay -= (int)Math.Round(40f * (1f - lifeRatio));
                    if (Main.getGoodWorld)
                        chargeDelay -= 30;

                    float slowDownGateValue = chargeDelay * (death ? (masterMode ? 0.9f : 0.75f) : (masterMode ? 0.8f : 0.65f));

                    npc.ai[2] += 1f;
                    if (npc.ai[2] >= slowDownGateValue)
                    {
                        // Avoid cheap bullshit
                        npc.damage = 0;

                        float decelerationScalar = death ? ((lifeRatio - phase2LifeRatio) / (1f - phase2LifeRatio)) : 1f;
                        if (decelerationScalar < 0f)
                            decelerationScalar = 0f;

                        npc.velocity *= (MathHelper.Lerp(masterMode ? 0.76f : 0.92f, masterMode ? 0.88f : 0.96f, decelerationScalar));
                        if (Main.getGoodWorld)
                            npc.velocity *= 0.99f;

                        if (npc.velocity.X > -0.1 && npc.velocity.X < 0.1)
                            npc.velocity.X = 0f;
                        if (npc.velocity.Y > -0.1 && npc.velocity.Y < 0.1)
                            npc.velocity.Y = 0f;
                    }
                    else
                        npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;

                    if (npc.ai[2] >= chargeDelay)
                    {
                        npc.ai[3] += 1f;
                        npc.ai[2] = 0f;
                        npc.rotation = eyeRotation;

                        float numCharges = death ? 4f : 3f;
                        if (npc.ai[3] >= numCharges)
                        {
                            // Avoid cheap bullshit
                            npc.damage = 0;

                            npc.ai[1] = 0f;
                            npc.ai[3] = 0f;
                        }
                        else
                            npc.ai[1] = 1f;
                    }
                }

                if (phase2)
                {
                    // Avoid cheap bullshit
                    npc.damage = 0;

                    npc.ai[0] = 1f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.ai[3] = 0f;
                    npc.TargetClosest();
                    npc.netUpdate = true;

                    if (npc.netSpam > 10)
                        npc.netSpam = 10;
                }
            }

            else if (npc.ai[0] == 1f || npc.ai[0] == 2f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                if (Main.getGoodWorld)
                    npc.reflectsProjectiles = true;

                if (npc.ai[0] == 1f)
                {
                    npc.ai[2] += 0.005f;
                    if (npc.ai[2] > 0.5f)
                        npc.ai[2] = 0.5f;
                }
                else
                {
                    npc.ai[2] -= 0.005f;
                    if (npc.ai[2] < 0f)
                        npc.ai[2] = 0f;
                }

                npc.rotation += npc.ai[2];

                float phaseChangeRate = death ? 2f : 1f;
                float servantSpawnGateValue = Main.getGoodWorld ? 4f : 20f;
                npc.ai[1] += phaseChangeRate;
                if (npc.ai[1] % servantSpawnGateValue == 0f)
                {
                    float servantVelocity = masterMode ? 11.3f : 5.65f;
                    Vector2 servantSpawnVelocity = Main.rand.NextVector2CircularEdge(servantVelocity, servantVelocity);
                    if (Main.getGoodWorld)
                        servantSpawnVelocity *= 3f;

                    Vector2 servantSpawnCenter = npc.Center + servantSpawnVelocity.SafeNormalize(Vector2.UnitY) * ProjectileOffset;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int spawnType = NPCID.ServantofCthulhu;
                        if (masterMode)
                        {
                            int maxBloodServants = 2;
                            bool spawnBloodServant = NPC.CountNPCS(ModContent.NPCType<BloodlettingServant>()) < maxBloodServants;
                            if (spawnBloodServant)
                                spawnType = ModContent.NPCType<BloodlettingServant>();
                        }

                        int servantSpawn = NPC.NewNPC(npc.GetSource_FromAI(), (int)servantSpawnCenter.X, (int)servantSpawnCenter.Y, spawnType, 0, 0f, 0f, enrageScale);
                        Main.npc[servantSpawn].velocity.X = servantSpawnVelocity.X;
                        Main.npc[servantSpawn].velocity.Y = servantSpawnVelocity.Y;

                        if (Main.netMode == NetmodeID.Server && servantSpawn < Main.maxNPCs)
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, servantSpawn);

                        if (CalamityWorld.LegendaryMode)
                        {
                            int type = ProjectileID.BloodNautilusShot;
                            Vector2 projectileVelocity = Main.rand.NextVector2CircularEdge(15f, 15f);
                            int numProj = 3;
                            int spread = 20;
                            float rotation = MathHelper.ToRadians(spread);
                            for (int i = 0; i < numProj; i++)
                            {
                                Vector2 perturbedSpeed = projectileVelocity.RotatedBy(MathHelper.Lerp(-rotation, rotation, i / (float)(numProj - 1)));
                                int proj = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + perturbedSpeed.SafeNormalize(Vector2.UnitY) * ProjectileOffset, perturbedSpeed, type, 15, 0f, Main.myPlayer);
                                Main.projectile[proj].timeLeft = 600;
                            }
                        }
                    }

                    for (int n = 0; n < 10; n++)
                        Dust.NewDust(servantSpawnCenter, 20, 20, DustID.Blood, servantSpawnVelocity.X * 0.4f, servantSpawnVelocity.Y * 0.4f, 0, default, 1f);
                }

                if (npc.ai[1] == 100f)
                {
                    npc.ai[0] += 1f;
                    npc.ai[1] = 0f;

                    if (npc.ai[0] == 3f)
                    {
                        npc.ai[2] = 0f;
                    }
                    else
                    {
                        SoundEngine.PlaySound(SoundID.NPCHit1, npc.Center);

                        if (Main.netMode != NetmodeID.Server)
                        {
                            for (int phase2Gore = 0; phase2Gore < 2; phase2Gore++)
                            {
                                Gore.NewGore(npc.GetSource_FromAI(), npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), 8, 1f);
                                Gore.NewGore(npc.GetSource_FromAI(), npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), 7, 1f);
                                Gore.NewGore(npc.GetSource_FromAI(), npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), 6, 1f);
                            }
                        }

                        for (int i = 0; i < 20; i++)
                            Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f, 0, default, 1f);

                        SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                    }
                }

                Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f, 0, default, 1f);
                npc.velocity *= 0.98f;

                if (npc.velocity.X > -0.1 && npc.velocity.X < 0.1)
                    npc.velocity.X = 0f;
                if (npc.velocity.Y > -0.1 && npc.velocity.Y < 0.1)
                    npc.velocity.Y = 0f;
            }

            else
            {
                npc.defense = 0;
                int setDamage = (int)Math.Round(npc.defDamage * (phase3 ? 1.4 : 1.2));
                int reducedSetDamage = (int)Math.Round(setDamage * 0.5);

                if (npc.ai[1] == 0f & phase3)
                    npc.ai[1] = 5f;

                if (npc.ai[1] == 0f)
                {
                    // Deal less damage overall while not charging
                    npc.damage = reducedSetDamage;

                    float hoverSpeed = 5.5f + 3f * (phase2LifeRatio - lifeRatio);
                    float hoverAcceleration = 0.06f + 0.02f * (phase2LifeRatio - lifeRatio);
                    hoverSpeed += 4f * enrageScale;
                    hoverAcceleration += 0.04f * enrageScale;

                    if (death)
                    {
                        hoverSpeed += 5.5f * (phase2LifeRatio - lifeRatio);
                        hoverAcceleration += 0.06f * (phase2LifeRatio - lifeRatio);
                    }

                    Vector2 hoverDestination = Main.player[npc.target].Center - Vector2.UnitY * 400f;
                    float distanceFromHoverDestination = npc.Distance(hoverDestination);

                    if (distanceFromHoverDestination > 400f)
                    {
                        hoverSpeed += 1.25f;
                        hoverAcceleration += 0.075f;
                        if (distanceFromHoverDestination > 600f)
                        {
                            hoverSpeed += 1.25f;
                            hoverAcceleration += 0.075f;
                            if (distanceFromHoverDestination > 800f)
                            {
                                hoverSpeed += 1.25f;
                                hoverAcceleration += 0.075f;
                            }
                        }
                    }

                    if (Main.getGoodWorld)
                    {
                        hoverSpeed += 1f;
                        hoverAcceleration += 0.1f;
                    }

                    float phaseLimit = (masterMode ? 160f : 200f) - (death ? 150f * (phase2LifeRatio - lifeRatio) : 0f);
                    bool timeToCharge = npc.ai[2] >= phaseLimit;
                    Vector2 idealHoverVelocity = npc.SafeDirectionTo(hoverDestination) * (hoverSpeed + (timeToCharge ? ((npc.ai[2] - phaseLimit) * 0.01f) : 0f));
                    npc.SimpleFlyMovement(idealHoverVelocity, hoverAcceleration + (timeToCharge ? ((npc.ai[2] - phaseLimit) * 0.001f) : 0f));

                    npc.ai[2] += 1f;
                    float projectileGateValue = (lifeRatio < 0.5f && death) ? (masterMode ? 40f : 50f) : (masterMode ? 60f : 80f);
                    if (npc.ai[2] % projectileGateValue == 0f && shootProjectile)
                    {
                        Vector2 projectileVelocity = (Main.player[npc.target].Center - npc.Center).SafeNormalize(Vector2.UnitY) * servantAndProjectileVelocity * 2f;
                        Vector2 projectileSpawnCenter = npc.Center + projectileVelocity;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int type = ProjectileID.BloodNautilusShot;
                            int damage = npc.GetProjectileDamage(type);
                            int numProj = masterMode ? Main.rand.Next(4, 6) : 3;
                            int spread = masterMode ? numProj * 3 : 10;
                            float rotation = MathHelper.ToRadians(spread);
                            for (int i = 0; i < numProj; i++)
                            {
                                Vector2 perturbedSpeed = projectileVelocity.RotatedBy(MathHelper.Lerp(-rotation, rotation, i / (float)(numProj - 1)));
                                int proj = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + perturbedSpeed.SafeNormalize(Vector2.UnitY) * ProjectileOffset, perturbedSpeed, type, damage, 0f, Main.myPlayer);
                                Main.projectile[proj].timeLeft = 600;
                            }
                        }
                    }

                    if (timeToCharge && charge)
                    {
                        npc.ai[1] = 1f;
                        npc.ai[2] = 0f;
                        npc.ai[3] = 0f;
                        npc.netUpdate = true;
                    }
                }

                else if (npc.ai[1] == 1f)
                {
                    // Set damage
                    npc.damage = setDamage;

                    SoundEngine.PlaySound(SoundID.ForceRoar, npc.Center);
                    npc.rotation = eyeRotation;

                    float additionalVelocityPerCharge = 3f;
                    float chargeSpeed = 10f + (3.5f * (phase2LifeRatio - lifeRatio)) + npc.ai[3] * additionalVelocityPerCharge;
                    chargeSpeed += 4f * enrageScale;
                    if (death)
                        chargeSpeed += 6.5f * (phase2LifeRatio - lifeRatio);
                    if (npc.ai[3] == 1f)
                        chargeSpeed *= 1.15f;
                    if (npc.ai[3] == 2f)
                        chargeSpeed *= 1.3f;
                    if (Main.getGoodWorld)
                        chargeSpeed *= 1.2f;

                    npc.velocity = npc.SafeDirectionTo(Main.player[npc.target].Center) * chargeSpeed;
                    npc.ai[1] = 2f;
                    npc.netUpdate = true;

                    if (npc.netSpam > 10)
                        npc.netSpam = 10;
                }

                else if (npc.ai[1] == 2f)
                {
                    // Set damage
                    npc.damage = setDamage;

                    int phase2ChargeDelay = masterMode ? 60 : 80;
                    if (death)
                        phase2ChargeDelay -= (int)Math.Round(35f * (phase2LifeRatio - lifeRatio));

                    float slowDownGateValue = phase2ChargeDelay * (death ? (masterMode ? 0.95f : 0.85f) : (masterMode ? 0.9f : 0.75f));

                    npc.ai[2] += 1f;
                    if (npc.ai[2] >= slowDownGateValue)
                    {
                        // Deal less damage overall while not charging
                        npc.damage = reducedSetDamage;

                        float decelerationScalar = death ? ((lifeRatio - phase3LifeRatio) / (phase2LifeRatio - phase3LifeRatio)) : 1f;
                        if (decelerationScalar < 0f)
                            decelerationScalar = 0f;

                        npc.velocity *= (MathHelper.Lerp(masterMode ? 0.6f : 0.9f, masterMode ? 0.7f : 0.95f, decelerationScalar));
                        if (npc.velocity.X > -0.1 && npc.velocity.X < 0.1)
                            npc.velocity.X = 0f;
                        if (npc.velocity.Y > -0.1 && npc.velocity.Y < 0.1)
                            npc.velocity.Y = 0f;
                    }
                    else
                        npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;

                    if (npc.ai[2] >= phase2ChargeDelay)
                    {
                        npc.ai[3] += 1f;
                        npc.ai[2] = 0f;
                        npc.rotation = eyeRotation;

                        float numCharges = death ? 4f : 3f;
                        if (npc.ai[3] >= numCharges)
                        {
                            // Deal less damage overall while not charging
                            npc.damage = reducedSetDamage;

                            npc.ai[1] = 0f;
                            npc.ai[3] = 0f;
                            npc.netUpdate = true;

                            if (npc.netSpam > 10)
                                npc.netSpam = 10;
                        }
                        else
                            npc.ai[1] = 1f;
                    }
                }

                else if (npc.ai[1] == 3f)
                {
                    if ((npc.ai[3] == 4f & phase3) && npc.Center.Y > Main.player[npc.target].Center.Y)
                    {
                        // Deal less damage overall while not charging
                        npc.damage = reducedSetDamage;

                        npc.ai[1] = 0f;
                        npc.ai[2] = 0f;
                        npc.ai[3] = 0f;
                        npc.netUpdate = true;

                        if (npc.netSpam > 10)
                            npc.netSpam = 10;
                    }
                    else if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // Set damage
                        npc.damage = setDamage;

                        float speedBoost = death ? 10f * (phase3LifeRatio - lifeRatio) : 7f * (phase3LifeRatio - lifeRatio);
                        float finalChargeSpeed = 18f + speedBoost;
                        finalChargeSpeed += 10f * enrageScale;

                        Vector2 eyeChargeDirection = npc.Center;
                        float targetX = Main.player[npc.target].Center.X - eyeChargeDirection.X;
                        float targetY = Main.player[npc.target].Center.Y - eyeChargeDirection.Y;
                        float targetVelocity = Math.Abs(Main.player[npc.target].velocity.X) + Math.Abs(Main.player[npc.target].velocity.Y) / 4f;
                        targetVelocity += 10f - targetVelocity;

                        if (targetVelocity < (masterMode ? 2f : 5f))
                            targetVelocity = (masterMode ? 2f : 5f);
                        if (targetVelocity > (masterMode ? 6f : 15f))
                            targetVelocity = (masterMode ? 6f : 15f);

                        if (npc.ai[2] == -1f)
                        {
                            targetVelocity *= 4f;
                            finalChargeSpeed *= 1.3f;
                        }

                        targetX -= Main.player[npc.target].velocity.X * targetVelocity;
                        targetY -= Main.player[npc.target].velocity.Y * targetVelocity / 4f;

                        float targetDistance = (float)Math.Sqrt(targetX * targetX + targetY * targetY);
                        float targetDistCopy = targetDistance;

                        targetDistance = finalChargeSpeed / targetDistance;
                        npc.velocity.X = targetX * targetDistance;
                        npc.velocity.Y = targetY * targetDistance;

                        if (targetDistCopy < 100f)
                        {
                            if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y))
                            {
                                float absoluteXVel = Math.Abs(npc.velocity.X);
                                float absoluteYVel = Math.Abs(npc.velocity.Y);

                                if (npc.Center.X > Main.player[npc.target].Center.X)
                                    absoluteYVel *= -1f;
                                if (npc.Center.Y > Main.player[npc.target].Center.Y)
                                    absoluteXVel *= -1f;

                                npc.velocity.X = absoluteYVel;
                                npc.velocity.Y = absoluteXVel;
                            }
                        }
                        else if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y))
                        {
                            float absoluteEyeVel = (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) / 2f;
                            float absoluteEyeVelBackup = absoluteEyeVel;

                            if (npc.Center.X > Main.player[npc.target].Center.X)
                                absoluteEyeVelBackup *= -1f;
                            if (npc.Center.Y > Main.player[npc.target].Center.Y)
                                absoluteEyeVel *= -1f;

                            npc.velocity.X = absoluteEyeVelBackup;
                            npc.velocity.Y = absoluteEyeVel;
                        }

                        npc.ai[1] = 4f;
                        npc.netUpdate = true;

                        if (npc.netSpam > 10)
                            npc.netSpam = 10;
                    }
                }

                else if (npc.ai[1] == 4f)
                {
                    // Set damage
                    npc.damage = setDamage;

                    if (npc.ai[2] == 0f)
                        SoundEngine.PlaySound(SoundID.ForceRoarPitched, npc.Center);

                    float lineUpDistControl = lineUpDist;
                    npc.ai[2] += 1f;

                    if (npc.ai[2] == lineUpDistControl && Vector2.Distance(npc.position, Main.player[npc.target].position) < 200f)
                        npc.ai[2] -= 1f;

                    if (npc.ai[2] >= lineUpDistControl)
                    {
                        // Deal less damage overall while not charging
                        npc.damage = reducedSetDamage;

                        npc.velocity *= 0.95f;
                        if (npc.velocity.X > -0.1 && npc.velocity.X < 0.1)
                            npc.velocity.X = 0f;
                        if (npc.velocity.Y > -0.1 && npc.velocity.Y < 0.1)
                            npc.velocity.Y = 0f;
                    }
                    else
                        npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;

                    float lineUpDistNetUpdate = lineUpDistControl + 13f;
                    if (npc.ai[2] >= lineUpDistNetUpdate)
                    {
                        npc.netUpdate = true;

                        if (npc.netSpam > 10)
                            npc.netSpam = 10;

                        npc.ai[3] += 1f;
                        npc.ai[2] = 0f;

                        float maxCharges = death ? (finalPhaseDeath ? 0f : penultimatePhaseDeath ? 1f : 2f) : finalPhaseRev ? 2f : 3f;
                        if (npc.ai[3] >= maxCharges)
                        {
                            // Deal less damage overall while not charging
                            npc.damage = reducedSetDamage;

                            npc.ai[1] = 0f;
                            npc.ai[3] = 0f;
                        }
                        else
                            npc.ai[1] = 3f;
                    }
                }

                else if (npc.ai[1] == 5f)
                {
                    // Deal less damage overall while not charging
                    npc.damage = reducedSetDamage;

                    float offset = death ? 540f : 600f;
                    float speedBoost = death ? 15f * (phase3LifeRatio - lifeRatio) : 5f * (phase3LifeRatio - lifeRatio);
                    float accelerationBoost = death ? 0.425f * (phase3LifeRatio - lifeRatio) : 0.125f * (phase3LifeRatio - lifeRatio);
                    float hoverSpeed = 8f + speedBoost;
                    float hoverAcceleration = 0.25f + accelerationBoost;

                    bool horizontalCharge = calamityGlobalNPC.newAI[0] == 1f || calamityGlobalNPC.newAI[0] == 3f;
                    float timeGateValue = horizontalCharge ? (100f - (death ? 80f * (phase3LifeRatio - lifeRatio) : 0f)) : (85f - (death ? 70f * (phase3LifeRatio - lifeRatio) : 0f));
                    if (npc.ai[2] > timeGateValue)
                    {
                        float velocityScalar = npc.ai[2] - timeGateValue;
                        hoverSpeed += velocityScalar * 0.05f;
                        hoverAcceleration += velocityScalar * 0.0025f;
                    }

                    hoverSpeed += enrageScale * 4f;
                    hoverAcceleration += enrageScale * 0.125f;

                    Vector2 eyeLineUpChargeDirection = npc.Center;
                    float lineUpChargeTargetX = Main.player[npc.target].Center.X - eyeLineUpChargeDirection.X;
                    float lineUpChargeTargetY = Main.player[npc.target].Center.Y + offset - eyeLineUpChargeDirection.Y;
                    Vector2 hoverDestination = Main.player[npc.target].Center + Vector2.UnitY * offset;

                    if (horizontalCharge)
                    {
                        float horizontalChargeOffset = death ? 450f : 500f;
                        offset = calamityGlobalNPC.newAI[0] == 1f ? -horizontalChargeOffset : horizontalChargeOffset;
                        hoverSpeed *= 1.5f;
                        hoverAcceleration *= 1.5f;
                        hoverDestination = Main.player[npc.target].Center + Vector2.UnitX * offset;
                    }

                    Vector2 idealHoverVelocity = npc.SafeDirectionTo(hoverDestination) * hoverSpeed;
                    npc.SimpleFlyMovement(idealHoverVelocity, hoverAcceleration);

                    float servantSpawnGateValue = horizontalCharge ? (death ? 23f : 35f) : (death ? 17f : 27f);
                    float maxServantSpawnsPerAttack = 2f;

                    npc.ai[2] += 1f;
                    if (npc.ai[2] % servantSpawnGateValue == 0f && shootProjectile && npc.ai[2] <= servantSpawnGateValue * maxServantSpawnsPerAttack)
                    {
                        Vector2 servantSpawnVelocity = (Main.player[npc.target].Center - npc.Center).SafeNormalize(Vector2.UnitY) * servantAndProjectileVelocity;
                        Vector2 servantSpawnCenter = npc.Center + servantSpawnVelocity.SafeNormalize(Vector2.UnitY) * ProjectileOffset;

                        int spawnType = NPCID.ServantofCthulhu;
                        bool spawnServant = false;
                        float enrageScaleToPass = enrageScale;
                        if (masterMode)
                        {
                            int maxBloodServants = 2;
                            bool spawnBloodServant = NPC.CountNPCS(ModContent.NPCType<BloodlettingServant>()) < maxBloodServants;
                            if (spawnBloodServant)
                            {
                                spawnType = ModContent.NPCType<BloodlettingServant>();
                                spawnServant = true;
                                enrageScaleToPass += 0.5f;
                            }
                            else
                            {
                                int maxServants = death ? 1 : 2;
                                spawnServant = (penultimatePhaseDeath || finalPhaseRev) ? false : NPC.CountNPCS(NPCID.ServantofCthulhu) < maxServants;
                            }
                        }
                        else
                        {
                            int maxServants = death ? (finalPhaseDeath ? 1 : penultimatePhaseDeath ? 2 : 3) : (finalPhaseRev ? 2 : 4);
                            spawnServant = NPC.CountNPCS(NPCID.ServantofCthulhu) < maxServants;
                        }

                        if (spawnServant)
                            SoundEngine.PlaySound(SoundID.NPCDeath13, servantSpawnCenter);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (spawnServant)
                            {
                                int eye = NPC.NewNPC(npc.GetSource_FromAI(), (int)servantSpawnCenter.X, (int)servantSpawnCenter.Y, spawnType, 0, 0f, 0f, enrageScaleToPass);
                                Main.npc[eye].velocity.X = servantSpawnVelocity.X;
                                Main.npc[eye].velocity.Y = servantSpawnVelocity.Y;

                                if (Main.netMode == NetmodeID.Server && eye < Main.maxNPCs)
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, eye);
                            }
                            else if (!CalamityWorld.LegendaryMode)
                            {
                                int projType = ProjectileID.BloodNautilusShot;
                                int projDamage = npc.GetProjectileDamage(projType);
                                int proj = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + servantSpawnVelocity.SafeNormalize(Vector2.UnitY) * ProjectileOffset, servantSpawnVelocity * 2f, projType, projDamage, 0f, Main.myPlayer);
                                Main.projectile[proj].timeLeft = 600;
                            }

                            if (CalamityWorld.LegendaryMode)
                            {
                                int type = ProjectileID.BloodNautilusShot;
                                Vector2 projectileVelocity = servantSpawnVelocity * 3f;
                                int numProj = 3;
                                int spread = 20;
                                float rotation = MathHelper.ToRadians(spread);
                                for (int i = 0; i < numProj; i++)
                                {
                                    Vector2 perturbedSpeed = projectileVelocity.RotatedBy(MathHelper.Lerp(-rotation, rotation, i / (float)(numProj - 1)));
                                    int proj = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + perturbedSpeed.SafeNormalize(Vector2.UnitY) * ProjectileOffset, perturbedSpeed, type, 15, 0f, Main.myPlayer);
                                    Main.projectile[proj].timeLeft = 600;
                                }
                            }
                        }

                        if (spawnServant)
                        {
                            for (int m = 0; m < 10; m++)
                                Dust.NewDust(servantSpawnCenter, 20, 20, DustID.Blood, servantSpawnVelocity.X * 0.4f, servantSpawnVelocity.Y * 0.4f, 0, default, 1f);
                        }
                    }

                    float requiredDistanceForHorizontalCharge = 160f;
                    if (npc.ai[2] >= timeGateValue && (npc.Distance(hoverDestination) < requiredDistanceForHorizontalCharge || !horizontalCharge))
                    {
                        switch ((int)calamityGlobalNPC.newAI[0])
                        {
                            case 0: // Normal Eye behavior
                                npc.ai[1] = 3f;
                                npc.ai[2] = -1f;
                                npc.ai[3] = -1f;
                                break;

                            case 1: // Charge from the left
                                npc.ai[1] = 6f;
                                npc.ai[2] = 0f;
                                break;

                            case 2: // Normal Eye behavior
                                npc.ai[1] = 3f;
                                npc.ai[2] = -1f;
                                break;

                            case 3: // Charge from the right
                                npc.ai[1] = 6f;
                                npc.ai[2] = 0f;
                                break;

                            default:
                                break;
                        }

                        calamityGlobalNPC.newAI[0] += ((masterMode && calamityGlobalNPC.newAI[0] % 2f != 0f) ? Main.rand.Next(2) + 1f : 1f);
                        if (calamityGlobalNPC.newAI[0] > 3f)
                            calamityGlobalNPC.newAI[0] = masterMode ? Main.rand.Next(2) : 0f;

                        npc.SyncExtraAI();
                    }

                    npc.netUpdate = true;

                    if (npc.netSpam > 10)
                        npc.netSpam = 10;
                }

                else if (npc.ai[1] == 6f)
                {
                    // Set damage
                    npc.damage = setDamage;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float speedBoost = death ? 15f * (phase3LifeRatio - lifeRatio) : 5f * (phase3LifeRatio - lifeRatio);
                        float chargeSpeed = 18f + speedBoost;
                        chargeSpeed += 10f * enrageScale;
                        npc.velocity = npc.SafeDirectionTo(Main.player[npc.target].Center) * chargeSpeed;

                        npc.ai[1] = 7f;
                        npc.netUpdate = true;

                        if (npc.netSpam > 10)
                            npc.netSpam = 10;
                    }
                }

                else if (npc.ai[1] == 7f)
                {
                    // Set damage
                    npc.damage = setDamage;

                    if (npc.ai[2] == 0f)
                        SoundEngine.PlaySound(SoundID.ForceRoar, npc.Center);

                    float lineUpDistControl = (float)Math.Round(lineUpDist * 2.5f);
                    npc.ai[2] += 1f;

                    if (npc.ai[2] == lineUpDistControl && Vector2.Distance(npc.position, Main.player[npc.target].position) < 200f)
                        npc.ai[2] -= 1f;

                    if (npc.ai[2] >= lineUpDistControl)
                    {
                        // Deal less damage overall while not charging
                        npc.damage = reducedSetDamage;

                        npc.velocity *= 0.95f;
                        if (npc.velocity.X > -0.1 && npc.velocity.X < 0.1)
                            npc.velocity.X = 0f;
                        if (npc.velocity.Y > -0.1 && npc.velocity.Y < 0.1)
                            npc.velocity.Y = 0f;
                    }
                    else
                        npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;

                    float lineUpDistNetUpdate = lineUpDistControl + 13f;
                    if (npc.ai[2] >= lineUpDistNetUpdate)
                    {
                        // Deal less damage overall while not charging
                        npc.damage = reducedSetDamage;

                        npc.netUpdate = true;

                        if (npc.netSpam > 10)
                            npc.netSpam = 10;

                        npc.ai[2] = 0f;
                        npc.ai[1] = 0f;
                    }
                }
            }

            return false;
        }

        // If you think for a fucking second that I'm going to refactor this...
        public static bool VanillaEyeofCthulhuAI(NPC npc, Mod mod)
        {
            bool flag2 = false;
            if (Main.expertMode && (double)npc.life < (double)npc.lifeMax * (Main.masterMode ? 0.2 : 0.12))
                flag2 = true;

            bool flag3 = false;
            if (Main.expertMode && (double)npc.life < (double)npc.lifeMax * (Main.masterMode ? 0.08 : 0.04))
                flag3 = true;

            float num4 = Main.masterMode ? 15f : 20f;
            if (flag3)
                num4 = Main.masterMode ? 8f : 10f;

            if (npc.target < 0 || npc.target == Main.maxPlayers || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            bool dead = Main.player[npc.target].dead;
            float num5 = npc.Center.X - Main.player[npc.target].position.X - (float)(Main.player[npc.target].width / 2);
            float num6 = npc.position.Y + (float)npc.height - 59f - Main.player[npc.target].position.Y - (float)(Main.player[npc.target].height / 2);
            float num7 = (float)Math.Atan2(num6, num5) + MathHelper.PiOver2;
            if (num7 < 0f)
                num7 += MathHelper.TwoPi;
            else if ((double)num7 > MathHelper.TwoPi)
                num7 -= MathHelper.TwoPi;

            float num8 = 0f;
            if (npc.ai[0] == 0f && npc.ai[1] == 0f)
                num8 = 0.02f;

            if (npc.ai[0] == 0f && npc.ai[1] == 2f && npc.ai[2] > 40f)
                num8 = 0.05f;

            if (npc.ai[0] == 3f && npc.ai[1] == 0f)
                num8 = 0.05f;

            if (npc.ai[0] == 3f && npc.ai[1] == 2f && npc.ai[2] > 40f)
                num8 = 0.08f;

            if (npc.ai[0] == 3f && npc.ai[1] == 4f && npc.ai[2] > num4)
                num8 = 0.15f;

            if (npc.ai[0] == 3f && npc.ai[1] == 5f)
                num8 = 0.05f;

            if (Main.expertMode)
                num8 *= (Main.masterMode ? 2f : 1.5f);

            if (flag3 && Main.expertMode)
                num8 = 0f;

            if (npc.rotation < num7)
            {
                if ((double)(num7 - npc.rotation) > MathHelper.Pi)
                    npc.rotation -= num8;
                else
                    npc.rotation += num8;
            }
            else if (npc.rotation > num7)
            {
                if ((double)(npc.rotation - num7) > MathHelper.Pi)
                    npc.rotation += num8;
                else
                    npc.rotation -= num8;
            }

            if (npc.rotation > num7 - num8 && npc.rotation < num7 + num8)
                npc.rotation = num7;

            if (npc.rotation < 0f)
                npc.rotation += MathHelper.TwoPi;
            else if ((double)npc.rotation > MathHelper.TwoPi)
                npc.rotation -= MathHelper.TwoPi;

            if (npc.rotation > num7 - num8 && npc.rotation < num7 + num8)
                npc.rotation = num7;

            if (Main.rand.NextBool(5))
            {
                int num9 = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y + (float)npc.height * 0.25f), npc.width, (int)((float)npc.height * 0.5f), DustID.Blood, npc.velocity.X, 2f);
                Main.dust[num9].velocity.X *= 0.5f;
                Main.dust[num9].velocity.Y *= 0.1f;
            }

            npc.reflectsProjectiles = false;
            if (Main.IsItDay() || dead)
            {
                npc.velocity.Y -= 0.04f;
                npc.EncourageDespawn(10);
                return false;
            }

            if (npc.ai[0] == 0f)
            {
                if (npc.ai[1] == 0f)
                {
                    // Avoid cheap bullshit
                    npc.damage = 0;

                    float num10 = 5f;
                    float num11 = 0.04f;
                    if (Main.expertMode)
                    {
                        num11 = Main.masterMode ? 0.2f : 0.15f;
                        num10 = Main.masterMode ? 9f : 7f;
                    }

                    if (Main.getGoodWorld)
                    {
                        num11 += 0.05f;
                        num10 += 1f;
                    }

                    Vector2 vector = npc.Center;
                    float num12 = Main.player[npc.target].Center.X - vector.X;
                    float num13 = Main.player[npc.target].Center.Y - 200f - vector.Y;
                    float num14 = (float)Math.Sqrt(num12 * num12 + num13 * num13);
                    float num15 = num14;
                    num14 = num10 / num14;
                    num12 *= num14;
                    num13 *= num14;
                    if (npc.velocity.X < num12)
                    {
                        npc.velocity.X += num11;
                        if (npc.velocity.X < 0f && num12 > 0f)
                            npc.velocity.X += num11;
                    }
                    else if (npc.velocity.X > num12)
                    {
                        npc.velocity.X -= num11;
                        if (npc.velocity.X > 0f && num12 < 0f)
                            npc.velocity.X -= num11;
                    }

                    if (npc.velocity.Y < num13)
                    {
                        npc.velocity.Y += num11;
                        if (npc.velocity.Y < 0f && num13 > 0f)
                            npc.velocity.Y += num11;
                    }
                    else if (npc.velocity.Y > num13)
                    {
                        npc.velocity.Y -= num11;
                        if (npc.velocity.Y > 0f && num13 < 0f)
                            npc.velocity.Y -= num11;
                    }

                    npc.ai[2] += 1f;
                    float num16 = Main.masterMode ? 400f : 600f;
                    if (Main.expertMode)
                        num16 *= 0.35f;

                    if (npc.ai[2] >= num16)
                    {
                        npc.ai[1] = 1f;
                        npc.ai[2] = 0f;
                        npc.ai[3] = 0f;
                        npc.TargetClosest();
                        npc.netUpdate = true;
                    }
                    else if ((npc.position.Y + (float)npc.height < Main.player[npc.target].position.Y && num15 < 500f) || (Main.expertMode && num15 < (Main.masterMode ? 750f : 500f)))
                    {
                        if (!Main.player[npc.target].dead)
                            npc.ai[3] += 1f;

                        float num17 = Main.masterMode ? 70f : 110f;
                        if (Main.expertMode)
                            num17 *= 0.4f;

                        if (Main.getGoodWorld)
                            num17 *= 0.8f;

                        if (npc.ai[3] >= num17)
                        {
                            npc.ai[3] = 0f;
                            npc.rotation = num7;
                            float num18 = 5f;
                            if (Main.expertMode)
                                num18 = 6f;
                            if (Main.masterMode)
                                num18 = 9f;

                            float num19 = Main.player[npc.target].Center.X - vector.X;
                            float num20 = Main.player[npc.target].Center.Y - vector.Y;
                            float num21 = (float)Math.Sqrt(num19 * num19 + num20 * num20);
                            num21 = num18 / num21;
                            Vector2 vector2 = vector;
                            Vector2 vector3 = default(Vector2);
                            vector3.X = num19 * num21;
                            vector3.Y = num20 * num21;
                            vector2.X += vector3.X * 10f;
                            vector2.Y += vector3.Y * 10f;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                int num22 = NPC.NewNPC(npc.GetSource_FromAI(), (int)vector2.X, (int)vector2.Y, NPCID.ServantofCthulhu);
                                Main.npc[num22].velocity.X = vector3.X;
                                Main.npc[num22].velocity.Y = vector3.Y;
                                if (Main.netMode == NetmodeID.Server && num22 < Main.maxNPCs)
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, num22);
                            }

                            SoundEngine.PlaySound(SoundID.NPCHit1, vector2);
                            for (int m = 0; m < 10; m++)
                                Dust.NewDust(vector2, 20, 20, DustID.Blood, vector3.X * 0.4f, vector3.Y * 0.4f);
                        }
                    }
                }
                else if (npc.ai[1] == 1f)
                {
                    // Set damage
                    npc.damage = npc.defDamage;

                    npc.rotation = num7;
                    float num23 = 6f;
                    if (Main.expertMode)
                        num23 = 7f;
                    if (Main.masterMode)
                        num23 = 9f;

                    if (Main.getGoodWorld)
                        num23 += 1f;

                    Vector2 vector4 = npc.Center;
                    float num24 = Main.player[npc.target].Center.X - vector4.X;
                    float num25 = Main.player[npc.target].Center.Y - vector4.Y;
                    float num26 = (float)Math.Sqrt(num24 * num24 + num25 * num25);
                    num26 = num23 / num26;
                    npc.velocity.X = num24 * num26;
                    npc.velocity.Y = num25 * num26;
                    npc.ai[1] = 2f;
                    npc.netUpdate = true;
                    if (npc.netSpam > 10)
                        npc.netSpam = 10;
                }
                else if (npc.ai[1] == 2f)
                {
                    // Set damage
                    npc.damage = npc.defDamage;

                    npc.ai[2] += 1f;
                    if (npc.ai[2] >= 40f)
                    {
                        // Avoid cheap bullshit
                        npc.damage = 0;

                        npc.velocity *= 0.98f;
                        if (Main.expertMode)
                            npc.velocity *= 0.985f;
                        if (Main.masterMode)
                            npc.velocity *= 0.99f;

                        if (Main.getGoodWorld)
                            npc.velocity *= 0.99f;

                        if ((double)npc.velocity.X > -0.1 && (double)npc.velocity.X < 0.1)
                            npc.velocity.X = 0f;

                        if ((double)npc.velocity.Y > -0.1 && (double)npc.velocity.Y < 0.1)
                            npc.velocity.Y = 0f;
                    }
                    else
                        npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) - MathHelper.PiOver2;

                    int num27 = 150;
                    if (Main.expertMode)
                        num27 = 100;
                    if (Main.masterMode)
                        num27 = 80;

                    if (Main.getGoodWorld)
                        num27 -= 15;

                    if (npc.ai[2] >= (float)num27)
                    {
                        npc.ai[3] += 1f;
                        npc.ai[2] = 0f;
                        npc.rotation = num7;
                        if (npc.ai[3] >= (Main.masterMode ? 4f : 3f))
                        {
                            // Avoid cheap bullshit
                            npc.damage = 0;

                            npc.ai[1] = 0f;
                            npc.ai[3] = 0f;
                        }
                        else
                            npc.ai[1] = 1f;
                    }
                }

                float num28 = 0.5f;
                if (Main.expertMode)
                    num28 = 0.65f;
                if (Main.masterMode)
                    num28 = 0.75f;

                if ((float)npc.life < (float)npc.lifeMax * num28)
                {
                    // Avoid cheap bullshit
                    npc.damage = 0;

                    npc.ai[0] = 1f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.ai[3] = 0f;
                    npc.TargetClosest();
                    npc.netUpdate = true;
                    if (npc.netSpam > 10)
                        npc.netSpam = 10;
                }

                return false;
            }

            if (npc.ai[0] == 1f || npc.ai[0] == 2f)
            {
                // Avoid cheap bullshit
                npc.damage = 0;

                if (npc.ai[0] == 1f || npc.ai[3] == 1f)
                {
                    npc.ai[2] += 0.005f;
                    if ((double)npc.ai[2] > 0.5)
                        npc.ai[2] = 0.5f;
                }
                else
                {
                    npc.ai[2] -= 0.005f;
                    if (npc.ai[2] < 0f)
                        npc.ai[2] = 0f;
                }

                npc.rotation += npc.ai[2];
                npc.ai[1] += 1f;
                if (Main.getGoodWorld)
                    npc.reflectsProjectiles = true;

                int num29 = 20;
                if (Main.getGoodWorld && npc.life < npc.lifeMax / 3)
                    num29 = 10;

                if (Main.expertMode && npc.ai[1] % (float)num29 == 0f)
                {
                    float num30 = Main.masterMode ? 8f : 5f;
                    Vector2 vector5 = npc.Center;
                    float num31 = Main.rand.Next(-200, 200);
                    float num32 = Main.rand.Next(-200, 200);
                    if (Main.getGoodWorld)
                    {
                        num31 *= 3f;
                        num32 *= 3f;
                    }

                    float num33 = (float)Math.Sqrt(num31 * num31 + num32 * num32);
                    num33 = num30 / num33;
                    Vector2 vector6 = vector5;
                    Vector2 vector7 = default(Vector2);
                    vector7.X = num31 * num33;
                    vector7.Y = num32 * num33;
                    vector6.X += vector7.X * 10f;
                    vector6.Y += vector7.Y * 10f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int num34 = NPC.NewNPC(npc.GetSource_FromAI(), (int)vector6.X, (int)vector6.Y, NPCID.ServantofCthulhu);
                        Main.npc[num34].velocity.X = vector7.X;
                        Main.npc[num34].velocity.Y = vector7.Y;
                        if (Main.netMode == NetmodeID.Server && num34 < Main.maxNPCs)
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, num34);
                    }

                    for (int n = 0; n < 10; n++)
                        Dust.NewDust(vector6, 20, 20, DustID.Blood, vector7.X * 0.4f, vector7.Y * 0.4f);
                }

                if (npc.ai[1] >= 100f)
                {
                    if (npc.ai[3] == 1f)
                    {
                        npc.ai[3] = 0f;
                        npc.ai[1] = 0f;
                    }
                    else
                    {
                        npc.ai[0] += 1f;
                        npc.ai[1] = 0f;
                        if (npc.ai[0] == 3f)
                        {
                            npc.ai[2] = 0f;
                        }
                        else
                        {
                            SoundEngine.PlaySound(SoundID.NPCHit1, npc.Center);
                            for (int num35 = 0; num35 < 2; num35++)
                            {
                                Gore.NewGore(npc.GetSource_FromAI(), npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), 8);
                                Gore.NewGore(npc.GetSource_FromAI(), npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), 7);
                                Gore.NewGore(npc.GetSource_FromAI(), npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), 6);
                            }

                            for (int num36 = 0; num36 < 20; num36++)
                                Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, (float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f);

                            SoundEngine.PlaySound(SoundID.ForceRoar, npc.Center);
                        }
                    }
                }

                Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, (float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f);
                npc.velocity *= 0.98f;
                if ((double)npc.velocity.X > -0.1 && (double)npc.velocity.X < 0.1)
                    npc.velocity.X = 0f;

                if ((double)npc.velocity.Y > -0.1 && (double)npc.velocity.Y < 0.1)
                    npc.velocity.Y = 0f;

                return false;
            }

            npc.defense = 0;
            if (Main.expertMode)
            {
                if (flag2)
                    npc.defense = -(Main.masterMode ? 10 : 15);
                if (flag3)
                    npc.defense = -(Main.masterMode ? 20 : 30);
            }

            double phase2DamageMultiplier = 1.5;
            npc.damage = (int)Math.Round(npc.defDamage * phase2DamageMultiplier);
            int reducedSetDamage = (int)Math.Round(npc.damage * 0.5);

            if (npc.ai[1] == 0f && flag2)
                npc.ai[1] = 5f;

            if (npc.ai[1] == 0f)
            {
                // Deal less damage while not charging
                npc.damage = reducedSetDamage;

                float num39 = Main.masterMode ? 8f : 6f;
                float num40 = Main.masterMode ? 0.1f : 0.07f;
                Vector2 vector8 = npc.Center;
                float num41 = Main.player[npc.target].Center.X - vector8.X;
                float num42 = Main.player[npc.target].Center.Y - 120f - vector8.Y;
                float num43 = (float)Math.Sqrt(num41 * num41 + num42 * num42);
                float distanceVelocityIncrease = Main.masterMode ? 1.5f : 1f;
                float distanceAccelerationIncrease = Main.masterMode ? 0.08f : 0.05f;
                if (num43 > 400f && Main.expertMode)
                {
                    num39 += distanceVelocityIncrease;
                    num40 += distanceAccelerationIncrease;
                    if (num43 > 600f)
                    {
                        num39 += distanceVelocityIncrease;
                        num40 += distanceAccelerationIncrease;
                        if (num43 > 800f)
                        {
                            num39 += distanceVelocityIncrease;
                            num40 += distanceAccelerationIncrease;
                        }
                    }
                }

                if (Main.getGoodWorld)
                {
                    num39 += 1f;
                    num40 += 0.1f;
                }

                num43 = num39 / num43;
                num41 *= num43;
                num42 *= num43;
                if (npc.velocity.X < num41)
                {
                    npc.velocity.X += num40;
                    if (npc.velocity.X < 0f && num41 > 0f)
                        npc.velocity.X += num40;
                }
                else if (npc.velocity.X > num41)
                {
                    npc.velocity.X -= num40;
                    if (npc.velocity.X > 0f && num41 < 0f)
                        npc.velocity.X -= num40;
                }

                if (npc.velocity.Y < num42)
                {
                    npc.velocity.Y += num40;
                    if (npc.velocity.Y < 0f && num42 > 0f)
                        npc.velocity.Y += num40;
                }
                else if (npc.velocity.Y > num42)
                {
                    npc.velocity.Y -= num40;
                    if (npc.velocity.Y > 0f && num42 < 0f)
                        npc.velocity.Y -= num40;
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= (Main.masterMode ? 120f : 200f))
                {
                    npc.ai[1] = 1f;
                    npc.ai[2] = 0f;
                    npc.ai[3] = 0f;
                    if (Main.expertMode && (double)npc.life < (double)npc.lifeMax * (Main.masterMode ? 0.45 : 0.35))
                        npc.ai[1] = 3f;

                    npc.netUpdate = true;
                }

                if (Main.expertMode && flag3)
                {
                    npc.netUpdate = true;
                    npc.ai[1] = 3f;
                    npc.ai[2] = 0f;
                    npc.ai[3] -= 1000f;
                }
            }
            else if (npc.ai[1] == 1f)
            {
                SoundEngine.PlaySound(SoundID.ForceRoar, npc.Center);
                npc.rotation = num7;
                float num44 = Main.masterMode ? 8f : 6.8f;
                if (Main.expertMode && npc.ai[3] == 1f)
                    num44 *= 1.15f;

                if (Main.expertMode && npc.ai[3] == 2f)
                    num44 *= 1.3f;

                if (Main.getGoodWorld)
                    num44 *= 1.2f;

                Vector2 vector9 = npc.Center;
                float num45 = Main.player[npc.target].Center.X - vector9.X;
                float num46 = Main.player[npc.target].Center.Y - vector9.Y;
                float num47 = (float)Math.Sqrt(num45 * num45 + num46 * num46);
                num47 = num44 / num47;
                npc.velocity.X = num45 * num47;
                npc.velocity.Y = num46 * num47;
                npc.ai[1] = 2f;
                npc.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
            }
            else if (npc.ai[1] == 2f)
            {
                float num48 = 40f;
                npc.ai[2] += 1f;
                if (Main.expertMode)
                    num48 = 50f;
                if (Main.masterMode)
                    num48 = 60f;

                if (npc.ai[2] >= num48)
                {
                    // Deal less damage while not charging
                    npc.damage = reducedSetDamage;

                    npc.velocity *= 0.97f;
                    if (Main.expertMode)
                        npc.velocity *= 0.98f;
                    if (Main.masterMode)
                        npc.velocity *= 0.99f;

                    if ((double)npc.velocity.X > -0.1 && (double)npc.velocity.X < 0.1)
                        npc.velocity.X = 0f;

                    if ((double)npc.velocity.Y > -0.1 && (double)npc.velocity.Y < 0.1)
                        npc.velocity.Y = 0f;
                }
                else
                    npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) - MathHelper.PiOver2;

                int num49 = 130;
                if (Main.expertMode)
                    num49 = 90;
                if (Main.masterMode)
                    num49 = 80;

                if (npc.ai[2] >= (float)num49)
                {
                    npc.ai[3] += 1f;
                    npc.ai[2] = 0f;
                    npc.rotation = num7;
                    if (npc.ai[3] >= 3f)
                    {
                        // Deal less damage while not charging
                        npc.damage = reducedSetDamage;

                        npc.ai[1] = 0f;
                        npc.ai[3] = 0f;
                        if (Main.expertMode && Main.netMode != NetmodeID.MultiplayerClient && (double)npc.life < (double)npc.lifeMax * (Main.masterMode ? 0.6 : 0.5))
                        {
                            npc.ai[1] = 3f;
                            npc.ai[3] += Main.rand.Next(1, 4);
                        }

                        npc.netUpdate = true;
                        if (npc.netSpam > 10)
                            npc.netSpam = 10;
                    }
                    else
                        npc.ai[1] = 1f;
                }
            }
            else if (npc.ai[1] == 3f)
            {
                if (npc.ai[3] == 4f && flag2 && npc.Center.Y > Main.player[npc.target].Center.Y)
                {
                    // Deal less damage while not charging
                    npc.damage = reducedSetDamage;

                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.ai[3] = 0f;
                    npc.netUpdate = true;
                    if (npc.netSpam > 10)
                        npc.netSpam = 10;
                }
                else if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float num50 = Main.masterMode ? 24f : 20f;
                    Vector2 vector10 = npc.Center;
                    float num51 = Main.player[npc.target].Center.X - vector10.X;
                    float num52 = Main.player[npc.target].Center.Y - vector10.Y;
                    float num53 = Math.Abs(Main.player[npc.target].velocity.X) + Math.Abs(Main.player[npc.target].velocity.Y) / 4f;
                    num53 += 10f - num53;
                    if (num53 < 5f)
                        num53 = 5f;

                    if (num53 > 15f)
                        num53 = 15f;

                    if (npc.ai[2] == -1f && !flag3)
                    {
                        num53 *= 4f;
                        num50 *= 1.3f;
                    }

                    if (flag3)
                        num53 *= 2f;

                    num51 -= Main.player[npc.target].velocity.X * num53;
                    num52 -= Main.player[npc.target].velocity.Y * num53 / 4f;
                    num51 *= 1f + (float)Main.rand.Next(-10, 11) * 0.01f;
                    num52 *= 1f + (float)Main.rand.Next(-10, 11) * 0.01f;
                    if (flag3)
                    {
                        num51 *= 1f + (float)Main.rand.Next(-10, 11) * 0.01f;
                        num52 *= 1f + (float)Main.rand.Next(-10, 11) * 0.01f;
                    }

                    float num54 = (float)Math.Sqrt(num51 * num51 + num52 * num52);
                    float num55 = num54;
                    num54 = num50 / num54;
                    npc.velocity.X = num51 * num54;
                    npc.velocity.Y = num52 * num54;
                    npc.velocity.X += (float)Main.rand.Next(-20, 21) * 0.1f;
                    npc.velocity.Y += (float)Main.rand.Next(-20, 21) * 0.1f;
                    if (flag3)
                    {
                        npc.velocity.X += (float)Main.rand.Next(-50, 51) * 0.1f;
                        npc.velocity.Y += (float)Main.rand.Next(-50, 51) * 0.1f;
                        float num56 = Math.Abs(npc.velocity.X);
                        float num57 = Math.Abs(npc.velocity.Y);
                        if (npc.Center.X > Main.player[npc.target].Center.X)
                            num57 *= -1f;

                        if (npc.Center.Y > Main.player[npc.target].Center.Y)
                            num56 *= -1f;

                        npc.velocity.X = num57 + npc.velocity.X;
                        npc.velocity.Y = num56 + npc.velocity.Y;
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY);
                        npc.velocity *= num50;
                        npc.velocity.X += (float)Main.rand.Next(-20, 21) * 0.1f;
                        npc.velocity.Y += (float)Main.rand.Next(-20, 21) * 0.1f;
                    }
                    else if (num55 < 100f)
                    {
                        if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y))
                        {
                            float num58 = Math.Abs(npc.velocity.X);
                            float num59 = Math.Abs(npc.velocity.Y);
                            if (npc.Center.X > Main.player[npc.target].Center.X)
                                num59 *= -1f;

                            if (npc.Center.Y > Main.player[npc.target].Center.Y)
                                num58 *= -1f;

                            npc.velocity.X = num59;
                            npc.velocity.Y = num58;
                        }
                    }
                    else if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y))
                    {
                        float num60 = (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) / 2f;
                        float num61 = num60;
                        if (npc.Center.X > Main.player[npc.target].Center.X)
                            num61 *= -1f;

                        if (npc.Center.Y > Main.player[npc.target].Center.Y)
                            num60 *= -1f;

                        npc.velocity.X = num61;
                        npc.velocity.Y = num60;
                    }

                    npc.ai[1] = 4f;
                    npc.netUpdate = true;
                    if (npc.netSpam > 10)
                        npc.netSpam = 10;
                }
            }
            else if (npc.ai[1] == 4f)
            {
                if (npc.ai[2] == 0f)
                    SoundEngine.PlaySound(SoundID.ForceRoarPitched, npc.Center);

                float num62 = num4;
                npc.ai[2] += 1f;
                if (npc.ai[2] == num62 && Vector2.Distance(npc.position, Main.player[npc.target].position) < 200f)
                    npc.ai[2] -= 1f;

                if (npc.ai[2] >= num62)
                {
                    // Deal less damage while not charging
                    npc.damage = reducedSetDamage;

                    npc.velocity *= 0.95f;
                    if ((double)npc.velocity.X > -0.1 && (double)npc.velocity.X < 0.1)
                        npc.velocity.X = 0f;

                    if ((double)npc.velocity.Y > -0.1 && (double)npc.velocity.Y < 0.1)
                        npc.velocity.Y = 0f;
                }
                else
                    npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) - MathHelper.PiOver2;

                float num63 = num62 + 13f;
                if (npc.ai[2] >= num63)
                {
                    npc.netUpdate = true;
                    if (npc.netSpam > 10)
                        npc.netSpam = 10;

                    npc.ai[3] += 1f;
                    npc.ai[2] = 0f;
                    if (npc.ai[3] >= 5f)
                    {
                        // Deal less damage while not charging
                        npc.damage = reducedSetDamage;

                        npc.ai[1] = 0f;
                        npc.ai[3] = 0f;
                        if (npc.target >= 0 && Main.getGoodWorld && Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, npc.width, npc.height))
                        {
                            SoundEngine.PlaySound(SoundID.ForceRoarPitched, npc.Center);
                            npc.ai[0] = 2f;
                            npc.ai[1] = 0f;
                            npc.ai[2] = 0f;
                            npc.ai[3] = 1f;
                            npc.netUpdate = true;
                        }
                    }
                    else
                        npc.ai[1] = 3f;
                }
            }
            else if (npc.ai[1] == 5f)
            {
                // Deal less damage while not charging
                npc.damage = reducedSetDamage;

                float num64 = 600f;
                float num65 = Main.masterMode ? 12f : 9f;
                float num66 = Main.masterMode ? 0.4f : 0.3f;
                Vector2 vector11 = npc.Center;
                float num67 = Main.player[npc.target].Center.X - vector11.X;
                float num68 = Main.player[npc.target].Center.Y + num64 - vector11.Y;
                float num69 = (float)Math.Sqrt(num67 * num67 + num68 * num68);
                num69 = num65 / num69;
                num67 *= num69;
                num68 *= num69;
                if (npc.velocity.X < num67)
                {
                    npc.velocity.X += num66;
                    if (npc.velocity.X < 0f && num67 > 0f)
                        npc.velocity.X += num66;
                }
                else if (npc.velocity.X > num67)
                {
                    npc.velocity.X -= num66;
                    if (npc.velocity.X > 0f && num67 < 0f)
                        npc.velocity.X -= num66;
                }

                if (npc.velocity.Y < num68)
                {
                    npc.velocity.Y += num66;
                    if (npc.velocity.Y < 0f && num68 > 0f)
                        npc.velocity.Y += num66;
                }
                else if (npc.velocity.Y > num68)
                {
                    npc.velocity.Y -= num66;
                    if (npc.velocity.Y > 0f && num68 < 0f)
                        npc.velocity.Y -= num66;
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= (Main.masterMode ? 40f : 70f))
                {
                    npc.ai[1] = 3f;
                    npc.ai[2] = -1f;
                    npc.ai[3] = Main.rand.Next(-3, 1);
                    npc.netUpdate = true;
                }
            }

            if (flag3 && npc.ai[1] == 5f)
                npc.ai[1] = 3f;

            return false;
        }
    }
}
