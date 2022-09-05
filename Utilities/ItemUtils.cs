﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalamityMod.UI.CalamitasEnchants;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace CalamityMod
{
    public static partial class CalamityUtils
    {
        public static bool IsTrueMelee(this Item item)
        {
            // Even if you for some reason mark an accessory as true melee, it won't count.
            if (item is null || item.IsAir || item.accessory)
                return false;
            return item.CountsAsClass<TrueMeleeDamageClass>() || item.CountsAsClass<TrueMeleeNoSpeedDamageClass>();
        }

        /// <summary>
        /// Determines if a given item is a whip based on what it shoots.
        /// </summary>
        /// <param name="item">The item to check.</param>
        public static bool IsWhip(this Item item) => item.shoot > ProjectileID.None && ProjectileID.Sets.IsAWhip[item.shoot];

        #region Color Constants
        internal static readonly Color DevItemColor = new Color(255, 0, 255);
        internal static readonly Color DonatorItemColor = new Color(139, 0, 0);
        #endregion

        // TODO -- This probably isn't the best place to put this but it needs to be somewhere easily accessible.
        #region Accessory Prefix Stats
        internal static int GetScalingDefense(int prefixID)
        {
            switch (prefixID)
            {
                default:
                    return 0;

                case PrefixID.Hard:
                    if (DownedBossSystem.downedYharon)
                        return 4;
                    else if (DownedBossSystem.downedPolterghast || DownedBossSystem.downedDoG)
                        return 3;
                    else if (Main.hardMode || NPC.downedGolemBoss || NPC.downedMoonlord || DownedBossSystem.downedProvidence)
                        return 2;
                    return 1;

                case PrefixID.Guarding:
                    if (DownedBossSystem.downedYharon)
                        return 7;
                    else if (DownedBossSystem.downedDoG)
                        return 6;
                    else if (DownedBossSystem.downedProvidence || DownedBossSystem.downedPolterghast)
                        return 5;
                    else if (NPC.downedGolemBoss || NPC.downedMoonlord)
                        return 4;
                    else if (Main.hardMode)
                        return 3;
                    return 2;

                case PrefixID.Armored:
                    if (DownedBossSystem.downedYharon)
                        return 10;
                    else if (DownedBossSystem.downedDoG)
                        return 9;
                    else if (DownedBossSystem.downedPolterghast)
                        return 8;
                    else if (DownedBossSystem.downedProvidence)
                        return 7;
                    else if (NPC.downedMoonlord)
                        return 6;
                    else if (Main.hardMode || NPC.downedGolemBoss)
                        return 5;
                    return 3;

                case PrefixID.Warding:
                    if (DownedBossSystem.downedYharon)
                        return 12;
                    else if (DownedBossSystem.downedDoG)
                        return 11;
                    else if (DownedBossSystem.downedPolterghast)
                        return 10;
                    else if (DownedBossSystem.downedProvidence)
                        return 9;
                    else if (NPC.downedMoonlord)
                        return 8;
                    else if (NPC.downedGolemBoss)
                        return 7;
                    else if (Main.hardMode)
                        return 6;
                    return 4;
            }
        }
        #endregion

        #region Reforging Algorithm
        internal static int GetReworkedReforge(Item item, UnifiedRandom rand, int currentPrefix)
        {
            CalamityMod mod = CalamityMod.Instance;
            int GetCalPrefix(string name)
            {
                bool found = mod.TryFind(name, out ModPrefix ret);
                return found ? ret.Type : 0;
            }

            // This is the hardcoded value to "do nothing", and is thus the default choice.
            int prefix = -1;

            // ACCESSORIES
            if (item.accessory)
            {
                int[][] accessoryReforgeTiers = new int[][]
                {
                    /* 0 */ new int[] { PrefixID.Hard, PrefixID.Jagged, PrefixID.Brisk, PrefixID.Wild, GetCalPrefix("Quiet") },
                    /* 1 */ new int[] { PrefixID.Guarding, PrefixID.Spiked, PrefixID.Precise, PrefixID.Fleeting, PrefixID.Rash, GetCalPrefix("Cloaked") },
                    /* 2 */ new int[] { PrefixID.Armored, PrefixID.Angry, PrefixID.Hasty2, PrefixID.Intrepid, PrefixID.Arcane, GetCalPrefix("Camouflaged") },
                    /* 3 */ new int[] { PrefixID.Warding, PrefixID.Menacing, PrefixID.Lucky, PrefixID.Quick2, PrefixID.Violent, GetCalPrefix("Silent") },
                };
                prefix = IteratePrefix(rand, accessoryReforgeTiers, currentPrefix);
            }

            // MELEE (includes tools and whips)
            else if (item.CountsAsClass<MeleeDamageClass>() || item.CountsAsClass<SummonMeleeSpeedDamageClass>())
            {
                // Terrarian (has its own special "Legendary" for marketing reasons)
                if (item.type == ItemID.Terrarian)
                {
                    int[][] terrarianReforgeTiers = new int[][]
                    {
                        /* 0 */ new int[] { PrefixID.Keen, PrefixID.Forceful, PrefixID.Strong },
                        /* 1 */ new int[] { PrefixID.Hurtful, PrefixID.Ruthless, PrefixID.Zealous },
                        /* 2 */ new int[] { PrefixID.Superior, PrefixID.Demonic, PrefixID.Godly },
                        /* 3 */ new int[] { PrefixID.Legendary2 },
                    };
                    prefix = IteratePrefix(rand, terrarianReforgeTiers, currentPrefix);
                }
                
                // Yoyos, Flails, Spears, etc.
                // Spears actually work fine with Legendary, but vanilla doesn't give it to them, so we won't either.
                // Zenith and rapiers are specifically excluded from this, so they get broadsword reforges despite not scaling with melee speed.
                else if ((item.channel || item.noMelee) && item.type != ItemID.Zenith && item.useStyle != ItemUseStyleID.Rapier)
                {
                    int[][] meleeNoSpeedReforgeTiers = new int[][]
                    {
                        /* 0 */ new int[] { PrefixID.Keen, PrefixID.Forceful, PrefixID.Strong },
                        /* 1 */ new int[] { PrefixID.Hurtful, PrefixID.Ruthless, PrefixID.Zealous },
                        /* 2 */ new int[] { PrefixID.Superior, PrefixID.Demonic },
                        /* 3 */ new int[] { PrefixID.Godly }
                    };
                    prefix = IteratePrefix(rand, meleeNoSpeedReforgeTiers, currentPrefix);
                }

                // All other melee weapons
                else
                {
                    int[][] meleeReforgeTiers = new int[][]
                    {
                        /* 0 */ new int[] { PrefixID.Keen, PrefixID.Nimble, PrefixID.Nasty, PrefixID.Light, PrefixID.Heavy, PrefixID.Light, PrefixID.Forceful, PrefixID.Strong },
                        /* 1 */ new int[] { PrefixID.Hurtful, PrefixID.Ruthless, PrefixID.Zealous, PrefixID.Quick, PrefixID.Pointy, PrefixID.Bulky },
                        /* 2 */ new int[] { PrefixID.Murderous, PrefixID.Agile, PrefixID.Large, PrefixID.Dangerous, PrefixID.Sharp },
                        /* 3 */ new int[] { PrefixID.Massive, PrefixID.Unpleasant, PrefixID.Savage, PrefixID.Superior },
                        /* 4 */ new int[] { PrefixID.Demonic, PrefixID.Deadly2, PrefixID.Godly },
                        /* 5 */ new int[] { PrefixID.Legendary } // for non-tools, Light is a mediocre low-tier reforge
                    };
                    int[][] toolReforgeTiers = new int[][]
                    {
                        /* 0 */ new int[] { PrefixID.Keen, PrefixID.Nimble, PrefixID.Nasty, PrefixID.Heavy, PrefixID.Light, PrefixID.Forceful, PrefixID.Strong },
                        /* 1 */ new int[] { PrefixID.Hurtful, PrefixID.Ruthless, PrefixID.Zealous, PrefixID.Quick, PrefixID.Pointy, PrefixID.Bulky },
                        /* 2 */ new int[] { PrefixID.Murderous, PrefixID.Agile, PrefixID.Large, PrefixID.Dangerous, PrefixID.Sharp },
                        /* 3 */ new int[] { PrefixID.Massive, PrefixID.Unpleasant, PrefixID.Savage, PrefixID.Superior },
                        /* 4 */ new int[] { PrefixID.Demonic, PrefixID.Deadly2, PrefixID.Godly },
                        /* 5 */ new int[] { PrefixID.Legendary, PrefixID.Light } // for some tools, light is better than legendary. for others, it's equal
                    };

                    var tierListToUse = (item.pick > 0 || item.axe > 0 || item.hammer > 0) ? toolReforgeTiers : meleeReforgeTiers;
                    prefix = IteratePrefix(rand, tierListToUse, currentPrefix);
                }
            }

            // RANGED
            else if (item.CountsAsClass<RangedDamageClass>())
            {
                int[][] rangedReforgeTiers = new int[][]
                {
                    /* 0 */ new int[] { PrefixID.Keen, PrefixID.Nimble, PrefixID.Nasty, PrefixID.Powerful, PrefixID.Forceful, PrefixID.Strong },
                    /* 1 */ new int[] { PrefixID.Hurtful, PrefixID.Ruthless, PrefixID.Zealous, PrefixID.Quick, PrefixID.Intimidating },
                    /* 2 */ new int[] { PrefixID.Murderous, PrefixID.Agile, PrefixID.Hasty, PrefixID.Staunch, PrefixID.Unpleasant },
                    /* 3 */ new int[] { PrefixID.Superior, PrefixID.Demonic, PrefixID.Sighted },
                    /* 4 */ new int[] { PrefixID.Godly, PrefixID.Rapid, /* ranged Deadly */ PrefixID.Deadly, /* universal Deadly */ PrefixID.Deadly2 },
                    /* 5 */ new int[] { PrefixID.Unreal }
                };
                prefix = IteratePrefix(rand, rangedReforgeTiers, currentPrefix);
            }

            // MAGIC
            else if (item.CountsAsClass<MagicDamageClass>() || item.CountsAsClass<MagicSummonHybridDamageClass>())
            {
                int[][] magicReforgeTiers = new int[][]
                {
                    /* 0 */ new int[] { PrefixID.Keen, PrefixID.Nimble, PrefixID.Nasty, PrefixID.Furious, PrefixID.Forceful, PrefixID.Strong },
                    /* 1 */ new int[] { PrefixID.Hurtful, PrefixID.Ruthless, PrefixID.Zealous, PrefixID.Quick, PrefixID.Taboo, PrefixID.Manic },
                    /* 2 */ new int[] { PrefixID.Murderous, PrefixID.Agile, PrefixID.Adept, PrefixID.Celestial, PrefixID.Unpleasant },
                    /* 3 */ new int[] { PrefixID.Superior, PrefixID.Demonic, PrefixID.Mystic },
                    /* 4 */ new int[] { PrefixID.Godly, PrefixID.Masterful, PrefixID.Deadly2 },
                    /* 5 */ new int[] { PrefixID.Mythical }
                };
                prefix = IteratePrefix(rand, magicReforgeTiers, currentPrefix);
            }

            // SUMMON (not whips)
            else if (item.CountsAsClass<SummonDamageClass>())
            {
                int[][] summonReforgeTiers = new int[][]
                {
                    /* 0 */ new int[] { PrefixID.Nimble, PrefixID.Furious },
                    /* 1 */ new int[] { PrefixID.Forceful, PrefixID.Strong, PrefixID.Quick, PrefixID.Taboo, PrefixID.Manic },
                    /* 2 */ new int[] { PrefixID.Hurtful, PrefixID.Adept, PrefixID.Celestial },
                    /* 3 */ new int[] { PrefixID.Superior, PrefixID.Demonic, PrefixID.Mystic, PrefixID.Deadly2 },
                    /* 4 */ new int[] { PrefixID.Masterful, PrefixID.Godly },
                    /* 5 */ new int[] { PrefixID.Mythical, PrefixID.Ruthless } // you may want mythical early game for the knockback.
                };
                prefix = IteratePrefix(rand, summonReforgeTiers, currentPrefix);
            }

            // ROGUE (Calamity adds these reforges to all 1.4 TML throwing items)
            else if (item.CountsAsClass<ThrowingDamageClass>())
            {
                // Added appropriate universal reforges to rogue, so they don't ONLY get modded prefixes.
                int[][] rogueReforgeTiers = new int[][]
                {
                    /* 0 */ new int[] { PrefixID.Keen, PrefixID.Nimble, PrefixID.Nasty, PrefixID.Forceful, PrefixID.Strong, GetCalPrefix("Radical"), GetCalPrefix("Pointy") },
                    /* 1 */ new int[] { PrefixID.Hurtful, PrefixID.Ruthless, PrefixID.Zealous, PrefixID.Quick, GetCalPrefix("Sharp"), GetCalPrefix("Glorious") },
                    /* 2 */ new int[] { PrefixID.Murderous, PrefixID.Agile, PrefixID.Unpleasant, GetCalPrefix("Feathered"), GetCalPrefix("Sleek"), GetCalPrefix("Hefty") },
                    /* 3 */ new int[] { PrefixID.Superior, PrefixID.Demonic, GetCalPrefix("Mighty"), GetCalPrefix("Serrated") },
                    /* 4 */ new int[] { PrefixID.Godly, PrefixID.Deadly2, GetCalPrefix("Vicious"), GetCalPrefix("Lethal") },
                    /* 5 */ new int[] { GetCalPrefix("Flawless") }
                };
                prefix = IteratePrefix(rand, rogueReforgeTiers, currentPrefix);
            }

            return prefix;
        }

        private static int GetPrefixTier(int[][] tiers, int currentPrefix)
        {
            for (int checkingTier = 0; checkingTier < tiers.Length; ++checkingTier)
            {
                int[] tierList = tiers[checkingTier];
                for (int i = 0; i < tierList.Length; ++i)
                    if (tierList[i] == currentPrefix)
                        return checkingTier;
            }

            // If an invalid or modded prefix is detected, return -1.
            // This will give a random tier 0 prefix (the "next tier"), starting fresh with a low-tier vanilla or Calamity prefix.
            return -1;
        }

        private static int IteratePrefix(UnifiedRandom rand, int[][] reforgeTiers, int currentPrefix)
        {
            int currentTier = GetPrefixTier(reforgeTiers, currentPrefix);

            // If max tier: give max tier reforges forever
            // Otherwise: go up by 1 tier with every reforge, guaranteed
            int newTier = currentTier == reforgeTiers.Length - 1 ? currentTier : currentTier + 1;
            return rand.Next(reforgeTiers[newTier]);
        }
        #endregion

        /// <summary>
        /// Converts the given ModKeybind into a string for insertion into item tooltips.<br></br>
        /// This allows the user's actual keybind choices to be shown to them in tooltips.
        /// </summary>
        /// <param name="mhk">The ModKeybind to convert to a string.</param>
        /// <returns></returns>
        public static string TooltipHotkeyString(this ModKeybind mhk)
        {
            if (Main.dedServ || mhk is null)
                return "";

            List<string> keys = mhk.GetAssignedKeys();
            if (keys.Count == 0)
                return "[NONE]";
            else
            {
                StringBuilder sb = new StringBuilder(16);
                sb.Append(keys[0]);

                // In almost all cases, this code won't run, because there won't be multiple bindings for the hotkey. But just in case...
                for (int i = 1; i < keys.Count; ++i)
                    sb.Append(" / ").Append(keys[i]);
                return sb.ToString();
            }
        }

        private const float WorldInsertionOffset = 15f;
        /// <summary>
        /// If the given item is outside the world, force it to be within the world boundaries.
        /// </summary>
        /// <param name="item">The item to possibly relocate.</param>
        /// <param name="dist">The minimum distance in pixels the item can be from the world boundary.</param>
        /// <returns>Whether the item was relocated.</returns>
        public static bool ForceItemIntoWorld(Item item, float desiredDist = WorldInsertionOffset)
        {
            if (item is null || !item.active)
                return false;

            // The world edge needs to be accounted for regardless of the distance chosen as an argument.
            float worldEdge = Main.offLimitBorderTiles * 16f;
            float dist = worldEdge + desiredDist;

            float maxPosX = Main.maxTilesX * 16f;
            float maxPosY = Main.maxTilesY * 16f;
            bool moved = false;
            if (item.position.X < worldEdge)
            {
                item.position.X = dist;
                moved = true;
            }
            else if (item.position.X + item.width > maxPosX - worldEdge)
            {
                item.position.X = maxPosX - item.width - dist;
                moved = true;
            }
            if (item.position.Y < worldEdge)
            {
                item.position.Y = dist;
                moved = true;
            }
            else if (item.position.Y + item.height > maxPosY - worldEdge)
            {
                item.position.Y = maxPosY - item.height - dist;
                moved = true;
            }
            return moved;
        }

        public static bool IsEnchantable(this Item item)
        {
            // If the item is air just immediately return false.
            // It will not have a CalamityGlobalItem instance to use and attempting to do anything with it
            // would just result in errors.
            if (item.IsAir)
                return false;

            // Items with a max stack greater than one cannot be enchanted, due to problems with data duplication.
            if (item.maxStack > 1)
                return false;

            // Ammo cannot be enchanted because it is practically useless when held directly.
            if (item.ammo != AmmoID.None)
                return false;

            // Ignore items that explicitly say to not be enchanted.
            if (item.Calamity().CannotBeEnchanted)
                return false;

            return true;
        }

        /// <summary>
        /// Determines if a given item is enchanted based on Calamitas' special system.
        /// </summary>
        /// <param name="item">The item to check.</param>
        public static bool IsEnchanted(this Item item)
        {
            // If the item is air just immediately return false.
            // It will not have a CalamityGlobalItem instance to use and attempting to do anything with it
            // would just result in errors.
            if (item.IsAir)
                return false;

            // If the item is contained in the enchant upgrade result relationship, return true.
            if (EnchantmentManager.ItemUpgradeRelationship.ContainsValue(item.type))
                return true;

            return item.Calamity().AppliedEnchantment.HasValue;
        }

        public static bool CheckWoodenAmmo(int type, Player player)
        {
            if (player.hasMoltenQuiver && type == ProjectileID.FireArrow)
                return true;
            return type == ProjectileID.WoodenArrowFriendly;
        }

        public static Rectangle FixSwingHitbox(float hitboxWidth, float hitboxHeight)
        {
            Player player = Main.player[Main.myPlayer];
            Item item = player.ActiveItem();
            float hitbox_X, hitbox_Y;
            float mountOffsetY = player.mount.PlayerOffsetHitbox;

            // Third hitbox shifting values (last third of the use animation)
            if (player.itemAnimation < player.itemAnimationMax * 0.333)
            {
                //Value to shift the position in the X axis depending on hitbox width (not sure why vanilla has this specific values)
                float shiftX = 10f;
                if (hitboxWidth >= 92)
                    shiftX = 38f;
                else if (hitboxWidth >= 64)
                    shiftX = 28f;
                else if (hitboxWidth >= 52)
                    shiftX = 24f;
                else if (hitboxWidth > 32)
                    shiftX = 14f;
                //Position in X axis for the hitbox, starts basing it off the player x position, shifts it by half of the players width
                //Then shifts it by half the width of the hitbox minus the aforementioned X shift, finally multiplied by player direction so it switches sides
                hitbox_X = player.position.X + player.width * 0.5f + (hitboxWidth * 0.5f - shiftX) * player.direction;
                //Position in the Y axis for the hitbox, simply puts in front of you, no shift needed since it lets hitbox go below your feet.
                hitbox_Y = player.position.Y + 24f + mountOffsetY;
            }

            // Second hitbox shifting values (2nd third of the use animation)
            else if (player.itemAnimation < player.itemAnimationMax * 0.666)
            {
                //Shift value in X axis
                float shift = 10f;
                if (hitboxWidth >= 92)
                    shift = 38f;
                else if (hitboxWidth >= 64)
                    shift = 28f;
                else if (hitboxWidth >= 52)
                    shift = 24f;
                else if (hitboxWidth > 32)
                    shift = 18f;
                //Calculates X position in the same way as before, extra parenthesis is probably not needed
                hitbox_X = player.position.X + (player.width * 0.5f + (hitboxWidth * 0.5f - shift) * player.direction);

                //Shift for the Y axis dependant on size, since second hitbox shifts down from your head a bit (Can be renamed to shiftY)
                shift = 10f;
                if (hitboxHeight > 64)
                    shift = 14f;
                else if (hitboxHeight > 52)
                    shift = 12f;
                else if (hitboxHeight > 32)
                    shift = 8f;

                //Calculates Y position with the shift instead of just 24f
                hitbox_Y = player.position.Y + shift + mountOffsetY;
            }

            // First hitbox shifting values
            else
            {
                //Obtains shift value for X axis dependant on hitbox width (feel free to rename this to shiftX)
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
                //Calculates hitbox position in X axis
                hitbox_X = player.position.X + player.width * 0.5f - (hitboxWidth * 0.5f - shift) * player.direction;

                //Shift value on Y axis dependant on hitbox height (this is so the hitbox is sure to reach the tip of the blade at its highest)
                //Also so the hitbox doesnt phase into your head too much
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

            // Hitbox size adjustments (defaults size to 32x32)
            Rectangle hitbox = new Rectangle((int)hitbox_X, (int)hitbox_Y, 32, 32);
            if (item.damage >= 0 && item.type > ItemID.None && !item.noMelee && player.itemAnimation > 0)
            {
                if (!Main.dedServ)
                {
                    // Dedicated server ignores this bit. Not sure why.
                    hitbox = new Rectangle((int)hitbox_X, (int)hitbox_Y, (int)hitboxWidth, (int)hitboxHeight);
                }
                //Scales hitbox with scale value from reforges and such
                hitbox.Width = (int)(hitbox.Width * item.scale);
                hitbox.Height = (int)(hitbox.Height * item.scale);
                //Shifts X and Y values with width or height depending on if theyre facing the other way or have normal gravity
                if (player.direction == -1)
                {
                    hitbox.X -= hitbox.Width;
                }
                if (player.gravDir == 1f)
                {
                    hitbox.Y -= hitbox.Height;
                }

                // Calculates size (this first if can be removed but its a failsafe in case its used for smth that isnt broadswords)
                if (item.useStyle == ItemUseStyleID.Swing)
                {
                    // Third hitbox size adjustments
                    if (player.itemAnimation < player.itemAnimationMax * 0.333)
                    {
                        if (player.direction == -1)
                        {
                            //I am, starting to lose track on why it even shifts it again
                            hitbox.X -= (int)(hitbox.Width * 1.4 - hitbox.Width);
                        }
                        //Width becomes 1.4 times itself since the last hitbox is a wide rectangle, trimmed with int
                        hitbox.Width = (int)(hitbox.Width * 1.4);
                        hitbox.Y += (int)(hitbox.Height * 0.5 * player.gravDir);
                        //Height also gets multiplied by 1.1 since third hitbox is larger than second in both width and height
                        hitbox.Height = (int)(hitbox.Height * 1.1);
                    }

                    //Second hitbox size is the given size in parameters, hence it not being here

                    // First hitbox size adjustments
                    else if (player.itemAnimation >= player.itemAnimationMax * 0.666)
                    {
                        if (player.direction == 1)
                        {
                            hitbox.X -= (int)(hitbox.Width * 1.2);
                        }
                        //Multiplies normal width by 2 since first hitbox is way wider than the second
                        hitbox.Width *= 2;
                        hitbox.Y -= (int)((hitbox.Height * 1.4 - hitbox.Height) * player.gravDir);
                        //And it is also 1.4 times taller
                        hitbox.Height = (int)(hitbox.Height * 1.4);
                    }
                }
            }
            //Returns rectangle of hitbox in current time
            return hitbox;
        }

        // TODO -- this is not very well understood old boffin code. It should probably be inspected thoroughly and re-documented.
        #region Melee Dust Helper
        /// <summary>
        /// Dust helper to spawn dust for an item. Allows you to specify where on the item to spawn the dust, essentially. (ONLY WORKS FOR SWINGING WEAPONS?)
        /// </summary>
        /// <param name="player">The player using the item.</param>
        /// <param name="dustType">The type of dust to use.</param>
        /// <param name="chancePerFrame">The chance per frame to spawn the dust (0f-1f)</param>
        /// <param name="minDistance">The minimum distance between the player and the dust</param>
        /// <param name="maxDistance">The maximum distance between the player and the dust</param>
        /// <param name="minRandRot">The minimum random rotation offset for the dust</param>
        /// <param name="maxRandRot">The maximum random rotation offset for the dust</param>
        /// <param name="minSpeed">The minimum speed that the dust should travel</param>
        /// <param name="maxSpeed">The maximum speed that the dust should travel</param>
        public static Dust MeleeDustHelper(Player player, int dustType, float chancePerFrame, float minDistance, float maxDistance, float minRandRot = -0.2f, float maxRandRot = 0.2f, float minSpeed = 0.9f, float maxSpeed = 1.1f)
        {
            if (Main.rand.NextFloat(1f) < chancePerFrame)
            {
                //Calculate values
                //distance from player,
                //the vector offset from the player center
                //the vector between the pos and the player
                float distance = Main.rand.NextFloat(minDistance, maxDistance);
                Vector2 offset = (player.itemRotation - (MathHelper.PiOver4 * player.direction) + Main.rand.NextFloat(minRandRot, maxRandRot)).ToRotationVector2() * distance * player.direction;
                Vector2 pos = player.Center + offset;
                Vector2 vec = pos - player.Center;
                //spawn the dust
                Dust d = Dust.NewDustPerfect(pos, dustType);
                //normalise vector and multiply by velocity magnitude
                vec.Normalize();
                d.velocity = vec * Main.rand.NextFloat(minSpeed, maxSpeed);
                return d;
            }
            return null;
        }
        #endregion

        /// <summary>
        /// Determines if an item can be enchanted by any item at all via Calamitas' enchantment system.
        /// </summary>
        /// <param name="item">The item to check.</param>
        public static bool CanBeEnchantedBySomething(this Item item) => EnchantmentManager.EnchantmentList.Any(enchantment => enchantment.ApplyRequirement(item));

        public static void ConsumeItemViaQuickBuff(Player player, Item item, int buffType, int buffTime, bool reducedPotionSickness)
        {
            bool showsOver = false;
            //Fail if you have the buff
            for (int l = 0; l < Player.MaxBuffs; l++)
            {
                int hasBuff = player.buffType[l];
                if (player.buffTime[l] > 0 && hasBuff == buffType)
                    showsOver = true;
            }
            //Fail if you have potion sickness
            if (player.potionDelay > 0 || player.Calamity().potionTimer > 0)
                showsOver = true;

            if (!showsOver)
            {
                SoundEngine.PlaySound(item.UseSound.GetValueOrDefault(), player.Center);

                int healAmt = (int)(item.healLife * player.Calamity().healingPotBonus);
                if (healAmt > 0 && player.QuickHeal_GetItemToUse() != null)
                {
                    if (player.QuickHeal_GetItemToUse().type != item.type)
                        healAmt = 0;
                }

                player.statLife += healAmt;
                player.statMana += item.healMana;
                if (player.statMana > player.statManaMax2)
                {
                    player.statMana = player.statManaMax2;
                }
                if (player.statLife > player.statLifeMax2)
                {
                    player.statLife = player.statLifeMax2;
                }
                if (item.healMana > 0)
                    player.AddBuff(BuffID.ManaSickness, Player.manaSickTime, true);
                if (Main.myPlayer == player.whoAmI)
                {
                    if (healAmt > 0)
                        player.HealEffect(healAmt, true);
                    if (item.healMana > 0)
                        player.ManaEffect(item.healMana);
                }
                if (item.potion && healAmt > 0) //Don't inflict Potion Sickness if you don't actually heal
                {
                    int duration = reducedPotionSickness ? 3000 : 3600;
                    if (player.pStone)
                        duration = (int)(duration * 0.75);
                    player.AddBuff(BuffID.PotionSickness, duration);
                }

                player.AddBuff(buffType, buffTime);

                --item.stack;
                if (item.stack <= 0)
                    item.TurnToAir();
                Recipe.FindRecipes();
            }
        }

        public static void TreasureBagLightAndDust(this Item item)
        {
            // Spawn some light and dust when dropped in the world
            Lighting.AddLight(item.Center, Color.White.ToVector3() * 0.4f);

            if (item.timeSinceItemSpawned % 12 == 0)
            {
                Vector2 center = item.Center + new Vector2(0f, item.height * -0.1f);

                // This creates a randomly rotated vector of length 1, which gets it's components multiplied by the parameters
                Vector2 direction = Main.rand.NextVector2CircularEdge(item.width * 0.6f, item.height * 0.6f);
                float distance = 0.3f + Main.rand.NextFloat() * 0.5f;
                Vector2 velocity = new Vector2(0f, -Main.rand.NextFloat() * 0.3f - 1.5f);

                Dust dust = Dust.NewDustPerfect(center + direction * distance, DustID.SilverFlame, velocity);
                dust.scale = 0.5f;
                dust.fadeIn = 1.1f;
                dust.noGravity = true;
                dust.noLight = true;
                dust.alpha = 0;
            }
        }

        #region Rogue Prefixes
        public static bool CanGetRoguePrefix(this Item item) => item.CountsAsClass<RogueDamageClass>() && item.maxStack == 1;

        public static int RandomRoguePrefix()
        {
            Mod mod = ModContent.GetInstance<CalamityMod>();
            int roguePrefix = Utils.SelectRandom(Main.rand, new int[]
            {
                mod.Find<ModPrefix>("Radical").Type,
                mod.Find<ModPrefix>("Pointy").Type,
                mod.Find<ModPrefix>("Sharp").Type,
                mod.Find<ModPrefix>("Glorious").Type,
                mod.Find<ModPrefix>("Feathered").Type,
                mod.Find<ModPrefix>("Sleek").Type,
                mod.Find<ModPrefix>("Hefty").Type,
                mod.Find<ModPrefix>("Mighty").Type,
                mod.Find<ModPrefix>("Serrated").Type,
                mod.Find<ModPrefix>("Vicious").Type,
                mod.Find<ModPrefix>("Lethal").Type,
                mod.Find<ModPrefix>("Flawless").Type,
                mod.Find<ModPrefix>("Blunt").Type,
                mod.Find<ModPrefix>("Flimsy").Type,
                mod.Find<ModPrefix>("Unbalanced").Type,
                mod.Find<ModPrefix>("Atrocious").Type,
				PrefixID.Keen,
				PrefixID.Superior,
				PrefixID.Forceful,
				PrefixID.Broken,
				PrefixID.Damaged,
				PrefixID.Hurtful,
				PrefixID.Strong,
				PrefixID.Unpleasant,
				PrefixID.Weak,
				PrefixID.Ruthless,
				PrefixID.Godly,
				PrefixID.Demonic,
				PrefixID.Zealous,
				PrefixID.Quick,
				PrefixID.Deadly2,
				PrefixID.Agile,
				PrefixID.Nimble,
				PrefixID.Murderous,
				PrefixID.Slow,
				PrefixID.Sluggish,
				PrefixID.Lazy,
				PrefixID.Annoying, // Vanilla does not include this in "bad" reforges so we won't either
				PrefixID.Nasty
            });
            return roguePrefix;
        }

        public static bool NegativeRoguePrefix(int prefix)
        {
            Mod mod = ModContent.GetInstance<CalamityMod>();
            List<int> badPrefixes = new List<int>()
            {
                mod.Find<ModPrefix>("Blunt").Type,
                mod.Find<ModPrefix>("Flimsy").Type,
                mod.Find<ModPrefix>("Unbalanced").Type,
                mod.Find<ModPrefix>("Atrocious").Type,
				PrefixID.Broken,
				PrefixID.Damaged,
				PrefixID.Shoddy,
				PrefixID.Slow,
				PrefixID.Sluggish,
				PrefixID.Lazy
            };
            return badPrefixes.Contains(prefix);
        }
        #endregion
    }
}
