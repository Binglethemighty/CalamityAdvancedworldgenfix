using System;
using CalamityMod.CalPlayer;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Pets
{
    public class LadShark : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Pets";
        public override void SetStaticDefaults()
        {
            Main.projPet[Projectile.type] = true;

            ProjectileID.Sets.CharacterPreviewAnimations[Projectile.type] = ProjectileID.Sets.SimpleLoop(0, 0, 1)
            .WithOffset(-12f, -12f).WithSpriteDirection(-1);
        }

        public override void SetDefaults()
        {
            Projectile.netImportant = true;
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft *= 5;
            Projectile.aiStyle = ProjAIStyleID.Pet;
            AIType = ProjectileID.BabySkeletronHead;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            CalamityPlayer modPlayer = player.Calamity();
            if (!player.active)
            {
                Projectile.active = false;
                return;
            }
            if (player.dead)
            {
                modPlayer.ladShark = false;
            }
            if (modPlayer.ladShark)
            {
                Projectile.timeLeft = 2;
            }
            Projectile.rotation += (Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y)) * 0.01f * (float)Projectile.direction;

            //occasionally burst in hearts
            if (Main.rand.NextBool(10000))
            {
                if (Projectile.owner == Main.myPlayer)
                {
                    if (Main.netMode != NetmodeID.Server)
                    {
                        int heartCount = Main.rand.Next(20, 31);
                        for (int i = 0; i < heartCount; i++)
                        {
                            Vector2 velocity = new Vector2((float)Main.rand.Next(-10, 11), (float)Main.rand.Next(-10, 11));
                            velocity.Normalize();
                            velocity.X *= 0.66f;
                            int heart = Gore.NewGore(Projectile.GetSource_FromAI(), Projectile.Center, velocity * Main.rand.NextFloat(3f, 5f) * 0.33f, 331, Main.rand.NextFloat(40f, 120f) * 0.01f);
                            Main.gore[heart].sticky = false;
                            Main.gore[heart].velocity *= 5f;
                        }
                    }

                    SoundEngine.PlaySound(SoundID.Zombie15, Projectile.position); //mouse squeak sound

                    float radius = 240f; // 15 blocks
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.dontTakeDamage && Vector2.Distance(Projectile.Center, npc.Center) <= radius)
                        {
                            if (npc.Calamity().ladHearts <= 0)
                                npc.Calamity().ladHearts = CalamityUtils.SecondsToFrames(9f);
                        }
                    }
                    foreach (Player players in Main.ActivePlayers)
                    {
                        if (!players.dead && Vector2.Distance(Projectile.Center, players.Center) <= radius)
                        {
                            if (players.Calamity().ladHearts <= 0)
                                players.Calamity().ladHearts = CalamityUtils.SecondsToFrames(9f);
                        }
                    }
                }
            }
        }
    }
}
