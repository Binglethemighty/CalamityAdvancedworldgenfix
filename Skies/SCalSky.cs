using CalamityMod.NPCs;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace CalamityMod.Skies
{
    public class SCalSky : CustomSky
    {
        public class Cinder
        {
            public int Time;
            public int Lifetime;
            public int IdentityIndex;
            public float Scale;
            public float Depth;
            public Color DrawColor;
            public Vector2 Velocity;
            public Vector2 Center;
            public Cinder(int lifetime, int identity, float depth, Color color, Vector2 startingPosition, Vector2 startingVelocity)
            {
                Lifetime = lifetime;
                IdentityIndex = identity;
                Depth = depth;
                DrawColor = color;
                Center = startingPosition;
                Velocity = startingVelocity;
            }
        }

        private bool isActive = false;
        private float intensity = 0f;
        private int SCalIndex = -1;
        public List<Cinder> Cinders = new List<Cinder>();
        public static int CinderReleaseChance
        {
            get
            {
                if (!Main.npc.IndexInRange(CalamityGlobalNPC.SCal) || Main.npc[CalamityGlobalNPC.SCal].type != ModContent.NPCType<SupremeCalamitas>())
                    return int.MaxValue;

                NPC scal = Main.npc[CalamityGlobalNPC.SCal];
                float lifeRatio = scal.life / (float)scal.lifeMax;

                // Release a good amount of cinders during bullet hells. If this is too distracting this can be
                // inverted so that less appear than usual instead.
                if (scal.ModNPC<SupremeCalamitas>().bulletHellCounter2 % 900 != 0)
                    return lifeRatio <= 0.1f ? 8 : 11;

                // Release more and more cinders at the end of the battle until the acceptance phase, where it all slows down dramatically, with just a few ashes.
                if (lifeRatio < 0.1f)
                {
                    if (lifeRatio <= 0.01f)
                        return 32;

                    return (int)Math.Round(MathHelper.Lerp(2f, 11f, Utils.InverseLerp(0.03f, 0.1f, lifeRatio, true)));
                }

                // Release a good amount of cinders while brothers or Sepulcher-1 are alive. Sepulcher-2 falls into an above case and does not execute this return.
                if (NPC.AnyNPCs(ModContent.NPCType<SupremeCataclysm>()) || NPC.AnyNPCs(ModContent.NPCType<SupremeCatastrophe>()) || NPC.AnyNPCs(ModContent.NPCType<SCalWormHead>()))
                    return 10;

                // Release a moderate amount of cinders normally.
                return 18;
            }
        }
        public static float CinderSpeed
        {
            get
            {
                if (!Main.npc.IndexInRange(CalamityGlobalNPC.SCal) || Main.npc[CalamityGlobalNPC.SCal].type != ModContent.NPCType<SupremeCalamitas>())
                    return 0f;

                NPC scal = Main.npc[CalamityGlobalNPC.SCal];
                float lifeRatio = scal.life / (float)scal.lifeMax;

                // Move more and more quickly at the end of the battle until the acceptance phase, where it all slows down dramatically.
                if (lifeRatio < 0.1f)
                {
                    if (lifeRatio <= 0.01f)
                        return 4.5f;

                    return MathHelper.Lerp(10.75f, 6.7f, Utils.InverseLerp(0.03f, 0.1f, lifeRatio, true));
                }

                // Move a little quickly while brothers or Sepulcher-1 are alive. Sepulcher-2 falls into an above case and does not execute this return.
                if (NPC.AnyNPCs(ModContent.NPCType<SupremeCataclysm>()) || NPC.AnyNPCs(ModContent.NPCType<SupremeCatastrophe>()) || NPC.AnyNPCs(ModContent.NPCType<SCalWormHead>()))
                    return 7.4f;

                // Move moderately quickly usually.
                return 5.6f;
            }
        }
        public static float OverridingIntensity = 0f;

        public override void Update(GameTime gameTime)
        {
            if (isActive && intensity < 1f)
            {
                intensity += 0.01f;
            }
            else if (!isActive && intensity > 0f)
            {
                intensity -= 0.01f;
            }

            Color selectCinderColor()
            {
                if (!Main.npc.IndexInRange(CalamityGlobalNPC.SCal) || Main.npc[CalamityGlobalNPC.SCal].type != ModContent.NPCType<SupremeCalamitas>())
                    return Color.Transparent;

                NPC scal = Main.npc[CalamityGlobalNPC.SCal];
                float lifeRatio = scal.life / (float)scal.lifeMax;
                if (lifeRatio > 0.5f)
                    return Color.Lerp(Color.Red, Color.Orange, Main.rand.NextFloat(0.8f));
                else if (lifeRatio > 0.3f)
                    return Color.Lerp(Color.Blue, Color.DarkBlue, Main.rand.NextFloat() * 0.65f);
                else if (lifeRatio > 0.01f)
                    return Color.Lerp(Color.Red, Color.Yellow, Main.rand.NextFloat(0.2f, 0.9f));
                else
                    return Color.Gray;
            }

            // Randomly add cinders.
            if (intensity >= 1f && Main.rand.NextBool(CinderReleaseChance))
            {
                int lifetime = Main.rand.Next(285, 445);
                float depth = Main.rand.NextFloat(1.8f, 5f);
                Vector2 startingPosition = Main.screenPosition + new Vector2(Main.screenWidth * Main.rand.NextFloat(-0.1f, 1.1f), Main.screenHeight * 1.05f);
                Vector2 startingVelocity = -Vector2.UnitY.RotatedByRandom(0.91f);
                Cinders.Add(new Cinder(lifetime, Cinders.Count, depth, selectCinderColor(), startingPosition, startingVelocity));
            }

            // Update all cinders.
            for (int i = 0; i < Cinders.Count; i++)
            {
                Cinders[i].Scale = Utils.InverseLerp(Cinders[i].Lifetime, Cinders[i].Lifetime - 50, Cinders[i].Time, true) * Utils.InverseLerp(0f, 50f, Cinders[i].Time, true);
                Cinders[i].Scale *= MathHelper.Lerp(0.55f, 0.8f, Cinders[i].IdentityIndex % 6f / 6f);

                Vector2 idealVelocity = -Vector2.UnitY.RotatedBy(MathHelper.Lerp(-0.94f, 0.94f, (float)Math.Sin(Cinders[i].Time / 36f + Cinders[i].IdentityIndex) * 0.5f + 0.5f)) * CinderSpeed;
                float movementInterpolant = MathHelper.Lerp(0.01f, 0.08f, Utils.InverseLerp(45f, 145f, Cinders[i].Time, true));
                Cinders[i].Velocity = Vector2.Lerp(Cinders[i].Velocity, idealVelocity, movementInterpolant);
                Cinders[i].Velocity = Cinders[i].Velocity.SafeNormalize(-Vector2.UnitY) * CinderSpeed;
                Cinders[i].Time++;

                Cinders[i].Center += Cinders[i].Velocity;
            }

            // Clear away all dead cinders.
            Cinders.RemoveAll(c => c.Time >= c.Lifetime);
        }

        private float GetIntensity()
        {
            if (CalamityUtils.CountProjectiles(ModContent.ProjectileType<SCalRitualDrama>()) > 0)
                return OverridingIntensity;

            OverridingIntensity = 0f;
            if (UpdateSCalIndex())
            {
                float x = 0f;
                if (SCalIndex != -1)
                    x = Vector2.Distance(Main.player[Main.myPlayer].Center, Main.npc[this.SCalIndex].Center);

                return 1f - Utils.SmoothStep(3000f, 6000f, x);
            }
            return 0f;
        }

        public override Color OnTileColor(Color inColor) => new Color(Vector4.Lerp(new Vector4(0.5f, 0.8f, 1f, 1f), inColor.ToVector4(), 1f - GetIntensity()));

        private bool UpdateSCalIndex()
        {
            int SCalType = ModContent.NPCType<SupremeCalamitas>();
            if (SCalIndex >= 0 && Main.npc[SCalIndex].active && Main.npc[SCalIndex].type == SCalType)
            {
                return true;
            }
            SCalIndex = -1;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == SCalType)
                {
                    SCalIndex = i;
                    break;
                }
            }
            return SCalIndex != -1;
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (maxDepth >= 0 && minDepth < 0)
            {
                float intensity = GetIntensity();
                spriteBatch.Draw(Main.blackTileTexture, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * intensity);
            }

            // Draw cinders.
            Texture2D cinderTexture = ModContent.GetTexture("CalamityMod/ExtraTextures/CalamitasCinder");
            for (int i = 0; i < Cinders.Count; i++)
            {
                Vector2 drawPosition = Cinders[i].Center - Main.screenPosition;
                spriteBatch.Draw(cinderTexture, drawPosition, null, Cinders[i].DrawColor, 0f, cinderTexture.Size() * 0.5f, Cinders[i].Scale, SpriteEffects.None, 0f);
            }
        }

        public override float GetCloudAlpha()
        {
            return 0f;
        }

        public override void Activate(Vector2 position, params object[] args)
        {
            isActive = true;
        }

        public override void Deactivate(params object[] args)
        {
            isActive = false;
        }

        public override void Reset()
        {
            isActive = false;
        }

        public override bool IsActive()
        {
            return isActive || intensity > 0f;
        }
    }
}
