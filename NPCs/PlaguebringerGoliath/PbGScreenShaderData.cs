using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace CalamityMod.NPCs.PlaguebringerGoliath
{
    public class PbGScreenShaderData : ScreenShaderData
    {
        private int PbGIndex;

        public PbGScreenShaderData(string passName)
            : base(passName)
        {
        }

        private void UpdatePbGIndex()
        {
            int PbGType = ModContent.NPCType<PlaguebringerGoliath>();
            if (PbGIndex >= 0 && Main.npc[PbGIndex].active && Main.npc[PbGIndex].type == PbGType)
            {
                return;
            }
            PbGIndex = NPC.FindFirstNPC(PbGType);
        }

        public override void Update(GameTime gameTime)
        {
            if (PbGIndex == -1 && Main.LocalPlayer.Calamity().monolithPlagueShader <= 0)
            {
                UpdatePbGIndex();
                if (PbGIndex == -1 && Main.LocalPlayer.Calamity().monolithPlagueShader <= 0)
                    Filters.Scene["CalamityMod:PlaguebringerGoliath"].Deactivate();
            }
        }

        public override void Apply()
        {
            UpdatePbGIndex();
            if (PbGIndex != -1)
            {
                UseTargetPosition(Main.npc[PbGIndex].Center);
            }
            if (Main.LocalPlayer.Calamity().monolithPlagueShader > 0)
            {
                UseTargetPosition(Main.LocalPlayer.Center);
            }
            base.Apply();
        }
    }
}
