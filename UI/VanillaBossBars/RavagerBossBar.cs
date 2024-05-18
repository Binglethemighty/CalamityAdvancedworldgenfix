using System;
using System.Collections.Generic;
using CalamityMod.NPCs.Ravager;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace CalamityMod.UI.VanillaBossBars
{
    public class RavagerBossBar : ModBossBar
    {
        // Used to determine the max health of a multi-segmented boss
        public NPC FalseNPCSegment;
        public List<int> RavagerParts = new List<int>
        {
            NPCType<RavagerClawLeft>(),
            NPCType<RavagerClawRight>(),
            NPCType<RavagerHead>(),
            NPCType<RavagerLegLeft>(),
            NPCType<RavagerLegRight>()
        };

        public override Asset<Texture2D> GetIconTexture(ref Rectangle? iconFrame) => TextureAssets.NpcHeadBoss[NPCID.Sets.BossHeadTextures[NPCType<RavagerBody>()]];

        public override bool? ModifyInfo(ref BigProgressBarInfo info, ref float life, ref float lifeMax, ref float shield, ref float shieldMax)
        {
            NPC target = Main.npc[info.npcIndexToAimAt];
            if (!target.active && !FindRavagerBody(ref info))
                return false;

            life = target.life;
            lifeMax = target.lifeMax;

            // Add max health by feeding the data of false NPCs
            foreach (int type in RavagerParts)
            {
                FalseNPCSegment = new NPC();
                FalseNPCSegment.SetDefaults(type, target.GetMatchingSpawnParams());
                lifeMax += FalseNPCSegment.lifeMax;
            }

            // Determine the current health of the parts
            foreach (NPC part in Main.ActiveNPCs)
            {
                if (RavagerParts.Contains(part.type))
                {
                    life += part.life;
                }
            }
            return true;
        }

        public bool FindRavagerBody(ref BigProgressBarInfo info)
        {
            info.npcIndexToAimAt = NPC.FindFirstNPC(ModContent.NPCType<RavagerBody>());
            return info.npcIndexToAimAt != -1;
        }
    }
}
