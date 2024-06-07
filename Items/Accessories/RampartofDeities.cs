using System;
using CalamityMod.CalPlayer;
using CalamityMod.Items.Materials;
using CalamityMod.Rarities;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Accessories
{
    [AutoloadEquip(EquipType.Shield)]
    public class RampartofDeities : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Accessories";
        public override void SetDefaults()
        {
            Item.width = 64;
            Item.height = 62;
            Item.value = CalamityGlobalItem.RarityVioletBuyPrice;
            Item.defense = 12;
            Item.accessory = true;
            Item.rare = ModContent.RarityType<Violet>();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityPlayer modPlayer = player.Calamity();

            // Directly inherits both Cross Necklace and Deific Amulet iframe boosts
            player.longInvince = true;
            modPlayer.dAmulet = true;

            // Large hit iframe effect taken from Seraph Tracers
            modPlayer.rampartOfDeities = true;

            // Ice Barrier buff inherited from Frozen Turtle Shell
            if (player.statLife <= player.statLifeMax2 * 0.5)
                player.AddBuff(BuffID.IceBarrier, 5);

            // Knockback immunity inherited from Paladin's Shield
            player.noKnockback = true;

            // Paladin's Shield application
            if (player.statLife > player.statLifeMax2 * 0.25f)
            {
                player.hasPaladinShield = true;
                if (player.whoAmI != Main.myPlayer && player.miscCounter % 10 == 0)
                {
                    int myPlayer = Main.myPlayer;
                    if (Main.player[myPlayer].team == player.team && player.team != 0)
                    {
                        float teamPlayerXDist = player.position.X - Main.player[myPlayer].position.X;
                        float teamPlayerYDist = player.position.Y - Main.player[myPlayer].position.Y;
                        if ((float)Math.Sqrt(teamPlayerXDist * teamPlayerXDist + teamPlayerYDist * teamPlayerYDist) < 800f)
                            Main.player[myPlayer].AddBuff(BuffID.PaladinsShield, 20);
                    }
                }
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.FrozenShield).
                AddIngredient<DeificAmulet>().
                AddIngredient<AuricBar>(5).
                AddIngredient<AscendantSpiritEssence>(4).
                AddTile<CosmicAnvil>().
                Register();
        }
    }
}
