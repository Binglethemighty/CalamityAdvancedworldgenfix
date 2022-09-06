﻿using CalamityMod.Items.Materials;
using CalamityMod.Projectiles.Rogue;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Rogue
{
    public class TimeBolt : RogueWeapon
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Time Bolt");
            Tooltip.SetDefault("There should be no boundary to human endeavor.\n" +
            "Stealth strikes can hit more enemies and create a larger time field");
            SacrificeTotal = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 46;
            Item.damage = 432;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 20;
            Item.knockBack = 4f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = CalamityGlobalItem.Rarity13BuyPrice;
            Item.rare = ModContent.RarityType<PureGreen>();
            Item.Calamity().donorItem = true;
            Item.shoot = ModContent.ProjectileType<TimeBoltKnife>();
            Item.shootSpeed = 16f;
            Item.DamageType = RogueDamageClass.Instance;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.Calamity().StealthStrikeAvailable())
                damage = (int)(damage * 0.68);

            int proj = Projectile.NewProjectile(source, position.X, position.Y, velocity.X, velocity.Y, type, damage, knockback, player.whoAmI);
            if (player.Calamity().StealthStrikeAvailable() && proj.WithinBounds(Main.maxProjectiles))
            {
                Main.projectile[proj].Calamity().stealthStrike = true;
                Main.projectile[proj].penetrate = 11;
            }
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<CosmicKunai>().
                AddIngredient(ItemID.FastClock).
                AddIngredient<RuinousSoul>(5).
                AddIngredient<Phantoplasm>(20).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }
}
