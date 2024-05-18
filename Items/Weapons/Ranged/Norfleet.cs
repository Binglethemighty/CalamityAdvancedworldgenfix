using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Projectiles.Ranged;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Ranged
{
    public class Norfleet : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Ranged";
        public int loadedShots = 3;

        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 140;
            Item.height = 42;
            Item.damage = 7077;
            Item.knockBack = 15f;
            Item.shootSpeed = 30f;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 70;
            Item.useTime = 70;
            Item.UseSound = null;
            Item.shoot = ModContent.ProjectileType<NorfleetCannon>();
            Item.value = CalamityGlobalItem.RarityDarkBlueBuyPrice;
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.DamageType = DamageClass.Ranged;
            Item.channel = true;
            Item.autoReuse = true;
        }

        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

        // Makes the rotation of the mouse around the player sync in multiplayer.
        public override void HoldItem(Player player) => player.Calamity().mouseRotationListener = true;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Funny punishment for trying to cheeese with Norfleet
            bool cheater = (player.Calamity().NorfleetCounter >= 3 && player.Calamity().NorfleetCounter < 1000);
            if (player.Calamity().NorfleetCounter >= 1000)
                loadedShots = 1;

            if (cheater)
            {
                SoundStyle fire = new("CalamityMod/Sounds/Item/NuhUhUh");
                SoundEngine.PlaySound(fire, player.Center);
            }

            Projectile holdout = Projectile.NewProjectileDirect(source, player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<NorfleetCannon>(), damage, knockback, player.whoAmI, 0, (cheater ? 1000 : 0), loadedShots);
            
            // We set the rotation to the direction to the mouse so the first frame doesn't appear bugged out.
            holdout.velocity = (player.Calamity().mouseWorld - player.MountedCenter).SafeNormalize(Vector2.Zero);

            player.Calamity().NorfleetCounter++;
            loadedShots--;

            if (loadedShots <= 0)
            {
                loadedShots = 3;
            }
            return false;
        }
    }
}
