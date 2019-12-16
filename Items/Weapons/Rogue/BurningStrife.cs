﻿using CalamityMod.Projectiles.Rogue;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Rogue
{
    public class BurningStrife : RogueWeapon
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Burning Strife");
            Tooltip.SetDefault("Throws a shadowflame spiky ball that bursts into flames\n" + 
                               "Stealth Strikes make the ball linger and explode more violently\n" +
                               "'Definitely not pocket safe'");
        }

        public override void SafeSetDefaults()
        {
            item.width = 16;
            item.height = 28;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.value = Item.buyPrice(0, 36, 0, 0);
            item.rare = 4;
            item.autoReuse = true;
            item.noUseGraphic = true;
            item.noMelee = true;

            item.useAnimation = 25;
            item.useTime = 25;
            item.damage = 50;
            item.crit = 8;
            item.shootSpeed = 8f;
            item.shoot = ModContent.ProjectileType<BurningStrifeProj>();

            item.Calamity().rogue = true; 
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            int proj = Projectile.NewProjectile(position, new Vector2(speedX, speedY), ModContent.ProjectileType<BurningStrifeProj>(), damage, knockBack, player.whoAmI);
            if (player.Calamity().StealthStrikeAvailable())
            {
                Main.projectile[proj].Calamity().stealthStrike = true;
                Main.projectile[proj].penetrate = 8;            
            }
            return false;
        }
    }
}
