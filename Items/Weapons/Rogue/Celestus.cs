﻿using CalamityMod.Items.Materials;
using CalamityMod.Projectiles.Rogue;
using CalamityMod.Rarities;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Rogue
{
    public class Celestus : RogueWeapon
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Celestus");
            Tooltip.SetDefault("Throws a scythe that splits into multiple scythes on enemy hits\n" +
            "Stealth strikes reverse direction and home in on enemies after returning to the player");
            SacrificeTotal = 1;
        }

        public override void SetDefaults()
        {
            Item.damage = 280;
            Item.knockBack = 6f;
            Item.useAnimation = Item.useTime = 22;
            Item.DamageType = RogueDamageClass.Instance;
            Item.autoReuse = true;
            Item.shootSpeed = 25f;
            Item.shoot = ModContent.ProjectileType<CelestusBoomerang>();

            Item.width = Item.height = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.value = CalamityGlobalItem.Rarity15BuyPrice;
            Item.rare = ModContent.RarityType<Violet>();
        }

		public override float StealthDamageMultiplier => 0.8f;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.Calamity().StealthStrikeAvailable()) //setting the stealth strike
            {
                int stealth = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
                if (stealth.WithinBounds(Main.maxProjectiles))
                    Main.projectile[stealth].Calamity().stealthStrike = true;
				return false;
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<ElementalDisk>().
                AddIngredient<AlphaVirus>().
                AddIngredient<MoltenAmputator>().
                AddIngredient<FrostcrushValari>().
                AddIngredient<EnchantedAxe>().
                AddIngredient<MiracleMatter>().
                AddTile<DraedonsForge>().
                Register();
        }
    }
}
