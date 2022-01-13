﻿using CalamityMod.Items.Accessories;
using CalamityMod.Items.DifficultyItems;
using CalamityMod.Items.Materials;
using CalamityMod.Items.PermanentBoosters;
using CalamityMod.Items.Potions;
using CalamityMod.Items.Weapons.Magic;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Items.Weapons.Summon;
using CalamityMod.World;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items
{
	public class CalamityGlobalItemLoot : GlobalItem
	{
		public override bool InstancePerEntity => false;
		public override bool CloneNewInstances => false;

		// NOTE: this function applies to all right-click-to-open items, even modded ones (despite the name).
		// This means it applies to boss bags, crates, lockboxes, herb bags, goodie bags, and presents.
		// The internal names of these cases are as follows (TML 0.11 API):
		// bossBag, crate, lockBox, herbBag, goodieBag, present
		public override void OpenVanillaBag(string context, Player player, int itemID)
		{
			if (context == "crate")
				CrateLoot(player, itemID);

			else if (context == "bossBag")
				BossBagLoot(player, itemID);

			// Bat Hook is now acquired from Vampires.
			else if (context == "goodieBag")
				DropHelper.BlockDrops(ItemID.BatHook);
		}

		#region Boss Bags
		private static void BossBagLoot(Player player, int itemID)
		{
			// Give a chance for Laudanum, Stress Pills and Heart of Darkness from every boss bag
			DropHelper.DropRevBagAccessories(player);

			switch (itemID)
			{
				case ItemID.KingSlimeBossBag:
					DropHelper.DropItem(player, ModContent.ItemType<CrownJewel>());
					break;

				case ItemID.EyeOfCthulhuBossBag:
					DropHelper.DropItemChance(player, ModContent.ItemType<DeathstareRod>(), DropHelper.BagWeaponDropRateFloat);
					DropHelper.DropItem(player, ModContent.ItemType<TeardropCleaver>());
					DropHelper.DropItem(player, ModContent.ItemType<CounterScarf>());
					break;

				case ItemID.QueenBeeBossBag:
					// Drop weapons Calamity style instead of mutually exclusive.
					int[] queenBeeWeapons = new int[]
					{
						ItemID.BeeKeeper,
						ItemID.BeesKnees,
						ItemID.BeeGun,
					};
					DropHelper.DropEntireSet(player, DropHelper.BagWeaponDropRateFloat, queenBeeWeapons);
					DropHelper.BlockDrops(queenBeeWeapons);

					DropHelper.DropItem(player, ItemID.Stinger, 8, 12); // Extra stingers
					DropHelper.DropItem(player, ModContent.ItemType<HardenedHoneycomb>(), 50, 75);
					DropHelper.DropItem(player, ModContent.ItemType<TheBee>());
					break;

				case ItemID.SkeletronBossBag:
					DropHelper.DropItem(player, ModContent.ItemType<ClothiersWrath>());
					break;

				case ItemID.WallOfFleshBossBag:
					// Drop weapons Calamity style instead of mutually exclusive -- this includes Calamity weapons.
					int[] wofWeapons = new int[]
					{
						ItemID.BreakerBlade,
						ItemID.ClockworkAssaultRifle,
						ModContent.ItemType<Meowthrower>(),
						ItemID.LaserRifle,
						ModContent.ItemType<BlackHawkRemote>(),
						ModContent.ItemType<BlastBarrel>(),
					};
					DropHelper.DropEntireSet(player, DropHelper.BagWeaponDropRateFloat, wofWeapons);
					DropHelper.BlockDrops(wofWeapons);

					// Drop emblems Calamity style instead of mutually exclusive -- this includes the Rogue Emblem.
					int[] emblems = new int[]
					{
						ItemID.WarriorEmblem,
						ItemID.RangerEmblem,
						ItemID.SorcererEmblem,
						ItemID.SummonerEmblem,
						ModContent.ItemType<RogueEmblem>(),
					};
					DropHelper.DropEntireSet(player, 0.25f, emblems);
					DropHelper.BlockDrops(emblems);

					DropHelper.DropItemFromSetChance(player, 0.2f, ItemID.CorruptionKey, ItemID.CrimsonKey);
					DropHelper.DropItem(player, ModContent.ItemType<EvilSmasher>());
					break;

				case ItemID.DestroyerBossBag:
					// Only drop hallowed bars after all mechs are down.
					if ((!NPC.downedMechBoss1 || !NPC.downedMechBoss2 || !NPC.downedMechBoss3) && CalamityConfig.Instance.EarlyHardmodeProgressionRework)
						DropHelper.BlockDrops(ItemID.HallowedBar);

					DropHelper.DropItem(player, ModContent.ItemType<SHPC>());
					break;

				case ItemID.TwinsBossBag:
					// Only drop hallowed bars after all mechs are down.
					if ((!NPC.downedMechBoss1 || !NPC.downedMechBoss2 || !NPC.downedMechBoss3) && CalamityConfig.Instance.EarlyHardmodeProgressionRework)
						DropHelper.BlockDrops(ItemID.HallowedBar);

					DropHelper.DropItem(player, ModContent.ItemType<Arbalest>());
					break;

				case ItemID.SkeletronPrimeBossBag:
					// Only drop hallowed bars after all mechs are down.
					if ((!NPC.downedMechBoss1 || !NPC.downedMechBoss2 || !NPC.downedMechBoss3) && CalamityConfig.Instance.EarlyHardmodeProgressionRework)
						DropHelper.BlockDrops(ItemID.HallowedBar);

					DropHelper.DropItem(player, ModContent.ItemType<GoldBurdenBreaker>());
					DropHelper.DropItem(player, ModContent.ItemType<SpearofDestiny>());
					break;

				case ItemID.PlanteraBossBag:
					// Drop weapons Calamity style instead of mutually exclusive.
					int[] planteraWeapons = new int[]
					{
						ItemID.FlowerPow,
						ItemID.Seedler,
						ItemID.GrenadeLauncher,
						ItemID.VenusMagnum,
						ItemID.LeafBlower,
						ItemID.NettleBurst,
						ItemID.WaspGun
					};
					DropHelper.DropEntireSet(player, DropHelper.BagWeaponDropRateFloat, planteraWeapons);
					DropHelper.BlockDrops(planteraWeapons);

					// Equipment
					DropHelper.DropItemChance(player, ModContent.ItemType<BloomStone>(), 5);

					DropHelper.DropItem(player, ModContent.ItemType<LivingShard>(), 16, 22);
					DropHelper.DropItemChance(player, ItemID.JungleKey, 5);
					DropHelper.DropItem(player, ModContent.ItemType<BlossomFlux>());
					DropHelper.DropItem(player, ModContent.ItemType<ThornBlossom>());
					break;

				case ItemID.GolemBossBag:
					// Drop loot Calamity style instead of mutually exclusive.
					int[] golemItems = new int[]
					{
						ItemID.GolemFist,
						ItemID.PossessedHatchet,
						ItemID.Stynger,
						ItemID.HeatRay,
						ItemID.StaffofEarth,
						ItemID.EyeoftheGolem,
						ItemID.SunStone,
					};
					DropHelper.DropEntireSet(player, DropHelper.BagWeaponDropRateFloat, golemItems);
					DropHelper.BlockDrops(golemItems);

					// The Picksaw always drops if the player doesn't have one in their inventory. Otherwise it has a 25% chance.
					bool playerHasPicksaw = player.InventoryHas(ItemID.Picksaw);
					DropHelper.DropItemChance(player, ItemID.Picksaw, playerHasPicksaw ? 0.25f : 1.0f);

					DropHelper.DropItem(player, ModContent.ItemType<EssenceofCinder>(), 8, 13);
					DropHelper.DropItem(player, ModContent.ItemType<AegisBlade>());
					DropHelper.DropItem(player, ModContent.ItemType<LeadWizard>());
					break;

				case ItemID.BossBagBetsy:
					// Drop weapons Calamity style instead of mutually exclusive.
					int[] betsyWeapons = new int[]
					{
						ItemID.DD2SquireBetsySword, // Flying Dragon
						ItemID.MonkStaffT3, // Sky Dragon's Fury
						ItemID.DD2BetsyBow, // Aerial Bane
						ItemID.ApprenticeStaffT3, // Betsy's Wrath
					};
					DropHelper.DropEntireSet(player, DropHelper.BagWeaponDropRateFloat, betsyWeapons);
					DropHelper.BlockDrops(betsyWeapons);
					break;

				case ItemID.FishronBossBag:
					// Drop weapons Calamity style instead of mutually exclusive -- this includes Calamity weapons.
					int[] dukeWeapons = new int[]
					{
						ItemID.Flairon,
						ItemID.Tsunami,
						ItemID.BubbleGun,
						ItemID.RazorbladeTyphoon,
						ItemID.TempestStaff,
						ModContent.ItemType<DukesDecapitator>(),
					};
					DropHelper.DropEntireSet(player, DropHelper.BagWeaponDropRateFloat, dukeWeapons);
					DropHelper.BlockDrops(dukeWeapons);
					DropHelper.DropItem(player, ModContent.ItemType<BrinyBaron>());
					break;

				case ItemID.CultistBossBag:
					DropHelper.DropItem(player, ModContent.ItemType<EyeofMagnus>());
					DropHelper.DropItem(player, ModContent.ItemType<StardustStaff>());
					break;

				case ItemID.MoonLordBossBag:
					// Drop weapons Calamity style instead of mutually exclusive -- this includes Calamity weapons.
					int[] moonLordWeapons = new int[]
					{
						ItemID.Meowmere,
						ItemID.StarWrath,
						ItemID.Terrarian,
						ItemID.FireworksLauncher, // Celebration
						// ItemID.CelebrationMK2,
						ItemID.SDMG,
						ItemID.LastPrism,
						ItemID.LunarFlareBook,
						ItemID.MoonlordTurretStaff, // Lunar Portal Staff
						ItemID.RainbowCrystalStaff,
						ModContent.ItemType<UtensilPoker>(),
					};
					DropHelper.DropEntireSet(player, DropHelper.BagWeaponDropRateFloat, moonLordWeapons);
					DropHelper.BlockDrops(moonLordWeapons);

					// The Celestial Onion only drops if the player hasn't used one and doesn't have one in their inventory.
					int celestialOnion = ModContent.ItemType<MLGRune2>();
					DropHelper.DropItemCondition(player, celestialOnion, !player.Calamity().extraAccessoryML && !player.InventoryHas(celestialOnion));
					DropHelper.DropItem(player, ModContent.ItemType<GrandDad>());
					DropHelper.DropItem(player, ModContent.ItemType<Infinity>());
					break;
			}
		}
		#endregion

		#region Fishing Crates
		private static void CrateLoot(Player player, int itemID)
		{
			switch (itemID)
			{
				case ItemID.WoodenCrate:
					BlockCrateDrops();
					DropHelper.DropItemChance(player, ModContent.ItemType<WulfrumShard>(), 0.25f, 3, 5);
					break;

				case ItemID.IronCrate:
					BlockCrateDrops();
					DropHelper.DropItemChance(player, ModContent.ItemType<WulfrumShard>(), 0.25f, 5, 8);
					DropHelper.DropItemChance(player, ModContent.ItemType<AncientBoneDust>(), 0.25f, 5, 8);
					break;

				case ItemID.GoldenCrate:
					BlockCrateDrops();
					break;

				case ItemID.CorruptFishingCrate:
				case ItemID.CrimsonFishingCrate:
					BlockCrateDrops();
					DropHelper.DropItemChance(player, ModContent.ItemType<EbonianGel>(), 0.15f, 5, 8);
					DropHelper.DropItemChance(player, ModContent.ItemType<MurkySludge>(), 0.15f, 1, 3);
					break;

				case ItemID.HallowedFishingCrate:
					BlockCrateDrops();
					int potion = WorldGen.crimson ? ModContent.ItemType<ProfanedRagePotion>() : ModContent.ItemType<HolyWrathPotion>();
					DropHelper.DropItemCondition(player, ModContent.ItemType<UnholyEssence>(), CalamityWorld.downedProvidence, 0.15f, 5, 10);
					DropHelper.DropItemCondition(player, potion, CalamityWorld.downedProvidence, 0.1f, 1, 2);
					break;

				case ItemID.DungeonFishingCrate:
					BlockCrateDrops();
					DropHelper.DropItemCondition(player, ItemID.Ectoplasm, NPC.downedPlantBoss, 0.1f, 1, 5);
					DropHelper.DropItemCondition(player, ModContent.ItemType<Phantoplasm>(), CalamityWorld.downedPolterghast, 0.1f, 1, 5);
					break;

				case ItemID.JungleFishingCrate:
					BlockCrateDrops();
					DropHelper.DropItemChance(player, ModContent.ItemType<MurkyPaste>(), 0.2f, 1, 3);
					DropHelper.DropItemCondition(player, ModContent.ItemType<BeetleJuice>(), Main.hardMode, 0.2f, 1, 3);
					DropHelper.DropItemCondition(player, ModContent.ItemType<TrapperBulb>(), Main.hardMode, 0.2f, 1, 3);
					DropHelper.DropItemCondition(player, ItemID.ChlorophyteBar, CalamityWorld.downedCalamitas || NPC.downedPlantBoss, 0.1f, 1, 3);
					DropHelper.DropItemCondition(player, ModContent.ItemType<DraedonBar>(), NPC.downedPlantBoss, 0.1f, 1, 3);
					DropHelper.DropItemCondition(player, ModContent.ItemType<PlagueCellCluster>(), NPC.downedGolemBoss, 0.2f, 3, 6);
					DropHelper.DropItemCondition(player, ModContent.ItemType<UeliaceBar>(), CalamityWorld.downedProvidence, 0.1f, 1, 3);
					break;

				case ItemID.FloatingIslandFishingCrate:
					BlockCrateDrops();
					DropHelper.DropItemCondition(player, ModContent.ItemType<AerialiteBar>(), CalamityWorld.downedHiveMind || CalamityWorld.downedPerforator, 0.1f, 1, 3);
					DropHelper.DropItemCondition(player, ModContent.ItemType<EssenceofCinder>(), Main.hardMode, 0.2f, 2, 4);
					DropHelper.DropItemCondition(player, ModContent.ItemType<GalacticaSingularity>(), NPC.downedMoonlord, 0.1f, 1, 3);
					break;
			}
		}

		private static void BlockCrateDrops()
		{
			bool twoMechsDowned =
				(NPC.downedMechBoss1 && NPC.downedMechBoss2) ||
				(NPC.downedMechBoss2 && NPC.downedMechBoss3) ||
				(NPC.downedMechBoss3 && NPC.downedMechBoss1);
			int[] preMechBlockedDrops_Crate = new int[]
			{
				ItemID.MythrilOre,
				ItemID.OrichalcumOre,
				ItemID.AdamantiteOre,
				ItemID.TitaniumOre,
				ItemID.MythrilBar,
				ItemID.OrichalcumBar,
				ItemID.AdamantiteBar,
				ItemID.TitaniumBar,
			};
			int[] postOneMechBlockedDrops_Crate = new int[]
			{
				ItemID.AdamantiteOre,
				ItemID.TitaniumOre,
				ItemID.AdamantiteBar,
				ItemID.TitaniumBar,
			};
			if (CalamityConfig.Instance.EarlyHardmodeProgressionRework)
			{
				if (!NPC.downedMechBossAny)
					DropHelper.BlockDrops(preMechBlockedDrops_Crate);
				else if (!twoMechsDowned)
					DropHelper.BlockDrops(postOneMechBlockedDrops_Crate);
			}
		}
		#endregion
	}
}
