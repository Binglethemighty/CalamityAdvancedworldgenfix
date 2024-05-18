using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.Items.Weapons.Summon.HarvestStaff;

namespace CalamityMod.Projectiles.Summon
{
    public class HarvestStaffMinion : ModProjectile, ILocalizedModType
    {
        #region Members and Enums

        public new string LocalizationCategory => "Projectiles.Summon";

        /// <summary>
        /// The size of the pumpkin.
        /// </summary>
        public ref float Variant => ref Projectile.ai[0];

        /// <summary>
        /// The states of behaviour of this pumpkin.
        /// </summary>
        public enum AIState { Still, Idle, Attack }

        /// <summary>
        /// The current state of behaviour of this pumpkin.
        /// </summary>
        public AIState State
        {
            get => (AIState)Projectile.ai[1];
            set
            {
                Projectile.ai[1] = (float)value;

                if (value == AIState.Attack)
                {
                    Animation = AnimationState.Run;
                    SoundEngine.PlaySound(_screamSound, Projectile.Center);
                }
                else if (value == AIState.Idle)
                    Animation = AnimationState.Idle;
            }
        }

        /// <summary>
        /// The maximum time that the pumpkin will walk on its idle state.<br/>
        /// It's set to a random amount at a random moment.
        /// </summary>
        public int IdleWalkingTime { get; set; }

        /// <summary>
        /// The timer on which the pumpkin is while walking.<br/>
        /// When the timer reaches 0, the pumpkin will stop walking.
        /// </summary>
        public int IdleWalkingTimer { get; set; }

        /// <summary>
        /// The direction in which the pumpkin walks while idle.<br/>
        /// If the pumpkin is within a certain range from the sentry (Or if it doesn't exist), it'll go to a random direction.<br/>
        /// Otherwise, it'll try to always be nearby the sentry.
        /// </summary>
        public int IdleWalkingDirection { get; set; }

        /// <summary>
        /// Keeps track of the amount of jumps that the pumpkin has done when the owner is nearby.<br/>
        /// When it reaches a certain amount, the minion will go on a cooldown before jumping again.
        /// </summary>
        public int IdleJumpCount { get; set; }

        /// <summary>
        /// The cooldown of the pumpkin for jumping again.<br/>
        /// When it reaches 0, the pumpkin will stop walking.
        /// </summary>
        public int IdleJumpCooldown { get; set; }

        /// <summary>
        /// The states of the animation that the pumpkin has.<br/>
        /// Their indeces are used when selecting which spritesheet it uses.
        /// </summary>
        public enum AnimationState { None = -1, Grow, Rise, Idle, Run, Jump }

        /// <summary>
        /// The current state of animation of the pumpkin.
        /// </summary>
        public AnimationState Animation
        {
            get => (AnimationState)Projectile.ai[2];
            set
            {
                if (value != Animation)
                {
                    Projectile.frame = 0;
                    Projectile.frameCounter = 0;
                }

                Projectile.ai[2] = (float)value;

                switch (value)
                {
                    case AnimationState.Grow:
                        AnimationFrames = Variant == 0 ? 6 : 4;
                        FramesUntilNextAnimationFrame = 6;
                        break;

                    case AnimationState.Rise:
                        AnimationFrames = 10;
                        FramesUntilNextAnimationFrame = 5;
                        break;

                    case AnimationState.Idle:
                    case AnimationState.Jump:
                        AnimationFrames = 1;
                        FramesUntilNextAnimationFrame = 0;
                        break;

                    case AnimationState.Run:
                        AnimationFrames = 6;
                        FramesUntilNextAnimationFrame = 5;
                        break;
                }
            }
        }

        /// <summary>
        /// The amount of frames that the current animation's spritesheet has.
        /// </summary>
        public int AnimationFrames { get; set; }

        /// <summary>
        /// The amount of time, in frames, that it'll take to go to the next frame of animation.<br/>
        /// Defaults to 1 so it doesn't divide by 0.
        /// </summary>
        public int FramesUntilNextAnimationFrame { get; set; } = 1;

        /// <summary>
        /// A convienent bool for when the animation has been completed.<br/>
        /// Ends at <see cref="AnimationFrames"/> - 1 because <see cref="Projectile.frame"/> starts at 0.
        /// </summary>
        public bool CompletedAnimation => Projectile.frame >= AnimationFrames - 1;

        /// <summary>
        /// A convient way to set the direction of the sprite without typing it out the long way.
        /// </summary>
        public int Direction
        {
            get => Projectile.spriteDirection;
            set => Projectile.spriteDirection = Projectile.direction = value;
        }

        private SoundStyle _growSound = new("CalamityMod/Sounds/Custom/PumpkinEmerge", 3);

        private SoundStyle _idleSound = new("CalamityMod/Sounds/Custom/PumpkinIdle", 4);

        private SoundStyle _idleRareSound = new("CalamityMod/Sounds/Custom/PumpkinRareIdle") { Volume = 0.2f };

        private SoundStyle _screamSound = new("CalamityMod/Sounds/Custom/PumpkinScream", 2) { Volume = 0.5f };

        private SoundStyle _jumpSound = new("CalamityMod/Sounds/Custom/PumpkinJump");

        private SoundStyle _boomSound = new("CalamityMod/Sounds/Custom/PumpkinExplode", 2) { Volume = 0.6f };

        private SoundStyle _boomSoundGFB = new("CalamityMod/Sounds/Custom/PumpkinExplodeGFB", 2);

        /// <summary>
        /// The owner of this minion.
        /// </summary>
        public Player Owner { get; set; }

        public Player AnyPlayer
        {
            get
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p == null || !p.active || p.dead || !Projectile.Center.WithinRange(p.Center, 64f))
                        continue;
                    return p;
                }
                return null;
            }
        }

        /// <summary>
        /// The target of this minion.
        /// </summary>
        public NPC Target { get; set; }

        /// <summary>
        /// The sentry of this minion.
        /// </summary>
        public Projectile MySentry
        {
            get
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (proj == null || !proj.active || proj.owner != Projectile.owner || proj.type != ModContent.ProjectileType<HarvestStaffSentry>())
                        continue;
                    return proj;
                }
                return null;
            }
        }

        public bool KILLYOURSELF { get; set; }

        #endregion

        #region AI and Collisions

        public override void AI()
        {
            Owner ??= Main.player[Projectile.owner];
            Projectile.width = Projectile.height = Variant == 0 ? 28 : (Variant == 1 ? 22 : 20);

            if (Animation == AnimationState.Grow && (MySentry is null || Projectile.Distance(MySentry.Center) > 600f))
                Projectile.Kill();

            Target = Projectile.Center.MinionHoming(State == AIState.Still ? PlantedEnemyDistanceDetection : NormalEnemyDistanceDetection, Owner, false);

            switch (State)
            {
                case AIState.Still:
                    StillState();
                    break;
                case AIState.Idle:
                    IdleState();
                    break;
                case AIState.Attack:
                    AttackState();
                    break;
            }

            if (IdleJumpCooldown > 0)
                IdleJumpCooldown--;

            if (KILLYOURSELF)
                Projectile.Kill();

            Projectile.timeLeft = 2;
            DoGravity();
            DoAnimation();
            NetUpdate();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // When the pumpkin touches the ground after spawning, start the growing animation.
            if (Animation == AnimationState.None)
                Animation = AnimationState.Grow;

            if (State == AIState.Idle && Projectile.velocity.Y == 0f)
            {
                // While the pumpkin's idle and standing still on the ground, when the player gets close it'll jump.
                if (AnyPlayer is not null && Projectile.WithinRange(AnyPlayer.Center, 64f) && IdleJumpCooldown == 0)
                    NearOwnerJump();
                else
                    Animation = IdleWalkingTimer == 0f ? AnimationState.Idle : AnimationState.Run;
            }

            // If the minion's standing still at a certain distance from the target: jump.
            if (Target is not null && State == AIState.Attack && Projectile.velocity.Y == 0f)
            {
                if (MathF.Abs(Target.Center.X - Projectile.Center.X) < 160f && Target.Top.Y < Projectile.Bottom.Y)
                {
                    if (PlatformBetweenMinionAndTarget(out Vector2 platformPosition))
                        JumpTowards(platformPosition - Vector2.UnitY * 32f);
                    else
                        JumpTowards(Target.Top);
                }
                else
                    Animation = AnimationState.Run;
            }

            return false;
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = Target is not null && Projectile.Bottom.Y < Target.Top.Y;
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            if (State != AIState.Attack)
                return;

            Projectile.ExpandHitboxBy(4f);
            Projectile.Damage();

            for (int i = 0; i < (int)Utils.Remap(Variant, 0f, 2f, 4f, 2f); i++)
            {
                float angle = MathHelper.TwoPi / 4 * i;
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(3f, 5f);
                Gore gore = Gore.NewGoreDirect(Projectile.GetSource_Death(), Projectile.Center, velocity, Mod.Find<ModGore>($"PumpkinGore{Main.rand.Next(6) + 1}").Type, Utils.Remap(Variant, 0f, 2f, 1f, 0.5f));
                gore.timeLeft = 15;
            }

            KILLYOURSELF = true;

            if (Main.dedServ)
                return;

            for (int i = 0; i < 20; i++)
            {
                if (BirthdayParty.PartyIsUp)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.Next(139, 143), (new Vector2(2, 2).RotatedByRandom(100) * Main.rand.NextFloat(0.5f, 1.5f)) + new Vector2(0, -0.75f));
                    dust.scale = 0.8f;
                }
                else
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(4) ? 278 : 51, (new Vector2(2, 2).RotatedByRandom(100) * Main.rand.NextFloat(0.5f, 1.5f)) + new Vector2(0, -0.75f));
                    dust.noGravity = false;
                    dust.scale = Main.rand.NextFloat(0.8f, 1.4f);
                    dust.color = Color.Chocolate;
                }
            }

            if (Main.zenithWorld)
                SoundEngine.PlaySound(_boomSoundGFB, Projectile.Center);
            else
                SoundEngine.PlaySound(_boomSound, Projectile.Center);
        }

        /// <summary>
        /// The behaviour of this summon while it is planted on the ground.
        /// </summary>
        public void StillState()
        {
            // When the pumpkin has done its growing animation and there's a target nearby or the owner's on top of it,
            // it'll do the jump out animation.
            if ((Target is not null || (AnyPlayer is not null && Projectile.getRect().Intersects(AnyPlayer.getRect()))) && Animation == AnimationState.Grow && CompletedAnimation)
            {
                Animation = AnimationState.Rise;
                SoundEngine.PlaySound(_growSound, Projectile.Center);
            }

            // And when they have completed their jumping out animation, they idle until they find a target.
            else if (Animation == AnimationState.Rise && CompletedAnimation)
                State = AIState.Idle;
        }

        /// <summary>
        /// The behaviour of this usmmon while it's idle.
        /// </summary>
        public void IdleState()
        {
            // If a target has been detected, go to the attack state.
            if (Target is not null)
            {
                State = AIState.Attack;
                return;
            }

            // If the pumpkin's not already walking, at a random chance it'll decide to walk.
            if (IdleWalkingTimer == 0f && Main.rand.NextBool(400))
            {
                IdleWalkingTime = IdleWalkingTimer = Main.rand.Next(60, 180);
                IdleWalkingDirection = MySentry == null || Projectile.WithinRange(MySentry.Center, 960f) ? (Main.rand.NextBool() ? -1 : 1) : MathF.Sign(MySentry.Center.X - Projectile.Center.X);
            }

            else if (IdleWalkingTimer != 0f)
            {
                // The pumpkin will accelerate when starting to walk and deaccelerate when it's about to end.
                Projectile.velocity.X = MathHelper.Lerp(0f, 3f, CalamityUtils.Convert01To010(Utils.GetLerpValue(0f, IdleWalkingTime, IdleWalkingTimer))) * IdleWalkingDirection;

                Direction = MathF.Sign(Projectile.velocity.X);

                IdleWalkingTimer--;
                if (IdleWalkingTimer == 0f)
                {
                    Projectile.velocity.X = 0f;
                    Animation = AnimationState.Idle;
                }
            }

            // When the pumpkin encounters a 1-tile-height obstacle, it'll climb it, like the player.
            Collision.StepUp(ref Projectile.position, ref Projectile.velocity, Projectile.width, Projectile.height, ref Projectile.stepSpeed, ref Projectile.gfxOffY);

            if (Main.rand.NextBool(700))
                SoundEngine.PlaySound(Main.rand.NextBool(20) ? _idleRareSound : _idleSound, Projectile.Center);
        }

        /// <summary>
        /// The behaviour of this summon in its attack state.
        /// </summary>
        public void AttackState()
        {
            if (Target is not null)
            {
                MoveToTarget();
                Direction = MathF.Sign(Projectile.velocity.X);

                // When the pumpkin encounters a 1-tile-height obstacle, it'll climb it, like the player.
                Collision.StepUp(ref Projectile.position, ref Projectile.velocity, Projectile.width, Projectile.height, ref Projectile.stepSpeed, ref Projectile.gfxOffY);

                // Pumpkins relase a bit of fire from their heads when running after enemies
                if (!Main.dedServ)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + new Vector2(0, -Projectile.height * 0.5f), 6, new Vector2(0, -4).RotatedBy(0.7f * -Projectile.direction) * Main.rand.NextFloat(0.1f, 0.8f));
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(0.4f, 0.95f);
                }
            }
            else
            {
                Projectile.velocity.X = 0f;
                State = AIState.Idle;
            }
        }

        /// <summary>
        /// The minion does a small jump, to give it some flavor and cuteness.
        /// </summary>
        public void NearOwnerJump()
        {
            // Actually jumps.
            Projectile.velocity.Y = -8f;

            // After 2 hops the summon goes on a cooldown before jumping again.
            IdleJumpCount++;
            if (IdleJumpCount == 2)
            {
                IdleJumpCount = 0;
                IdleJumpCooldown = 30;
            }

            Direction = MathF.Sign(AnyPlayer.Center.X - Projectile.Center.X);
            Animation = AnimationState.Jump;

            SoundEngine.PlaySound(_jumpSound with { Pitch = Utils.Remap(Variant, 0f, 2f, -0.3f, 0.3f) }, Projectile.Center);
        }

        /// <summary>
        /// The minion will start moving to the target with acceleration.
        /// </summary>
        public void MoveToTarget()
        {
            // If the variant's smaller (Variant 2 is the smallest), it has a faster acceleration and a higher max velocity.
            float maxVelocity = Utils.Remap(Variant, 0f, 2f, 5f, 8f);
            float acceleration = Utils.Remap(Variant, 0f, 2f, 0.1f, 0.3f);
            float accelerationDirection = MathF.Sign(Target.Center.X - Projectile.Center.X);

            Projectile.velocity.X += acceleration * accelerationDirection;
            if (MathF.Abs(Projectile.velocity.X) > maxVelocity)
                Projectile.velocity.X = maxVelocity * accelerationDirection;
        }

        /// <summary>
        /// The minion will jump towards a destination in the Y-axis.
        /// </summary>
        /// <param name="destination">The Y position at which the minion will jump towards</param>
        public void JumpTowards(Vector2 destination)
        {
            // Equation of a free fall independent of time: v = sqrt(2 * gravity * distance).
            // Because we want it to go up and now down, we need to negate the constant: v = sqrt(-2 * gravity * distance).
            // And now we need to negate the velocity to get it on Terraria's coordinate system: v = -sqrt(-2 * gravity * distance).
            Projectile.velocity.Y = -MathF.Sqrt(-2f * PumpkinGravityStrength * (destination.Y - Projectile.Bottom.Y));
            Animation = AnimationState.Jump;
            SoundEngine.PlaySound(_jumpSound with { Pitch = Utils.Remap(Variant, 0f, 2f, -0.3f, 0.3f) }, Projectile.Center);
        }

        /// <summary>
        /// Detects a platform between the minion and the target.
        /// </summary>
        /// <param name="tilePosition">The position of said platform as a <see cref="Vector2"/>.</param>
        /// <returns>Whether there's a platform or not.</returns>
        public bool PlatformBetweenMinionAndTarget(out Vector2 tilePosition)
        {
            Point minionPosition = Projectile.Center.ToSafeTileCoordinates();
            Point targetPosition = Target.Center.ToSafeTileCoordinates();
            for (int coordY = minionPosition.Y; coordY > targetPosition.Y; coordY--)
            {
                if (Main.tile[targetPosition.X, coordY].IsTileSolidGround())
                {
                    tilePosition = new Vector2(targetPosition.X, coordY) * 16f;
                    return true;
                }
            }

            tilePosition = Vector2.Zero;
            return false;
        }

        /// <summary>
        /// Applies gravity to the minion.
        /// </summary>
        public void DoGravity()
        {
            float speed = Projectile.velocity.Y;
            if (speed < PumpkinMaxGravity)
                speed = MathF.Min(speed + PumpkinGravityStrength, PumpkinMaxGravity);
            Projectile.velocity.Y = speed;
        }

        /// <summary>
        /// Does the animation of the minion.
        /// </summary>
        public void DoAnimation()
        {
            // If the state of the animation's spritsheet is only 1 frame, no need to animate.
            if (Animation == AnimationState.None || Animation == AnimationState.Idle || Animation == AnimationState.Jump)
                return;

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= FramesUntilNextAnimationFrame)
            {
                Projectile.frame = Math.Min(Projectile.frame + 1, AnimationFrames - 1);
                Projectile.frameCounter = 0;

                // If it's the run animation, loop it.
                if (Animation == AnimationState.Run && CompletedAnimation)
                    Projectile.frame = 0;
            }
        }

        /// <summary>
        /// A covenient way to do a <see cref="Projectile.netUpdate"/> while also handling <see cref="Projectile.netSpam"/>.
        /// </summary>
        public void NetUpdate()
        {
            Projectile.netSpam = 0;
            Projectile.netUpdate = true;
        }

        #endregion

        #region Other Overrides

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 68;
            ProjectileID.Sets.MinionShot[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.localNPCHitCooldown = 30;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.netImportant = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Direction);
            writer.Write(IdleWalkingTime);
            writer.Write(IdleWalkingTimer);
            writer.Write(IdleJumpCount);
            writer.Write(IdleJumpCooldown);
            writer.Write(IdleWalkingDirection);
            writer.Write(AnimationFrames);
            writer.Write(FramesUntilNextAnimationFrame);
            writer.Write(KILLYOURSELF);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Direction = reader.ReadInt32();
            IdleWalkingTime = reader.ReadInt32();
            IdleWalkingTimer = reader.ReadInt32();
            IdleJumpCount = reader.ReadInt32();
            IdleJumpCooldown = reader.ReadInt32();
            IdleWalkingDirection = reader.ReadInt32();
            AnimationFrames = reader.ReadInt32();
            FramesUntilNextAnimationFrame = reader.ReadInt32();
            KILLYOURSELF = reader.ReadBoolean();
        }

        public override bool? CanDamage() => State == AIState.Attack ? null : false;

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.SourceDamage *= Utils.Remap(Variant, 0f, 2f, 1.5f, 0.5f);

        public override void OnSpawn(IEntitySource source)
        {
            Direction = Main.rand.NextBool() ? -1 : 1;
            Animation = AnimationState.None;
        }

        #endregion

        #region Drawing

        public override bool PreDraw(ref Color lightColor)
        {
            if (Animation == AnimationState.None)
                return false;

            Asset<Texture2D> texture = ModContent.Request<Texture2D>(Texture);
            Vector2 drawPosition = Projectile.Bottom - Vector2.UnitY * (24f + Projectile.gfxOffY) - Main.screenPosition;
            Rectangle frame = texture.Frame(15, 10, (int)Variant * 5 + (int)Animation, Projectile.frame);
            Color drawColor = Projectile.GetAlpha(lightColor);
            Vector2 rotationPoint = frame.Size() * 0.5f;
            SpriteEffects flip = Direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Main.EntitySpriteDraw(texture.Value, drawPosition, frame, drawColor, Projectile.rotation, rotationPoint, Projectile.scale, flip);

            return false;
        }

        #endregion
    }
}
