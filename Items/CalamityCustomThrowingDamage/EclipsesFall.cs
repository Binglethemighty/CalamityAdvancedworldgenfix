using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.Items;
using CalamityMod;
using static CalamityMod.CalPlayer.CalamityPlayer;

namespace CalamityMod.Items.CalamityCustomThrowingDamage
{
	public class EclipsesFall : CalamityDamageItem
    {
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Eclipse's Fall");
			Tooltip.SetDefault("Summons spears from the sky on hit\n" +
			"Stealth strikes impale enemies and summon a constant barrage of spears");
		}

		public override void SafeSetDefaults()
		{
			item.width = 72;
			item.damage = 1800;
			item.noMelee = true;
			item.noUseGraphic = true;
			item.useAnimation = 20;
			item.useStyle = 1;
			item.useTime = 20;
			item.knockBack = 3.5f;
			item.UseSound = SoundID.Item1;
			item.autoReuse = true;
			item.height = 72;
            item.value = Item.buyPrice(1, 80, 0, 0);
            item.rare = 10;
			item.GetGlobalItem<CalamityGlobalItem>(mod).postMoonLordRarity = 14;
			item.shoot = mod.ProjectileType("EclipsesFallMain");
			item.shootSpeed = 15f;
			item.GetGlobalItem<CalamityGlobalItem>(mod).rogue = true;
		}

        public override bool Shoot(Player player, ref Microsoft.Xna.Framework.Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            if (player.GetCalamityPlayer().StealthStrikeAvailable())
			{
				Projectile.NewProjectile(position.X, position.Y, speedX, speedY, mod.ProjectileType("EclipsesStealth"), damage, knockBack, player.whoAmI, 0f, 0f);
			}
			else
			{
				Projectile.NewProjectile(position.X, position.Y, speedX, speedY, type, damage, knockBack, player.whoAmI, 0f, 0f);
			}
			return false;
		}
	    
	    public override void AddRecipes()
	    {
	        ModRecipe recipe = new ModRecipe(mod);
	        recipe.AddIngredient(3543, 1); //Daybreak weapon because I don't know internal file name
	        recipe.AddIngredient(mod.ItemType("DarksunFragment"), 15);
	        recipe.AddIngredient(mod.ItemType("CoreofCinder"), 6);
	        recipe.AddTile(TileID.LunarCraftingStation);
	        recipe.SetResult(this);
	        recipe.AddRecipe();
	    }
	}
}
