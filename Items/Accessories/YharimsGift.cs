using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Items.Potions.Alcohol;
using CalamityMod.Projectiles.Typeless;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Accessories
{
    public class YharimsGift : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Accessories";
        public int dragonTimer = 60;

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 22;
            Item.defense = 15; // Why did this give 30 defense? This thing really needs a rework lol
            Item.accessory = true;
            Item.value = CalamityGlobalItem.RarityVioletBuyPrice;
            Item.rare = ModContent.RarityType<Violet>();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var source = player.GetSource_Accessory(Item);
            player.moveSpeed += 0.15f;
            player.GetDamage<GenericDamageClass>() += 0.15f;
            if (!player.StandingStill())
            {
                dragonTimer--;
                if (dragonTimer <= 0)
                {
                    if (player.whoAmI == Main.myPlayer)
                    {
                        int damage = (int)player.GetBestClassDamage().ApplyTo(175);
                        damage = player.ApplyArmorAccDamageBonusesTo(damage);

                        int projectile1 = Projectile.NewProjectile(source, player.Center, Vector2.Zero, ModContent.ProjectileType<DragonDust>(), damage, 5f, player.whoAmI, 0f, 0f);
                        Main.projectile[projectile1].timeLeft = 60;
                    }
                    dragonTimer = 60;
                }
            }
            else
            {
                dragonTimer = 60;
            }
            if (player.immune)
            {
                if (player.miscCounter % 8 == 0)
                {
                    if (player.whoAmI == Main.myPlayer)
                    {
                        int damage = (int)player.GetBestClassDamage().ApplyTo(375);
                        damage = player.ApplyArmorAccDamageBonusesTo(damage);

                        CalamityUtils.ProjectileRain(source, player.Center, 400f, 100f, 500f, 800f, 22f, ModContent.ProjectileType<SkyFlareFriendly>(), damage, 9f, player.whoAmI);
                    }
                }
            }
        }
    }
}
