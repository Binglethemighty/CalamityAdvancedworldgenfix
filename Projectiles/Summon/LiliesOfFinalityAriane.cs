using System.IO;
using CalamityMod.Buffs.Summon;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using static System.MathF;
using static CalamityMod.Items.Weapons.Summon.LiliesOfFinality;
using static Terraria.ModLoader.ModContent;

namespace CalamityMod.Projectiles.Summon
{
    public class LiliesOfFinalityAriane : BaseMinionProjectile
    {
        #region Members

        private enum EyeFrame
        {
            EyeOpen,
            EyeHalfClosed,
            EyeClosed
        }

        private enum EyeState
        {
            NormalBlinking,
            InStorm
        }

        private EyeState state;
        private int timeInState;

        public int EyeFrameToShow { get; private set; }

        public enum AIState { Idle, ReturnToOwner, Attack }
        public AIState State
        {
            get => (AIState)Projectile.ai[0];
            set
            {
                Projectile.ai[0] = (float)value;

                // The minion'll be able to walk on tiles when they're not in a flying state.
                Projectile.tileCollide = value == AIState.Idle;

                // Because some states mess with the rotation, we reset it so it doesn't cause visual issues on the states that don't deal with rotation.
                Projectile.rotation = 0f;

                // When the minion goes into its attack state, it spawns a lingering AoE.
                if (value == AIState.Attack && Main.myPlayer == Projectile.owner)
                    Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ProjectileType<LiliesOfFinalityAoE>(), Projectile.damage, 0f, Projectile.owner, Projectile.whoAmI);

                Timer = 0f;
            }
        }

        public enum AnimationState { Still, Walk, Fly }
        public AnimationState Animation
        {
            get => (AnimationState)Projectile.ai[1];
            set
            {
                Projectile.ai[1] = (float)value;

                switch (value)
                {
                    case AnimationState.Still:
                        Projectile.width = 32;
                        FrameAmount = 1;
                        break;

                    case AnimationState.Walk:
                        Projectile.width = 32;
                        FrameAmount = 9;
                        break;

                    case AnimationState.Fly:
                        Projectile.width = 56;
                        FrameAmount = 8;
                        break;
                }

                // If the animation state is being set to the same thing every frame, don't reset the animation.
                // Or if the current frame of animation is outside the amount of frames that the next animation will have, reset it.
                if (Animation != value || Projectile.frame + 1 > FrameAmount)
                {
                    Projectile.frame = 0;
                    Projectile.frameCounter = 0;
                }
            }
        }

        private ref float Timer => ref Projectile.ai[2];

        /// <summary>
        /// The amount of animation frames there is in the current animation.
        /// </summary>
        private int FrameAmount = 1;

        private Projectile Elster;

        #endregion

        #region Overridden Members

        public override int AssociatedProjectileTypeID => ProjectileType<LiliesOfFinalityAriane>();
        public override int AssociatedBuffTypeID => BuffType<LiliesOfFinalityBuff>();
        public override ref bool AssociatedMinionBool => ref ModdedOwner.LiliesOfFinalityBool;
        public override float EnemyDistanceDetection => MaxEnemyDistanceDetection;
        public override int AnimationFrames => 18;
        public override bool Grounded => true;

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 66;
            base.SetDefaults();
        }

        public override void CheckMinionExistence()
        {
            if (Elster is null)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p == null || !p.active || p.owner != Projectile.owner || p.type != ProjectileType<LiliesOfFinalityElster>())
                        continue;
                    Elster = p;
                    break;
                }
            }

            Projectile.timeLeft = 2;
            if (Elster is null || !Elster.active || Elster.owner != Projectile.owner || Elster.type != ProjectileType<LiliesOfFinalityElster>())
                Projectile.Kill();
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => false;

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            // If the projectile's above the player, it can fall through platforms to reach the player.
            if (State == AIState.Idle)
                fallThrough = Projectile.Bottom.Y < Owner.Top.Y;
            return true;
        }

        public override bool? CanDamage() => false;

        #endregion

        #region AI

        public override void MinionAI()
        {
            SetStateByPlayerInfo(Owner);
            UpdateEyeFrameToShow(Owner);
            timeInState++;

            switch (State)
            {
                case AIState.Idle:
                    IdleState();
                    break;
                case AIState.ReturnToOwner:
                    ReturnToOwnerState();
                    break;
                case AIState.Attack:
                    AttackState();
                    break;
            }

            Timer++;

            Projectile.netSpam = 0;
            Projectile.netUpdate = true;

            // If on a dedicated server, don't bother running the visuals and sounds to save resources.
            if (Main.dedServ)
                return;

            if (Elster.ModProjectile<LiliesOfFinalityElster>().State == LiliesOfFinalityElster.AIState.ReturnToOwner || Elster.ModProjectile<LiliesOfFinalityElster>().State == LiliesOfFinalityElster.AIState.AttackFlying)
            {
                Dust connectionArianeElster = Dust.NewDustPerfect(
                    Vector2.Lerp(Projectile.Center, Elster.Center, Main.rand.NextFloat()) + Main.rand.NextVector2Circular(5f, 5f),
                    CommonDustID,
                    Vector2.Zero,
                    Scale: Main.rand.NextFloat(0.8f, 1f));
                connectionArianeElster.noGravity = true;
            }
        }

        private void IdleState()
        {
            if (Timer > 15f)
            {
                if (Target is not null)
                {
                    State = AIState.Attack;
                    return;
                }

                if (IsTileBetweenOwnerAndMinionVertically() || !Collision.CanHitLine(Projectile.Center, 1, 1, Owner.Center, 1, 1) || !Projectile.WithinRange(Owner.Center, 960f))
                {
                    State = AIState.ReturnToOwner;
                    return;
                }

                if (!Projectile.getRect().Intersects(Elster.getRect()))
                {
                    int walkDirection = Sign(Elster.Center.X - Projectile.Center.X);
                    float maxSpeed = Utils.Remap(Abs(Elster.Center.X - Projectile.Center.X), 160f, 0f, 8f, 0f);
                    Projectile.velocity.X += 0.08f * walkDirection;
                    if (Abs(Projectile.velocity.X) > maxSpeed)
                        Projectile.velocity.X = maxSpeed * walkDirection;
                }
                else
                {
                    Projectile.velocity.X *= 0.8f;
                    Projectile.spriteDirection = Sign(Elster.Center.X - Projectile.Center.X);
                    Elster.spriteDirection = Sign(Projectile.Center.X - Elster.Center.X);

                    if (!Main.dedServ && Main.rand.NextBool(100))
                    {
                        Vector2 emoteDirection = -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver2 * 0.7f);
                        Particle heart = new LiliesOfFinalityHeartParticle((Main.rand.NextBool() ? Projectile.Center : Elster.Center) + emoteDirection * 15f, emoteDirection * Main.rand.NextFloat(1f, 2f), Main.rand.Next(30, 45), Main.rand.NextFloat(0.6f, 1f));
                        GeneralParticleHandler.SpawnParticle(heart);
                    }
                }
            }

            DoGravity();
            Collision.StepUp(ref Projectile.position, ref Projectile.velocity, Projectile.width, Projectile.height, ref Projectile.stepSpeed, ref Projectile.gfxOffY);
            Projectile.spriteDirection = Sign(Projectile.velocity.X);
            Animation = Abs(Projectile.velocity.X) < 0.04f || IsMinionFacingTile() ? AnimationState.Still : AnimationState.Walk;
        }

        private void ReturnToOwnerState()
        {
            if (Target is not null)
            {
                State = AIState.Attack;
                return;
            }

            if (!IsTileBetweenOwnerAndMinionVertically() && Collision.CanHitLine(Projectile.Center, 1, 1, Owner.Center, 1, 1) && Projectile.WithinRange(Owner.Center, 960f))
            {
                State = AIState.Idle;
                Projectile.velocity.Y = 5f * Sign(Projectile.velocity.Y);
                return;
            }

            FlyTowardsPlace(Owner.velocity.Length() + 8f, Owner.Center, Sign(Projectile.velocity.X));
            Animation = AnimationState.Fly;
        }

        private void AttackState()
        {
            if (Target is not null)
            {
                Vector2 targetSpot = Elster.Center + new Vector2(-100f * Projectile.spriteDirection, -60f);
                if (!Projectile.WithinRange(targetSpot, 8f))
                    FlyTowardsPlace(Ariane_TargettingFlySpeed, targetSpot, Sign(Target.Center.X - Projectile.Center.X));
                else
                    Projectile.velocity *= 0.8f;

                if (Timer >= Ariane_BoltFireRate)
                    ShootBolt();

                Animation = AnimationState.Fly;
            }
            else
                State = AIState.Idle;
        }

        private void ShootBolt()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Vector2 shootDirection = Projectile.SafeDirectionTo(Target.Center).RotatedByRandom(MathHelper.PiOver4);

            Projectile.NewProjectileDirect(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                shootDirection * Ariane_BoltProjectileSpeed,
                ProjectileType<LiliesOfFinalityBolt>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner);

            Timer = 0f;

            // If on a dedicated server, don't bother running the visuals and sounds to save resources.
            if (Main.dedServ)
                return;

            Vector2 visualBoltSpawn = Projectile.Center + shootDirection * Projectile.Size.Length() / 2f;

            for (int i = 0; i < 10; i++)
            {
                Dust shootDust = Dust.NewDustPerfect(
                    visualBoltSpawn + (Vector2.UnitY * MathHelper.Lerp(-8f, 8f, Main.rand.NextFloat())).RotatedBy(visualBoltSpawn.ToRotation()),
                    CommonDustID,
                    shootDirection * Main.rand.NextFloat(3f, 6f),
                    Scale: Main.rand.NextFloat(0.6f, 0.8f));
                shootDust.noGravity = true;
            }

            Particle outerRing = new DirectionalPulseRing(
                visualBoltSpawn,
                Vector2.Zero,
                Color.Red,
                new Vector2(0.4f, 1f),
                shootDirection.ToRotation(),
                0.05f,
                0.3f,
                20);

            Particle innerRing = new DirectionalPulseRing(
                visualBoltSpawn,
                Vector2.Zero,
                Color.Fuchsia,
                new Vector2(0.5f, 1f),
                shootDirection.ToRotation(),
                0.05f,
                0.15f,
                20);

            GeneralParticleHandler.SpawnParticle(outerRing);
            GeneralParticleHandler.SpawnParticle(innerRing);

            SoundEngine.PlaySound(new("CalamityMod/Sounds/Custom/SCalSounds/BrimstoneHellblastSound") { Pitch = 0.4f }, Projectile.Center);
        }

        private void FlyTowardsPlace(float speed, Vector2 place, int spriteDirection)
        {
            Projectile.velocity = (Projectile.velocity * 20f + Projectile.SafeDirectionTo(place) * speed) / 21f;
            Projectile.spriteDirection = spriteDirection;
        }

        #endregion

        #region Syncing

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(FrameAmount);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            FrameAmount = reader.ReadInt32();
        }

        #endregion

        #region Drawing & Animation

        public override void DoAnimation()
        {
            if (Animation == AnimationState.Still)
                return;

            FramesUntilNextAnimationFrame = Animation == AnimationState.Walk ? (int)Utils.Remap(Abs(Projectile.velocity.X), 0f, 6f, 8f, 4f) : 5;

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= FramesUntilNextAnimationFrame)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % FrameAmount;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Texture2D glowTexture = Request<Texture2D>(GlowTexture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(3, 9, (int)Animation, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            SpriteEffects flipSprite = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, flipSprite);
            Main.EntitySpriteDraw(glowTexture, drawPosition, frame, Color.White, Projectile.rotation, origin, Projectile.scale, flipSprite);

            if (Animation == AnimationState.Still || Animation == AnimationState.Walk)
            {
                float yOffset = 12f;
                if (Animation == AnimationState.Walk)
                {
                    switch (Projectile.frame)
                    {
                        default:
                            break;

                        case 1:
                        case 2:
                        case 5:
                        case 6:
                        case 7:
                            yOffset = 14f;
                            break;
                    }
                }

                Texture2D blinkTexture_FirstEye = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/ArianeAndElsterBlink_FirstEye", AssetRequestMode.ImmediateLoad).Value;
                Texture2D blinkTexture_SecondEye = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/ArianeAndElsterBlink_SecondEye", AssetRequestMode.ImmediateLoad).Value;

                Rectangle blinkFrame = blinkTexture_FirstEye.Frame(1, 3, 0, EyeFrameToShow);
                drawPosition += new Vector2(Projectile.spriteDirection == -1 ? 7f : 11f, yOffset);
                Vector2 blinkDrawPos = new Vector2(drawPosition.X, drawPosition.Y);
                Color skinColor = new Color(186, 144, 113, 255);
                Main.EntitySpriteDraw(Projectile.spriteDirection == -1 ? blinkTexture_SecondEye : blinkTexture_FirstEye, blinkDrawPos, blinkFrame, skinColor, Projectile.rotation, origin, Projectile.scale, flipSprite);

                blinkFrame = blinkTexture_SecondEye.Frame(1, 3, 0, EyeFrameToShow);
                blinkDrawPos = new Vector2(drawPosition.X + 8f, drawPosition.Y);
                Main.EntitySpriteDraw(Projectile.spriteDirection == -1 ? blinkTexture_FirstEye : blinkTexture_SecondEye, blinkDrawPos, blinkFrame, skinColor, Projectile.rotation, origin, Projectile.scale, flipSprite);
            }

            return false;
        }

        private void UpdateEyeFrameToShow(Player player)
        {
            EyeFrame eyeFrameToShow = EyeFrame.EyeOpen;
            switch (state)
            {
                case EyeState.NormalBlinking:
                    {
                        int eyeFrameChoiceBasedOnTime = timeInState % 240 - 234;
                        eyeFrameToShow = ((eyeFrameChoiceBasedOnTime >= 4) ? EyeFrame.EyeHalfClosed : ((eyeFrameChoiceBasedOnTime < 2) ? ((eyeFrameChoiceBasedOnTime >= 0) ? EyeFrame.EyeHalfClosed : EyeFrame.EyeOpen) : EyeFrame.EyeClosed));
                        break;
                    }

                case EyeState.InStorm:
                    eyeFrameToShow = ((timeInState % 120 - 114 < 0) ? EyeFrame.EyeHalfClosed : EyeFrame.EyeClosed);
                    break;
            }

            EyeFrameToShow = (int)eyeFrameToShow;
        }

        private void SetStateByPlayerInfo(Player player)
        {
            bool storming = player.ZoneSandstorm || (player.ZoneSnow && Main.IsItRaining);
            bool behindBackWall = false;
            Tile tileSafely = Framing.GetTileSafely(Projectile.Center);
            if (tileSafely != null)
                behindBackWall = tileSafely.WallType > 0;
            if (behindBackWall)
                storming = false;

            if (storming)
                SwitchToState(EyeState.InStorm);
            else
                SwitchToState(EyeState.NormalBlinking);
        }

        private void SwitchToState(EyeState newState, bool resetStateTimerEvenIfAlreadyInState = false)
        {
            if (state != newState || resetStateTimerEvenIfAlreadyInState)
            {
                state = newState;
                timeInState = 0;
            }
        }

        #endregion
    }
}
