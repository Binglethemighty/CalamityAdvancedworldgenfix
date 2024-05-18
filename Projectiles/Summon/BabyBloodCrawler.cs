using CalamityMod.Buffs.Summon;
using CalamityMod.CalPlayer;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityMod.Projectiles.Summon
{
    public class BabyBloodCrawler : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Summon";
        public static int bloodCooldown = 0;
        public float dust = 0f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 11;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.minionSlots = 1f;
            Projectile.aiStyle = ProjAIStyleID.Pet;
            Projectile.timeLeft = 18000;
            Projectile.penetrate = -1;
            Projectile.timeLeft *= 5;
            Projectile.minion = true;
            Projectile.tileCollide = false;
            AIType = ProjectileID.VenomSpider;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.MaxUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 33 * Projectile.MaxUpdates;
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = false;
            return true;
        }

        public override void AI()
        {
            if (bloodCooldown > 0)
                bloodCooldown--;

            Player player = Main.player[Projectile.owner];
            CalamityPlayer modPlayer = player.Calamity();

            foreach (Projectile proj in Main.ActiveProjectiles)
            {
                // Short circuits to make the loop as fast as possible
                if (proj.owner != Projectile.owner || !proj.minion || proj.Calamity().lineColor != 1)
                    continue;
                if (proj.type == Projectile.type)
                {
                    proj.Calamity().lineColor = 2;
                }
            }
            if (Projectile.Calamity().lineColor == 0)
                Projectile.Calamity().lineColor = 1;

            if (dust == 0f)
            {
                int constant = 16;
                for (int i = 0; i < constant; i++)
                {
                    Vector2 rotate = Vector2.Normalize(Projectile.velocity) * new Vector2((float)Projectile.width / 2f, (float)Projectile.height) * 0.75f;
                    rotate = rotate.RotatedBy((double)((float)(i - (constant / 2 - 1)) * 6.28318548f / (float)constant), default) + Projectile.Center;
                    Vector2 faceDirection = rotate - Projectile.Center;
                    int bloody = Dust.NewDust(rotate + faceDirection, 0, 0, DustID.Blood, faceDirection.X * 1f, faceDirection.Y * 1f, 100, default, 1.1f);
                    Main.dust[bloody].noGravity = true;
                    Main.dust[bloody].noLight = true;
                    Main.dust[bloody].velocity = faceDirection;
                }
                dust += 1f;
            }
            bool isMinion = Projectile.type == ModContent.ProjectileType<BabyBloodCrawler>();
            player.AddBuff(ModContent.BuffType<BabyBloodCrawlerBuff>(), 3600);
            if (isMinion)
            {
                if (player.dead)
                {
                    modPlayer.scabRipper = false;
                }
                if (modPlayer.scabRipper)
                {
                    Projectile.timeLeft = 2;
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => false;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => OnHitEffects(target.Center);

        public override void OnHitPlayer(Player target, Player.HurtInfo info) => OnHitEffects(target.Center);

        private void OnHitEffects(Vector2 targetPos)
        {
            if (bloodCooldown == 0)
            {
                bloodCooldown = 17;
                int projAmt = 2;
                var source = Projectile.GetSource_FromThis();
                for (int n = 0; n < projAmt; n++)
                {
                    CalamityUtils.ProjectileRain(source, targetPos, 400f, 100f, 400f, 700f, 29f, ModContent.ProjectileType<BloodRain>(), Projectile.damage, Projectile.knockBack * Main.rand.NextFloat(0.7f, 1f), Projectile.owner);
                }
            }
        }
    }
}
