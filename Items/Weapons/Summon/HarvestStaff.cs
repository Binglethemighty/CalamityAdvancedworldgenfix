using CalamityMod.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Summon
{
    public class HarvestStaff : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Summon";

        public static int PumpkinsPerSentry = 5;
        public static float TimePerPumpkin = 150f;
        public static float PlantedEnemyDistanceDetection = 160f;
        public static float NormalEnemyDistanceDetection = 1200f;
        public static float PumpkinGravityStrength = 0.8f;
        public static float PumpkinMaxGravity = 20f;

        public override void SetStaticDefaults() => Item.staff[Type] = true;

        public override void SetDefaults()
        {
            Item.damage = 27;
            Item.DamageType = DamageClass.Summon;
            Item.shoot = ModContent.ProjectileType<HarvestStaffSentry>();
            Item.knockBack = 5f;

            Item.useTime = Item.useAnimation = 15;
            Item.mana = 10;
            Item.width = 44;
            Item.height = 46;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.value = CalamityGlobalItem.RarityGreenBuyPrice;
            Item.rare = ItemRarityID.Green;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.UseSound = SoundID.Grass with { Volume = 0.6f, Pitch = -0.4f };

            Item.shootSpeed = 0.1f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.FindSentryRestingSpot(type, out int XPosition, out int YPosition, out int YOffset);
            YOffset -= 10;
            position = new Vector2((float)XPosition, (float)(YPosition - YOffset));
            Projectile.NewProjectileDirect(source, position, Vector2.Zero, type, damage, knockback, player.whoAmI);
            player.UpdateMaxTurrets();
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddRecipeGroup("Wood", 20).
                AddIngredient(ItemID.Pumpkin, 20).
                AddIngredient(ItemID.PumpkinSeed, 5).
                AddTile(TileID.Anvils).
                Register();
        }
    }
}
