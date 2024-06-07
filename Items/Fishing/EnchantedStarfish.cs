using Terraria;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Fishing
{
    public class EnchantedStarfish : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Fishing";
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 10;
            ItemID.Sets.CanBePlacedOnWeaponRacks[Item.type] = true;
            // For some reason Life/Mana boosting items are in this set (along with Magic Mirror+)
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 21; // Mana Crystal
            ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.ArcaneCrystal;
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 26;
            Item.rare = ItemRarityID.Green;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = SoundID.Item29;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(silver: 50);
            Item.autoReuse = true;
            Item.consumable = true;
        }

        public override bool? UseItem(Player player)
        {
            if (player.itemAnimation > 0 && (player.ConsumedManaCrystals < Player.ManaCrystalMax && player.itemTime == 0))
            {
                player.itemTime = Item.useTime;
                player.UseManaMaxIncreasingItem(20);
                player.ConsumedManaCrystals++;
                AchievementsHelper.HandleSpecialEvent(player, 1);
            }
            return true;
        }
    }
}
