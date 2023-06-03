using StarlightRiver.Content.Tiles.Underground.EvasionShrineBullets;
using StarlightRiver.Content.Abilities;
using StarlightRiver.Core.Systems.DummyTileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using StarlightRiver.Content.Items.BaseTypes;

namespace StarlightRiver.Content.Tiles.Underground
{
	public class ShrinePlayer : ModPlayer
	{
		public bool CombatShrineActive;
		public bool EvasionShrineActive;
		//public bool WitShrineActive;

		public override void ResetEffects()
		{
			CombatShrineActive = false;
			EvasionShrineActive = false;
			//WitShrineActive = false;
		}

		public static void SimulateGoldChest(Projectile source, bool twiceReforge)
		{
			int[] chestItems = new int[] { ItemID.BandofRegeneration, ItemID.MagicMirror, ItemID.CloudinaBottle, ItemID.HermesBoots, ItemID.Mace, ItemID.EnchantedBoomerang, ItemID.ShoeSpikes, ItemID.FlareGun };

			int chosenItem = Main.rand.Next(chestItems);

			if (chosenItem == ItemID.FlareGun)
			{
				int i = Item.NewItem(source.GetSource_FromAI(), source.getRect(), chosenItem, prefixGiven: -1);

				if (twiceReforge)
					Main.item[i].Prefix(-2);

				Main.item[i].GetGlobalItem<RelicItem>().isRelic = twiceReforge && Main.item[i].CanHavePrefixes();

				Item.NewItem(source.GetSource_FromAI(), source.getRect(), ItemID.Flare, 50);
			}
			else
			{
				int i = Item.NewItem(source.GetSource_FromAI(), source.getRect(), chosenItem, prefixGiven: -1);

				if (twiceReforge)
					Main.item[i].Prefix(-2);

				Main.item[i].GetGlobalItem<RelicItem>().isRelic = twiceReforge && Main.item[i].CanHavePrefixes();

			}
		}

		public static void SimulateWoodenChest(Projectile source)
		{
			int[] chestItems = new int[] { ItemID.Spear, ItemID.Blowpipe, ItemID.WoodenBoomerang, ItemID.Aglet, ItemID.ClimbingClaws, ItemID.Umbrella, 3068, ItemID.WandofSparking, ItemID.Radar, ItemID.PortableStool }; // 3068 is guide to plant fiber cortilage or whatever

			int chosenItem = Main.rand.Next(chestItems);

			Item.NewItem(source.GetSource_FromAI(), source.getRect(), chosenItem, prefixGiven: -1);
		}
	}
}