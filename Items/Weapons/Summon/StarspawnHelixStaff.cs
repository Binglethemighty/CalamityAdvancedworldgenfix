using CalamityMod.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Summon
{
    [LegacyName("GodspawnHelixStaff")]
    public class StarspawnHelixStaff : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Summon";
        public override void SetDefaults()
        {
            Item.width = 54;
            Item.height = 52;
            Item.damage = 103;
            Item.knockBack = 1.25f;
            Item.mana = 10;

            Item.shoot = ModContent.ProjectileType<AstralProbeSummon>();
            Item.useTime = Item.useAnimation = 20;
            Item.DamageType = DamageClass.Summon;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item44;
            Item.rare = ItemRarityID.Cyan;
            Item.value = CalamityGlobalItem.RarityCyanBuyPrice;
            Item.noMelee = true;
            Item.autoReuse = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse != 2)
            {
                int p = Projectile.NewProjectile(source, Main.MouseWorld, velocity, type, damage, knockback, player.whoAmI, 0f, 1f);
                if (Main.projectile.IndexInRange(p))
                {
                    Main.projectile[p].originalDamage = Item.damage;
                    Main.projectile[p].ModProjectile<AstralProbeSummon>().ProbeIndex = player.ownedProjectileCounts[type];
                }

                int bladeIndex = 0;
                foreach (Projectile pro in Main.ActiveProjectiles)
                {
                    if (pro.type == type && pro.owner == player.whoAmI)
                    {
                        pro.ModProjectile<AstralProbeSummon>().ProbeIndex = bladeIndex++;
                        pro.ModProjectile<AstralProbeSummon>().AITimer = 0f;
                        pro.netUpdate = true;
                    }
                }
            }

            return false;
        }
    }
}
