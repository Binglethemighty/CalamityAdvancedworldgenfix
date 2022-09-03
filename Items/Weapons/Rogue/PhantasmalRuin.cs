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
    public class PhantasmalRuin : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Phantasmal Ruin");
            Tooltip.SetDefault(@"Fires an enormous ghost lance that emits lost souls as it flies
Explodes into tormented souls on enemy hits
Stealth strikes continuously leave spectral clones in their wake");
            SacrificeTotal = 1;
        }

        public override void SetDefaults()
        {
            Item.damage = 955;
            Item.knockBack = 8f;

            Item.width = 102;
            Item.height = 98;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useTime = 35;
            Item.useAnimation = 35;
            Item.autoReuse = true;
            Item.shootSpeed = 14.5f;
            Item.shoot = ModContent.ProjectileType<PhantasmalRuinProj>();
            Item.UseSound = SoundID.Item1;
            Item.DamageType = RogueDamageClass.Instance;

            Item.value = CalamityGlobalItem.Rarity13BuyPrice;
            Item.rare = ModContent.RarityType<PureGreen>();
        }

		public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup)
		{
			itemGroup = (ContentSamples.CreativeHelper.ItemGroup)CalamityResearchSorting.RogueWeapon;
		}

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.Calamity().StealthStrikeAvailable())
            {
                damage = (int)(damage * 1.22);
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
                AddIngredient<LuminousStriker>().
                AddIngredient<PhantomLance>(500).
                AddIngredient<RuinousSoul>(4).
                AddIngredient<Phantoplasm>(20).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }
}
