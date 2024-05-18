using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace CalamityMod.NPCs.DevourerofGods
{
    public class DoGScreenShaderData : ScreenShaderData
    {
        private int DoGIndex;

        public DoGScreenShaderData(string passName)
            : base(passName)
        {
        }

        private void UpdateDoGIndex()
        {
            int DoGType = ModContent.NPCType<DevourerofGodsHead>();
            if (DoGIndex >= 0 && Main.npc[DoGIndex].active && Main.npc[DoGIndex].type == DoGType)
            {
                return;
            }
            DoGIndex = -1;
            foreach (NPC n in Main.ActiveNPCs)
            {
                if (n.type == DoGType)
                {
                    DoGIndex = n.whoAmI;
                    break;
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (DoGIndex == -1)
            {
                UpdateDoGIndex();
                if (DoGIndex == -1 && Main.LocalPlayer.Calamity().monolithDevourerPShader <= 0 && Main.LocalPlayer.Calamity().monolithDevourerBShader <= 0)
                    Filters.Scene["CalamityMod:DevourerofGodsHead"].Deactivate();
            }
        }

        public override void Apply()
        {
            UpdateDoGIndex();
            if (DoGIndex != -1)
            {
                UseTargetPosition(Main.npc[DoGIndex].Center);
            }
            else if (Main.LocalPlayer.Calamity().monolithDevourerPShader > 0 || Main.LocalPlayer.Calamity().monolithDevourerBShader > 0)
            {
                UseTargetPosition(Main.LocalPlayer.Center);
            }
            base.Apply();
        }
    }
}
