using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Events;
using CalamityMod.Items.TreasureBags;
using CalamityMod.Projectiles.Boss;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityMod.NPCs.SlimeGod
{
	[AutoloadBossHead]
    public class SlimeGodRun : ModNPC
    {
        private float bossLife;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crimulan Slime God");
            Main.npcFrameCount[npc.type] = 6;
        }

        public override void SetDefaults()
        {
			npc.GetNPCDamage();
			npc.width = 150;
            npc.height = 92;
            npc.scale = 1.1f;
            npc.defense = 12;
            npc.LifeMaxNERB(3500, 4820, 1600000);
            double HPBoost = CalamityConfig.Instance.BossHealthBoost * 0.01;
            npc.lifeMax += (int)(npc.lifeMax * HPBoost);
            npc.knockBackResist = 0f;
            animationType = NPCID.KingSlime;
            npc.buffImmune[ModContent.BuffType<TimeSlow>()] = false;
            npc.value = 0f;
            npc.alpha = 55;
            npc.lavaImmune = false;
            npc.noGravity = false;
            npc.noTileCollide = false;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            Mod calamityModMusic = ModLoader.GetMod("CalamityModMusic");
            if (calamityModMusic != null)
                music = calamityModMusic.GetSoundSlot(SoundType.Music, "Sounds/Music/SlimeGod");
            else
                music = MusicID.Boss1;
            npc.aiStyle = -1;
            aiType = -1;
            bossBag = ModContent.ItemType<SlimeGodBag>();
        }

		public override void SendExtraAI(BinaryWriter writer)
		{
			writer.Write(npc.localAI[1]);
		}

		public override void ReceiveExtraAI(BinaryReader reader)
		{
			npc.localAI[1] = reader.ReadSingle();
		}

		public override void AI()
        {
            CalamityGlobalNPC.slimeGodRed = npc.whoAmI;
            bool expertMode = Main.expertMode || BossRushEvent.BossRushActive;
            bool revenge = CalamityWorld.revenge || BossRushEvent.BossRushActive;
			bool death = CalamityWorld.death || BossRushEvent.BossRushActive || npc.localAI[1] == 1f;
			Vector2 vector = npc.Center;

			npc.defense = npc.defDefense;
			npc.damage = npc.defDamage;
			if (npc.localAI[1] == 1f)
			{
				npc.defense = npc.defDefense + 24;
				npc.damage = npc.defDamage + 25;
			}

			npc.aiAction = 0;
			npc.noTileCollide = false;
			npc.noGravity = false;

			// Get a target
			if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
				npc.TargetClosest(true);

			Player player = Main.player[npc.target];

			if (npc.ai[0] != 6f && (player.dead || !player.active))
			{
				npc.TargetClosest(true);
				player = Main.player[npc.target];
				if (player.dead || !player.active)
				{
					npc.ai[0] = 6f;
					npc.ai[1] = 0f;
					npc.ai[2] = 0f;
					npc.ai[3] = 0f;
				}
			}
			else if (npc.timeLeft < 1800)
				npc.timeLeft = 1800;

			if (Vector2.Distance(player.Center, vector) > 5400f)
            {
                npc.position.X = player.Center.X / 16 * 16f - (npc.width / 2);
                npc.position.Y = player.Center.Y / 16 * 16f - (npc.height / 2) - 150f;
            }

			float distanceSpeedBoost = Vector2.Distance(player.Center, vector) * 0.005f;

			if (npc.life <= npc.lifeMax * 0.5 && Main.netMode != NetmodeID.MultiplayerClient && expertMode)
            {
                Main.PlaySound(SoundID.NPCDeath1, npc.position);
                Vector2 spawnAt = vector + new Vector2(0f, npc.height / 2f);
                NPC.NewNPC((int)spawnAt.X - 30, (int)spawnAt.Y, ModContent.NPCType<SlimeGodRunSplit>());
                NPC.NewNPC((int)spawnAt.X + 30, (int)spawnAt.Y, ModContent.NPCType<SlimeGodRunSplit>());
                npc.active = false;
                npc.netUpdate = true;
                return;
            }

            bool flag100 = false;
            bool hyperMode = npc.localAI[1] == 1f;
            if (CalamityGlobalNPC.slimeGodPurple != -1)
            {
                if (Main.npc[CalamityGlobalNPC.slimeGodPurple].active)
                {
                    flag100 = true;
                }
            }
            if (CalamityGlobalNPC.slimeGod < 0 || !Main.npc[CalamityGlobalNPC.slimeGod].active)
            {
				npc.localAI[1] = 0f;
				hyperMode = true;
                flag100 = false;
            }

			if (npc.localAI[1] != 1f)
			{
				if (!flag100)
					npc.defense = npc.defDefense * 2;
			}

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
				npc.localAI[0] += flag100 ? 1f : 2f;
				if (revenge)
					npc.localAI[0] += 0.5f;

				if (npc.localAI[0] >= 450f && Vector2.Distance(player.Center, npc.Center) > 160f)
				{
					npc.localAI[0] = 0f;
					if (expertMode && Main.rand.NextBool(2))
					{
						float num179 = revenge ? 9f : 8f;
						if (BossRushEvent.BossRushActive)
							num179 *= 1.5f;
						Vector2 value9 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
						float num180 = player.position.X + (float)player.width * 0.5f - value9.X;
						float num181 = Math.Abs(num180) * 0.1f;
						float num182 = player.position.Y + (float)player.height * 0.5f - value9.Y - num181;
						float num183 = (float)Math.Sqrt((double)(num180 * num180 + num182 * num182));
						npc.netUpdate = true;
						num183 = num179 / num183;
						num180 *= num183;
						num182 *= num183;
						int type = ModContent.ProjectileType<AbyssMine2>();
						int damage = npc.GetProjectileDamage(type);
						value9.X += num180;
						value9.Y += num182;
						for (int num186 = 0; num186 < 2; num186++)
						{
							num180 = player.position.X + (float)player.width * 0.5f - value9.X;
							num182 = player.position.Y + (float)player.height * 0.5f - value9.Y;
							num183 = (float)Math.Sqrt((double)(num180 * num180 + num182 * num182));
							num183 = num179 / num183;
							num180 += (float)Main.rand.Next(-60, 61);
							num182 += (float)Main.rand.Next(-60, 61);
							num180 *= num183;
							num182 *= num183;
							Projectile.NewProjectile(value9.X, value9.Y, num180, num182, type, damage, 0f, Main.myPlayer, 0f, 0f);
						}
					}
					else
					{
						float num179 = revenge ? 9f : 8f;
						if (BossRushEvent.BossRushActive)
							num179 *= 1.5f;
						Vector2 value9 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
						float num180 = player.position.X + (float)player.width * 0.5f - value9.X;
						float num181 = Math.Abs(num180) * 0.1f;
						float num182 = player.position.Y + (float)player.height * 0.5f - value9.Y - num181;
						float num183 = (float)Math.Sqrt((double)(num180 * num180 + num182 * num182));
						npc.netUpdate = true;
						num183 = num179 / num183;
						num180 *= num183;
						num182 *= num183;
						int type = ModContent.ProjectileType<AbyssBallVolley2>();
						int damage = npc.GetProjectileDamage(type);
						value9.X += num180;
						value9.Y += num182;
						for (int num186 = 0; num186 < 2; num186++)
						{
							num180 = player.position.X + (float)player.width * 0.5f - value9.X;
							num182 = player.position.Y + (float)player.height * 0.5f - value9.Y;
							num183 = (float)Math.Sqrt((double)(num180 * num180 + num182 * num182));
							num183 = num179 / num183;
							num180 += (float)Main.rand.Next(-30, 31);
							num182 += (float)Main.rand.Next(-30, 31);
							num180 *= num183;
							num182 *= num183;
							Projectile.NewProjectile(value9.X, value9.Y, num180, num182, type, damage, 0f, Main.myPlayer, 0f, 0f);
						}
					}
				}
            }

            if (npc.ai[0] == 0f)
            {
                npc.TargetClosest(true);
                npc.ai[0] = 1f;
                npc.ai[1] = 0f;
            }
            else if (npc.ai[0] == 1f)
            {
                if ((player.Center - vector).Length() > (hyperMode ? 1200f : 2400f))
                {
                    npc.ai[0] = 4f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.ai[3] = 0f;
                }
                if (npc.velocity.Y == 0f)
                {
                    npc.TargetClosest(true);
                    npc.velocity.X *= 0.8f;
                    npc.ai[1] += 1f;
                    float num1879 = 50f;
                    float num1880 = BossRushEvent.BossRushActive ? 16f : 4f;
					if (revenge)
					{
						float moveBoost = death ? 15f : 30f * (1f - npc.life / (float)npc.lifeMax);
						float speedBoost = death ? 2f : 4f * (1f - npc.life / (float)npc.lifeMax);
						num1879 -= moveBoost;
						num1880 += speedBoost;
					}
					float num1881 = 4f;
                    if (!Collision.CanHit(vector, 1, 1, player.Center, 1, 1))
                    {
                        num1881 += 2f;
                    }
                    if (npc.ai[1] > num1879)
                    {
                        npc.ai[3] += 1f;
                        if (npc.ai[3] >= 2f)
                        {
                            npc.ai[3] = 0f;
                            num1881 *= 0.75f;
                            num1880 *= 1.25f;
                        }
                        npc.ai[1] = 0f;
                        npc.velocity.Y -= num1881;
                        npc.velocity.X = (num1880 + distanceSpeedBoost) * npc.direction;
                    }
                }
                else
                {
                    npc.velocity.X *= 0.99f;
                    if (npc.direction < 0 && npc.velocity.X > -1f)
                    {
                        npc.velocity.X = -1f;
                    }
                    if (npc.direction > 0 && npc.velocity.X < 1f)
                    {
                        npc.velocity.X = 1f;
                    }
                }
                npc.ai[2] += 1f;
				if (revenge)
				{
					npc.ai[2] += death ? 1f : 2f * (1f - npc.life / (float)npc.lifeMax);
				}
				if (npc.ai[2] >= 360f && npc.velocity.Y == 0f && Main.netMode != NetmodeID.MultiplayerClient)
                {
					switch (Main.rand.Next(3))
					{
						case 0:
							npc.ai[0] = 2f;
							break;
						case 1:
							npc.ai[0] = 3f;
							npc.noTileCollide = true;
							npc.velocity.Y = BossRushEvent.BossRushActive ? -13f : -9f;
							break;
						case 2:
							npc.ai[0] = 5f;
							break;
						default:
							break;
					}
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.ai[3] = 0f;
                }
            }
            else if (npc.ai[0] == 2f)
            {
				npc.velocity.X *= 0.85f;
                npc.ai[1] += 1f;
                if (npc.ai[1] >= 40f)
                {
                    npc.ai[0] = 1f;
                    npc.ai[1] = 0f;
                }
            }
            else if (npc.ai[0] == 3f)
            {
                npc.noTileCollide = true;
                npc.noGravity = true;
                if (npc.velocity.X < 0f)
                {
                    npc.direction = -1;
                }
                else
                {
                    npc.direction = 1;
                }
                npc.spriteDirection = npc.direction;
                npc.TargetClosest(true);
                Vector2 center40 = player.Center;
                center40.Y -= 350f;
                Vector2 vector272 = center40 - vector;
                if (npc.ai[2] == 1f)
                {
                    npc.ai[1] += 1f;
                    vector272 = player.Center - vector;
                    vector272.Normalize();
                    vector272 *= BossRushEvent.BossRushActive ? 13f : 9f;
                    npc.velocity = (npc.velocity * 4f + vector272) / 5f;
                    if (npc.ai[1] > 12f)
                    {
                        npc.ai[1] = 0f;
                        npc.ai[0] = 3.1f;
                        npc.ai[2] = 0f;
                        npc.velocity = vector272;
                    }
                }
                else
                {
                    if (Math.Abs(vector.X - player.Center.X) < 40f && vector.Y < player.Center.Y - 300f)
                    {
                        npc.ai[1] = 0f;
                        npc.ai[2] = 1f;
                        return;
                    }
                    vector272.Normalize();
					vector272 *= (BossRushEvent.BossRushActive ? 20f : 11f) + distanceSpeedBoost;
					npc.velocity = (npc.velocity * 5f + vector272) / 6f;
                }
            }
            else if (npc.ai[0] == 3.1f)
            {
				bool atTargetPosition = npc.position.Y + npc.height >= player.position.Y;
				if (npc.ai[2] == 0f && (atTargetPosition || npc.localAI[1] == 0f) && Collision.CanHit(vector, 1, 1, player.Center, 1, 1) && !Collision.SolidCollision(npc.position, npc.width, npc.height))
                {
                    npc.ai[2] = 1f;
                }
                if (atTargetPosition || npc.velocity.Y <= 0f)
                {
                    npc.ai[1] += 1f;
                    if (npc.ai[1] > 10f)
                    {
                        npc.ai[0] = 1f;
                        npc.ai[1] = 0f;
                        npc.ai[2] = 0f;
                        npc.ai[3] = 0f;
                        if (Collision.SolidCollision(npc.position, npc.width, npc.height))
                        {
                            npc.ai[0] = 4f;
                        }
                    }
                }
                else if (npc.ai[2] == 0f)
                {
                    npc.noTileCollide = true;
                    npc.noGravity = true;
                }
                npc.velocity.Y += 0.2f;
				float velocityLimit = BossRushEvent.BossRushActive ? 20f : 14f;
				if (npc.velocity.Y > velocityLimit)
                {
                    npc.velocity.Y = velocityLimit;
                }
            }
            else
            {
                if (npc.ai[0] == 4f)
                {
                    if (npc.velocity.X > 0f)
                    {
                        npc.direction = 1;
                    }
                    else
                    {
                        npc.direction = -1;
                    }
                    npc.spriteDirection = npc.direction;
                    npc.noTileCollide = true;
                    npc.noGravity = true;
                    Vector2 value74 = player.Center - vector;
                    value74.Y -= 40f;
                    if (value74.Length() < 320f && !Collision.SolidCollision(npc.position, npc.width, npc.height))
                    {
                        npc.ai[0] = 1f;
                        npc.ai[1] = 0f;
                        npc.ai[2] = 0f;
                        npc.ai[3] = 0f;
                    }
                    if (value74.Length() > 100f)
                    {
                        value74.Normalize();
						value74 *= (BossRushEvent.BossRushActive ? 22f : 11f) + distanceSpeedBoost;
					}
                    npc.velocity = (npc.velocity * 4f + value74) / 5f;
                    return;
                }
                if (npc.ai[0] == 5f)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.TargetClosest(true);
                        npc.velocity.X *= 0.8f;
                        npc.ai[1] += 1f;
                        if (npc.ai[1] > 5f)
                        {
                            npc.ai[1] = 0f;
                            npc.velocity.Y -= 4f;
                            if (player.position.Y + player.height < vector.Y)
                            {
                                npc.velocity.Y -= 1.25f;
                            }
                            if (player.position.Y + player.height < vector.Y - 40f)
                            {
                                npc.velocity.Y -= 1.5f;
                            }
                            if (player.position.Y + player.height < vector.Y - 80f)
                            {
                                npc.velocity.Y -= 1.75f;
                            }
                            if (player.position.Y + player.height < vector.Y - 120f)
                            {
                                npc.velocity.Y -= 2f;
                            }
                            if (player.position.Y + player.height < vector.Y - 160f)
                            {
                                npc.velocity.Y -= 2.25f;
                            }
                            if (player.position.Y + player.height < vector.Y - 200f)
                            {
                                npc.velocity.Y -= 2.5f;
                            }
                            if (!Collision.CanHit(vector, 1, 1, player.Center, 1, 1))
                            {
                                npc.velocity.Y -= 2f;
                            }
							npc.velocity.X = ((BossRushEvent.BossRushActive ? 22f : 11f) + distanceSpeedBoost) * npc.direction;
							npc.ai[2] += 1f;
                        }
                    }
                    else
                    {
                        npc.velocity.X *= 0.98f;
						float velocityLimit = (BossRushEvent.BossRushActive ? 12f : 6f) + distanceSpeedBoost;
						if (npc.direction < 0 && npc.velocity.X > -velocityLimit)
                        {
                            npc.velocity.X = -velocityLimit;
                        }
                        if (npc.direction > 0 && npc.velocity.X < velocityLimit)
                        {
                            npc.velocity.X = velocityLimit;
                        }
                    }
                    if (npc.ai[2] >= 3f && npc.velocity.Y == 0f)
                    {
                        npc.ai[0] = 1f;
                        npc.ai[1] = 0f;
                        npc.ai[2] = 0f;
                        npc.ai[3] = 0f;
                    }
                }
                else if (npc.ai[0] == 6f)
                {
                    npc.life = npc.lifeMax;
                    npc.defense = 9999;
                    npc.noTileCollide = true;
                    npc.alpha += 7;
                    if (npc.timeLeft > 10)
                    {
                        npc.timeLeft = 10;
                    }
                    if (npc.alpha > 255)
                    {
                        npc.alpha = 255;
                    }
                    npc.velocity.X *= 0.98f;
                }
            }
            int num658 = Dust.NewDust(npc.position, npc.width, npc.height, 260, npc.velocity.X, npc.velocity.Y, 255, new Color(0, 80, 255, 80), npc.scale * 1.5f);
            Main.dust[num658].noGravity = true;
            Main.dust[num658].velocity *= 0.5f;
            if (bossLife == 0f && npc.life > 0)
            {
                bossLife = (float)npc.lifeMax;
            }
            float num644 = 1f;
            if (npc.life > 0)
            {
                float num659 = (float)npc.life / (float)npc.lifeMax;
                num659 = num659 * 0.5f + 0.75f;
                num659 *= num644;
                if (num659 != npc.scale)
                {
                    npc.position.X = npc.position.X + (float)(npc.width / 2);
                    npc.position.Y = npc.position.Y + (float)npc.height;
                    npc.scale = num659;
                    npc.width = (int)(150f * npc.scale);
                    npc.height = (int)(92f * npc.scale);
                    npc.position.X = npc.position.X - (float)(npc.width / 2);
                    npc.position.Y = npc.position.Y - (float)npc.height;
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int num660 = (int)((double)npc.lifeMax * 0.15);
					if ((float)(npc.life + num660) < bossLife)
					{
						bossLife = (float)npc.life;
						int x = (int)(npc.position.X + (float)Main.rand.Next(npc.width - 32));
						int y = (int)(npc.position.Y + (float)Main.rand.Next(npc.height - 32));
						int num663 = ModContent.NPCType<SlimeSpawnCrimson>();
						if (Main.rand.NextBool(3))
						{
							num663 = ModContent.NPCType<SlimeSpawnCrimson2>();
						}
						int num664 = NPC.NewNPC(x, y, num663, 0, 0f, 0f, 0f, 0f, 255);
						Main.npc[num664].SetDefaults(num663, -1f);
						Main.npc[num664].velocity.X = (float)Main.rand.Next(-15, 16) * 0.1f;
						Main.npc[num664].velocity.Y = (float)Main.rand.Next(-30, 1) * 0.1f;
						Main.npc[num664].ai[0] = (float)(-1000 * Main.rand.Next(3));
						Main.npc[num664].ai[1] = 0f;
						if (Main.netMode == NetmodeID.Server && num664 < 200)
						{
							NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, num664, 0f, 0f, 0f, 0, 0, 0);
						}
					}
                }
            }
        }

		public override Color? GetAlpha(Color drawColor)
		{
			Color lightColor = new Color(Main.DiscoR, 100, 150, npc.alpha);
			Color newColor = npc.localAI[1] == 1f ? lightColor : drawColor;
			return newColor;
		}

		public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.HealingPotion;
        }

        // If the un-split Crimulan Slime God gets one-shotted last, it should drop the boss loot
        public override void NPCLoot()
        {
            bool otherSlimeGodsAlive =
                NPC.AnyNPCs(ModContent.NPCType<SlimeGodCore>()) ||
                NPC.AnyNPCs(ModContent.NPCType<SlimeGod>()) ||
                NPC.AnyNPCs(ModContent.NPCType<SlimeGodSplit>()) ||
                NPC.AnyNPCs(ModContent.NPCType<SlimeGodRunSplit>());
            if (!otherSlimeGodsAlive)
                SlimeGodCore.DropSlimeGodLoot(npc);
        }

        public override bool CheckActive()
        {
            if (CalamityGlobalNPC.slimeGod != -1)
            {
                if (Main.npc[CalamityGlobalNPC.slimeGod].active)
                    return false;
            }
            return true;
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int k = 0; k < 5; k++)
            {
                Dust.NewDust(npc.position, npc.width, npc.height, 4, hitDirection, -1f, 0, default, 1f);
            }
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax * 0.8f * bossLifeScale);
        }

        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
            player.AddBuff(BuffID.Cursed, 90, true);
        }
    }
}
