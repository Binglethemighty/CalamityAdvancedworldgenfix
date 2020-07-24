using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ModLoader;

namespace CalamityMod.Items.TreasureBags
{
    public class BleachedAnglingKit : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bleached Angling Kit");
            Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }

        public override void SetDefaults()
        {
            item.maxStack = 999;
            item.consumable = true;
            item.width = 24;
            item.height = 24;
            item.rare = 1;
        }

        public override bool CanRightClick() => true;

        public override void RightClick(Player player)
        {
			int anglerTackleBagChance = !Main.expertMode ? 15 : 18;
			int fishingAccChance = !Main.expertMode ? 12 : 10;
			int fishFindAccChance = !Main.expertMode ? 9 : 8;
			int anglerArmorChance = !Main.expertMode ? 4 : 2;
			int potionChance = !Main.expertMode ? 4 : 2;
			int bugNetChance = !Main.expertMode ? 15 : 12;
			// Fishing
			DropHelper.DropItemChance(player, ItemID.AnglerTackleBag, anglerTackleBagChance, 1, 1);
			DropHelper.DropItemChance(player, ItemID.HighTestFishingLine, fishingAccChance);
			DropHelper.DropItemChance(player, ItemID.TackleBox, fishingAccChance);
			DropHelper.DropItemChance(player, ItemID.AnglerEarring, fishingAccChance);
			DropHelper.DropItemChance(player, ItemID.FishermansGuide, fishFindAccChance);
			DropHelper.DropItemChance(player, ItemID.WeatherRadio, fishFindAccChance);
			DropHelper.DropItemChance(player, ItemID.Sextant, fishFindAccChance);
			DropHelper.DropItemChance(player, ItemID.AnglerHat, anglerArmorChance);
			DropHelper.DropItemChance(player, ItemID.AnglerVest, anglerArmorChance);
			DropHelper.DropItemChance(player, ItemID.AnglerPants, anglerArmorChance);
			DropHelper.DropItemChance(player, ItemID.FishingPotion, potionChance, 2, 3);
			DropHelper.DropItemChance(player, ItemID.SonarPotion, potionChance, 2, 3);
			DropHelper.DropItemChance(player, ItemID.CratePotion, potionChance, 2, 3);
			DropHelper.DropItemChance(player, ItemID.GoldenBugNet, bugNetChance, 1, 1);
        }
    }
}
