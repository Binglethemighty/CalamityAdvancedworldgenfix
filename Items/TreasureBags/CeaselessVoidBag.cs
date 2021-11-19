using CalamityMod.Items.Accessories;
using CalamityMod.Items.Armor.Vanity;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Items.Weapons.Summon;
using CalamityMod.NPCs.CeaselessVoid;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.TreasureBags
{
    public class CeaselessVoidBag : ModItem
    {
        public override int BossBagNPC => ModContent.NPCType<CeaselessVoid>();

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Treasure Bag");
            Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }

        public override void SetDefaults()
        {
            item.maxStack = 999;
            item.consumable = true;
            item.width = 24;
            item.height = 24;
            item.rare = ItemRarityID.Cyan;
            item.expert = true;
        }

        public override bool CanRightClick() => true;

        public override void OpenBossBag(Player player)
        {
            // Materials
            DropHelper.DropItem(player, ModContent.ItemType<DarkPlasma>(), 4, 6);

            // Weapons
			DropHelper.DropItemChance(player, ModContent.ItemType<MirrorBlade>(), DropHelper.BagWeaponDropRateInt);
            DropHelper.DropItemChance(player, ModContent.ItemType<VoidConcentrationStaff>(), DropHelper.BagWeaponDropRateInt);

            // Equipment
            DropHelper.DropItem(player, ModContent.ItemType<ArcanumoftheVoid>());

            // Vanity
			DropHelper.DropItemChance(player, ModContent.ItemType<CeaselessVoidMask>(), 7);
			if (Main.rand.NextBool(20))
			{
				DropHelper.DropItem(player, ModContent.ItemType<AncientGodSlayerHelm>());
				DropHelper.DropItem(player, ModContent.ItemType<AncientGodSlayerChestplate>());
				DropHelper.DropItem(player, ModContent.ItemType<AncientGodSlayerLeggings>());
				DropHelper.DropItem(player, ModContent.ItemType<GodSlayerHornedHelm>());
				DropHelper.DropItem(player, ModContent.ItemType<GodSlayerVisage>());
			}
        }
    }
}
