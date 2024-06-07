using System;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Melee
{
    public class DarkMasterClone : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public Player Owner => Main.player[Projectile.owner];
        public Player clone;

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ContinuouslyUpdateDamageStats = true;
        }

        public override void AI()
        {
            // if the player isn't holding the sword, DIE.
            if (Owner.HeldItem.type != ModContent.ItemType<TheDarkMaster>() || !Owner.active || Owner.CCed || Owner == null)
            {
                Projectile.Kill();
                return;
            }
            // if the velocity is not zero, the visuals get offset weirdly
            Projectile.velocity = Vector2.Zero;
            // how far the clone should move from the player
            Vector2 moveTo = Vector2.UnitY * -160f;
            switch (Projectile.ai[0])
            {
                case 1:
                    moveTo = new Vector2(-180, 120);
                    break;
                case 2:
                    moveTo = new Vector2(180, 120);
                    break;
                default:
                    break;
            }
            // if all conditions above aren't met, the clone can stick around forever
            Projectile.timeLeft = 2;
            // move the clone to the desired position
            Projectile.Center = Vector2.Lerp(Projectile.Center, Owner.Center + moveTo, 0.4f);
            // produce smoke during initial move
            if (Projectile.Distance(Owner.Center + moveTo) < 16)
            {
                Projectile.ai[2] = 1;
            }
            if (Projectile.ai[2] == 0)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Vector2 angleVec = angle.ToRotationVector2();
                Particle smoke = new HeavySmokeParticle(Projectile.Center, angleVec * Main.rand.NextFloat(1f, 2f), Color.Black, 30, Main.rand.NextFloat(0.25f, 1f), 0.5f, 0.1f);
                GeneralParticleHandler.SpawnParticle(smoke);
            }
            // shoot beams while the player is left clicking
            if (Projectile.ai[1] > 0)
            {
                Projectile.ai[1] = 0;
                Vector2 direction = Projectile.Center.DirectionTo(Main.MouseWorld);
                Projectile.direction = Math.Sign(direction.X);
                if (Projectile.owner == Main.myPlayer)
                {
                    // ai[1] not being 0 determines if the projectile should always ignore tiles
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, direction * Owner.HeldItem.shootSpeed, ModContent.ProjectileType<DarkMasterBeam>(), (int)(Projectile.damage * 0.4f), Projectile.knockBack, Projectile.owner, 1, 1);
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.NPCDeath6, Projectile.Center);

            for (int i = 0; i < 8; i++)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Vector2 angleVec = angle.ToRotationVector2();
                Particle smoke = new HeavySmokeParticle(Projectile.Center, angleVec * Main.rand.NextFloat(2f, 4f), Color.Black, 60, Main.rand.NextFloat(0.45f, 1.22f), 0.6f, 0.1f);
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // make a player visual clone. it inherits the player's hair type and clothes style and is otherwise all black with red pupils
            // Main.playerVisualClone[Projectile.owner] will throw stack trace errors on reloads
            clone ??= new Player();
            clone.CopyVisuals(Owner);
            clone.skinColor = Color.Black;
            clone.shirtColor = Color.Black;
            clone.underShirtColor = Color.Black;
            clone.pantsColor = Color.Black;
            clone.shoeColor = Color.Black;
            clone.hairColor = Color.Black;
            clone.eyeColor = Color.Red;
            // become one with the shadows
            for (int i = 0; i < clone.dye.Length; i++)
            {
                if (clone.dye[i].type != ItemID.ShadowDye)
                {
                    clone.dye[i].SetDefaults(ItemID.ShadowDye);
                }
            }
            // update everything for our little dummy player
            clone.ResetEffects();
            clone.ResetVisibleAccessories();
            clone.DisplayDollUpdate();
            clone.UpdateSocialShadow();
            clone.UpdateDyes();
            clone.PlayerFrame();
            // copy the player's arm movements while swinging, otherwise idle
            if (Owner.ItemAnimationActive && Owner.altFunctionUse != 2)
                clone.bodyFrame = Owner.bodyFrame;
            else
                clone.bodyFrame.Y = 0;
            // legs never jump or walk
            clone.legFrame.Y = 0;
            // face towards the player's cursor
            clone.direction = Math.Sign(Projectile.DirectionTo(Main.MouseWorld).X);
            Main.PlayerRenderer.DrawPlayer(Main.Camera, clone, Projectile.position, 0f, clone.fullRotationOrigin, 0f, 1f);
            // draw the sword
            if (Owner.ItemAnimationActive && Owner.altFunctionUse != 2)
            {
                Texture2D Sword = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Melee/TheDarkMaster").Value;
                Vector2 distToPlayer = Projectile.position - Owner.position;
                Main.EntitySpriteDraw(Sword, (Vector2)Owner.HandPosition + distToPlayer - Main.screenPosition, null, lightColor, Owner.direction == clone.direction ? Owner.itemRotation : -Owner.itemRotation, new Vector2(clone.direction == 1 ? 0 : Sword.Width, Sword.Height), 1f, clone.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally);
            }
            return false;
        }

        public override bool? CanDamage() => false;
    }
}
