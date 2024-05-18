using CalamityMod.Projectiles.Summon;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static Humanizer.In;

namespace CalamityMod.Items.Weapons.Summon
{
    [LegacyName("SeaboundStaff")]
    public class BrittleStarStaff : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Summon";
        public float Knockback = 2f;
        public override void SetDefaults()
        {
            Item.width = Item.height = 44;
            Item.damage = 10;
            Item.mana = 10;
            Item.useTime = Item.useAnimation = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = Knockback;
            Item.value = CalamityGlobalItem.RarityGreenBuyPrice;
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Item44 with { Pitch = 0.5f };
            Item.autoReuse = true;
            Item.shootSpeed = 5;
            Item.shoot = ModContent.ProjectileType<BrittleStarMinion>();
            Item.DamageType = DamageClass.Summon;
        }
        public override void SetStaticDefaults()
        {
            Item.staff[Item.type] = true;
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse != 2)
            {
                Item.noUseGraphic = false;
                position = Main.MouseWorld;
                velocity = Vector2.Zero;
                int SummonNumber = player.ownedProjectileCounts[type];
                int p = Projectile.NewProjectile(source, position.X, position.Y, velocity.X, velocity.Y, type, damage, knockback, player.whoAmI, 0, 0, SummonNumber);
                if (Main.projectile.IndexInRange(p))
                    Main.projectile[p].originalDamage = Item.damage;
            }
            if (player.altFunctionUse == 2)
            {
                Item.noUseGraphic = true;
            }
            int bladeIndex = 0;
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.type == type && p.owner == player.whoAmI)
                {
                    p.ModProjectile<BrittleStarMinion>().StarIndex = bladeIndex++;
                    p.ModProjectile<BrittleStarMinion>().AITimer = 0f;
                    p.netUpdate = true;
                }
            }
            return false;
        }
        public override void HoldItem(Player player)
        {
            //player.Calamity().rightClickListener = true;
            //player.Calamity().mouseWorldListener = true;
        }
    }
}
