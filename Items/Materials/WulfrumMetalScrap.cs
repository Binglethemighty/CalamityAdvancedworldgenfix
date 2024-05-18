using CalamityMod.Cooldowns;
using CalamityMod.Items.Accessories;
using CalamityMod.Items.Armor.Wulfrum;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;

namespace CalamityMod.Items.Materials
{
    [LegacyName("WulfrumShard")]
    public class WulfrumMetalScrap : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Materials";

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 25;
        }

        public override void SetDefaults()
        {
            Item.width = 13;
            Item.height = 10;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(copper: 80);
            Item.rare = ItemRarityID.Blue;
            Item.ammo = Item.type;
        }

        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup)
        {
            itemGroup = ContentSamples.CreativeHelper.ItemGroup.Material;
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (source is EntitySource_Loot)
            {
                if (Main.rand.NextBool())
                    return;

                bool closePlayer = false;

                foreach (Player player in Main.ActivePlayers)
                {
                    if ((player.Center - Item.Center).Length() < 1200 && player.GetModPlayer<WulfrumBatteryPlayer>().battery)
                    {
                        closePlayer = true;
                        break;
                    }
                }

                if (closePlayer)
                {
                    Item.stack++;
                    SoundEngine.PlaySound(WulfrumBattery.ExtraDropSound, Item.Center);

                    int numDust = Main.rand.Next(3, 7);
                    for (int i = 0; i < numDust; i++)
                    {
                        Dust.NewDustDirect(Item.position, Item.width, Item.height, Main.rand.NextBool() ? 246 : 247, 0, -3f, Scale: Main.rand.NextFloat(0.9f, 1f));
                    }
                }
            }
        }

        public override void Load() => Terraria.On_Item.CanFillEmptyAmmoSlot += AvoidDefaultingToAmmoSlot;

        private bool AvoidDefaultingToAmmoSlot(Terraria.On_Item.orig_CanFillEmptyAmmoSlot orig, Item self)
        {
            if (self.type == Type)
                return false;
            return orig(self);
        }
    }
}
