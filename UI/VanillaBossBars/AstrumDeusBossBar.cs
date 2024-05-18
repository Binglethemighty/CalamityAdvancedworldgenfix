using System;
using System.Collections.Generic;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.World;
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
    public class AstrumDeusBossBar : ModBossBar
    {
        public override Asset<Texture2D> GetIconTexture(ref Rectangle? iconFrame) => TextureAssets.NpcHeadBoss[NPCID.Sets.BossHeadTextures[NPCType<AstrumDeusHead>()]];

        public override bool? ModifyInfo(ref BigProgressBarInfo info, ref float life, ref float lifeMax, ref float shield, ref float shieldMax)
        {
            NPC target = Main.npc[info.npcIndexToAimAt];
            if (!target.active && !FindMoreWorms(ref info))
                return false;

            // Reset the health
            life = 0f;
            lifeMax = 0f;

            // Determine the real health by finding more of itself
            foreach (NPC worm in Main.ActiveNPCs)
            {
                if (worm.type == target.type)
                {
                    // In Death Mode, every worm must be killed
                    if (CalamityWorld.death)
                    {
                        life += worm.life;
                        lifeMax += worm.lifeMax;
                    }
                    // Otherwise, find the minimum
                    else
                    {
                        if (life <= 0)
                            life = worm.life;
                        else
                            life = Math.Min(life, worm.life);

                        lifeMax = worm.lifeMax;
                    }
                }
            }
            return true;
        }

        public bool FindMoreWorms(ref BigProgressBarInfo info)
        {
            info.npcIndexToAimAt = NPC.FindFirstNPC(ModContent.NPCType<AstrumDeusHead>());
            return info.npcIndexToAimAt != -1;
        }
    }
}
