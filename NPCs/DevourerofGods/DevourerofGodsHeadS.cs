﻿using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.Items.Armor.Vanity;
using CalamityMod.Items.LoreItems;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Placeables.Furniture.Trophies;
using CalamityMod.Items.Placeables.FurnitureCosmilite;
using CalamityMod.Items.Potions;
using CalamityMod.Items.TreasureBags;
using CalamityMod.Items.Weapons.Magic;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Items.Weapons.Summon;
using CalamityMod.NPCs.TownNPCs;
using CalamityMod.Projectiles.Boss;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityMod.NPCs.DevourerofGods
{
    [AutoloadBossHead]
    public class DevourerofGodsHeadS : ModNPC
    {
		private enum LaserWallPhase
		{
			SetUp = 0,
			FireLaserWalls = 1,
			End = 2
		}

		private enum LaserWallType
		{
			Normal = 0,
			Offset = 1,
			MultiLayered = 2,
			DiagonalHorizontal = 3,
			DiagonalVertical = 4
		}

		private bool tail = false;
        private const int minLength = 100;
        private const int maxLength = 101;
        private bool halfLife = false;

		private const int shotSpacingMax = 1050;
		private int[] shotSpacing = new int[4] { shotSpacingMax, shotSpacingMax, shotSpacingMax, shotSpacingMax };
        private const int spacingVar = 105;
		private const int diagonalSpacingVar = 350;
        private const int totalShots = 20;
		private const int totalDiagonalShots = 6;
		private const float laserWallSpacingOffset = 16f;
		private int laserWallType = 0;
		public int laserWallPhase = 0;

		private const int idleCounterMax = 360;
        private int idleCounter = idleCounterMax;
		private int postTeleportTimer = 0;
		private int teleportTimer = -1;

		private int preventBullshitHitsAtStartofFinalPhaseTimer = 0;

		private const float alphaGateValue = 669f;

		public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("The Devourer of Gods");
        }

        public override void SetDefaults()
        {
			npc.Calamity().canBreakPlayerDefense = true;
			npc.GetNPCDamage();
			npc.npcSlots = 5f;
            npc.width = 186;
            npc.height = 186;
            npc.defense = 50;
			npc.LifeMaxNERB(517500, 621000, 9200000);
			double HPBoost = CalamityConfig.Instance.BossHealthBoost * 0.01;
            npc.lifeMax += (int)(npc.lifeMax * HPBoost);
            npc.takenDamageMultiplier = 1.1f;
            npc.aiStyle = -1;
            aiType = -1;
            npc.knockBackResist = 0f;
            npc.boss = true;
            npc.value = Item.buyPrice(0, 80, 0, 0);
            npc.alpha = 255;
            npc.behindTiles = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
			npc.DeathSound = SoundID.NPCDeath14;
            npc.netAlways = true;
            Mod calamityModMusic = CalamityMod.Instance.musicMod;
            if (calamityModMusic != null)
                music = calamityModMusic.GetSoundSlot(SoundType.Music, "Sounds/Music/UniversalCollapse");
            else
                music = MusicID.LunarBoss;
            bossBag = ModContent.ItemType<DevourerofGodsBag>();
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
			writer.Write(npc.dontTakeDamage);
            writer.Write(halfLife);
            writer.Write(shotSpacing[0]);
            writer.Write(shotSpacing[1]);
            writer.Write(shotSpacing[2]);
            writer.Write(shotSpacing[3]);
            writer.Write(idleCounter);
            writer.Write(laserWallPhase);
			writer.Write(postTeleportTimer);
			writer.Write(preventBullshitHitsAtStartofFinalPhaseTimer);
			writer.Write(laserWallType);
			writer.Write(teleportTimer);
            writer.Write(npc.alpha);
		}

        public override void ReceiveExtraAI(BinaryReader reader)
        {
			npc.dontTakeDamage = reader.ReadBoolean();
            halfLife = reader.ReadBoolean();
            shotSpacing[0] = reader.ReadInt32();
            shotSpacing[1] = reader.ReadInt32();
            shotSpacing[2] = reader.ReadInt32();
            shotSpacing[3] = reader.ReadInt32();
            idleCounter = reader.ReadInt32();
            laserWallPhase = reader.ReadInt32();
			postTeleportTimer = reader.ReadInt32();
			preventBullshitHitsAtStartofFinalPhaseTimer = reader.ReadInt32();
			laserWallType = reader.ReadInt32();
			teleportTimer = reader.ReadInt32();
            npc.alpha = reader.ReadInt32();
		}

        public override void BossHeadRotation(ref float rotation)
        {
            rotation = npc.rotation;
        }

        public override void AI()
        {
            CalamityGlobalNPC calamityGlobalNPC = npc.Calamity();

            // whoAmI variable
            CalamityGlobalNPC.DoGHead = npc.whoAmI;

            // Percent life remaining
            float lifeRatio = npc.life / (float)npc.lifeMax;

			// Increase aggression if player is taking a long time to kill the boss
			if (lifeRatio > calamityGlobalNPC.killTimeRatio_IncreasedAggression)
				lifeRatio = calamityGlobalNPC.killTimeRatio_IncreasedAggression;

			// Variables
			Vector2 vector = npc.Center;
            bool flies = npc.ai[2] == 0f;
			bool malice = CalamityWorld.malice;
			bool expertMode = Main.expertMode || BossRushEvent.BossRushActive || malice;
			bool revenge = CalamityWorld.revenge || BossRushEvent.BossRushActive || malice;
			bool death = CalamityWorld.death || BossRushEvent.BossRushActive || malice;
            bool phase2 = lifeRatio < 0.75f;
            bool phase3 = lifeRatio < 0.3f;
            bool breathFireMore = lifeRatio < 0.15f;

			// Light
			Lighting.AddLight((int)((npc.position.X + (npc.width / 2)) / 16f), (int)((npc.position.Y + (npc.height / 2)) / 16f), 0.2f, 0.05f, 0.2f);

			// Worm shit again
			if (npc.ai[3] > 0f)
				npc.realLife = (int)npc.ai[3];

			// Get a target
			if (npc.target < 0 || npc.target == Main.maxPlayers || Main.player[npc.target].dead || !Main.player[npc.target].active)
				npc.TargetClosest();

			// Despawn safety, make sure to target another player if the current player target is too far away
			if (Vector2.Distance(Main.player[npc.target].Center, npc.Center) > CalamityGlobalNPC.CatchUpDistance200Tiles)
				npc.TargetClosest();

			Player player = Main.player[npc.target];

			float distanceFromTarget = Vector2.Distance(player.Center, vector);
			bool increaseSpeed = distanceFromTarget > CalamityGlobalNPC.CatchUpDistance200Tiles;
			bool increaseSpeedMore = distanceFromTarget > CalamityGlobalNPC.CatchUpDistance350Tiles;

			float takeLessDamageDistance = 1600f;
			if (distanceFromTarget > takeLessDamageDistance)
			{
				float damageTakenScalar = MathHelper.Clamp(1f - ((distanceFromTarget - takeLessDamageDistance) / takeLessDamageDistance), 0f, 1f);
				npc.takenDamageMultiplier = MathHelper.Lerp(1f, 1.1f, damageTakenScalar);
			}
			else
				npc.takenDamageMultiplier = 1.1f;

			// Immunity after teleport
			npc.dontTakeDamage = postTeleportTimer > 0 || preventBullshitHitsAtStartofFinalPhaseTimer > 0;

			// Teleport
			if (teleportTimer >= 0)
			{
				teleportTimer--;
				if (teleportTimer == 0)
					Teleport(player, death, revenge, expertMode);
			}

			// Laser walls
			if (phase2 && !phase3 && postTeleportTimer <= 0)
            {
				if (laserWallPhase == (int)LaserWallPhase.SetUp)
				{
					// Increment next laser wall phase timer
					calamityGlobalNPC.newAI[3] += 1f;

					// Set alpha value prior to firing laser walls
					if (calamityGlobalNPC.newAI[3] > alphaGateValue)
					{
						// Disable teleports
						if (teleportTimer >= 0)
						{
							GetRiftLocation(false);
							teleportTimer = -1;
						}

						npc.alpha = (int)MathHelper.Clamp((calamityGlobalNPC.newAI[3] - alphaGateValue) * 5f, 0f, 255f);
					}

					// Fire laser walls every 12 seconds after a laser wall phase ends
					if (calamityGlobalNPC.newAI[3] >= 720f)
					{
						npc.alpha = 255;

						// Reset laser wall timer to 0
						calamityGlobalNPC.newAI[1] = 0f;

						calamityGlobalNPC.newAI[3] = 0f;
						laserWallPhase = (int)LaserWallPhase.FireLaserWalls;
					}
				}
				else if (laserWallPhase == (int)LaserWallPhase.FireLaserWalls)
				{
					// Remain in laser wall firing phase for 6 seconds
					idleCounter--;
					if (idleCounter <= 0)
					{
						SpawnTeleportLocation(player);
						laserWallPhase = (int)LaserWallPhase.End;
						idleCounter = idleCounterMax;
					}
				}
				else if (laserWallPhase == (int)LaserWallPhase.End)
				{
					// End laser wall phase after 4.25 seconds
					npc.alpha -= 1;
					if (npc.alpha <= 0)
					{
						npc.alpha = 0;
						laserWallPhase = (int)LaserWallPhase.SetUp;
					}
				}
            }
            else
            {
				// Set alpha after teleport
				if (postTeleportTimer > 0)
				{
					postTeleportTimer--;
					if (postTeleportTimer < 0)
						postTeleportTimer = 0;

					npc.alpha = postTeleportTimer;
				}
				else
				{
					npc.alpha -= 6;
					if (npc.alpha < 0)
						npc.alpha = 0;
				}

				// This exists so that DoG doesn't sometimes instantly kill the player when he goes to final phase
				if (preventBullshitHitsAtStartofFinalPhaseTimer > 0)
				{
					preventBullshitHitsAtStartofFinalPhaseTimer--;

					if (npc.alpha < 1)
						npc.alpha = 1;
				}

				// Reset laser wall phase
                if (laserWallPhase > (int)LaserWallPhase.SetUp)
                    laserWallPhase = (int)LaserWallPhase.SetUp;

				// Enter final phase
				if (!halfLife && phase3)
				{
					SpawnTeleportLocation(player);

					preventBullshitHitsAtStartofFinalPhaseTimer = 180;

					// Anger message
					string key = "Mods.CalamityMod.EdgyBossText11";
					Color messageColor = Color.Cyan;
                    CalamityUtils.DisplayLocalizedText(key, messageColor);

                    // Summon Thots
                    if (Main.netMode != NetmodeID.MultiplayerClient)
					{
						Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/DevourerAttack"), (int)player.position.X, (int)player.position.Y);

						for (int i = 0; i < 3; i++)
							NPC.SpawnOnPlayer(npc.FindClosestPlayer(), ModContent.NPCType<DevourerofGodsHead2>());
					}

					halfLife = true;
				}
			}

			// Spawn segments and fire projectiles
			if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Segments
                if (!tail && npc.ai[0] == 0f)
                {
                    int Previous = npc.whoAmI;
                    for (int segmentSpawn = 0; segmentSpawn < maxLength; segmentSpawn++)
                    {
                        int segment;
                        if (segmentSpawn >= 0 && segmentSpawn < minLength)
                            segment = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<DevourerofGodsBodyS>(), npc.whoAmI);
                        else
                            segment = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<DevourerofGodsTailS>(), npc.whoAmI);

                        Main.npc[segment].realLife = npc.whoAmI;
                        Main.npc[segment].ai[2] = npc.whoAmI;
                        Main.npc[segment].ai[1] = Previous;
                        Main.npc[Previous].ai[0] = segment;
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, segment, 0f, 0f, 0f, 0);
                        Previous = segment;
                    }
                    tail = true;
                }

                // Fireballs
                if (npc.alpha <= 0 && distanceFromTarget > 500f)
                {
                    calamityGlobalNPC.newAI[0] += 1f;
					if (calamityGlobalNPC.newAI[0] >= 150f && calamityGlobalNPC.newAI[0] % (breathFireMore ? 60f : 120f) == 0f)
					{
						Vector2 vector44 = new Vector2(npc.position.X + npc.width * 0.5f, npc.position.Y + npc.height * 0.5f);
						float num427 = player.position.X + (player.width / 2) - vector44.X;
						float num428 = player.position.Y + (player.height / 2) - vector44.Y;
						float num430 = 16f;
						float num429 = (float)Math.Sqrt(num427 * num427 + num428 * num428);
						num429 = num430 / num429;
						num427 *= num429;
						num428 *= num429;
						num428 += npc.velocity.Y * 0.5f;
						num427 += npc.velocity.X * 0.5f;
						vector44.X -= num427 * 1f;
						vector44.Y -= num428 * 1f;

						int type = ModContent.ProjectileType<DoGFire>();
						int damage = npc.GetProjectileDamage(type);
						Projectile.NewProjectile(vector44.X, vector44.Y, num427, num428, type, damage, 0f, Main.myPlayer);
					}
                }
                else if (distanceFromTarget < 250f)
                    calamityGlobalNPC.newAI[0] = 0f;

                // Laser walls
                if (!phase3 && (laserWallPhase == (int)LaserWallPhase.FireLaserWalls || calamityGlobalNPC.enraged > 0))
                {
                    float speed = 12f;
                    float spawnOffset = 1500f;
                    float divisor = malice ? 100f : 120f;

					if (calamityGlobalNPC.newAI[1] % divisor == 0f)
					{
						Main.PlaySound(SoundID.Item12, player.position);

						// Side walls
						float targetPosY = player.position.Y;
						int type = ModContent.ProjectileType<DoGDeath>();
						int damage = npc.GetProjectileDamage(type);
						int halfTotalDiagonalShots = totalDiagonalShots / 2;
						Vector2 start = default;
						Vector2 velocity = default;
						Vector2 aim = expertMode ? player.Center + player.velocity * 20f : Vector2.Zero;

						switch (laserWallType)
						{
							case (int)LaserWallType.Normal:

								for (int x = 0; x < totalShots; x++)
								{
									Projectile.NewProjectile(player.position.X + spawnOffset, targetPosY + shotSpacing[0], -speed, 0f, type, damage, 0f, Main.myPlayer);
									Projectile.NewProjectile(player.position.X - spawnOffset, targetPosY + shotSpacing[0], speed, 0f, type, damage, 0f, Main.myPlayer);
									shotSpacing[0] -= spacingVar;
								}

								if (expertMode)
								{
									Projectile.NewProjectile(player.position.X + spawnOffset, targetPosY, -speed, 0f, type, damage, 0f, Main.myPlayer);
									Projectile.NewProjectile(player.position.X - spawnOffset, targetPosY, speed, 0f, type, damage, 0f, Main.myPlayer);
								}

								laserWallType = (int)LaserWallType.Offset;
								break;

							case (int)LaserWallType.Offset:

								targetPosY += 50f;
								for (int x = 0; x < totalShots; x++)
								{
									Projectile.NewProjectile(player.position.X + spawnOffset, targetPosY + shotSpacing[0], -speed, 0f, type, damage, 0f, Main.myPlayer);
									Projectile.NewProjectile(player.position.X - spawnOffset, targetPosY + shotSpacing[0], speed, 0f, type, damage, 0f, Main.myPlayer);
									shotSpacing[0] -= spacingVar;
								}

								if (expertMode)
								{
									Projectile.NewProjectile(player.position.X, targetPosY + spawnOffset, 0f, -speed, type, damage, 0f, Main.myPlayer);
									Projectile.NewProjectile(player.position.X, targetPosY - spawnOffset, 0f, speed, type, damage, 0f, Main.myPlayer);
								}

								laserWallType = revenge ? (int)LaserWallType.MultiLayered : expertMode ? (int)LaserWallType.DiagonalHorizontal : (int)LaserWallType.Normal;
								break;

							case (int)LaserWallType.MultiLayered:

								for (int x = 0; x < totalShots; x++)
								{
									Projectile.NewProjectile(player.position.X + spawnOffset, targetPosY + shotSpacing[0], -speed, 0f, type, damage, 0f, Main.myPlayer);
									Projectile.NewProjectile(player.position.X - spawnOffset, targetPosY + shotSpacing[0], speed, 0f, type, damage, 0f, Main.myPlayer);
									shotSpacing[0] -= spacingVar;
								}

								int totalBonusLasers = totalShots / 2;
								for (int x = 0; x < totalBonusLasers; x++)
								{
									Projectile.NewProjectile(player.position.X + spawnOffset, targetPosY + shotSpacing[3], -speed, 0f, type, damage, 0f, Main.myPlayer);
									Projectile.NewProjectile(player.position.X - spawnOffset, targetPosY + shotSpacing[3], speed, 0f, type, damage, 0f, Main.myPlayer);
									shotSpacing[3] -= Main.rand.NextBool(2) ? 180 : 200;
								}

								Projectile.NewProjectile(player.position.X + spawnOffset, targetPosY, -speed, 0f, type, damage, 0f, Main.myPlayer);
								Projectile.NewProjectile(player.position.X - spawnOffset, targetPosY, speed, 0f, type, damage, 0f, Main.myPlayer);

								laserWallType = (int)LaserWallType.DiagonalHorizontal;
								break;

							case (int)LaserWallType.DiagonalHorizontal:

								for (int x = 0; x < totalDiagonalShots + 1; x++)
								{
									start = new Vector2(player.position.X + spawnOffset, targetPosY + shotSpacing[0]);
									aim.Y += laserWallSpacingOffset * (x - halfTotalDiagonalShots);
									velocity = Vector2.Normalize(aim - start) * speed;
									Projectile.NewProjectile(start, velocity, type, damage, 0f, Main.myPlayer);

									start = new Vector2(player.position.X - spawnOffset, targetPosY + shotSpacing[0]);
									velocity = Vector2.Normalize(aim - start) * speed;
									Projectile.NewProjectile(start, velocity, type, damage, 0f, Main.myPlayer);

									shotSpacing[0] -= diagonalSpacingVar;
								}

								Projectile.NewProjectile(player.position.X, targetPosY + spawnOffset, 0f, -speed, type, damage, 0f, Main.myPlayer);
								Projectile.NewProjectile(player.position.X, targetPosY - spawnOffset, 0f, speed, type, damage, 0f, Main.myPlayer);

								laserWallType = revenge ? (int)LaserWallType.DiagonalVertical : (int)LaserWallType.Normal;
								break;

							case (int)LaserWallType.DiagonalVertical:

								for (int x = 0; x < totalDiagonalShots + 1; x++)
								{
									start = new Vector2(player.position.X + shotSpacing[0], targetPosY + spawnOffset);
									aim.X += laserWallSpacingOffset * (x - halfTotalDiagonalShots);
									velocity = Vector2.Normalize(aim - start) * speed;
									Projectile.NewProjectile(start, velocity, type, damage, 0f, Main.myPlayer);

									start = new Vector2(player.position.X + shotSpacing[0], targetPosY - spawnOffset);
									velocity = Vector2.Normalize(aim - start) * speed;
									Projectile.NewProjectile(start, velocity, type, damage, 0f, Main.myPlayer);

									shotSpacing[0] -= diagonalSpacingVar;
								}

								Projectile.NewProjectile(player.position.X + spawnOffset, targetPosY, -speed, 0f, type, damage, 0f, Main.myPlayer);
								Projectile.NewProjectile(player.position.X - spawnOffset, targetPosY, speed, 0f, type, damage, 0f, Main.myPlayer);

								laserWallType = (int)LaserWallType.Normal;
								break;
						}

						// Lower wall
						for (int x = 0; x < totalShots; x++)
						{
							Projectile.NewProjectile(player.position.X + shotSpacing[1], player.position.Y + spawnOffset, 0f, -speed, type, damage, 0f, Main.myPlayer);
							shotSpacing[1] -= spacingVar;
						}

						// Upper wall
						for (int x = 0; x < totalShots; x++)
						{
							Projectile.NewProjectile(player.position.X + shotSpacing[2], player.position.Y - spawnOffset, 0f, speed, type, damage, 0f, Main.myPlayer);
							shotSpacing[2] -= spacingVar;
						}

						for (int i = 0; i < shotSpacing.Length; i++)
							shotSpacing[i] = shotSpacingMax;
					}

					calamityGlobalNPC.newAI[1] += 1f;
				}
            }

            // Despawn
            if (!NPC.AnyNPCs(ModContent.NPCType<DevourerofGodsTailS>()))
                npc.active = false;

            float fallSpeed = malice ? 19.5f : death ? 17.75f : 16f;

			if (expertMode)
				fallSpeed += 3.5f * (1f - lifeRatio);

			if (player.dead)
            {
				npc.TargetClosest(false);
				flies = true;
                npc.velocity.Y -= 3f;
                if ((double)npc.position.Y < Main.topWorld + 16f)
                {
                    npc.velocity.Y -= 3f;
                    fallSpeed = 32f;
                }
                if ((double)npc.position.Y < Main.topWorld + 16f)
                {
                    for (int a = 0; a < Main.maxNPCs; a++)
                    {
                        if (Main.npc[a].type == ModContent.NPCType<DevourerofGodsHeadS>() || Main.npc[a].type == ModContent.NPCType<DevourerofGodsBodyS>() || Main.npc[a].type == ModContent.NPCType<DevourerofGodsTailS>())
                            Main.npc[a].active = false;
                    }
                }
            }

			// Movement
			int num180 = (int)(npc.position.X / 16f) - 1;
            int num181 = (int)((npc.position.X + npc.width) / 16f) + 2;
            int num182 = (int)(npc.position.Y / 16f) - 1;
            int num183 = (int)((npc.position.Y + npc.height) / 16f) + 2;

            if (num180 < 0)
                num180 = 0;
            if (num181 > Main.maxTilesX)
                num181 = Main.maxTilesX;
            if (num182 < 0)
                num182 = 0;
            if (num183 > Main.maxTilesY)
                num183 = Main.maxTilesY;

            if (npc.velocity.X < 0f)
                npc.spriteDirection = -1;
            else if (npc.velocity.X > 0f)
                npc.spriteDirection = 1;

			int phaseLimit = death ? 600 : 900;

			// Flight
			if (npc.ai[2] == 0f)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    if (!Main.player[Main.myPlayer].dead && Main.player[Main.myPlayer].active && Vector2.Distance(Main.player[Main.myPlayer].Center, vector) < CalamityGlobalNPC.CatchUpDistance350Tiles)
                        Main.player[Main.myPlayer].AddBuff(ModContent.BuffType<Warped>(), 2);
                }

				// Charge in a direction for a second until the timer is back at 0
				if (postTeleportTimer > 0)
				{
					npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + MathHelper.PiOver2;
					return;
				}

				calamityGlobalNPC.newAI[2] += 1f;

                npc.localAI[1] = 0f;

				float speed = malice ? 18f : death ? 16.5f : 15f;
                float turnSpeed = malice ? 0.36f : death ? 0.33f : 0.3f;
                float homingSpeed = malice ? 36f : death ? 30f : 24f;
                float homingTurnSpeed = malice ? 0.48f : death ? 0.405f : 0.33f;

				if (expertMode)
				{
					phaseLimit /= 1 + (int)(5f * (1f - lifeRatio));

					if (phaseLimit < 180)
						phaseLimit = 180;

					speed += 3f * (1f - lifeRatio);
					turnSpeed += 0.06f * (1f - lifeRatio);
					homingSpeed += 12f * (1f - lifeRatio);
					homingTurnSpeed += 0.15f * (1f - lifeRatio);
				}

				// Go to ground phase sooner
				if (increaseSpeedMore)
				{
					if (laserWallPhase == (int)LaserWallPhase.SetUp && calamityGlobalNPC.newAI[3] <= alphaGateValue)
						SpawnTeleportLocation(player);
					else
						calamityGlobalNPC.newAI[2] += 10f;
				}
				else
					calamityGlobalNPC.newAI[2] += 2f;

				float num188 = speed;
                float num189 = turnSpeed;
                Vector2 vector18 = npc.Center;
                float num191 = player.Center.X;
                float num192 = player.Center.Y;
                int num42 = -1;
                int num43 = (int)(player.Center.X / 16f);
                int num44 = (int)(player.Center.Y / 16f);

                // Charge at target for 1.5 seconds
                bool flyAtTarget = (!phase2 || phase3) && calamityGlobalNPC.newAI[2] > phaseLimit - 90 && revenge;

                for (int num45 = num43 - 2; num45 <= num43 + 2; num45++)
                {
                    for (int num46 = num44; num46 <= num44 + 15; num46++)
                    {
                        if (WorldGen.SolidTile2(num45, num46))
                        {
                            num42 = num46;
                            break;
                        }
                    }
                    if (num42 > 0)
                        break;
                }

                if (!flyAtTarget)
                {
                    if (num42 > 0)
                    {
                        num42 *= 16;
                        float num47 = num42 - 800;
                        if (player.position.Y > num47)
                        {
                            num192 = num47;
                            if (Math.Abs(npc.Center.X - player.Center.X) < 500f)
                            {
                                if (npc.velocity.X > 0f)
                                    num191 = player.Center.X + 600f;
                                else
                                    num191 = player.Center.X - 600f;
                            }
                        }
                    }
                }
                else
                {
                    num188 = homingSpeed;
                    num189 = homingTurnSpeed;
                }

				if (expertMode)
				{
					num188 += Vector2.Distance(player.Center, npc.Center) * 0.005f * (1f - lifeRatio);
					num189 += Vector2.Distance(player.Center, npc.Center) * 0.0001f * (1f - lifeRatio);
				}

				float num48 = num188 * 1.3f;
                float num49 = num188 * 0.7f;
                float num50 = npc.velocity.Length();
                if (num50 > 0f)
                {
                    if (num50 > num48)
                    {
                        npc.velocity.Normalize();
                        npc.velocity *= num48;
                    }
                    else if (num50 < num49)
                    {
                        npc.velocity.Normalize();
                        npc.velocity *= num49;
                    }
                }

                num191 = (int)(num191 / 16f) * 16;
                num192 = (int)(num192 / 16f) * 16;
                vector18.X = (int)(vector18.X / 16f) * 16;
                vector18.Y = (int)(vector18.Y / 16f) * 16;
                num191 -= vector18.X;
                num192 -= vector18.Y;
                float num193 = (float)Math.Sqrt(num191 * num191 + num192 * num192);
                float num196 = Math.Abs(num191);
                float num197 = Math.Abs(num192);
                float num198 = num188 / num193;
                num191 *= num198;
                num192 *= num198;

                if ((npc.velocity.X > 0f && num191 > 0f) || (npc.velocity.X < 0f && num191 < 0f) || (npc.velocity.Y > 0f && num192 > 0f) || (npc.velocity.Y < 0f && num192 < 0f))
                {
                    if (npc.velocity.X < num191)
                        npc.velocity.X += num189;
                    else
                    {
                        if (npc.velocity.X > num191)
                            npc.velocity.X -= num189;
                    }

                    if (npc.velocity.Y < num192)
                        npc.velocity.Y += num189;
                    else
                    {
                        if (npc.velocity.Y > num192)
                            npc.velocity.Y -= num189;
                    }

                    if (Math.Abs(num192) < num188 * 0.2 && ((npc.velocity.X > 0f && num191 < 0f) || (npc.velocity.X < 0f && num191 > 0f)))
                    {
                        if (npc.velocity.Y > 0f)
                            npc.velocity.Y += num189 * 2f;
                        else
                            npc.velocity.Y -= num189 * 2f;
                    }

                    if (Math.Abs(num191) < num188 * 0.2 && ((npc.velocity.Y > 0f && num192 < 0f) || (npc.velocity.Y < 0f && num192 > 0f)))
                    {
                        if (npc.velocity.X > 0f)
                            npc.velocity.X += num189 * 2f;
                        else
                            npc.velocity.X -= num189 * 2f;
                    }
                }
                else
                {
                    if (num196 > num197)
                    {
                        if (npc.velocity.X < num191)
                            npc.velocity.X += num189 * 1.1f;
                        else if (npc.velocity.X > num191)
                            npc.velocity.X -= num189 * 1.1f;

                        if ((Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < num188 * 0.5)
                        {
                            if (npc.velocity.Y > 0f)
                                npc.velocity.Y += num189;
                            else
                                npc.velocity.Y -= num189;
                        }
                    }
                    else
                    {
                        if (npc.velocity.Y < num192)
                            npc.velocity.Y += num189 * 1.1f;
                        else if (npc.velocity.Y > num192)
                            npc.velocity.Y -= num189 * 1.1f;

                        if ((Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < num188 * 0.5)
                        {
                            if (npc.velocity.X > 0f)
                                npc.velocity.X += num189;
                            else
                                npc.velocity.X -= num189;
                        }
                    }
                }

                npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + MathHelper.PiOver2;

                if (calamityGlobalNPC.newAI[2] > phaseLimit)
                {
                    npc.ai[2] = 1f;
					calamityGlobalNPC.newAI[2] = 0f;
					npc.TargetClosest();
                    npc.netUpdate = true;
                }
            }

            // Ground
            else
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    if (!Main.player[Main.myPlayer].dead && Main.player[Main.myPlayer].active && Vector2.Distance(Main.player[Main.myPlayer].Center, vector) < CalamityGlobalNPC.CatchUpDistance350Tiles)
                        Main.player[Main.myPlayer].AddBuff(ModContent.BuffType<ExtremeGrav>(), 2);
                }

				// Charge in a direction for a second until the timer is back at 0
				if (postTeleportTimer > 0)
				{
					npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + MathHelper.PiOver2;
					return;
				}

				calamityGlobalNPC.newAI[2] += 1f;

                float turnSpeed = malice ? 0.3f : death ? 0.24f : 0.18f;

				if (expertMode)
				{
					turnSpeed += 0.1f * (1f - lifeRatio);
					turnSpeed += Vector2.Distance(player.Center, npc.Center) * 0.00005f * (1f - lifeRatio);
				}

				// Enrage
				if (increaseSpeedMore)
				{
					if (laserWallPhase == (int)LaserWallPhase.SetUp && calamityGlobalNPC.newAI[3] <= alphaGateValue)
						SpawnTeleportLocation(player);
					else
					{
						fallSpeed *= 3f;
						turnSpeed *= 6f;
					}
				}
				else if (increaseSpeed)
				{
					fallSpeed *= 1.5f;
					turnSpeed *= 3f;
				}

                if (!flies)
                {
                    for (int num952 = num180; num952 < num181; num952++)
                    {
                        for (int num953 = num182; num953 < num183; num953++)
                        {
                            if (Main.tile[num952, num953] != null && ((Main.tile[num952, num953].nactive() && (Main.tileSolid[Main.tile[num952, num953].type] || (Main.tileSolidTop[Main.tile[num952, num953].type] && Main.tile[num952, num953].frameY == 0))) || Main.tile[num952, num953].liquid > 64))
                            {
                                Vector2 vector105;
                                vector105.X = num952 * 16;
                                vector105.Y = num953 * 16;
                                if (npc.position.X + npc.width > vector105.X && npc.position.X < vector105.X + 16f && npc.position.Y + npc.height > vector105.Y && npc.position.Y < vector105.Y + 16f)
                                {
                                    flies = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (!flies)
                {
                    npc.localAI[1] = 1f;

                    Rectangle rectangle12 = new Rectangle((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height);

                    int num954 = death ? 1125 : 1200;
                    if (lifeRatio < 0.8f && lifeRatio > 0.2f && !death)
                        num954 = 1400;

					if (expertMode)
						num954 -= (int)(150f * (1f - lifeRatio));

					if (num954 < 1050)
						num954 = 1050;

                    bool flag95 = true;
                    if (npc.position.Y > player.position.Y)
                    {
                        for (int num955 = 0; num955 < Main.maxPlayers; num955++)
                        {
                            if (Main.player[num955].active)
                            {
                                Rectangle rectangle13 = new Rectangle((int)Main.player[num955].position.X - 1000, (int)Main.player[num955].position.Y - 1000, 2000, num954);
                                if (rectangle12.Intersects(rectangle13))
                                {
                                    flag95 = false;
                                    break;
                                }
                            }
                        }
                        if (flag95)
                            flies = true;
                    }
                }
                else
                    npc.localAI[1] = 0f;

                float num189 = turnSpeed;
                Vector2 vector18 = npc.Center;
                float num191 = player.Center.X;
                float num192 = player.Center.Y;
                num191 = (int)(num191 / 16f) * 16;
                num192 = (int)(num192 / 16f) * 16;
                vector18.X = (int)(vector18.X / 16f) * 16;
                vector18.Y = (int)(vector18.Y / 16f) * 16;
                num191 -= vector18.X;
                num192 -= vector18.Y;
                float num193 = (float)Math.Sqrt(num191 * num191 + num192 * num192);

                if (!flies)
                {
                    npc.velocity.Y += turnSpeed;
                    if (npc.velocity.Y > fallSpeed)
                        npc.velocity.Y = fallSpeed;

                    if ((Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < fallSpeed * 2.2)
                    {
                        if (npc.velocity.X < 0f)
                            npc.velocity.X -= num189 * 1.1f;
                        else
                            npc.velocity.X += num189 * 1.1f;
                    }
                    else if (npc.velocity.Y == fallSpeed)
                    {
                        if (npc.velocity.X < num191)
                            npc.velocity.X += num189;
                        else if (npc.velocity.X > num191)
                            npc.velocity.X -= num189;
                    }
                    else if (npc.velocity.Y > 4f)
                    {
                        if (npc.velocity.X < 0f)
                            npc.velocity.X += num189 * 0.9f;
                        else
                            npc.velocity.X -= num189 * 0.9f;
                    }
                }
                else
                {
                    double maximumSpeed1 = malice ? 0.52 : death ? 0.46 : 0.4;
                    double maximumSpeed2 = malice ? 1.25 : death ? 1.125 : 1D;

					if (increaseSpeedMore)
					{
						maximumSpeed1 *= 4;
						maximumSpeed2 *= 4;
					}
					if (increaseSpeed)
					{
						maximumSpeed1 *= 2;
						maximumSpeed2 *= 2;
					}

					if (expertMode)
					{
						maximumSpeed1 += 0.1f * (1f - lifeRatio);
						maximumSpeed2 += 0.2f * (1f - lifeRatio);
					}

                    num193 = (float)Math.Sqrt(num191 * num191 + num192 * num192);
                    float num25 = Math.Abs(num191);
                    float num26 = Math.Abs(num192);
                    float num27 = fallSpeed / num193;
                    num191 *= num27;
                    num192 *= num27;

                    if (((npc.velocity.X > 0f && num191 > 0f) || (npc.velocity.X < 0f && num191 < 0f)) && ((npc.velocity.Y > 0f && num192 > 0f) || (npc.velocity.Y < 0f && num192 < 0f)))
                    {
                        if (npc.velocity.X < num191)
                            npc.velocity.X += turnSpeed * 1.5f;
                        else if (npc.velocity.X > num191)
                            npc.velocity.X -= turnSpeed * 1.5f;

                        if (npc.velocity.Y < num192)
                            npc.velocity.Y += turnSpeed * 1.5f;
                        else if (npc.velocity.Y > num192)
                            npc.velocity.Y -= turnSpeed * 1.5f;
                    }

                    if ((npc.velocity.X > 0f && num191 > 0f) || (npc.velocity.X < 0f && num191 < 0f) || (npc.velocity.Y > 0f && num192 > 0f) || (npc.velocity.Y < 0f && num192 < 0f))
                    {
                        if (npc.velocity.X < num191)
                            npc.velocity.X += turnSpeed;
                        else if (npc.velocity.X > num191)
                            npc.velocity.X -= turnSpeed;

                        if (npc.velocity.Y < num192)
                            npc.velocity.Y += turnSpeed;
                        else if (npc.velocity.Y > num192)
                            npc.velocity.Y -= turnSpeed;

                        if (Math.Abs(num192) < fallSpeed * maximumSpeed1 && ((npc.velocity.X > 0f && num191 < 0f) || (npc.velocity.X < 0f && num191 > 0f)))
                        {
                            if (npc.velocity.Y > 0f)
                                npc.velocity.Y += turnSpeed * 2f;
                            else
                                npc.velocity.Y -= turnSpeed * 2f;
                        }

                        if (Math.Abs(num191) < fallSpeed * maximumSpeed1 && ((npc.velocity.Y > 0f && num192 < 0f) || (npc.velocity.Y < 0f && num192 > 0f)))
                        {
                            if (npc.velocity.X > 0f)
                                npc.velocity.X += turnSpeed * 2f;
                            else
                                npc.velocity.X -= turnSpeed * 2f;
                        }
                    }
                    else if (num25 > num26)
                    {
                        if (npc.velocity.X < num191)
                            npc.velocity.X += turnSpeed * 1.1f;
                        else if (npc.velocity.X > num191)
                            npc.velocity.X -= turnSpeed * 1.1f;

                        if ((Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < fallSpeed * maximumSpeed2)
                        {
                            if (npc.velocity.Y > 0f)
                                npc.velocity.Y += turnSpeed;
                            else
                                npc.velocity.Y -= turnSpeed;
                        }
                    }
                    else
                    {
                        if (npc.velocity.Y < num192)
                            npc.velocity.Y += turnSpeed * 1.1f;
                        else if (npc.velocity.Y > num192)
                            npc.velocity.Y -= turnSpeed * 1.1f;

                        if ((Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < fallSpeed * maximumSpeed2)
                        {
                            if (npc.velocity.X > 0f)
                                npc.velocity.X += turnSpeed;
                            else
                                npc.velocity.X -= turnSpeed;
                        }
                    }
                }

				npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + MathHelper.PiOver2;

                if (flies)
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

                if (calamityGlobalNPC.newAI[2] > phaseLimit)
                {
                    npc.ai[2] = 0f;
					calamityGlobalNPC.newAI[2] = 0f;
					npc.TargetClosest();
                    npc.netUpdate = true;
                }
            }
        }

		private void SpawnTeleportLocation(Player player)
		{
			if (teleportTimer > -1 || player.dead || !player.active)
				return;

			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				int randomRange = 48;
				float distance = 640f;
				Vector2 targetVector = player.Center + player.velocity.SafeNormalize(Vector2.UnitX) * distance + new Vector2(Main.rand.Next(-randomRange, randomRange + 1), Main.rand.Next(-randomRange, randomRange + 1));
				Main.PlaySound(SoundID.Item109, player.Center);
				Projectile.NewProjectile(targetVector, Vector2.Zero, ModContent.ProjectileType<DoGTeleportRift>(), 0, 0f, Main.myPlayer, npc.whoAmI);
			}

			teleportTimer = BossRushEvent.BossRushActive ? 100 : CalamityWorld.death ? 120 : CalamityWorld.revenge ? 140 : Main.expertMode ? 160 : 180;
		}

		private void Teleport(Player player, bool death, bool revenge, bool expertMode)
		{
			Vector2 newPosition = GetRiftLocation(true);

			if (player.dead || !player.active || newPosition == default)
				return;

			npc.TargetClosest();
			npc.position = newPosition;
			float chargeVelocity = BossRushEvent.BossRushActive ? 30f : death ? 24f : revenge ? 22f : expertMode ? 20f : 18f;
			float maxChargeDistance = 1200f;
			postTeleportTimer = (int)Math.Round(maxChargeDistance / chargeVelocity);
			npc.alpha = postTeleportTimer;
			npc.velocity = Vector2.Normalize(player.Center - npc.Center) * chargeVelocity;
			npc.netUpdate = true;

			for (int i = 0; i < Main.maxNPCs; i++)
			{
				if (Main.npc[i].active && (Main.npc[i].type == ModContent.NPCType<DevourerofGodsBodyS>() || Main.npc[i].type == ModContent.NPCType<DevourerofGodsTailS>()))
				{
					Main.npc[i].position = newPosition;
                    if (Main.npc[i].type == ModContent.NPCType<DevourerofGodsTailS>())
                    {
                        ((DevourerofGodsTailS)Main.npc[i].modNPC).setInvulTime(720);
                    }
					Main.npc[i].netUpdate = true;
				}
			}

			Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/DevourerAttack"), player.Center);
		}

		private Vector2 GetRiftLocation(bool spawnDust)
		{
			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				if (Main.projectile[i].type == ModContent.ProjectileType<DoGTeleportRift>())
				{
					if (!spawnDust)
						Main.projectile[i].ai[0] = -1f;

					Main.projectile[i].Kill();
					return Main.projectile[i].Center;
				}
			}
			return default;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			SpriteEffects spriteEffects = SpriteEffects.None;
			if (npc.spriteDirection == 1)
				spriteEffects = SpriteEffects.FlipHorizontally;

			Texture2D texture2D15 = Main.npcTexture[npc.type];
			Vector2 vector11 = new Vector2(Main.npcTexture[npc.type].Width / 2, Main.npcTexture[npc.type].Height / 2);

			Vector2 vector43 = npc.Center - Main.screenPosition;
			vector43 -= new Vector2(texture2D15.Width, texture2D15.Height) * npc.scale / 2f;
			vector43 += vector11 * npc.scale + new Vector2(0f, 4f + npc.gfxOffY);
			spriteBatch.Draw(texture2D15, vector43, npc.frame, npc.GetAlpha(lightColor), npc.rotation, vector11, npc.scale, spriteEffects, 0f);

			if (!npc.dontTakeDamage)
			{
				texture2D15 = ModContent.GetTexture("CalamityMod/NPCs/DevourerofGods/DevourerofGodsHeadSGlow");
				Color color37 = Color.Lerp(Color.White, Color.Fuchsia, 0.5f);

				spriteBatch.Draw(texture2D15, vector43, npc.frame, color37, npc.rotation, vector11, npc.scale, spriteEffects, 0f);

				texture2D15 = ModContent.GetTexture("CalamityMod/NPCs/DevourerofGods/DevourerofGodsHeadSGlow2");
				color37 = Color.Lerp(Color.White, Color.Cyan, 0.5f);

				spriteBatch.Draw(texture2D15, vector43, npc.frame, color37, npc.rotation, vector11, npc.scale, spriteEffects, 0f);
			}

			return false;
		}

		public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ModContent.ItemType<CosmiliteBrick>();
        }

        public override bool SpecialNPCLoot()
        {
            int closestSegmentID = DropHelper.FindClosestWormSegment(npc,
                ModContent.NPCType<DevourerofGodsHeadS>(),
                ModContent.NPCType<DevourerofGodsBodyS>(),
                ModContent.NPCType<DevourerofGodsTailS>());
            npc.position = Main.npc[closestSegmentID].position;
            return false;
        }

        public override void NPCLoot()
        {
            // Stop the countdown -- if you kill DoG in less than 60 frames, this will stop another one from spawning.
            CalamityWorld.DoGSecondStageCountdown = 0;

            DropHelper.DropBags(npc);

			// Legendary drops for DoG
			DropHelper.DropItemCondition(npc, ModContent.ItemType<CosmicDischarge>(), true, CalamityWorld.malice);
			DropHelper.DropItemCondition(npc, ModContent.ItemType<Norfleet>(), true, CalamityWorld.malice);
			DropHelper.DropItemCondition(npc, ModContent.ItemType<Skullmasher>(), true, CalamityWorld.malice);

			DropHelper.DropItem(npc, ModContent.ItemType<OmegaHealingPotion>(), 5, 15);
            DropHelper.DropItemChance(npc, ModContent.ItemType<DevourerofGodsTrophy>(), 10);
            DropHelper.DropItemCondition(npc, ModContent.ItemType<KnowledgeDevourerofGods>(), true, !CalamityWorld.downedDoG);
            DropHelper.DropResidentEvilAmmo(npc, CalamityWorld.downedDoG, 6, 3, 2);

			CalamityGlobalTownNPC.SetNewShopVariable(new int[] { ModContent.NPCType<THIEF>() }, CalamityWorld.downedDoG);

			// All other drops are contained in the bag, so they only drop directly on Normal
			if (!Main.expertMode)
            {
                // Materials
                DropHelper.DropItem(npc, ModContent.ItemType<CosmiliteBar>(), 25, 35);
                DropHelper.DropItem(npc, ModContent.ItemType<CosmiliteBrick>(), 150, 250);

                // Weapons
                float w = DropHelper.NormalWeaponDropRateFloat;
                DropHelper.DropEntireWeightedSet(npc,
                    DropHelper.WeightStack<Excelsus>(w),
                    DropHelper.WeightStack<TheObliterator>(w),
                    DropHelper.WeightStack<Deathwind>(w),
                    DropHelper.WeightStack<DeathhailStaff>(w),
                    DropHelper.WeightStack<StaffoftheMechworm>(w),
                    DropHelper.WeightStack<Eradicator>(w)
                );

                // Vanity
                DropHelper.DropItemChance(npc, ModContent.ItemType<DevourerofGodsMask>(), 7);
				if (Main.rand.NextBool(5))
				{
					DropHelper.DropItem(npc, ModContent.ItemType<SilvaHelm>());
					DropHelper.DropItem(npc, ModContent.ItemType<SilvaHornedHelm>());
					DropHelper.DropItem(npc, ModContent.ItemType<SilvaMask>());
				}
			}

            // If DoG has not been killed yet, notify players that the holiday moons are buffed
            if (!CalamityWorld.downedDoG)
            {
                string key = "Mods.CalamityMod.DoGBossText";
                Color messageColor = Color.Cyan;
                string key2 = "Mods.CalamityMod.DoGBossText2";
                Color messageColor2 = Color.Orange;
				string key3 = "Mods.CalamityMod.DargonBossText";

				CalamityUtils.DisplayLocalizedText(key, messageColor);
                CalamityUtils.DisplayLocalizedText(key2, messageColor2);
				CalamityUtils.DisplayLocalizedText(key3, messageColor2);
			}

            // Mark DoG as dead
            CalamityWorld.downedDoG = true;
			CalamityNetcode.SyncWorld();
		}

		// Can only hit the target if within certain distance
		public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            cooldownSlot = 1;

            Rectangle targetHitbox = target.Hitbox;

            float dist1 = Vector2.Distance(npc.Center, targetHitbox.TopLeft());
            float dist2 = Vector2.Distance(npc.Center, targetHitbox.TopRight());
            float dist3 = Vector2.Distance(npc.Center, targetHitbox.BottomLeft());
            float dist4 = Vector2.Distance(npc.Center, targetHitbox.BottomRight());

            float minDist = dist1;
            if (dist2 < minDist)
                minDist = dist2;
            if (dist3 < minDist)
                minDist = dist3;
            if (dist4 < minDist)
                minDist = dist4;

            return minDist <= 80f && (npc.alpha <= 0 || postTeleportTimer > 0) && preventBullshitHitsAtStartofFinalPhaseTimer <= 0;
        }

        public override bool StrikeNPC(ref double damage, int defense, ref float knockback, int hitDirection, ref bool crit)
        {
            if (CalamityUtils.AntiButcher(npc, ref damage, 0.5f))
            {
                string key = "Mods.CalamityMod.EdgyBossText2";
                Color messageColor = Color.Cyan;
                CalamityUtils.DisplayLocalizedText(key, messageColor);
                return false;
            }
            return true;
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            scale = 2f;
            return null;
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            if (npc.soundDelay == 0)
            {
                npc.soundDelay = 8;
                Main.PlaySound(mod.GetLegacySoundSlot(SoundType.NPCHit, "Sounds/NPCHit/OtherworldlyHit"), npc.Center);
            }
            if (npc.life <= 0)
            {
                Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/DoGS"), 1f);
                Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/DoGS2"), 1f);
                Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/DoGS5"), 1f);
                npc.position.X = npc.position.X + (npc.width / 2);
                npc.position.Y = npc.position.Y + (npc.height / 2);
                npc.width = 50;
                npc.height = 50;
                npc.position.X = npc.position.X - (npc.width / 2);
                npc.position.Y = npc.position.Y - (npc.height / 2);
                for (int num621 = 0; num621 < 15; num621++)
                {
                    int num622 = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, (int)CalamityDusts.PurpleCosmolite, 0f, 0f, 100, default, 2f);
                    Main.dust[num622].velocity *= 3f;
                    if (Main.rand.NextBool(2))
                    {
                        Main.dust[num622].scale = 0.5f;
                        Main.dust[num622].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                    }
                }
                for (int num623 = 0; num623 < 30; num623++)
                {
                    int num624 = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, (int)CalamityDusts.PurpleCosmolite, 0f, 0f, 100, default, 3f);
                    Main.dust[num624].noGravity = true;
                    Main.dust[num624].velocity *= 5f;
                    num624 = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, (int)CalamityDusts.PurpleCosmolite, 0f, 0f, 100, default, 2f);
                    Main.dust[num624].velocity *= 2f;
                }
            }
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax * 0.8f * bossLifeScale);
        }

        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
            player.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 300, true);
            player.AddBuff(ModContent.BuffType<WhisperingDeath>(), 420, true);
            player.AddBuff(BuffID.Frostburn, 300, true);
            /*if ((CalamityWorld.death || BossRushEvent.BossRushActive) && (npc.alpha <= 0 || postTeleportTimer > 0) && !player.Calamity().lol && preventBullshitHitsAtStartofFinalPhaseTimer <= 0)
            {
                player.KillMe(PlayerDeathReason.ByCustomReason(player.name + "'s essence was consumed by the devourer."), 1000.0, 0, false);
            }*/

			if (player.Calamity().dogTextCooldown <= 0)
			{
				string text = Utils.SelectRandom(Main.rand, new string[]
				{
					"Mods.CalamityMod.EdgyBossText3",
					"Mods.CalamityMod.EdgyBossText4",
					"Mods.CalamityMod.EdgyBossText5",
					"Mods.CalamityMod.EdgyBossText6",
					"Mods.CalamityMod.EdgyBossText7"
				});
				Color messageColor = Color.Cyan;
				Rectangle location = new Rectangle((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height);
				CombatText.NewText(location, messageColor, Language.GetTextValue(text), true);
				player.Calamity().dogTextCooldown = 60;
			}
        }
    }
}
