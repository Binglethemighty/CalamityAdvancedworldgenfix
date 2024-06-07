using CalamityMod.Items.Materials;
using CalamityMod.Projectiles.Melee.Spears;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Melee
{
    [LegacyName("SpatialLance")]
    public class ElementalLance : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Melee";
        public override void SetStaticDefaults()
        {
            ItemID.Sets.Spears[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 88;
            Item.height = 88;
            Item.damage = 240;
            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;
            Item.useTurn = true;
            Item.noUseGraphic = true;
            Item.useAnimation = 45;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 45;
            Item.knockBack = 9.5f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = CalamityGlobalItem.RarityPurpleBuyPrice;
            Item.rare = ItemRarityID.Purple;
            Item.shoot = ModContent.ProjectileType<ElementalLanceProjectile>();
            Item.shootSpeed = 12f;
        }

        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<BotanicPiercer>().
                AddIngredient(ItemID.LunarBar, 5).
                AddIngredient<LifeAlloy>(5).
                AddIngredient<GalacticaSingularity>(5).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }
}
