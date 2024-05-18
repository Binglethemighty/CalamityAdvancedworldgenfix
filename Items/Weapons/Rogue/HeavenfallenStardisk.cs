using CalamityMod.Projectiles.Rogue;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Rogue
{
    public class HeavenfallenStardisk : RogueWeapon
    {
        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 48;
            Item.damage = 165;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 40;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 8f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = CalamityGlobalItem.RarityLimeBuyPrice;
            Item.rare = ItemRarityID.Lime;
            Item.shoot = ModContent.ProjectileType<HeavenfallenStardiskBoomerang>();
            Item.shootSpeed = 10f;
            Item.DamageType = RogueDamageClass.Instance;
        }

        public override float StealthDamageMultiplier => 1.2f;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            {
                int proj = Projectile.NewProjectile(source, position, velocity.Length() * -Vector2.UnitY, type, damage, knockback, player.whoAmI);
                if (proj.WithinBounds(Main.maxProjectiles))
                    Main.projectile[proj].Calamity().stealthStrike = player.Calamity().StealthStrikeAvailable();
                if (player.Calamity().StealthStrikeAvailable()) //setting the stealth strike
                {
                    int spread = 15;
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 perturbedspeed = new Vector2(velocity.X + Main.rand.Next(-8, 8), velocity.Y + Main.rand.Next(-2, 3)).RotatedBy(MathHelper.ToRadians(spread));
                        Projectile.NewProjectile(source, position, perturbedspeed.Length() * -Vector2.UnitY, type, (int)(damage * 0.3), knockback, player.whoAmI);
                        if (proj.WithinBounds(Main.maxProjectiles))
                        {
                            Main.projectile[proj].Calamity().stealthStrike = player.Calamity().StealthStrikeAvailable();
                        }
                        spread -= Main.rand.Next(5, 8);
                    }
                }
                return false;
            }
        }
    }
}
