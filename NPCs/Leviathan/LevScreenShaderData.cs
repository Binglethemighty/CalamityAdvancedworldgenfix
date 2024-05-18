using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace CalamityMod.NPCs.Leviathan
{
    public class LevScreenShaderData : ScreenShaderData
    {
        private int LevIndex;

        public LevScreenShaderData(string passName)
            : base(passName)
        {
        }

        private void UpdateLIndex()
        {
            int LevType = Main.zenithWorld ? ModContent.NPCType<Anahita>() : ModContent.NPCType<Leviathan>();
            if (LevIndex >= 0 && Main.npc[LevIndex].active && Main.npc[LevIndex].type == LevType)
            {
                return;
            }
            LevIndex = -1;
            foreach (NPC n in Main.ActiveNPCs)
            {
                if (n.type == LevType)
                {
                    LevIndex = n.whoAmI;
                    break;
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            if ((LevIndex == -1 && Main.LocalPlayer.Calamity().monolithLeviathanShader <= 0) || BossRushEvent.BossRushActive)
            {
                UpdateLIndex();
                if (LevIndex == -1 || BossRushEvent.BossRushActive)
                    Filters.Scene["CalamityMod:Leviathan"].Deactivate();
            }
        }

        public override void Apply()
        {
            UpdateLIndex();
            if (LevIndex != -1)
            {
                UseTargetPosition(Main.npc[LevIndex].Center);
            }
            if (Main.LocalPlayer.Calamity().monolithLeviathanShader > 0)
            {
                UseTargetPosition(Main.LocalPlayer.Center);
            }
            base.Apply();
        }
    }
}
