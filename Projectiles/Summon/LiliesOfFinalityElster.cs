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
    public class LiliesOfFinalityElster : BaseMinionProjectile
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

        public enum AIState { Idle, ReturnToOwner, AttackOnGround, AttackFlying }
        public AIState State
        {
            get => (AIState)Projectile.ai[0];
            set
            {
                Projectile.ai[0] = (float)value;

                // Just in case it doesn't reset properly.
                HasShotOnce = false;

                // The minion'll be able to walk on tiles when they're not in a flying state.
                Projectile.tileCollide = value == AIState.Idle || value == AIState.AttackOnGround;

                // Because some states mess with the rotation, we reset it so it doesn't cause visual issues on the states that don't deal with rotation.
                Projectile.rotation = 0f;
            }
        }

        public enum AnimationState { Still, Walk, Fly, Shoot, FlyShoot }
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
                        Projectile.frame = 0;
                        FrameAmount = 1;
                        break;

                    case AnimationState.Walk:
                        Projectile.width = 32;
                        FrameAmount = 9;
                        break;

                    case AnimationState.Fly:
                        Projectile.width = 36;
                        FrameAmount = 4;
                        break;

                    case AnimationState.Shoot:
                    case AnimationState.FlyShoot:
                        Projectile.width = 56;
                        FrameAmount = 7;
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
        /// Because the fire rate is animation frame dependent, it can stay on the same animation for multiple ticks.<br/>
        /// We use this bool to only fire once in that span of time.
        /// </summary>
        private bool HasShotOnce;

        /// <summary>
        /// The amount of animation frames there is in the current animation.
        /// </summary>
        private int FrameAmount = 1;

        #endregion

        #region Overridden Members

        public override int AssociatedProjectileTypeID => ProjectileType<LiliesOfFinalityElster>();
        public override int AssociatedBuffTypeID => BuffType<LiliesOfFinalityBuff>();
        public override ref bool AssociatedMinionBool => ref ModdedOwner.LiliesOfFinalityBool;
        public override float EnemyDistanceDetection => MaxEnemyDistanceDetection;
        public override int AnimationFrames => 28;
        public override bool Grounded => true;

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 56;
            base.SetDefaults();
        }

        public override void CheckMinionExistence()
        {
            base.CheckMinionExistence();
            if (Timer > 10f && Main.player[Projectile.owner].ownedProjectileCounts[ProjectileType<LiliesOfFinalityAriane>()] == 0)
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
                case AIState.AttackOnGround:
                    AttackOnGroundState();
                    break;
                case AIState.AttackFlying:
                    AttackFlyingState();
                    break;
            }

            Timer++;

            Projectile.netSpam = 0;
            Projectile.netUpdate = true;
        }

        private void IdleState()
        {
            if (Timer > 15f)
            {
                if (Target is not null)
                {
                    State = AIState.AttackOnGround;
                    return;
                }

                if (IsTileBetweenOwnerAndMinionVertically() || !Collision.CanHitLine(Projectile.Center, 1, 1, Owner.Center, 1, 1) || !Projectile.WithinRange(Owner.Center, 960f))
                {
                    State = AIState.ReturnToOwner;
                    return;
                }

                Vector2 idlePosition = Owner.Center - Vector2.UnitX * 60f * Owner.direction;
                if (!Projectile.WithinRange(idlePosition, 8f))
                {
                    int walkDirection = Sign(idlePosition.X - Projectile.Center.X);
                    float maxSpeed = Utils.Remap(Abs(idlePosition.X - Projectile.Center.X), 160f, 0f, 8f, 0f);
                    Projectile.velocity.X += 0.08f * walkDirection;
                    if (Abs(Projectile.velocity.X) > maxSpeed)
                        Projectile.velocity.X = maxSpeed * walkDirection;
                }
                else
                    Projectile.velocity.X = 0f;
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
                State = AIState.AttackOnGround;
                return;
            }

            if (!IsTileBetweenOwnerAndMinionVertically() && Collision.CanHitLine(Projectile.Center, 1, 1, Owner.Center, 1, 1) && Projectile.WithinRange(Owner.Center, 960f))
            {
                State = AIState.Idle;

                // Does a little hop to confirm the minion reaches the desired height.
                Projectile.velocity.Y = 5f * Sign(Projectile.velocity.Y);

                return;
            }

            FlyTowardsPlace(Owner.velocity.Length() + 8f, Owner.Center, Sign(Projectile.velocity.X), AnimationState.Fly);
            Projectile.rotation = MathHelper.ToRadians(Projectile.velocity.Length()) * Sign(Projectile.velocity.X);
            ElevationDust(false);
        }

        private void AttackOnGroundState()
        {
            if (Target is not null)
            {
                if (Projectile.Center.Y > Target.Bottom.Y || Projectile.Center.Y < Target.Top.Y)
                {
                    State = AIState.AttackFlying;
                    return;
                }

                // If the frame of the animation is the shooting one (and hasn't shot once yet): shoot.
                if (Projectile.frame == 2 && !HasShotOnce)
                    ShootBullet();

                // Reset the bool for the next shot.
                if (Projectile.frame != 2 && HasShotOnce)
                    HasShotOnce = false;

                DoGravity();
                Projectile.velocity.X = 0f;
                Projectile.spriteDirection = Sign(Target.Center.X - Projectile.Center.X);
                Animation = AnimationState.Shoot;
            }
            else
                State = AIState.Idle;
        }

        private void AttackFlyingState()
        {
            if (Target is not null)
            {
                Vector2 targetSpot = Target.Center - Vector2.UnitX * (Elster_DistanceFromTarget + Target.width / 2f) * Sign(Target.Center.X - Projectile.Center.X);
                if (!Projectile.WithinRange(targetSpot, 5f))
                    FlyTowardsPlace(Elster_TargettingFlySpeed, targetSpot, Sign(Target.Center.X - Projectile.Center.X), AnimationState.FlyShoot);
                else
                    Projectile.velocity *= 0.8f;

                // If the frame of the animation is the shooting one (and hasn't shot once yet): shoot.
                if (Projectile.frame == 2 && !HasShotOnce)
                    ShootBullet();

                // Reset the bool for the next shot.
                if (Projectile.frame != 2 && HasShotOnce)
                    HasShotOnce = false;

                ElevationDust(true);
            }
            else
                State = AIState.Idle;
        }

        private void FlyTowardsPlace(float speed, Vector2 place, int spriteDirection, AnimationState spriteAnimation)
        {
            Projectile.velocity = (Projectile.velocity * 20f + Projectile.SafeDirectionTo(place) * speed) / 21f;
            Projectile.spriteDirection = spriteDirection;
            Animation = spriteAnimation;
        }

        private void ShootBullet()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Vector2 bulletSpawnPosition = Projectile.Center - Vector2.UnitX * 15f * Projectile.spriteDirection;
            Vector2 bulletVelocity = CalamityUtils.CalculatePredictiveAimToTargetMaxUpdates(bulletSpawnPosition, Target, Elster_BulletProjectileSpeed, Elster_BulletMaxUpdates);

            Projectile.NewProjectileDirect(
                Projectile.GetSource_FromThis(),
                bulletSpawnPosition,
                //Vector2.UnitX * Elster_BulletProjectileSpeed * Projectile.spriteDirection,
                bulletVelocity,
                ProjectileType<LiliesOfFinalityBullet>(),
                (int)(Projectile.damage * 1.2f),
                Projectile.knockBack,
                Projectile.owner);

            HasShotOnce = true;

            // If on a dedicated server, don't bother running the visuals and sounds to save resources.
            if (Main.dedServ)
                return;

            Particle shootRing = new DirectionalPulseRing(
                Projectile.Center + Vector2.UnitX * Projectile.width / 2f * Projectile.spriteDirection,
                Vector2.Zero,
                Color.Goldenrod,
                new Vector2(0.5f, 1f),
                0f,
                0.05f,
                0.25f,
                7);
            GeneralParticleHandler.SpawnParticle(shootRing);

            SoundEngine.PlaySound(new("CalamityMod/Sounds/Custom/ElsterShot", 4) { Volume = 0.3f }, Projectile.Center);
        }

        private void ElevationDust(bool shootingState)
        {
            // If on a dedicated server, don't bother running the visuals and sounds to save resources.
            if (Main.dedServ)
                return;

            Vector2 leftLeg = shootingState ? Projectile.BottomLeft + Vector2.UnitX * (Projectile.spriteDirection == -1 ? 28f : 10f) : Projectile.BottomLeft;
            Vector2 rightLeg = shootingState ? Projectile.BottomRight - Vector2.UnitX * (Projectile.spriteDirection == -1 ? 10f : 28f) : Projectile.BottomRight;

            if (Main.rand.NextBool())
            {
                float interpolant = Main.rand.NextFloat();
                Dust elevationDust = Dust.NewDustPerfect(
                    Vector2.Lerp(leftLeg, rightLeg, interpolant),
                    CommonDustID,
                    Vector2.UnitY.RotatedByRandom(MathHelper.Lerp(-MathHelper.ToRadians(15f), MathHelper.ToRadians(15f), interpolant) * Main.rand.NextFloat(8f, 12f)),
                    Scale: Main.rand.NextFloat(1f, 1.2f));
                elevationDust.noGravity = true;
            }

            if (Main.rand.NextBool(4))
            {
                float interpolant = Main.rand.NextFloat();
                Dust elevationDust = Dust.NewDustPerfect(
                    Vector2.Lerp(leftLeg, rightLeg, interpolant),
                    CommonDustID,
                    -Vector2.UnitY * Main.rand.NextFloat(3f, 6f),
                    Scale: Main.rand.NextFloat(1f, 1.2f));
                elevationDust.noGravity = true;
            }
        }

        #endregion

        #region Syncing

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(HasShotOnce);
            writer.Write(FrameAmount);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            HasShotOnce = reader.ReadBoolean();
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
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(5, 9, (int)Animation, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            SpriteEffects flipSprite = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, flipSprite);

            if (Animation == AnimationState.Still || Animation == AnimationState.Walk)
            {
                float yOffset = -2f;
                if (Animation == AnimationState.Walk)
                {
                    switch (Projectile.frame)
                    {
                        default:
                            break;

                        case 0:
                        case 1:
                            yOffset = 0f;
                            break;

                        case 6:
                        case 7:
                            yOffset = -4f;
                            break;
                    }
                }

                Texture2D blinkTexture_FirstEye = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/ArianeAndElsterBlink_FirstEye", AssetRequestMode.ImmediateLoad).Value;
                Texture2D blinkTexture_SecondEye = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/ArianeAndElsterBlink_SecondEye", AssetRequestMode.ImmediateLoad).Value;

                Rectangle blinkFrame = blinkTexture_FirstEye.Frame(1, 3, 0, EyeFrameToShow);
                drawPosition += new Vector2(Projectile.spriteDirection == -1 ? 2f : 6f, yOffset);
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
                        int eyeFrameChoiceBasedOnTime = timeInState % 300 - 294;
                        eyeFrameToShow = ((eyeFrameChoiceBasedOnTime >= 4) ? EyeFrame.EyeHalfClosed : ((eyeFrameChoiceBasedOnTime < 2) ? ((eyeFrameChoiceBasedOnTime >= 0) ? EyeFrame.EyeHalfClosed : EyeFrame.EyeOpen) : EyeFrame.EyeClosed));
                        break;
                    }

                case EyeState.InStorm:
                    eyeFrameToShow = ((timeInState % 150 - 144 < 0) ? EyeFrame.EyeHalfClosed : EyeFrame.EyeClosed);
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
