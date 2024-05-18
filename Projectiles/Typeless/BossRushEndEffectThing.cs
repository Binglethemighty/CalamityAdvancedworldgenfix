using CalamityMod.Events;
using CalamityMod.Items;
using CalamityMod.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Typeless
{
    public class BossRushEndEffectThing : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Typeless";
        public Player Owner => Main.player[Projectile.owner];
        public ref float Time => ref Projectile.ai[0];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = BossRushEvent.EndVisualEffectTime;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            if (Time == 25f)
                SoundEngine.PlaySound(BossRushEvent.VictorySound, Main.LocalPlayer.Center);

            Projectile.Center = Owner.Center;
            BossRushEvent.SyncEndTimer((int)Time);

            float currentShakePower = MathHelper.Lerp(1f, 20f, Utils.GetLerpValue(140f, 180f, Time, true) * Utils.GetLerpValue(10f, 40f, Projectile.timeLeft, true));
            if (Projectile.timeLeft > 5)
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = currentShakePower;

            MoonlordDeathDrama.RequestLight(Utils.GetLerpValue(220f, 265f, Time, true) * Utils.GetLerpValue(10f, 30f, Projectile.timeLeft, true), Main.LocalPlayer.Center);

            // Bit of an ugly solution, but this code prevents the end effect projectile from despawning while the end dialogue is playing.
            // This fixes a bug where the first-time end dialogue did not play as intended.
            if (Projectile.timeLeft < 5 && BossRushDialogueSystem.CurrentDialogueDelay != 0)
                Projectile.timeLeft = 5;

            Time++;
        }

        public override void OnKill(int timeLeft)
        {
            BossRushEvent.End();
            foreach (Player p in Main.ActivePlayers)
            {
                int rock = Item.NewItem(p.GetSource_Misc("CalamityMod_BossRushRock"), (int)p.position.X, (int)p.position.Y, p.width, p.height, ModContent.ItemType<Rock>());
                if (Main.netMode == NetmodeID.Server)
                {
                    Main.timeItemSlotCannotBeReusedFor[rock] = 54000;
                    NetMessage.SendData(MessageID.InstancedItem, p.whoAmI, -1, null, rock);
                }
            }
        }
    }
}
