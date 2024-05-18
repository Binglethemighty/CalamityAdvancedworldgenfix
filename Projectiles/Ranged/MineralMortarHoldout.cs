using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Ranged
{
    public class MineralMortarHoldout : BaseGunHoldoutProjectile
    {
        public override int AssociatedItemID => ModContent.ItemType<MineralMortar>();
        public override float MaxOffsetLengthFromArm => 15f;
        public override float OffsetXUpwards => -10f;
        public override float BaseOffsetY => -10f;
        public override float OffsetYDownwards => 5f;

        public ref float Time => ref Projectile.ai[0];

        public override void HoldoutAI()
        {
            Vector2 mouse = Owner.Calamity().mouseWorld;

            Time++;

            // If the time reaches Item.useTime, it'll shoot the projectile, consume ammo and reset the timer.
            if (Time >= Owner.itemTimeMax && Main.myPlayer == Projectile.owner)
            {
                Owner.PickAmmo(Owner.ActiveItem(), out _, out float speed, out int damage, out float knockback, out int usedItemAmmoId);

                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), GunTipPosition, Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed * 1.5f, ModContent.ProjectileType<MineralMortarProjectile>(), damage, knockback, Projectile.owner, usedItemAmmoId);

                // Doesn't sync this to the server, it's just effects.
                if (!Main.dedServ)
                {
                    for (int i = 0; i < 20; i++)
                    {
                        Vector2 randomDirectionToMouse = Owner.SafeDirectionTo(mouse).RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(8f, 10f);
                        Dust shootDust = Dust.NewDustPerfect(GunTipPosition, DustID.Torch, randomDirectionToMouse, Scale: Main.rand.NextFloat(1.5f, 2.5f));
                        shootDust.noGravity = true;
                        shootDust.noLight = true;
                        shootDust.noLightEmittence = true;
                    }

                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 randomDirectionToMouse = Owner.SafeDirectionTo(mouse).RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(8f, 10f);
                        Particle shootMist = new MediumMistParticle(GunTipPosition, randomDirectionToMouse, new Color(255, 100, 0), Color.Transparent, Main.rand.NextFloat(.3f, 1f), Main.rand.NextFloat(200f, 400f));
                        GeneralParticleHandler.SpawnParticle(shootMist);
                    }

                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 randomDirectionToMouse = Owner.SafeDirectionTo(mouse).RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(2f, 6f);
                        Particle shootDebri = new StoneDebrisParticle(GunTipPosition, randomDirectionToMouse, Color.Lerp(Color.White, Color.LightGray, Main.rand.NextFloat()), Main.rand.NextFloat(.6f, .8f), Main.rand.Next(30, 45 + 1));
                        GeneralParticleHandler.SpawnParticle(shootDebri);
                    }

                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ScorchedEarthShot", 3) with { Volume = .2f, Pitch = 1.2f, PitchVariance = 1.1f }, Projectile.Center);
                }

                // Makes the distance between the projectile and the player 0, this gives a recoil effect.
                OffsetLengthFromArm = 0f;
                Time = 0f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Vector2 position = Projectile.Center - Main.screenPosition;
            float rotation = Projectile.rotation + (Projectile.spriteDirection == -1 ? MathHelper.Pi : 0f);
            Vector2 origin = texture.Size() * 0.5f;
            SpriteEffects effects = (Projectile.spriteDirection * Owner.gravDir == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            float shake = Utils.Remap(Time, Owner.itemTimeMax * 0.33f, Owner.itemTimeMax, 0f, 3f);
            position += Main.rand.NextVector2Circular(shake, shake);

            Main.EntitySpriteDraw(texture, position, null, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale * Owner.gravDir, effects);

            return false;
        }
    }
}
