using CalamityMod.Projectiles.Magic;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityMod.Items.Weapons.Magic
{
    public class Thunderstorm : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Thunderstorm");
            Tooltip.SetDefault("Make it rain");
        }

        public override void SetDefaults()
        {
            item.damage = 165;
            item.mana = 50;
            item.magic = true;
            item.width = 48;
            item.height = 22;
            item.useTime = 24;
            item.useAnimation = 24;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.noMelee = true;
            item.knockBack = 2f;
			item.value = CalamityGlobalItem.Rarity13BuyPrice;
			item.Calamity().customRarity = CalamityRarity.PureGreen;
			item.rare = ItemRarityID.Purple;
            item.UseSound = mod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaBlast");
            item.autoReuse = true;
            item.shootSpeed = 6f;
            item.shoot = ModContent.ProjectileType<ThunderstormShot>();
			item.Calamity().challengeDrop = true;
		}

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-10, 0);
        }
    }
}
