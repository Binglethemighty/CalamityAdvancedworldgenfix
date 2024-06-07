using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Melee;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Melee
{
    public class TheDarkMaster : ModItem, ILocalizedModType
    {
        public const float DamagePerHealth = 0.001f; // 0.1 damage per additional health. 100 health = 10% damage.
        public new string LocalizationCategory => "Items.Weapons.Melee";

        [CloneByReference]
        public BloomRing ring;

        public override void SetStaticDefaults()
        {
            Item.staff[Item.type] = true;
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 60;
            Item.height = 60;
            Item.damage = 50;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = Item.useTime = 26;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.knockBack = 7f;
            Item.autoReuse = true;
            Item.value = CalamityGlobalItem.RarityLightRedBuyPrice;
            Item.rare = ItemRarityID.LightRed;
            Item.shoot = ModContent.ProjectileType<DarkMasterBeam>();
            Item.shootSpeed = 16f;
            Item.Calamity().donorItem = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse != 2)
            {
                // only fire beams at max health
                if (player.statLife >= (player.statLifeMax2 * 0.75f))
                {
                    SoundEngine.PlaySound(SoundID.Item71, player.Center);
                    // increase the beam's damage by the player's additional health starting from the vanilla maximum amount with just life crystals
                    int baseMaxHealth = 400;
                    int bonusHealth = player.statLifeMax2 - baseMaxHealth;
                    float bonusDamage = DamagePerHealth * bonusHealth;
                    Projectile.NewProjectile(source, position.X, position.Y, velocity.X, velocity.Y, type, (int)(damage * (1 + bonusDamage)), knockback, player.whoAmI, 0, 0);
                }
                // still play the sound if the clones are out since they always fire beams
                else if (player.ownedProjectileCounts[ModContent.ProjectileType<DarkMasterClone>()] > 0)
                {
                    SoundEngine.PlaySound(SoundID.Item71, player.Center);
                }

                // force clones to shoot regardless of health
                foreach (Projectile p in Main.ActiveProjectiles)
                {
                    if (p.type == ModContent.ProjectileType<DarkMasterClone>() && p.owner == player.whoAmI)
                        p.ai[1] = 1f;
                }
            }
            else
            {
                if (player.ownedProjectileCounts[ModContent.ProjectileType<DarkMasterClone>()] <= 0)
                {
                    // spawn a growing red ring 
                    ring = new BloomRing(player.Center, Vector2.Zero, Color.Red, 0.4f, 10);
                    GeneralParticleHandler.SpawnParticle(ring);
                    // summon the clones. position is determined by ai[0]
                    for (int i = 0; i < 3; i++)
                    {
                        // Stats are set to dynamically update, so damage, kb, crit have to be feeded
                        Projectile clone = Projectile.NewProjectileDirect(Item.GetSource_FromThis(), player.Center, Vector2.Zero, ModContent.ProjectileType<DarkMasterClone>(), Item.damage, Item.knockBack, player.whoAmI, i);
                        clone.OriginalCritChance = Item.crit;
                    }
                }
            }
            return false;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            // only allow right clicking if the player doesn't have clones out
            if (player.altFunctionUse == 2)
            {
                if (player.ownedProjectileCounts[ModContent.ProjectileType<DarkMasterClone>()] <= 0)
                {
                    Item.UseSound = SoundID.Item104;
                    Item.useStyle = ItemUseStyleID.Shoot;
                    Item.useTurn = false;
                    Item.noMelee = true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                Item.UseSound = SoundID.Item1;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.useTurn = true;
                Item.noMelee = false;
            }
            return base.CanUseItem(player);
        }

        // make the ring grow if it exists
        public override void UpdateInventory(Player player)
        {
            if (ring != null)
            {
                ring.Scale *= 1.3f;
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.BeamSword).
                AddIngredient(ItemID.LightsBane).
                AddIngredient(ItemID.SoulofNight, 20).
                AddIngredient<EssenceofHavoc>(3).
                AddIngredient(ItemID.Ruby). // as long as you have enough...
                AddTile(TileID.Anvils).
                Register();
        }
    }
}
