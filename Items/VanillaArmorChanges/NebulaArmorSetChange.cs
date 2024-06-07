using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.VanillaArmorChanges
{
    public class NebulaArmorSetChange : VanillaArmorChange
    {
        public override int? HeadPieceID => ItemID.NebulaHelmet;

        public override int? BodyPieceID => ItemID.NebulaBreastplate;

        public override int? LegPieceID => ItemID.NebulaLeggings;

        public override string ArmorSetName => "Nebula";

        // The only thing this class does is change the set bonus text.
        // Buff booster nerfs are handled separately as IL edits.
        public override void UpdateSetBonusText(ref string setBonusText)
        {
            setBonusText += $"\n{CalamityUtils.GetTextValue($"Vanilla.Armor.SetBonus.{ArmorSetName}")}";
        }
    }
}
