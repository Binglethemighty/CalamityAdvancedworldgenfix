using Terraria;
using Terraria.ID;

namespace CalamityMod.Items.VanillaArmorChanges
{
    public class ShadowArmorSetChange : VanillaArmorChange
    {
        public override int? HeadPieceID => ItemID.ShadowHelmet;

        public override int? BodyPieceID => ItemID.ShadowScalemail;

        public override int? LegPieceID => ItemID.ShadowGreaves;

        public override int[] AlternativeHeadPieceIDs => new int[] { ItemID.AncientShadowHelmet };
        public override int[] AlternativeBodyPieceIDs => new int[] { ItemID.AncientShadowScalemail };
        public override int[] AlternativeLegPieceIDs => new int[] { ItemID.AncientShadowGreaves };

        public override string ArmorSetName => "Shadow";

        private static void ApplyAnyPieceEffect(Player player)
        {
            // 15MAY2024: Ozzatron: Removed all standard changes to Shadow Armor.
            // As such, this method intentionally does nothing.
            //
            // Shadow Armor's movement speed boosting effect is separately nerfed via an IL edit.
            //
            // Class is being left here so that further changes can be made to Shadow Armor as needed.
        }

        public override void ApplyHeadPieceEffect(Player player) => ApplyAnyPieceEffect(player);

        public override void ApplyBodyPieceEffect(Player player) => ApplyAnyPieceEffect(player);

        public override void ApplyLegPieceEffect(Player player) => ApplyAnyPieceEffect(player);
    }
}
