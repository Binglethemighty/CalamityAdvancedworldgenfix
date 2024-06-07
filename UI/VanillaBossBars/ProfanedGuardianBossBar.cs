using System;
using System.Collections.Generic;
using CalamityMod.NPCs.ProfanedGuardians;
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
    public class ProfanedGuardianBossBar : ModBossBar
    {
        // Used to determine the max health of a multi-segmented boss
        public NPC FalseNPCSegment;

        public override Asset<Texture2D> GetIconTexture(ref Rectangle? iconFrame)
        {
            // Icons are based on whoever should die first
            if (NPC.AnyNPCs(NPCType<ProfanedGuardianHealer>()))
                return TextureAssets.NpcHeadBoss[NPCID.Sets.BossHeadTextures[NPCType<ProfanedGuardianHealer>()]];

            if (NPC.AnyNPCs(NPCType<ProfanedGuardianDefender>()))
                return TextureAssets.NpcHeadBoss[NPCID.Sets.BossHeadTextures[NPCType<ProfanedGuardianDefender>()]];

            return TextureAssets.NpcHeadBoss[NPCID.Sets.BossHeadTextures[NPCType<ProfanedGuardianCommander>()]];
        }

        public override bool? ModifyInfo(ref BigProgressBarInfo info, ref float life, ref float lifeMax, ref float shield, ref float shieldMax)
        {
            NPC target = Main.npc[info.npcIndexToAimAt];

            if (!target.active)
                return false;

            // Get the commander's health
            life = target.life;
            lifeMax = target.lifeMax;

            // Add max health by feeding the data of false NPCs
            FalseNPCSegment = new NPC();
            FalseNPCSegment.SetDefaults(NPCType<ProfanedGuardianDefender>(), target.GetMatchingSpawnParams());
            lifeMax += FalseNPCSegment.lifeMax;
            FalseNPCSegment.SetDefaults(NPCType<ProfanedGuardianHealer>(), target.GetMatchingSpawnParams());
            lifeMax += FalseNPCSegment.lifeMax;

            // Determine the current health of all guardians
            foreach (NPC guardian in Main.ActiveNPCs)
            {
                if (guardian.type == NPCType<ProfanedGuardianDefender>() || guardian.type == NPCType<ProfanedGuardianHealer>())
                    life += guardian.life;
            }
            return true;
        }
    }
}
