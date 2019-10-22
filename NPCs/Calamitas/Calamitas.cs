﻿using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Projectiles.Boss;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
namespace CalamityMod.NPCs.Calamitas
{
    [AutoloadBossHead]
    public class Calamitas : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Calamitas");
            Main.npcFrameCount[npc.type] = 6;
        }

        public override void SetDefaults()
        {
            npc.damage = 55;
            npc.npcSlots = 14f;
            npc.width = 120;
            npc.height = 120;
            npc.defense = 15;
            npc.Calamity().RevPlusDR(0.15f);
            npc.value = 0f;
            npc.LifeMaxNERD(37500, 51750, 82750, 5200000, 5500000);
            if (CalamityWorld.downedProvidence && !CalamityWorld.bossRushActive)
            {
                npc.damage *= 3;
                npc.defense *= 3;
                npc.lifeMax *= 3;
            }
            double HPBoost = Config.BossHealthPercentageBoost * 0.01;
            npc.lifeMax += (int)(npc.lifeMax * HPBoost);
            npc.aiStyle = -1;
            aiType = -1;
            npc.knockBackResist = 0f;
            NPCID.Sets.TrailCacheLength[npc.type] = 8;
            NPCID.Sets.TrailingMode[npc.type] = 1;
            for (int k = 0; k < npc.buffImmune.Length; k++)
            {
                npc.buffImmune[k] = true;
            }
            npc.buffImmune[BuffID.Ichor] = false;
            npc.buffImmune[ModContent.BuffType<MarkedforDeath>()] = false;
            npc.buffImmune[BuffID.CursedInferno] = false;
            npc.buffImmune[BuffID.Daybreak] = false;
            npc.buffImmune[ModContent.BuffType<AbyssalFlames>()] = false;
            npc.buffImmune[ModContent.BuffType<ArmorCrunch>()] = false;
            npc.buffImmune[ModContent.BuffType<DemonFlames>()] = false;
            npc.buffImmune[ModContent.BuffType<GodSlayerInferno>()] = false;
            npc.buffImmune[ModContent.BuffType<HolyFlames>()] = false;
            npc.buffImmune[ModContent.BuffType<Nightwither>()] = false;
            npc.buffImmune[ModContent.BuffType<Plague>()] = false;
            npc.buffImmune[ModContent.BuffType<Shred>()] = false;
            npc.buffImmune[ModContent.BuffType<WhisperingDeath>()] = false;
            npc.buffImmune[ModContent.BuffType<SilvaStun>()] = false;
            npc.buffImmune[ModContent.BuffType<MaxVenom>()] = false;
            npc.boss = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.HitSound = SoundID.NPCHit4;
            music = MusicID.Boss2;
        }

        public override void FindFrame(int frameHeight)
        {
            npc.frameCounter += 0.15f;
            npc.frameCounter %= Main.npcFrameCount[npc.type];
            int frame = (int)npc.frameCounter;
            npc.frame.Y = frame * frameHeight;
        }

        public override void AI()
        {
            if ((double)npc.life <= (double)npc.lifeMax * 0.75 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.NewNPC((int)npc.Center.X, (int)npc.position.Y + npc.height, ModContent.NPCType<CalamitasRun3>(), npc.whoAmI, 0f, 0f, 0f, 0f, 255);
                string key = "Mods.CalamityMod.CalamitasBossText";
                Color messageColor = Color.Orange;
                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    Main.NewText(Language.GetTextValue(key), messageColor);
                }
                else if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.BroadcastChatMessage(NetworkText.FromKey(key), messageColor);
                }
                npc.active = false;
                npc.netUpdate = true;
                return;
            }

            bool revenge = CalamityWorld.revenge || CalamityWorld.bossRushActive;
            bool expertMode = Main.expertMode || CalamityWorld.bossRushActive;
            bool dayTime = Main.dayTime && !CalamityWorld.bossRushActive;
            bool provy = CalamityWorld.downedProvidence && !CalamityWorld.bossRushActive;

            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest(true);

            Player player = Main.player[npc.target];

            float num801 = npc.position.X + (float)(npc.width / 2) - player.position.X - (float)(player.width / 2);
            float num802 = npc.position.Y + (float)npc.height - 59f - player.position.Y - (float)(player.height / 2);
            float num803 = (float)Math.Atan2((double)num802, (double)num801) + 1.57f;
            if (num803 < 0f)
                num803 += 6.283f;
            else if ((double)num803 > 6.283)
                num803 -= 6.283f;

            float num804 = 0.1f;
            if (npc.rotation < num803)
            {
                if ((double)(num803 - npc.rotation) > 3.1415)
                    npc.rotation -= num804;
                else
                    npc.rotation += num804;
            }
            else if (npc.rotation > num803)
            {
                if ((double)(npc.rotation - num803) > 3.1415)
                    npc.rotation += num804;
                else
                    npc.rotation -= num804;
            }

            if (npc.rotation > num803 - num804 && npc.rotation < num803 + num804)
                npc.rotation = num803;
            if (npc.rotation < 0f)
                npc.rotation += 6.283f;
            else if ((double)npc.rotation > 6.283)
                npc.rotation -= 6.283f;
            if (npc.rotation > num803 - num804 && npc.rotation < num803 + num804)
                npc.rotation = num803;

            if (!player.active || player.dead || (dayTime && !Main.eclipse))
            {
                npc.TargetClosest(false);
                player = Main.player[npc.target];
                if (!player.active || player.dead || (dayTime && !Main.eclipse))
                {
                    npc.velocity = new Vector2(0f, -10f);
                    if (npc.timeLeft > 150)
                        npc.timeLeft = 150;

                    return;
                }
            }
            else if (npc.timeLeft < 1800)
                npc.timeLeft = 1800;

            if (npc.ai[1] == 0f)
            {
                float num823 = expertMode ? 9.5f : 8f;
                float num824 = expertMode ? 0.175f : 0.15f;
                if (provy)
                {
                    num823 *= 1.25f;
                    num824 *= 1.25f;
                }
                if (CalamityWorld.bossRushActive)
                {
                    num823 *= 1.5f;
                    num824 *= 1.5f;
                }

                Vector2 vector82 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                float num825 = player.position.X + (float)(player.width / 2) - vector82.X;
                float num826 = player.position.Y + (float)(player.height / 2) - (CalamityWorld.bossRushActive ? 400f : 300f) - vector82.Y;
                float num827 = (float)Math.Sqrt((double)(num825 * num825 + num826 * num826));
                num827 = num823 / num827;
                num825 *= num827;
                num826 *= num827;

                if (npc.velocity.X < num825)
                {
                    npc.velocity.X = npc.velocity.X + num824;
                    if (npc.velocity.X < 0f && num825 > 0f)
                        npc.velocity.X = npc.velocity.X + num824;
                }
                else if (npc.velocity.X > num825)
                {
                    npc.velocity.X = npc.velocity.X - num824;
                    if (npc.velocity.X > 0f && num825 < 0f)
                        npc.velocity.X = npc.velocity.X - num824;
                }
                if (npc.velocity.Y < num826)
                {
                    npc.velocity.Y = npc.velocity.Y + num824;
                    if (npc.velocity.Y < 0f && num826 > 0f)
                        npc.velocity.Y = npc.velocity.Y + num824;
                }
                else if (npc.velocity.Y > num826)
                {
                    npc.velocity.Y = npc.velocity.Y - num824;
                    if (npc.velocity.Y > 0f && num826 < 0f)
                        npc.velocity.Y = npc.velocity.Y - num824;
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= 300f)
                {
                    npc.ai[1] = 1f;
                    npc.ai[2] = 0f;
                    npc.ai[3] = 0f;
                    npc.TargetClosest(true);
                    npc.netUpdate = true;
                }

                vector82 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                num825 = player.position.X + (float)(player.width / 2) - vector82.X;
                num826 = player.position.Y + (float)(player.height / 2) - vector82.Y;
                npc.rotation = (float)Math.Atan2((double)num826, (double)num825) - 1.57f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.localAI[1] += 1f;
                    if (revenge)
                        npc.localAI[1] += 0.5f;

                    if (npc.localAI[1] > 180f && Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height))
                    {
                        npc.localAI[1] = 0f;
                        float num828 = CalamityWorld.bossRushActive ? 16f : (expertMode ? 13f : 10.5f);
                        int num829 = expertMode ? 28 : 35;
                        int num830 = ModContent.ProjectileType<BrimstoneLaser>();
                        num827 = (float)Math.Sqrt((double)(num825 * num825 + num826 * num826));
                        num827 = num828 / num827;
                        num825 *= num827;
                        num826 *= num827;
                        vector82.X += num825 * 12f;
                        vector82.Y += num826 * 12f;
                        Projectile.NewProjectile(vector82.X, vector82.Y, num825, num826, num830, num829 + (provy ? 30 : 0), 0f, Main.myPlayer, 0f, 0f);
                    }
                }
            }
            else
            {
                int num831 = 1;
                if (npc.position.X + (float)(npc.width / 2) < player.position.X + (float)player.width)
                    num831 = -1;

                float num832 = expertMode ? 9.5f : 8f;
                float num833 = expertMode ? 0.25f : 0.2f;
                if (provy)
                {
                    num832 *= 1.25f;
                    num833 *= 1.25f;
                }
                if (CalamityWorld.bossRushActive)
                {
                    num832 *= 1.5f;
                    num833 *= 1.5f;
                }

                Vector2 vector83 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                float num834 = player.position.X + (float)(player.width / 2) + (float)(num831 * (CalamityWorld.bossRushActive ? 460 : 360)) - vector83.X;
                float num835 = player.position.Y + (float)(player.height / 2) - vector83.Y;
                float num836 = (float)Math.Sqrt((double)(num834 * num834 + num835 * num835));
                num836 = num832 / num836;
                num834 *= num836;
                num835 *= num836;

                if (npc.velocity.X < num834)
                {
                    npc.velocity.X = npc.velocity.X + num833;
                    if (npc.velocity.X < 0f && num834 > 0f)
                        npc.velocity.X = npc.velocity.X + num833;
                }
                else if (npc.velocity.X > num834)
                {
                    npc.velocity.X = npc.velocity.X - num833;
                    if (npc.velocity.X > 0f && num834 < 0f)
                        npc.velocity.X = npc.velocity.X - num833;
                }
                if (npc.velocity.Y < num835)
                {
                    npc.velocity.Y = npc.velocity.Y + num833;
                    if (npc.velocity.Y < 0f && num835 > 0f)
                        npc.velocity.Y = npc.velocity.Y + num833;
                }
                else if (npc.velocity.Y > num835)
                {
                    npc.velocity.Y = npc.velocity.Y - num833;
                    if (npc.velocity.Y > 0f && num835 < 0f)
                        npc.velocity.Y = npc.velocity.Y - num833;
                }

                vector83 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                num834 = player.position.X + (float)(player.width / 2) - vector83.X;
                num835 = player.position.Y + (float)(player.height / 2) - vector83.Y;
                npc.rotation = (float)Math.Atan2((double)num835, (double)num834) - 1.57f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.localAI[1] += 1f;
                    if (revenge)
                        npc.localAI[1] += 0.5f;

                    if (npc.localAI[1] > 60f && Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height))
                    {
                        npc.localAI[1] = 0f;
                        float num837 = CalamityWorld.bossRushActive ? 14f : 10.5f;
                        int num838 = expertMode ? 20 : 24;
                        int num839 = ModContent.ProjectileType<BrimstoneLaser>();
                        num836 = (float)Math.Sqrt((double)(num834 * num834 + num835 * num835));
                        num836 = num837 / num836;
                        num834 *= num836;
                        num835 *= num836;
                        vector83.X += num834 * 12f;
                        vector83.Y += num835 * 12f;
                        Projectile.NewProjectile(vector83.X, vector83.Y, num834, num835, num839, num838 + (provy ? 30 : 0), 0f, Main.myPlayer, 0f, 0f);
                    }
                }

                npc.ai[2] += 1f;
                if (npc.ai[2] >= 180f)
                {
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.ai[3] = 0f;
                    npc.TargetClosest(true);
                    npc.netUpdate = true;
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            Color color24 = npc.GetAlpha(drawColor);
            Color color25 = Lighting.GetColor((int)((double)npc.position.X + (double)npc.width * 0.5) / 16, (int)(((double)npc.position.Y + (double)npc.height * 0.5) / 16.0));
            Texture2D texture2D3 = Main.npcTexture[npc.type];
            int num156 = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
            int y3 = num156 * (int)npc.frameCounter;
            Rectangle rectangle = new Rectangle(0, y3, texture2D3.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;
            int num157 = 8;
            int num158 = 2;
            int num159 = 1;
            float num160 = 0f;
            int num161 = num159;
            while (((num158 > 0 && num161 < num157) || (num158 < 0 && num161 > num157)) && Lighting.NotRetro)
            {
                Color color26 = npc.GetAlpha(color25);
                {
                    goto IL_6899;
                }
                IL_6881:
                num161 += num158;
                continue;
                IL_6899:
                float num164 = (float)(num157 - num161);
                if (num158 < 0)
                {
                    num164 = (float)(num159 - num161);
                }
                color26 *= num164 / ((float)NPCID.Sets.TrailCacheLength[npc.type] * 1.5f);
                Vector2 value4 = npc.oldPos[num161];
                float num165 = npc.rotation;
                Main.spriteBatch.Draw(texture2D3, value4 + npc.Size / 2f - Main.screenPosition + new Vector2(0, npc.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color26, num165 + npc.rotation * num160 * (float)(num161 - 1) * -(float)spriteEffects.HasFlag(SpriteEffects.FlipHorizontally).ToDirectionInt(), origin2, npc.scale, spriteEffects, 0f);
                goto IL_6881;
            }
            var something = npc.direction == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            spriteBatch.Draw(texture2D3, npc.Center - Main.screenPosition + new Vector2(0, npc.gfxOffY), npc.frame, color24, npc.rotation, npc.frame.Size() / 2, npc.scale, something, 0);
            return false;
        }

        public override bool PreNPCLoot()
        {
            return false;
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            if (npc.life > 0)
            {
                for (int k = 0; k < 5; k++)
                {
                    Dust.NewDust(npc.position, npc.width, npc.height, 235, hitDirection, -1f, 0, default, 1f);
                }
            }
            else
            {
                npc.position.X = npc.position.X + (float)(npc.width / 2);
                npc.position.Y = npc.position.Y + (float)(npc.height / 2);
                npc.width = 100;
                npc.height = 100;
                npc.position.X = npc.position.X - (float)(npc.width / 2);
                npc.position.Y = npc.position.Y - (float)(npc.height / 2);
                for (int num621 = 0; num621 < 40; num621++)
                {
                    int num622 = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 235, 0f, 0f, 100, default, 2f);
                    Main.dust[num622].velocity *= 3f;
                    if (Main.rand.NextBool(2))
                    {
                        Main.dust[num622].scale = 0.5f;
                        Main.dust[num622].fadeIn = 1f + (float)Main.rand.Next(10) * 0.1f;
                    }
                }
                for (int num623 = 0; num623 < 70; num623++)
                {
                    int num624 = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 235, 0f, 0f, 100, default, 3f);
                    Main.dust[num624].noGravity = true;
                    Main.dust[num624].velocity *= 5f;
                    num624 = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 235, 0f, 0f, 100, default, 2f);
                    Main.dust[num624].velocity *= 2f;
                }
            }
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax * 0.8f * bossLifeScale);
            npc.damage = (int)(npc.damage * 0.8f);
        }

        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
            if (CalamityWorld.revenge)
            {
                player.AddBuff(ModContent.BuffType<Horror>(), 180, true);
            }
            player.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 300, true);
        }
    }
}
