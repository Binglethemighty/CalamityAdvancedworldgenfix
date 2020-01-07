﻿using CalamityMod.CalPlayer;
using CalamityMod.Items;
using CalamityMod.NPCs;
using CalamityMod.Projectiles;
using CalamityMod.Tiles;
using CalamityMod.Tiles.Abyss;
using CalamityMod.Tiles.Astral;
using CalamityMod.Tiles.AstralDesert;
using CalamityMod.Tiles.AstralSnow;
using CalamityMod.Tiles.Crags;
using CalamityMod.Tiles.FurnitureAbyss;
using CalamityMod.Tiles.FurnitureAshen;
using CalamityMod.Tiles.FurnitureEutrophic;
using CalamityMod.Tiles.FurnitureOccult;
using CalamityMod.Tiles.FurnitureProfaned;
using CalamityMod.Tiles.FurnitureVoid;
using CalamityMod.Tiles.Ores;
using CalamityMod.Tiles.SunkenSea;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace CalamityMod
{
    public static class CalamityUtils
    {
        #region Object Extensions
        public static CalamityPlayer Calamity(this Player player) => player.GetModPlayer<CalamityPlayer>();
        public static CalamityGlobalNPC Calamity(this NPC npc) => npc.GetGlobalNPC<CalamityGlobalNPC>();
        public static CalamityGlobalItem Calamity(this Item item) => item.GetGlobalItem<CalamityGlobalItem>();
        public static CalamityGlobalProjectile Calamity(this Projectile proj) => proj.GetGlobalProjectile<CalamityGlobalProjectile>();
        #endregion

        #region Player Utilities
        public static bool InCalamity(this Player player) => player.Calamity().ZoneCalamity;
        public static bool InAstral(this Player player) => player.Calamity().ZoneAstral;
        public static bool InSunkenSea(this Player player) => player.Calamity().ZoneSunkenSea;
        public static bool InSulphur(this Player player) => player.Calamity().ZoneSulphur;
        public static bool InAbyss(this Player player, int layer = 0)
        {
            switch (layer)
            {
                case 1:
                    return player.Calamity().ZoneAbyssLayer1;

                case 2:
                    return player.Calamity().ZoneAbyssLayer2;

                case 3:
                    return player.Calamity().ZoneAbyssLayer3;

                case 4:
                    return player.Calamity().ZoneAbyssLayer4;

                default:
                    return player.Calamity().ZoneAbyss;
            }
        }
        public static bool InventoryHas(this Player player, params int[] items)
        {
            return player.inventory.Any(item => items.Contains(item.type));
        }
        #endregion

        #region NPC Utilities
        /// <summary>
        /// Allows you to set the lifeMax value of a NPC to different values based on the mode. Called instead of npc.lifeMax = X.
        /// </summary>
        /// <param name="npc">The NPC whose lifeMax value you are trying to set.</param>
        /// <param name="normal">The value lifeMax will be set to in normal mode, this value gets doubled automatically in Expert mode.</param>
        /// <param name="revengeance">The value lifeMax will be set to in Revegeneance mode.</param>
        /// <param name="death">The value lifeMax will be set to in Death mode.</param>
        /// <param name="bossRush">The value lifeMax will be set to during the Boss Rush.</param>
        /// <param name="bossRushDeath">The value lifeMax will be set to during the Boss Rush, if Death mode is active.</param>
        public static void LifeMaxNERD(this NPC npc, int normal, int? revengeance = null, int? death = null, int? bossRush = null, int? bossRushDeath = null)
        {
            npc.lifeMax = normal;

            if (bossRush.HasValue && CalamityWorld.bossRushActive)
            {
                npc.lifeMax = bossRushDeath.HasValue && CalamityWorld.death ? bossRushDeath.Value : bossRush.Value;
            }
            else if (death.HasValue && CalamityWorld.death)
            {
                npc.lifeMax = death.Value;
            }
            else if (revengeance.HasValue && CalamityWorld.revenge)
            {
                npc.lifeMax = revengeance.Value;
            }
        }
        /// <summary>
        /// Detects nearby hostile NPCs from a given point
        /// </summary>
        /// <param name="origin">The position where we wish to check for nearby NPCs</param>
        /// <param name="maxDistanceToCheck">Maximum amount of pixels to check around the origin</param>
        public static NPC ClosestNPCAt(this Vector2 origin, float maxDistanceToCheck, bool bossPriority = false)
        {
            NPC closestTarget = null;
            float distance = maxDistanceToCheck;
            if (bossPriority)
            {
                bool bossFound = false;
                for (int index = 0; index < Main.npc.Length; index++)
                {
                    //if we've found a valid boss target, ignore ALL targets which aren't bosses.
                    if (bossFound && !Main.npc[index].boss)
                        continue;
                    if (Main.npc[index].CanBeChasedBy(null, false))
                    {
                        if (Vector2.Distance(origin, Main.npc[index].Center) < distance)
                        {
                            if (Main.npc[index].boss)
                                bossFound = true;
                            distance = Vector2.Distance(origin, Main.npc[index].Center);
                            closestTarget = Main.npc[index];
                        }
                    }
                }
            }
            else
            {
                for (int index = 0; index < Main.npc.Length; index++)
                {
                    if (Main.npc[index].CanBeChasedBy(null, false) && Collision.CanHit(origin, 1, 1, Main.npc[index].Center, 1, 1))
                    {
                        if (Vector2.Distance(origin, Main.npc[index].Center) < distance)
                        {
                            distance = Vector2.Distance(origin, Main.npc[index].Center);
                            closestTarget = Main.npc[index];
                        }
                    }
                }
            }
            return closestTarget;
        }
        /// <summary>
        /// Detects nearby hostile NPCs from a given point with minion support
        /// </summary>
        /// <param name="origin">The position where we wish to check for nearby NPCs</param>
        /// <param name="maxDistanceToCheck">Maximum amount of pixels to check around the origin</param>
        /// <param name="owner">Owner of the minion</param>
        public static NPC MinionHoming(this Vector2 origin, float maxDistanceToCheck, Player owner)
        {
            if (owner.HasMinionAttackTargetNPC)
            {
                return Main.npc[owner.MinionAttackTargetNPC];
            }
            return ClosestNPCAt(origin, maxDistanceToCheck);
        }

        /// <summary>
        /// Crude anti-butcher logic based on % max health.
        /// </summary>
        /// <param name="npc">The NPC attacked.</param>
        /// <param name="damage">How much damage the attack would deal.</param>
        /// <returns>Whether or not the anti-butcher was triggered.</returns>
        public static bool AntiButcher(NPC npc, ref double damage, float healthPercent)
        {
            if (damage <= npc.lifeMax * healthPercent)
                return false;
            damage = 0D;
            return true;
        }
        #endregion

        #region Item Utilities
        public static Rectangle FixSwingHitbox(float hitboxWidth, float hitboxHeight)
        {
            Player player = Main.player[Main.myPlayer];
            Item item = player.inventory[player.selectedItem];
            float hitbox_X, hitbox_Y;
            float mountOffsetY = player.mount.PlayerOffsetHitbox;

            // Third hitbox shifting values
            if (player.itemAnimation < player.itemAnimationMax * 0.333)
            {
                float shiftX = 10f;
                if (hitboxWidth >= 92)
                    shiftX = 38f;
                else if (hitboxWidth >= 64)
                    shiftX = 28f;
                else if (hitboxWidth >= 52)
                    shiftX = 24f;
                else if (hitboxWidth > 32)
                    shiftX = 14f;
                hitbox_X = player.position.X + player.width * 0.5f + (hitboxWidth * 0.5f - shiftX) * player.direction;
                hitbox_Y = player.position.Y + 24f + mountOffsetY;
            }

            // Second hitbox shifting values
            else if (player.itemAnimation < player.itemAnimationMax * 0.666)
            {
                float shift = 10f;
                if (hitboxWidth >= 92)
                    shift = 38f;
                else if (hitboxWidth >= 64)
                    shift = 28f;
                else if (hitboxWidth >= 52)
                    shift = 24f;
                else if (hitboxWidth > 32)
                    shift = 18f;
                hitbox_X = player.position.X + (player.width * 0.5f + (hitboxWidth * 0.5f - shift) * player.direction);

                shift = 10f;
                if (hitboxHeight > 64)
                    shift = 14f;
                else if (hitboxHeight > 52)
                    shift = 12f;
                else if (hitboxHeight > 32)
                    shift = 8f;

                hitbox_Y = player.position.Y + shift + mountOffsetY;
            }

            // First hitbox shifting values
            else
            {
                float shift = 6f;
                if (hitboxWidth >= 92)
                    shift = 38f;
                else if (hitboxWidth >= 64)
                    shift = 28f;
                else if (hitboxWidth >= 52)
                    shift = 24f;
                else if (hitboxWidth >= 48)
                    shift = 18f;
                else if (hitboxWidth > 32)
                    shift = 14f;
                hitbox_X = player.position.X + player.width * 0.5f - (hitboxWidth * 0.5f - shift) * player.direction;

                shift = 10f;
                if (hitboxHeight > 64)
                    shift = 14f;
                else if (hitboxHeight > 52)
                    shift = 12f;
                else if (hitboxHeight > 32)
                    shift = 10f;
                hitbox_Y = player.position.Y + shift + mountOffsetY;
            }

            // Inversion due to grav potion
            if (player.gravDir == -1f)
            {
                hitbox_Y = player.position.Y + player.height + (player.position.Y - hitbox_Y);
            }

            // Hitbox size adjustments
            Rectangle hitbox = new Rectangle((int)hitbox_X, (int)hitbox_Y, 32, 32);
            if (item.damage >= 0 && item.type > 0 && !item.noMelee && player.itemAnimation > 0)
            {
                if (!Main.dedServ)
                {
                    hitbox = new Rectangle((int)hitbox_X, (int)hitbox_Y, (int)hitboxWidth, (int)hitboxHeight);
                }
                hitbox.Width = (int)(hitbox.Width * item.scale);
                hitbox.Height = (int)(hitbox.Height * item.scale);
                if (player.direction == -1)
                {
                    hitbox.X -= hitbox.Width;
                }
                if (player.gravDir == 1f)
                {
                    hitbox.Y -= hitbox.Height;
                }

                // Broadsword use style
                if (item.useStyle == 1)
                {
                    // Third hitbox size adjustments
                    if (player.itemAnimation < player.itemAnimationMax * 0.333)
                    {
                        if (player.direction == -1)
                        {
                            hitbox.X -= (int)(hitbox.Width * 1.4 - hitbox.Width);
                        }
                        hitbox.Width = (int)(hitbox.Width * 1.4);
                        hitbox.Y += (int)(hitbox.Height * 0.5 * player.gravDir);
                        hitbox.Height = (int)(hitbox.Height * 1.1);
                    }

                    // First hitbox size adjustments
                    else if (player.itemAnimation >= player.itemAnimationMax * 0.666)
                    {
                        if (player.direction == 1)
                        {
                            hitbox.X -= (int)(hitbox.Width * 1.2);
                        }
                        hitbox.Width *= 2;
                        hitbox.Y -= (int)((hitbox.Height * 1.4 - hitbox.Height) * player.gravDir);
                        hitbox.Height = (int)(hitbox.Height * 1.4);
                    }
                }
            }
            return hitbox;
        }
        #endregion

        #region Projectile Utilities
        public static int CountProjectiles(int Type) => Main.projectile.Count(proj => proj.type == Type && proj.active);

        public static void KillAllHostileProjectiles()
        {
            int proj;
            for (int x = 0; x < Main.maxProjectiles; x = proj + 1)
            {
                Projectile projectile = Main.projectile[x];
                if (projectile.active && projectile.hostile && !projectile.friendly && projectile.damage > 0)
                {
                    projectile.Kill();
                }
                proj = x;
            }
        }
        /// <summary>
        /// Call this function in the ai of your projectile so it can stick to enemies, also requires ModifyHitNPCSticky to be called in ModifyHitNPC
        /// </summary>
        /// <param name="projectile">The projectile you're adding sticky behaviour to</param>
        public static void StickyProjAI (Projectile projectile)
        {
            if (projectile.ai[0] == 1f)
            {
                projectile.tileCollide = false;
                int num988 = 15;
                bool flag54 = false;
                bool flag55 = false;
                projectile.localAI[0]++;
                if (projectile.localAI[0] % 30f == 0f)
                {
                    flag55 = true;
                }
                int num989 = (int)projectile.ai[1];
                if (projectile.localAI[0] >= (float)(60 * num988))
                {
                    flag54 = true;
                }
                else if (num989 < 0 || num989 >= 200)
                {
                    flag54 = true;
                }
                else if (Main.npc[num989].active && !Main.npc[num989].dontTakeDamage)
                {
                    projectile.Center = Main.npc[num989].Center - projectile.velocity * 2f;
                    projectile.gfxOffY = Main.npc[num989].gfxOffY;
                    if (flag55)
                    {
                        Main.npc[num989].HitEffect(0, 1.0);
                    }
                }
                else
                {
                    flag54 = true;
                }
                if (flag54)
                {
                    projectile.Kill();
                }
            }
        }

        /// <summary>
        /// Call this function in ModifyHitNPC to make your projectiles stick to enemies, needs StickyProjAI to be called in the AI of the projectile
        /// </summary>
        /// <param name="projectile">The projectile you're giving sticky behaviour to</param>
        /// <param name="maxStick">How many projectiles of this type can stick to one enemy</param>
        /// <param name="constantDamage">Decides if you want the projectile to deal damage while its sticked to enemies or not</param>
        public static void ModifyHitNPCSticky(Projectile projectile, int maxStick, bool constantDamage)
        {
            Rectangle myRect = new Rectangle((int)projectile.position.X, (int)projectile.position.Y, projectile.width, projectile.height);
            if (projectile.owner == Main.myPlayer)
            {
                for (int i = 0; i < 200; i++)
                {
                    if (Main.npc[i].active && !Main.npc[i].dontTakeDamage &&
                        ((projectile.friendly && (!Main.npc[i].friendly || projectile.type == 318 || (Main.npc[i].type == 22 && projectile.owner < 255 && Main.player[projectile.owner].killGuide) || (Main.npc[i].type == 54 && projectile.owner < 255 && Main.player[projectile.owner].killClothier))) ||
                        (projectile.hostile && Main.npc[i].friendly && !Main.npc[i].dontTakeDamageFromHostiles)) && (projectile.owner < 0 || Main.npc[i].immune[projectile.owner] == 0 || projectile.maxPenetrate == 1))
                    {
                        if (Main.npc[i].noTileCollide || !projectile.ownerHitCheck || projectile.CanHit(Main.npc[i]))
                        {
                            bool flag3;
                            if (Main.npc[i].type == 414)
                            {
                                Rectangle rect = Main.npc[i].getRect();
                                int num5 = 8;
                                rect.X -= num5;
                                rect.Y -= num5;
                                rect.Width += num5 * 2;
                                rect.Height += num5 * 2;
                                flag3 = projectile.Colliding(myRect, rect);
                            }
                            else
                            {
                                flag3 = projectile.Colliding(myRect, Main.npc[i].getRect());
                            }
                            if (flag3)
                            {
                                if (Main.npc[i].reflectingProjectiles && projectile.CanReflect())
                                {
                                    Main.npc[i].ReflectProjectile(projectile.whoAmI);
                                    return;
                                }
                                projectile.ai[0] = 1f;
                                projectile.ai[1] = (float)i;
                                projectile.velocity = (Main.npc[i].Center - projectile.Center) * 0.75f;
                                projectile.netUpdate = true;
                                if (!constantDamage)
                                    projectile.damage = 0;
                                Point[] array2 = new Point[maxStick];
                                int num29 = 0;
                                for (int l = 0; l < 1000; l++)
                                {
                                    if (l != projectile.whoAmI && Main.projectile[l].active && Main.projectile[l].owner == Main.myPlayer && Main.projectile[l].type == projectile.type && Main.projectile[l].ai[0] == 1f && Main.projectile[l].ai[1] == (float)i)
                                    {
                                        array2[num29++] = new Point(l, Main.projectile[l].timeLeft);
                                        if (num29 >= array2.Length)
                                        {
                                            break;
                                        }
                                    }
                                }
                                if (num29 >= array2.Length)
                                {
                                    int num30 = 0;
                                    for (int m = 1; m < array2.Length; m++)
                                    {
                                        if (array2[m].Y < array2[num30].Y)
                                        {
                                            num30 = m;
                                        }
                                    }
                                    Main.projectile[array2[num30].X].Kill();
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Tile Utilities
        public static void SafeSquareTileFrame(int x, int y, bool resetFrame = true)
        {
            if (Main.tile[x, y] is null)
                return;

            for (int xIter = x - 1; xIter <= x + 1; ++xIter)
            {
                if (xIter < 0 || xIter >= Main.maxTilesX)
                    continue;
                for (int yIter = y - 1; yIter <= y + 1; yIter++)
                {
                    if (yIter < 0 || yIter >= Main.maxTilesY)
                        continue;
                    if (xIter == x && yIter == y)
                    {
                        WorldGen.TileFrame(x, y, resetFrame, false);
                    }
                    else
                    {
                        WorldGen.TileFrame(xIter, yIter, false, false);
                    }
                }
            }
        }

        public static void LightHitWire(int type, int i, int j, int tileX, int tileY)
        {
            int x = i - Main.tile[i, j].frameX / 18 % tileX;
            int y = j - Main.tile[i, j].frameY / 18 % tileY;
            for (int l = x; l < x + tileX; l++)
            {
                for (int m = y; m < y + tileY; m++)
                {
                    if (Main.tile[l, m] == null)
                    {
                        Main.tile[l, m] = new Tile();
                    }
                    if (Main.tile[l, m].active() && Main.tile[l, m].type == type)
                    {
                        if (Main.tile[l, m].frameX < (18 * tileX))
                        {
                            Main.tile[l, m].frameX += (short)(18 * tileX);
                        }
                        else
                        {
                            Main.tile[l, m].frameX -= (short)(18 * tileX);
                        }
                    }
                }
            }
            if (Wiring.running)
            {
                for (int k = 0; k < tileX; k++)
                {
                    for (int l = 0; l < tileY; l++)
                    {
                        Wiring.SkipWire(x + k, y + l);
                    }
                }
            }
        }

        #region Tile Merge Utilities
        /// <summary>
        /// Sets the mergeability state of two tiles. By default, enables tile merging.
        /// </summary>
        /// <param name="type1">The first tile type which should merge (or not).</param>
        /// <param name="type2">The second tile type which should merge (or not).</param>
        /// <param name="merge">The mergeability state of the tiles. Defaults to true if omitted.</param>
        public static void SetMerge(int type1, int type2, bool merge = true)
        {
            if (type1 != type2)
            {
                Main.tileMerge[type1][type2] = merge;
                Main.tileMerge[type2][type1] = merge;
            }
        }

        /// <summary>
        /// Makes the first tile type argument merge with all the other tile type arguments. Also accepts arrays.
        /// </summary>
        /// <param name="myType">The tile whose merging properties will be set.</param>
        /// <param name="otherTypes">Every tile that should be merged with.</param>
        public static void MergeWithSet(int myType, params int[] otherTypes)
        {
            for (int i = 0; i < otherTypes.Length; ++i)
                SetMerge(myType, otherTypes[i]);
        }

        /// <summary>
        /// Makes the specified tile merge with the most common types of tiles found in world generation.<br></br>
        /// Notably excludes Ice.
        /// </summary>
        /// <param name="type">The tile whose merging properties will be set.</param>
        public static void MergeWithGeneral(int type) => MergeWithSet(type, new int[] {
            // Soils
            TileID.Dirt,
            TileID.Mud,
            TileID.ClayBlock,
            // Stones
            TileID.Stone,
            TileID.Ebonstone,
            TileID.Crimstone,
            TileID.Pearlstone,
            // Sands
            TileID.Sand,
            TileID.Ebonsand,
            TileID.Crimsand,
            TileID.Pearlsand,
            // Snows
            TileID.SnowBlock,
            // Calamity Tiles
            ModContent.TileType<AstralDirt>(),
            ModContent.TileType<AstralStone>(),
            ModContent.TileType<Navystone>(),
            ModContent.TileType<EutrophicSand>(),
            ModContent.TileType<AbyssGravel>(),
            ModContent.TileType<Voidstone>(),
        });

        /// <summary>
        /// Makes the specified tile merge with all ores, vanilla and Calamity. Particularly useful for stone blocks.
        /// </summary>
        /// <param name="type">The tile whose merging properties will be set.</param>
        public static void MergeWithOres(int type) => MergeWithSet(type, new int[] {
            // Vanilla Ores
            TileID.Copper,
            TileID.Tin,
            TileID.Iron,
            TileID.Lead,
            TileID.Silver,
            TileID.Tungsten,
            TileID.Gold,
            TileID.Platinum,
            TileID.Demonite,
            TileID.Crimtane,
            TileID.Cobalt,
            TileID.Palladium,
            TileID.Mythril,
            TileID.Orichalcum,
            TileID.Adamantite,
            TileID.Titanium,
            TileID.LunarOre,
            // Calamity Ores
            ModContent.TileType<AerialiteOre>(),
            ModContent.TileType<CryonicOre>(),
            ModContent.TileType<PerennialOre>(),
            ModContent.TileType<CharredOre>(),
            ModContent.TileType<ChaoticOre>(),
            ModContent.TileType<AstralOre>(),
            ModContent.TileType<UelibloomOre>(),
            ModContent.TileType<AuricOre>(),
        });

        /// <summary>
        /// Makes the specified tile merge with all types of desert tiles, including the Calamity Sunken Sea.
        /// </summary>
        /// <param name="type">The tile whose merging properties will be set.</param>
        public static void MergeWithDesert(int type) => MergeWithSet(type, new int[] {
            // Sands
            TileID.Sand,
            TileID.Ebonsand,
            TileID.Crimsand,
            TileID.Pearlsand,
            // Hardened Sands
            TileID.HardenedSand,
            TileID.CorruptHardenedSand,
            TileID.CrimsonHardenedSand,
            TileID.HallowHardenedSand,
            // Sandstones
            TileID.Sandstone,
            TileID.CorruptSandstone,
            TileID.CrimsonSandstone,
            TileID.HallowSandstone,
            // Miscellaneous Desert Tiles
            TileID.FossilOre,
            TileID.DesertFossil,
            // Astral Desert
            ModContent.TileType<AstralSand>(),
            ModContent.TileType<HardenedAstralSand>(),
            ModContent.TileType<AstralSandstone>(),
            // Sunken Sea
            ModContent.TileType<EutrophicSand>(),
            ModContent.TileType<Navystone>(),
            ModContent.TileType<SeaPrism>(),
        });

        /// <summary>
        /// Makes the specified tile merge with all types of snow and ice tiles.
        /// </summary>
        /// <param name="type">The tile whose merging properties will be set.</param>
        public static void MergeWithSnow(int type) => MergeWithSet(type, new int[] {
            // Snows
            TileID.SnowBlock,
            // Ices
            TileID.IceBlock,
            TileID.CorruptIce,
            TileID.FleshIce,
            TileID.HallowedIce,
            // Astral Snow
            ModContent.TileType<AstralIce>(),
            ModContent.TileType<AstralSnow>(),
        });

        /// <summary>
        /// Makes the specified tile merge with all tiles which generate in hell. Does not include Charred Ore.
        /// </summary>
        /// <param name="type">The tile whose merging properties will be set.</param>
        public static void MergeWithHell(int type) => MergeWithSet(type, new int[] {
            TileID.Ash,
            TileID.Hellstone,
            TileID.ObsidianBrick,
            TileID.HellstoneBrick,
            ModContent.TileType<BrimstoneSlag>(),
        });

        /// <summary>
        /// Makes the specified tile merge with all tiles which generate in the Abyss or the Sulphurous Sea. Includes Chaotic Ore.
        /// </summary>
        /// <param name="type">The tile whose merging properties will be set.</param>
        public static void MergeWithAbyss(int type) => MergeWithSet(type, new int[] {
            // Sulphurous Sea
            ModContent.TileType<SulphurousSand>(),
            // Abyss
            ModContent.TileType<AbyssGravel>(),
            ModContent.TileType<Voidstone>(),
            ModContent.TileType<PlantyMush>(),
            ModContent.TileType<Tenebris>(),
            ModContent.TileType<ChaoticOre>(),
        });

        /// <summary>
        /// Makes the tile merge with all the tile types that generate within various types of astral tiles
        /// </summary>
        /// <param name="type"></param>
        public static void MergeAstralTiles(int type)
        {
            //Astral
            SetMerge(type, ModContent.TileType<AstralDirt>());
            SetMerge(type, ModContent.TileType<AstralStone>());
            SetMerge(type, ModContent.TileType<AstralMonolith>());
            //Astral Desert
            SetMerge(type, ModContent.TileType<AstralSand>());
            SetMerge(type, ModContent.TileType<HardenedAstralSand>());
            SetMerge(type, ModContent.TileType<AstralSandstone>());
            //Astral Snow
            SetMerge(type, ModContent.TileType<AstralIce>());
            SetMerge(type, ModContent.TileType<AstralSnow>());
        }

        /// <summary>
        /// Makes the tile merge with all the decorative 'smooth' tiles
        /// </summary>
        /// <param name="type"></param>
        public static void MergeSmoothTiles(int type)
        {
            //Vanilla
            SetMerge(type, TileID.MarbleBlock);
            SetMerge(type, TileID.GraniteBlock);
            //Calam
            SetMerge(type, ModContent.TileType<SmoothNavystone>());
            SetMerge(type, ModContent.TileType<SmoothBrimstoneSlag>());
            SetMerge(type, ModContent.TileType<SmoothAbyssGravel>());
            SetMerge(type, ModContent.TileType<SmoothVoidstone>());
        }

        /// <summary>
        /// Makes the tile merge with other mergable decorative tiles
        /// </summary>
        /// <param name="type"></param>
        public static void MergeDecorativeTiles(int type)
        {
            //Vanilla decor
            Main.tileBrick[type] = true;
            //Calam
            SetMerge(type, ModContent.TileType<CryonicBrick>());
            SetMerge(type, ModContent.TileType<PerennialBrick>());
            SetMerge(type, ModContent.TileType<UelibloomBrick>());
            SetMerge(type, ModContent.TileType<OccultStone>());
            SetMerge(type, ModContent.TileType<ProfanedSlab>());
            SetMerge(type, ModContent.TileType<RunicProfanedBrick>());
            SetMerge(type, ModContent.TileType<AshenSlab>());
            SetMerge(type, ModContent.TileType<VoidstoneSlab>());
        }
        #endregion

        #region Furniture Interaction
        public static bool BedRightClick(int i, int j)
        {
            Player player = Main.LocalPlayer;
            Tile tile = Main.tile[i, j];
            int spawnX = i - tile.frameX / 18;
            int spawnY = j + 2;
            spawnX += tile.frameX >= 72 ? 5 : 2;
            if (tile.frameY % 38 != 0)
            {
                spawnY--;
            }
            player.FindSpawn();
            if (player.SpawnX == spawnX && player.SpawnY == spawnY)
            {
                player.RemoveSpawn();
                Main.NewText("Spawn point removed!", 255, 240, 20, false);
            }
            else if (Player.CheckSpawn(spawnX, spawnY))
            {
                player.ChangeSpawn(spawnX, spawnY);
                Main.NewText("Spawn point set!", 255, 240, 20, false);
            }
            return true;
        }

        public static bool ChestRightClick(int i, int j)
        {
            Player player = Main.LocalPlayer;
            Tile tile = Main.tile[i, j];
            Main.mouseRightRelease = false;
            int left = i;
            int top = j;
            if (tile.frameX % 36 != 0)
            {
                left--;
            }
            if (tile.frameY != 0)
            {
                top--;
            }
            if (player.sign >= 0)
            {
                Main.PlaySound(SoundID.MenuClose);
                player.sign = -1;
                Main.editSign = false;
                Main.npcChatText = "";
            }
            if (Main.editChest)
            {
                Main.PlaySound(SoundID.MenuTick);
                Main.editChest = false;
                Main.npcChatText = "";
            }
            if (player.editedChestName)
            {
                NetMessage.SendData(33, -1, -1, NetworkText.FromLiteral(Main.chest[player.chest].name), player.chest, 1f, 0f, 0f, 0, 0, 0);
                player.editedChestName = false;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                if (left == player.chestX && top == player.chestY && player.chest >= 0)
                {
                    player.chest = -1;
                    Recipe.FindRecipes();
                    Main.PlaySound(SoundID.MenuClose);
                }
                else
                {
                    NetMessage.SendData(31, -1, -1, null, left, (float)top, 0f, 0f, 0, 0, 0);
                    Main.stackSplit = 600;
                }
            }
            else
            {
                int chest = Chest.FindChest(left, top);
                if (chest >= 0)
                {
                    Main.stackSplit = 600;
                    if (chest == player.chest)
                    {
                        player.chest = -1;
                        Main.PlaySound(SoundID.MenuClose);
                    }
                    else
                    {
                        player.chest = chest;
                        Main.playerInventory = true;
                        Main.recBigList = false;
                        player.chestX = left;
                        player.chestY = top;
                        Main.PlaySound(player.chest < 0 ? SoundID.MenuOpen : SoundID.MenuTick);
                    }
                    Recipe.FindRecipes();
                }
            }
            return true;
        }

        public static void ChestMouseOver<T>(string chestName, int i, int j) where T : ModItem
        {
            Player player = Main.LocalPlayer;
            Tile tile = Main.tile[i, j];
            int left = i;
            int top = j;
            if (tile.frameX % 36 != 0)
            {
                left--;
            }
            if (tile.frameY != 0)
            {
                top--;
            }
            int chest = Chest.FindChest(left, top);
            player.showItemIcon2 = -1;
            if (chest < 0)
            {
                player.showItemIconText = Language.GetTextValue("LegacyChestType.0");
            }
            else
            {
                player.showItemIconText = Main.chest[chest].name.Length > 0 ? Main.chest[chest].name : chestName;
                if (player.showItemIconText == chestName)
                {
                    player.showItemIcon2 = ModContent.ItemType<T>();
                    player.showItemIconText = "";
                }
            }
            player.noThrow = 2;
            player.showItemIcon = true;
        }

        public static void ChestMouseFar<T>(string name, int i, int j) where T : ModItem
        {
            ChestMouseOver<T>(name, i, j);
            Player player = Main.LocalPlayer;
            if (player.showItemIconText == "")
            {
                player.showItemIcon = false;
                player.showItemIcon2 = 0;
            }
        }

        public static bool ClockRightClick()
        {
            string text = "AM";

            // Get Terraria's current strange time variable
            double time = Main.time;

            // Correct for night time (which for some reason isn't just a different number) by adding 54000.
            if (!Main.dayTime)
                time += 54000D;

            // Divide by seconds in an hour
            time /= 3600D;

            // Terraria night starts at 7:30 PM, so offset accordingly
            time -= 19.5;

            // Offset time to ensure it is not negative, then change to PM if necessary
            if (time < 0D)
                time += 24D;
            if (time >= 12D)
                text = "PM";

            // Get the decimal (smaller than hours, so minutes) component of time.
            int intTime = (int)time;
            double deltaTime = time - intTime;

            // Convert decimal time into an exact number of minutes.
            deltaTime = (int)(deltaTime * 60D);

            string minuteText = deltaTime.ToString();

            // Ensure minutes has a leading zero
            if (deltaTime < 10D)
                minuteText = "0" + minuteText;

            // Convert from 24 to 12 hour time (PM already handled earlier)
            if (intTime > 12)
                intTime -= 12;
            // 12 AM is actually hour zero in 24 hour time
            if (intTime == 0)
                intTime = 12;

            // Create an overall time readout and send it to chat
            var newText = string.Concat("Time: ", intTime, ":", minuteText, " ", text);
            Main.NewText(newText, 255, 240, 20);
            return true;
        }

        public static bool DresserRightClick()
        {
            Player player = Main.LocalPlayer;
            if (Main.tile[Player.tileTargetX, Player.tileTargetY].frameY == 0)
            {
                Main.CancelClothesWindow(true);

                int left = (int)(Main.tile[Player.tileTargetX, Player.tileTargetY].frameX / 18);
                left %= 3;
                left = Player.tileTargetX - left;
                int top = Player.tileTargetY - (int)(Main.tile[Player.tileTargetX, Player.tileTargetY].frameY / 18);
                if (player.sign > -1)
                {
                    Main.PlaySound(SoundID.MenuClose);
                    player.sign = -1;
                    Main.editSign = false;
                    Main.npcChatText = string.Empty;
                }
                if (Main.editChest)
                {
                    Main.PlaySound(SoundID.MenuTick);
                    Main.editChest = false;
                    Main.npcChatText = string.Empty;
                }
                if (player.editedChestName)
                {
                    NetMessage.SendData(MessageID.SyncPlayerChest, -1, -1, NetworkText.FromLiteral(Main.chest[player.chest].name), player.chest, 1f, 0f, 0f, 0, 0, 0);
                    player.editedChestName = false;
                }
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    if (left == player.chestX && top == player.chestY && player.chest != -1)
                    {
                        player.chest = -1;
                        Recipe.FindRecipes();
                        Main.PlaySound(SoundID.MenuClose);
                    }
                    else
                    {
                        NetMessage.SendData(MessageID.RequestChestOpen, -1, -1, null, left, (float)top, 0f, 0f, 0, 0, 0);
                        Main.stackSplit = 600;
                    }
                    return true;
                }
                else
                {
                    player.flyingPigChest = -1;
                    int num213 = Chest.FindChest(left, top);
                    if (num213 != -1)
                    {
                        Main.stackSplit = 600;
                        if (num213 == player.chest)
                        {
                            player.chest = -1;
                            Recipe.FindRecipes();
                            Main.PlaySound(SoundID.MenuClose);
                        }
                        else if (num213 != player.chest && player.chest == -1)
                        {
                            player.chest = num213;
                            Main.playerInventory = true;
                            Main.recBigList = false;
                            Main.PlaySound(SoundID.MenuOpen);
                            player.chestX = left;
                            player.chestY = top;
                        }
                        else
                        {
                            player.chest = num213;
                            Main.playerInventory = true;
                            Main.recBigList = false;
                            Main.PlaySound(SoundID.MenuTick);
                            player.chestX = left;
                            player.chestY = top;
                        }
                        Recipe.FindRecipes();
                        return true;
                    }
                }
            }
            else
            {
                Main.playerInventory = false;
                player.chest = -1;
                Recipe.FindRecipes();
                Main.dresserX = Player.tileTargetX;
                Main.dresserY = Player.tileTargetY;
                Main.OpenClothesWindow();
                return true;
            }

            return false;
        }

        public static void DresserMouseFar<T>(string chestName) where T : ModItem
        {
            Player player = Main.LocalPlayer;
            Tile tile = Main.tile[Player.tileTargetX, Player.tileTargetY];
            int left = Player.tileTargetX;
            int top = Player.tileTargetY;
            left -= (int)(tile.frameX % 54 / 18);
            if (tile.frameY % 36 != 0)
            {
                top--;
            }
            int chestIndex = Chest.FindChest(left, top);
            player.showItemIcon2 = -1;
            if (chestIndex < 0)
            {
                player.showItemIconText = Language.GetTextValue("LegacyDresserType.0");
            }
            else
            {
                if (Main.chest[chestIndex].name != "")
                {
                    player.showItemIconText = Main.chest[chestIndex].name;
                }
                else
                {
                    player.showItemIconText = chestName;
                }
                if (player.showItemIconText == chestName)
                {
                    player.showItemIcon2 = ModContent.ItemType<T>();
                    player.showItemIconText = "";
                }
            }
            player.noThrow = 2;
            player.showItemIcon = true;
            if (player.showItemIconText == "")
            {
                player.showItemIcon = false;
                player.showItemIcon2 = 0;
            }
        }

        public static void DresserMouseOver<T>(string chestName) where T : ModItem
        {
            Player player = Main.LocalPlayer;
            Tile tile = Main.tile[Player.tileTargetX, Player.tileTargetY];
            int left = Player.tileTargetX;
            int top = Player.tileTargetY;
            left -= (int)(tile.frameX % 54 / 18);
            if (tile.frameY % 36 != 0)
            {
                top--;
            }
            int chestIndex = Chest.FindChest(left, top);
            player.showItemIcon2 = -1;
            if (chestIndex < 0)
            {
                player.showItemIconText = Language.GetTextValue("LegacyDresserType.0");
            }
            else
            {
                if (Main.chest[chestIndex].name != "")
                {
                    player.showItemIconText = Main.chest[chestIndex].name;
                }
                else
                {
                    player.showItemIconText = chestName;
                }
                if (player.showItemIconText == chestName)
                {
                    player.showItemIcon2 = ModContent.ItemType<T>();
                    player.showItemIconText = "";
                }
            }
            player.noThrow = 2;
            player.showItemIcon = true;
            if (Main.tile[Player.tileTargetX, Player.tileTargetY].frameY > 0)
            {
                player.showItemIcon2 = ItemID.FamiliarShirt;
            }
        }

        public static bool LockedChestRightClick(bool isLocked, int left, int top, int i, int j)
        {
            Player player = Main.LocalPlayer;

            // If the player right clicked the chest while editing a sign, finish that up
            if (player.sign >= 0)
            {
                Main.PlaySound(SoundID.MenuClose);
                player.sign = -1;
                Main.editSign = false;
                Main.npcChatText = "";
            }

            // If the player right clicked the chest while editing a chest, finish that up
            if (Main.editChest)
            {
                Main.PlaySound(SoundID.MenuTick);
                Main.editChest = false;
                Main.npcChatText = "";
            }

            // If the player right clicked the chest after changing another chest's name, finish that up
            if (player.editedChestName)
            {
                NetMessage.SendData(MessageID.SyncPlayerChest, -1, -1, NetworkText.FromLiteral(Main.chest[player.chest].name), player.chest, 1f, 0f, 0f, 0, 0, 0);
                player.editedChestName = false;
            }
            if (Main.netMode == 1 && !isLocked)
            {
                // Right clicking the chest you currently have open closes it. This counts as interaction.
                if (left == player.chestX && top == player.chestY && player.chest >= 0)
                {
                    player.chest = -1;
                    Recipe.FindRecipes();
                    Main.PlaySound(SoundID.MenuClose);
                }

                // Right clicking this chest opens it if it's not already open. This counts as interaction.
                else
                {
                    NetMessage.SendData(MessageID.RequestChestOpen, -1, -1, null, left, (float)top, 0f, 0f, 0, 0, 0);
                    Main.stackSplit = 600;
                }
                return true;
            }

            else
            {
                if (isLocked)
                {
                    // If you right click the locked chest and you can unlock it, it unlocks itself but does not open. This counts as interaction.
                    if (Chest.Unlock(left, top))
                    {
                        if (Main.netMode == 1)
                        {
                            NetMessage.SendData(MessageID.Unlock, -1, -1, null, player.whoAmI, 1f, (float)left, (float)top);
                        }
                        return true;
                    }
                }
                else
                {
                    int chest = Chest.FindChest(left, top);
                    if (chest >= 0)
                    {
                        Main.stackSplit = 600;

                        // If you right click the same chest you already have open, it closes. This counts as interaction.
                        if (chest == player.chest)
                        {
                            player.chest = -1;
                            Main.PlaySound(SoundID.MenuClose);
                        }

                        // If you right click this chest when you have a different chest selected, that one closes and this one opens. This counts as interaction.
                        else
                        {
                            player.chest = chest;
                            Main.playerInventory = true;
                            Main.recBigList = false;
                            player.chestX = left;
                            player.chestY = top;
                            Main.PlaySound(player.chest < 0 ? SoundID.MenuOpen : SoundID.MenuTick);
                        }

                        Recipe.FindRecipes();
                        return true;
                    }
                }
            }

            // This only occurs when the chest is locked and cannot be unlocked. You did not interact with the chest.
            return false;
        }

        public static void LockedChestMouseOver<K, C>(string chestName, int i, int j)
            where K : ModItem where C : ModItem
        {
            Player player = Main.LocalPlayer;
            Tile tile = Main.tile[i, j];
            int left = i;
            int top = j;
            if (tile.frameX % 36 != 0)
            {
                left--;
            }
            if (tile.frameY != 0)
            {
                top--;
            }
            int chest = Chest.FindChest(left, top);
            player.showItemIcon2 = -1;
            if (chest < 0)
            {
                player.showItemIconText = Language.GetTextValue("LegacyChestType.0");
            }
            else
            {
                player.showItemIconText = Main.chest[chest].name.Length > 0 ? Main.chest[chest].name : chestName;
                if (player.showItemIconText == chestName)
                {
                    player.showItemIcon2 = ModContent.ItemType<C>();
                    if (Main.tile[left, top].frameX / 36 == 1)
                        player.showItemIcon2 = ModContent.ItemType<K>();
                    player.showItemIconText = "";
                }
            }
            player.noThrow = 2;
            player.showItemIcon = true;
        }

        public static void LockedChestMouseOverFar<K, C>(string chestName, int i, int j)
            where K : ModItem where C : ModItem
        {
            LockedChestMouseOver<K, C>(chestName, i, j);
            Player player = Main.LocalPlayer;
            if (player.showItemIconText == "")
            {
                player.showItemIcon = false;
                player.showItemIcon2 = 0;
            }
        }
        #endregion

        #region Furniture SetDefaults
        /// <summary>
        /// Extension which initializes a ModTile to be a bathtub.
        /// </summary>
        /// <param name="mt">The ModTile which is being initialized.</param>
        /// <param name="lavaImmune">Whether this tile is supposed to be immune to lava. Defaults to false.</param>
        internal static void SetUpBathtub(this ModTile mt, bool lavaImmune = false)
        {
            Main.tileLighted[mt.Type] = true;
            Main.tileFrameImportant[mt.Type] = true;
            Main.tileLavaDeath[mt.Type] = !lavaImmune;
            Main.tileWaterDeath[mt.Type] = false;
            TileObjectData.newTile.Width = 4;
            TileObjectData.newTile.Height = 2;
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 18 };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.Origin = new Point16(1, 1);
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 4, 0);
            TileObjectData.newTile.LavaDeath = !lavaImmune;
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
            TileObjectData.addAlternate(1);
            TileObjectData.addTile(mt.Type);

            // All bathtubs count as tables.
            mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);
        }

        /// <summary>
        /// Extension which initializes a ModTile to be a bed.
        /// </summary>
        /// <param name="mt">The ModTile which is being initialized.</param>
        /// <param name="lavaImmune">Whether this tile is supposed to be immune to lava. Defaults to false.</param>
        internal static void SetUpBed(this ModTile mt, bool lavaImmune = false)
        {
            Main.tileFrameImportant[mt.Type] = true;
            Main.tileLavaDeath[mt.Type] = !lavaImmune;
            Main.tileWaterDeath[mt.Type] = false;
            TileID.Sets.HasOutlines[mt.Type] = true;
            TileObjectData.newTile.Width = 4;
            TileObjectData.newTile.Height = 2;
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 18 };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.Origin = new Point16(1, 1);
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 4, 0);
            TileObjectData.newTile.LavaDeath = !lavaImmune;
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
            TileObjectData.addAlternate(1);
            TileObjectData.addTile(mt.Type);

            // All beds count as chairs.
            mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsChair);
        }

        /// <summary>
        /// Extension which initializes a ModTile to be a bookcase.
        /// </summary>
        /// <param name="mt">The ModTile which is being initialized.</param>
        /// <param name="lavaImmune">Whether this tile is supposed to be immune to lava. Defaults to false.</param>
        internal static void SetUpBookcase(this ModTile mt, bool lavaImmune = false)
        {
            Main.tileSolidTop[mt.Type] = true;
            Main.tileLighted[mt.Type] = true;
            Main.tileFrameImportant[mt.Type] = true;
            Main.tileTable[mt.Type] = true;
            Main.tileLavaDeath[mt.Type] = !lavaImmune;
            Main.tileWaterDeath[mt.Type] = false;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4);
            TileObjectData.newTile.LavaDeath = !lavaImmune;
            TileObjectData.addTile(mt.Type);

            // All bookcases count as tables.
            mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);
        }

        /// <summary>
        /// Extension which initializes a ModTile to be a candelabra.
        /// </summary>
        /// <param name="mt">The ModTile which is being initialized.</param>
        /// <param name="lavaImmune">Whether this tile is supposed to be immune to lava. Defaults to false.</param>
        internal static void SetUpCandelabra(this ModTile mt, bool lavaImmune = false)
        {
            Main.tileLighted[mt.Type] = true;
            Main.tileFrameImportant[mt.Type] = true;
            Main.tileLavaDeath[mt.Type] = true;
            Main.tileWaterDeath[mt.Type] = false;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.LavaDeath = !lavaImmune;
            TileObjectData.addTile(mt.Type);

            // All candelabras count as light sources.
            mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
        }

        /// <summary>
        /// Extension which initializes a ModTile to be a candle.
        /// </summary>
        /// <param name="mt">The ModTile which is being initialized.</param>
        /// <param name="lavaImmune">Whether this tile is supposed to be immune to lava. Defaults to false.</param>
        internal static void SetUpCandle(this ModTile mt, bool lavaImmune = false)
        {
            Main.tileLighted[mt.Type] = true;
            Main.tileFrameImportant[mt.Type] = true;
            Main.tileLavaDeath[mt.Type] = !lavaImmune;
            Main.tileWaterDeath[mt.Type] = false;
            TileObjectData.newTile.CopyFrom(TileObjectData.StyleOnTable1x1);
            TileObjectData.newTile.CoordinateHeights = new int[] { 20 };
            TileObjectData.newTile.LavaDeath = !lavaImmune;
            TileObjectData.addTile(mt.Type);

            // All candles count as light sources.
            mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
        }

        /// <summary>
        /// Extension which initializes a ModTile to be a chair.
        /// </summary>
        /// <param name="mt">The ModTile which is being initialized.</param>
        /// <param name="lavaImmune">Whether this tile is supposed to be immune to lava. Defaults to false.</param>
        internal static void SetUpChair(this ModTile mt, bool lavaImmune = false)
        {
            Main.tileFrameImportant[mt.Type] = true;
            Main.tileNoAttach[mt.Type] = true;
            Main.tileLavaDeath[mt.Type] = !lavaImmune;
            Main.tileWaterDeath[mt.Type] = false;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 18 };
            TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
            TileObjectData.newTile.StyleWrapLimit = 2;
            TileObjectData.newTile.StyleMultiplier = 2;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = !lavaImmune;
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
            TileObjectData.addAlternate(1);
            TileObjectData.addTile(mt.Type);

            // As you could probably guess, all chairs count as chairs.
            mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsChair);
        }

        /// <summary>
        /// Extension which initializes a ModTile to be a chandelier.
        /// </summary>
        /// <param name="mt">The ModTile which is being initialized.</param>
        /// <param name="lavaImmune">Whether this tile is supposed to be immune to lava. Defaults to false.</param>
        internal static void SetUpChandelier(this ModTile mt, bool lavaImmune = false)
        {
            Main.tileLighted[mt.Type] = true;
            Main.tileFrameImportant[mt.Type] = true;
            Main.tileNoAttach[mt.Type] = true;
            Main.tileLavaDeath[mt.Type] = !lavaImmune;
            Main.tileWaterDeath[mt.Type] = false;
            TileObjectData.newTile.Width = 3;
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16, 16 };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.Origin = new Point16(1, 0);
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile, 1, 1);
            TileObjectData.newTile.LavaDeath = !lavaImmune;
            TileObjectData.addTile(mt.Type);

            // All chandeliers count as light sources.
            mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
        }

        /// <summary>
        /// Extension which initializes a ModTile to be a chest.
        /// </summary>
        /// <param name="mt">The ModTile which is being initialized.</param>
        internal static void SetUpChest(this ModTile mt)
        {
            Main.tileSpelunker[mt.Type] = true;
            Main.tileContainer[mt.Type] = true;
            Main.tileShine2[mt.Type] = true;
            Main.tileShine[mt.Type] = 1200;
            Main.tileFrameImportant[mt.Type] = true;
            Main.tileNoAttach[mt.Type] = true;
            Main.tileValue[mt.Type] = 500;
            TileID.Sets.HasOutlines[mt.Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Origin = new Point16(0, 1);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 18 };
            TileObjectData.newTile.HookCheck = new PlacementHook(new Func<int, int, int, int, int, int>(Chest.FindEmptyChest), -1, 0, true);
            TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(new Func<int, int, int, int, int, int>(Chest.AfterPlacement_Hook), -1, 0, false);
            TileObjectData.newTile.AnchorInvalidTiles = new int[] { 127 };
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
            TileObjectData.addTile(mt.Type);
        }

        /// <summary>
        /// Extension which initializes a ModTile to be a clock.
        /// </summary>
        /// <param name="mt">The ModTile which is being initialized.</param>
        /// <param name="lavaImmune">Whether this tile is supposed to be immune to lava. Defaults to false.</param>
        internal static void SetUpClock(this ModTile mt, bool lavaImmune = false)
        {
            Main.tileFrameImportant[mt.Type] = true;
            Main.tileNoAttach[mt.Type] = true;
            Main.tileLavaDeath[mt.Type] = !lavaImmune;
            TileID.Sets.HasOutlines[mt.Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
            TileObjectData.newTile.Height = 5;
            TileObjectData.newTile.CoordinateHeights = new int[]
            {
                16,
                16,
                16,
                16,
                16
            };
            TileObjectData.newTile.Origin = new Point16(0, 4);
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.LavaDeath = !lavaImmune;
            TileObjectData.addTile(mt.Type);
        }

        /// <summary>
        /// Extension which initializes a ModTile to be a closed door.
        /// </summary>
        /// <param name="mt">The ModTile which is being initialized.</param>
        /// <param name="lavaImmune">Whether this tile is supposed to be immune to lava. Defaults to false.</param>
        internal static void SetUpDoorClosed(this ModTile mt, bool lavaImmune = false)
        {
            Main.tileFrameImportant[mt.Type] = true;
            Main.tileBlockLight[mt.Type] = true;
            Main.tileSolid[mt.Type] = true;
            Main.tileNoAttach[mt.Type] = true;
            Main.tileLavaDeath[mt.Type] = !lavaImmune;
            Main.tileWaterDeath[mt.Type] = false;
            TileID.Sets.NotReallySolid[mt.Type] = true;
            TileID.Sets.DrawsWalls[mt.Type] = true;
            TileID.Sets.HasOutlines[mt.Type] = true;
            TileObjectData.newTile.Width = 1;
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.Origin = new Point16(0, 0);
            TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.LavaDeath = true;
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16, 16 };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.LavaDeath = !lavaImmune;
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.Origin = new Point16(0, 1);
            TileObjectData.addAlternate(0);
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.Origin = new Point16(0, 2);
            TileObjectData.addAlternate(0);
            TileObjectData.addTile(mt.Type);

            // As you could probably guess, all closed doors count as doors.
            mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsDoor);
        }

        /// <summary>
        /// Extension which initializes a ModTile to be an open door.
        /// </summary>
        /// <param name="mt">The ModTile which is being initialized.</param>
        /// <param name="lavaImmune">Whether this tile is supposed to be immune to lava. Defaults to false.</param>
        internal static void SetUpDoorOpen(this ModTile mt, bool lavaImmune = false)
        {
            Main.tileFrameImportant[mt.Type] = true;
            Main.tileSolid[mt.Type] = false;
            Main.tileLavaDeath[mt.Type] = !lavaImmune;
            Main.tileWaterDeath[mt.Type] = false;
            Main.tileNoSunLight[mt.Type] = true;
            TileObjectData.newTile.Width = 2;
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.Origin = new Point16(0, 0);
            TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile, 1, 0);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 0);
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.LavaDeath = true;
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16, 16 };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.StyleMultiplier = 2;
            TileObjectData.newTile.StyleWrapLimit = 2;
            TileObjectData.newTile.Direction = TileObjectDirection.PlaceRight;
            TileObjectData.newTile.LavaDeath = !lavaImmune;
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.Origin = new Point16(0, 1);
            TileObjectData.addAlternate(0);
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.Origin = new Point16(0, 2);
            TileObjectData.addAlternate(0);
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.Origin = new Point16(1, 0);
            TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.SolidTile, 1, 1);
            TileObjectData.newAlternate.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 1);
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceLeft;
            TileObjectData.addAlternate(1);
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.Origin = new Point16(1, 1);
            TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.SolidTile, 1, 1);
            TileObjectData.newAlternate.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 1);
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceLeft;
            TileObjectData.addAlternate(1);
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.Origin = new Point16(1, 2);
            TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.SolidTile, 1, 1);
            TileObjectData.newAlternate.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 1);
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceLeft;
            TileObjectData.addAlternate(1);
            TileObjectData.addTile(mt.Type);
            TileID.Sets.HousingWalls[mt.Type] = true;
            TileID.Sets.HasOutlines[mt.Type] = true;

            // As you could probably guess, all open doors count as doors.
            mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsDoor);
        }

        /// <summary>
        /// Extension which initializes a ModTile to be a dresser.
        /// </summary>
        /// <param name="mt">The ModTile which is being initialized.</param>
        internal static void SetUpDresser(this ModTile mt)
        {
            Main.tileSolidTop[mt.Type] = true;
            Main.tileFrameImportant[mt.Type] = true;
            Main.tileNoAttach[mt.Type] = true;
            Main.tileTable[mt.Type] = true;
            Main.tileContainer[mt.Type] = true;
            Main.tileWaterDeath[mt.Type] = false;
            Main.tileLavaDeath[mt.Type] = false;
            TileID.Sets.HasOutlines[mt.Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.newTile.Origin = new Point16(1, 1);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16 };
            TileObjectData.newTile.HookCheck = new PlacementHook(new Func<int, int, int, int, int, int>(Chest.FindEmptyChest), -1, 0, true);
            TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(new Func<int, int, int, int, int, int>(Chest.AfterPlacement_Hook), -1, 0, false);
            TileObjectData.newTile.AnchorInvalidTiles = new int[] { 127 };
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
            TileObjectData.addTile(mt.Type);

            // All dressers count as tables.
            mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);
        }

        /// <summary>
        /// Extension which initializes a ModTile to be a floor lamp.
        /// </summary>
        /// <param name="mt">The ModTile which is being initialized.</param>
        /// <param name="lavaImmune">Whether this tile is supposed to be immune to lava. Defaults to false.</param>
        internal static void SetUpLamp(this ModTile mt, bool lavaImmune = false)
        {
            Main.tileLighted[mt.Type] = true;
            Main.tileFrameImportant[mt.Type] = true;
            Main.tileLavaDeath[mt.Type] = !lavaImmune;
            Main.tileWaterDeath[mt.Type] = false;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style1xX);
            TileObjectData.newTile.LavaDeath = !lavaImmune;
            TileObjectData.addTile(mt.Type);

            // All floor lamps count as light sources.
            mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
        }

        /// <summary>
        /// Extension which initializes a ModTile to be a hanging lantern.
        /// </summary>
        /// <param name="mt">The ModTile which is being initialized.</param>
        /// <param name="lavaImmune">Whether this tile is supposed to be immune to lava. Defaults to false.</param>
        internal static void SetUpLantern(this ModTile mt, bool lavaImmune = false)
        {
            Main.tileLighted[mt.Type] = true;
            Main.tileFrameImportant[mt.Type] = true;
            Main.tileLavaDeath[mt.Type] = !lavaImmune;
            Main.tileWaterDeath[mt.Type] = false;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2Top);
            TileObjectData.newTile.LavaDeath = !lavaImmune;
            TileObjectData.addTile(mt.Type);

            // All hanging lanterns count as light sources.
            mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
        }

        /// <summary>
        /// Extension which initializes a ModTile to be a piano.
        /// </summary>
        /// <param name="mt">The ModTile which is being initialized.</param>
        /// <param name="lavaImmune">Whether this tile is supposed to be immune to lava. Defaults to false.</param>
        internal static void SetUpPiano(this ModTile mt, bool lavaImmune = false)
        {
            Main.tileLighted[mt.Type] = true;
            Main.tileFrameImportant[mt.Type] = true;
            Main.tileLavaDeath[mt.Type] = !lavaImmune;
            Main.tileWaterDeath[mt.Type] = false;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.addTile(mt.Type);

            // All pianos count as tables.
            mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);
        }

        /// <summary>
        /// Extension which initializes a ModTile to be a platform.
        /// </summary>
        /// <param name="mt">The ModTile which is being initialized.</param>
        /// <param name="lavaImmune">Whether this tile is supposed to be immune to lava. Defaults to false.</param>
        internal static void SetUpPlatform(this ModTile mt, bool lavaImmune = false)
        {
            Main.tileLighted[mt.Type] = true;
            Main.tileFrameImportant[mt.Type] = true;
            Main.tileSolidTop[mt.Type] = true;
            Main.tileSolid[mt.Type] = true;
            Main.tileNoAttach[mt.Type] = true;
            Main.tileTable[mt.Type] = true;
            Main.tileLavaDeath[mt.Type] = !lavaImmune;
            TileID.Sets.Platforms[mt.Type] = true;
            TileObjectData.newTile.CoordinateHeights = new int[] { 16 };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.StyleMultiplier = 27;
            TileObjectData.newTile.StyleWrapLimit = 27;
            TileObjectData.newTile.UsesCustomCanPlace = false;
            TileObjectData.newTile.LavaDeath = !lavaImmune;
            TileObjectData.addTile(mt.Type);

            // All platforms count as doors (so that you may have top-or-bottom entry/exit rooms)
            mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsDoor);
        }

        /// <summary>
        /// Extension which initializes a ModTile to be a sink.
        /// </summary>
        /// <param name="mt">The ModTile which is being initialized.</param>
        /// <param name="lavaImmune">Whether this tile is supposed to be immune to lava. Defaults to false.</param>
        internal static void SetUpSink(this ModTile mt, bool lavaImmune = false)
        {
            Main.tileLighted[mt.Type] = true;
            Main.tileFrameImportant[mt.Type] = true;
            Main.tileLavaDeath[mt.Type] = !lavaImmune;
            Main.tileWaterDeath[mt.Type] = false;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.LavaDeath = !lavaImmune;
            TileObjectData.addTile(mt.Type);
        }

        /// <summary>
        /// Extension which initializes a ModTile to be a sofa.
        /// </summary>
        /// <param name="mt">The ModTile which is being initialized.</param>
        /// <param name="lavaImmune">Whether this tile is supposed to be immune to lava. Defaults to false.</param>
        internal static void SetUpSofa(this ModTile mt, bool lavaImmune = false)
        {
            Main.tileLighted[mt.Type] = true;
            Main.tileFrameImportant[mt.Type] = true;
            Main.tileLavaDeath[mt.Type] = !lavaImmune;
            Main.tileWaterDeath[mt.Type] = false;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.newTile.LavaDeath = !lavaImmune;
            TileObjectData.addTile(mt.Type);

            // All sofas count as chairs.
            mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsChair);
        }

        /// <summary>
        /// Extension which initializes a ModTile to be a table.
        /// </summary>
        /// <param name="mt">The ModTile which is being initialized.</param>
        /// <param name="lavaImmune">Whether this tile is supposed to be immune to lava. Defaults to false.</param>
        internal static void SetUpTable(this ModTile mt, bool lavaImmune = false)
        {
            Main.tileSolidTop[mt.Type] = true;
            Main.tileLighted[mt.Type] = true;
            Main.tileFrameImportant[mt.Type] = true;
            Main.tileNoAttach[mt.Type] = true;
            Main.tileTable[mt.Type] = true;
            Main.tileLavaDeath[mt.Type] = !lavaImmune;
            Main.tileWaterDeath[mt.Type] = false;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.newTile.LavaDeath = !lavaImmune;
            TileObjectData.addTile(mt.Type);

            // As you could probably guess, all tables count as tables.
            mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);
        }

        /// <summary>
        /// Extension which initializes a ModTile to be a torch.
        /// </summary>
        /// <param name="mt">The ModTile which is being initialized.</param>
        /// <param name="lavaImmune">Whether this tile is supposed to be immune to lava. Defaults to false.</param>
        internal static void SetUpTorch(this ModTile mt, bool lavaImmune = false, bool waterImmune = false)
        {
            Main.tileLighted[mt.Type] = true;
            Main.tileFrameImportant[mt.Type] = true;
            Main.tileSolid[mt.Type] = false;
            Main.tileNoAttach[mt.Type] = true;
            Main.tileNoFail[mt.Type] = true;
            Main.tileWaterDeath[mt.Type] = !waterImmune;
            Main.tileLavaDeath[mt.Type] = !lavaImmune;
            TileObjectData.newTile.CopyFrom(TileObjectData.StyleTorch);
            TileObjectData.newTile.WaterDeath = !waterImmune;
            TileObjectData.newTile.LavaDeath = !lavaImmune;
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
            TileObjectData.newAlternate.CopyFrom(TileObjectData.StyleTorch);
            TileObjectData.newAlternate.WaterDeath = false;
            TileObjectData.newAlternate.AnchorLeft = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide | AnchorType.Tree | AnchorType.AlternateTile, TileObjectData.newTile.Height, 0);
            TileObjectData.newAlternate.AnchorAlternateTiles = new int[] { 124 };
            TileObjectData.addAlternate(1);
            TileObjectData.newAlternate.CopyFrom(TileObjectData.StyleTorch);
            TileObjectData.newAlternate.WaterDeath = false;
            TileObjectData.newAlternate.AnchorRight = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide | AnchorType.Tree | AnchorType.AlternateTile, TileObjectData.newTile.Height, 0);
            TileObjectData.newAlternate.AnchorAlternateTiles = new int[] { 124 };
            TileObjectData.addAlternate(2);
            TileObjectData.newAlternate.CopyFrom(TileObjectData.StyleTorch);
            TileObjectData.newAlternate.WaterDeath = false;
            TileObjectData.newAlternate.AnchorWall = true;
            TileObjectData.addAlternate(0);
            TileObjectData.addTile(mt.Type);

            // All torches count as light sources.
            mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
        }

        /// <summary>
        /// Extension which initializes a ModTile to be a work bench.
        /// </summary>
        /// <param name="mt">The ModTile which is being initialized.</param>
        /// <param name="lavaImmune">Whether this tile is supposed to be immune to lava. Defaults to false.</param>
        internal static void SetUpWorkBench(this ModTile mt, bool lavaImmune = false)
        {
            Main.tileSolidTop[mt.Type] = true;
            Main.tileFrameImportant[mt.Type] = true;
            Main.tileNoAttach[mt.Type] = true;
            Main.tileTable[mt.Type] = true;
            Main.tileLavaDeath[mt.Type] = !lavaImmune;
            Main.tileWaterDeath[mt.Type] = false;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x1);
            TileObjectData.newTile.CoordinateHeights = new int[] { 18 };
            TileObjectData.newTile.LavaDeath = !lavaImmune;
            TileObjectData.addTile(mt.Type);

            // All work benches count as tables.
            mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);
        }
        #endregion
        #endregion

        #region Drawing Utilities
        public static void DrawItemGlowmask(this Item item, SpriteBatch spriteBatch, int frameCount, float rotation, Texture2D glowmaskTexture)
        {
            Vector2 center = new Vector2((float)(Main.itemTexture[item.type].Width / 2), (float)(Main.itemTexture[item.type].Height / frameCount / 2));
            Rectangle frame = Main.itemAnimations[item.type].GetFrame(glowmaskTexture);
            Vector2 drawPosition = item.Center - Main.screenPosition;

            spriteBatch.Draw(glowmaskTexture, drawPosition,
                new Rectangle?(frame), Color.White, rotation, center, 1f, SpriteEffects.None, 0f);
        }

        public static void DrawFishingLine(this Projectile projectile, int fishingRodType, Color poleColor, int xPositionAdditive = 45, float yPositionAdditive = 35f)
        {
            Lighting.AddLight(projectile.Center, 0.4f, 0f, 0.4f);

            Player player = Main.player[projectile.owner];
            if (projectile.bobber && Main.player[projectile.owner].inventory[Main.player[projectile.owner].selectedItem].holdStyle > 0)
            {
                float pPosX = player.MountedCenter.X;
                float pPosY = player.MountedCenter.Y;
                pPosY += Main.player[projectile.owner].gfxOffY;
                int type = Main.player[projectile.owner].inventory[Main.player[projectile.owner].selectedItem].type;
                float gravDir = Main.player[projectile.owner].gravDir;

                if (type == fishingRodType)
                {
                    pPosX += (float)(xPositionAdditive * Main.player[projectile.owner].direction);
                    if (Main.player[projectile.owner].direction < 0)
                    {
                        pPosX -= 13f;
                    }
                    pPosY -= yPositionAdditive * gravDir;
                }

                if (gravDir == -1f)
                {
                    pPosY -= 12f;
                }
                Vector2 mountedCenter = new Vector2(pPosX, pPosY);
                mountedCenter = Main.player[projectile.owner].RotatedRelativePoint(mountedCenter + new Vector2(8f), true) - new Vector2(8f);
                float projPosX = projectile.position.X + (float)projectile.width * 0.5f - mountedCenter.X;
                float projPosY = projectile.position.Y + (float)projectile.height * 0.5f - mountedCenter.Y;
                Math.Sqrt((double)(projPosX * projPosX + projPosY * projPosY));
                bool canDraw = true;
                if (projPosX == 0f && projPosY == 0f)
                {
                    canDraw = false;
                }
                else
                {
                    float projPosXY = (float)Math.Sqrt((double)(projPosX * projPosX + projPosY * projPosY));
                    projPosXY = 12f / projPosXY;
                    projPosX *= projPosXY;
                    projPosY *= projPosXY;
                    mountedCenter.X -= projPosX;
                    mountedCenter.Y -= projPosY;
                    projPosX = projectile.position.X + (float)projectile.width * 0.5f - mountedCenter.X;
                    projPosY = projectile.position.Y + (float)projectile.height * 0.5f - mountedCenter.Y;
                }
                while (canDraw)
                {
                    float height = 12f;
                    float positionMagnitude = (float)Math.Sqrt((double)(projPosX * projPosX + projPosY * projPosY));
                    if (float.IsNaN(positionMagnitude) || float.IsNaN(positionMagnitude))
                    {
                        canDraw = false;
                    }
                    else
                    {
                        if (positionMagnitude < 20f)
                        {
                            height = positionMagnitude - 8f;
                            canDraw = false;
                        }
                        positionMagnitude = 12f / positionMagnitude;
                        projPosX *= positionMagnitude;
                        projPosY *= positionMagnitude;
                        mountedCenter.X += projPosX;
                        mountedCenter.Y += projPosY;
                        projPosX = projectile.position.X + (float)projectile.width * 0.5f - mountedCenter.X;
                        projPosY = projectile.position.Y + (float)projectile.height * 0.1f - mountedCenter.Y;
                        if (positionMagnitude > 12f)
                        {
                            float positionInverseMultiplier = 0.3f;
                            float absVelocitySum = Math.Abs(projectile.velocity.X) + Math.Abs(projectile.velocity.Y);
                            if (absVelocitySum > 16f)
                            {
                                absVelocitySum = 16f;
                            }
                            absVelocitySum = 1f - absVelocitySum / 16f;
                            positionInverseMultiplier *= absVelocitySum;
                            absVelocitySum = positionMagnitude / 80f;
                            if (absVelocitySum > 1f)
                            {
                                absVelocitySum = 1f;
                            }
                            positionInverseMultiplier *= absVelocitySum;
                            if (positionInverseMultiplier < 0f)
                            {
                                positionInverseMultiplier = 0f;
                            }
                            absVelocitySum = 1f - projectile.localAI[0] / 100f;
                            positionInverseMultiplier *= absVelocitySum;
                            if (projPosY > 0f)
                            {
                                projPosY *= 1f + positionInverseMultiplier;
                                projPosX *= 1f - positionInverseMultiplier;
                            }
                            else
                            {
                                absVelocitySum = Math.Abs(projectile.velocity.X) / 3f;
                                if (absVelocitySum > 1f)
                                {
                                    absVelocitySum = 1f;
                                }
                                absVelocitySum -= 0.5f;
                                positionInverseMultiplier *= absVelocitySum;
                                if (positionInverseMultiplier > 0f)
                                {
                                    positionInverseMultiplier *= 2f;
                                }
                                projPosY *= 1f + positionInverseMultiplier;
                                projPosX *= 1f - positionInverseMultiplier;
                            }
                        }
                        float rotation2 = (float)Math.Atan2((double)projPosY, (double)projPosX) - MathHelper.PiOver2;
                        Color color2 = Lighting.GetColor((int)mountedCenter.X / 16, (int)(mountedCenter.Y / 16f), poleColor); //cadet blue

                        Main.spriteBatch.Draw(Main.fishingLineTexture, new Vector2(mountedCenter.X - Main.screenPosition.X + (float)Main.fishingLineTexture.Width * 0.5f, mountedCenter.Y - Main.screenPosition.Y + (float)Main.fishingLineTexture.Height * 0.5f), new Microsoft.Xna.Framework.Rectangle?(new Microsoft.Xna.Framework.Rectangle(0, 0, Main.fishingLineTexture.Width, (int)height)), color2, rotation2, new Vector2((float)Main.fishingLineTexture.Width * 0.5f, 0f), 1f, SpriteEffects.None, 0f);
                    }
                }
            }
        }

        public static void DrawHook(this Projectile projectile, Texture2D hookTexture, float angleAdditive = 0f)
        {
            Player player = Main.player[projectile.owner];
            Vector2 center = projectile.Center;
            float angleToMountedCenter = projectile.AngleTo(player.MountedCenter) - MathHelper.PiOver2;
            bool canShowHook = true;
            while (canShowHook)
            {
                float distanceMagnitude = (player.MountedCenter - center).Length(); //Exact same as using a Sqrt
                if (distanceMagnitude < hookTexture.Height + 1f)
                {
                    canShowHook = false;
                }
                else if (float.IsNaN(distanceMagnitude))
                {
                    canShowHook = false;
                }
                else
                {
                    center += projectile.DirectionTo(player.MountedCenter) * hookTexture.Height;
                    Color tileAtCenterColor = Lighting.GetColor((int)center.X / 16, (int)(center.Y / 16f));
                    Main.spriteBatch.Draw(hookTexture, center - Main.screenPosition, 
                        new Rectangle?(new Rectangle(0, 0, hookTexture.Width, hookTexture.Height)), 
                        tileAtCenterColor, angleToMountedCenter + angleAdditive, 
                        hookTexture.Size() / 2, 1f, SpriteEffects.None, 0f);
                }
            }
        }

        internal static void IterateDisco(ref Color c, ref float aiParam, in byte discoIter = 7)
        {
            switch (aiParam)
            {
                case 0f:
                    c.G += discoIter;
                    if (c.G >= 255)
                    {
                        c.G = 255;
                        aiParam = 1f;
                    }
                    break;
                case 1f:
                    c.R -= discoIter;
                    if (c.R <= 0)
                    {
                        c.R = 0;
                        aiParam = 2f;
                    }
                    break;
                case 2f:
                    c.B += discoIter;
                    if (c.B >= 255)
                    {
                        c.B = 255;
                        aiParam = 3f;
                    }
                    break;
                case 3f:
                    c.G -= discoIter;
                    if (c.G <= 0)
                    {
                        c.G = 0;
                        aiParam = 4f;
                    }
                    break;
                case 4f:
                    c.R += discoIter;
                    if (c.R >= 255)
                    {
                        c.R = 255;
                        aiParam = 5f;
                    }
                    break;
                case 5f:
                    c.B -= discoIter;
                    if (c.B <= 0)
                    {
                        c.B = 0;
                        aiParam = 0f;
                    }
                    break;
                default:
                    aiParam = 0f;
                    c = Color.Red;
                    break;
            }
        }
        #endregion

        #region Miscellaneous Utilities
        public static T[] ShuffleArray<T>(T[] array, Random rand = null)
        {
            if (rand is null)
                rand = new Random();

            for (int i = array.Length; i > 0; --i)
            {
                int j = rand.Next(i);
                T tempElement = array[j];
                array[j] = array[i - 1];
                array[i - 1] = tempElement;
            }
            return array;
        }

        public static string CombineStrings(params object[] args)
        {
            StringBuilder result = new StringBuilder(1024);
            for(int i = 0; i < args.Length; ++i)
            {
                object o = args[i];
                result.Append(o.ToString());
                result.Append(' ');
            }
            return result.ToString();
        }

        /// <summary>
        /// Calculates the sound volume and panning for a sound which is played at the specified location in the game world.<br/>
        /// Note that sound does not play on dedicated servers or during world generation.
        /// </summary>
        /// <param name="soundPos">The position the sound is emitting from. If either X or Y is -1, the sound does not fade with distance.</param>
        /// <param name="ambient">Whether the sound is considered ambient, which makes it use the ambient sound slider in the options. Defaults to false.</param>
        /// <returns>Volume and pan, in that order. Volume is always between 0 and 1. Pan is always between -1 and 1.</returns>
        public static (float, float) CalculateSoundStats(Vector2 soundPos, bool ambient = false)
        {
            float volume = 0f;
            float pan = 0f;

            if (soundPos.X == -1f || soundPos.Y == -1f)
                volume = 1f;
            else if (WorldGen.gen || Main.dedServ || Main.netMode == NetmodeID.Server)
                volume = 0f;
            else
            {
                float topLeftX = Main.screenPosition.X - Main.screenWidth * 2f;
                float topLeftY = Main.screenPosition.Y - Main.screenHeight * 2f;

                // Sounds cannot be heard from more than ~2.5 screens away.
                // This rectangle is 5x5 screens centered on the current screen center position.
                Rectangle audibleArea = new Rectangle((int)topLeftX, (int)topLeftY, Main.screenWidth * 5, Main.screenHeight * 5);
                Rectangle soundHitbox = new Rectangle((int)soundPos.X, (int)soundPos.Y, 1, 1);
                Vector2 screenCenter = Main.screenPosition + new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
                if (audibleArea.Intersects(soundHitbox))
                {
                    pan = (soundPos.X - screenCenter.X) / (Main.screenWidth * 0.5f);
                    float dist = Vector2.Distance(soundPos, screenCenter);
                    volume = 1f - (dist / (Main.screenWidth * 1.5f));
                }
            }

            pan = MathHelper.Clamp(pan, -1f, 1f);
            volume = MathHelper.Clamp(volume, 0f, 1f);
            if (ambient)
                volume = Main.gameInactive ? 0f : volume * Main.ambientVolume;
            else
                volume *= Main.soundVolume;

            // This is actually done by vanilla. I guess if the sound volume gets corrupted during gameplay, you can't blast your eardrums out.
            volume = MathHelper.Clamp(volume, 0f, 1f);
            return (volume, pan);
        }

        /// <summary>
        /// Convenience function to utilize CalculateSoundStats immediately on an existing sound effect.<br/>
        /// This allows updating a looping sound every single frame to have the correct volume and pan, even if the player drags the audio sliders around.
        /// </summary>
        /// <param name="sfx">The SoundEffectInstance which is having its values updated.</param>
        /// <param name="soundPos">The position the sound is emitting from. If either X or Y is -1, the sound does not fade with distance.</param>
        /// <param name="ambient">Whether the sound is considered ambient, which makes it use the ambient sound slider in the options. Defaults to false.</param>
        public static void ApplySoundStats(ref SoundEffectInstance sfx, Vector2 soundPos, bool ambient = false)
        {
            if (sfx is null || sfx.IsDisposed)
                return;
            (sfx.Volume, sfx.Pan) = CalculateSoundStats(soundPos, ambient);
        }

        public static void StartSandstorm()
        {
            typeof(Terraria.GameContent.Events.Sandstorm).GetMethod("StartSandstorm", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        }

        public static void StopSandstorm()
        {
            Terraria.GameContent.Events.Sandstorm.Happening = false;
        }

        public static void AddWithCondition<T>(this List<T> list, T type, bool condition)
        {
            if (condition)
                list.Add(type);
        }
        #endregion
    }
}
