using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Generation;
using Terraria.Localization;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Enums;
using Terraria.ObjectData;
using Terraria.World.Generation;
using Terraria.ModLoader.IO;
using static Terraria.ModLoader.ModContent;

//YES I'm putting the whole thing in one file because fuck
namespace UnmasterMode.NPCs.Koopa
{
	//Statue
	public class ThwompStatue : ModTile
	{
		public override void SetDefaults() {
			Main.tileFrameImportant[Type] = true;
			Main.tileObsidianKill[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
			// We need to change the 3x3 default to allow only placement anchored to top rather than on bottom. Also, the 1,1 means that only the middle tile needs to attach
			TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
			TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
			// This is so we can place from above.
			TileObjectData.newTile.Origin = new Point16(1, 0);
			TileObjectData.addTile(Type);
			ModTranslation name = CreateMapEntryName();
			name.SetDefault("Statue");
			AddMapEntry(new Color(144, 148, 144), name);
			dustType = 11;
			disableSmartCursor = true;
		}

		public override void KillMultiTile(int i, int j, int frameX, int frameY) {
			Item.NewItem(i * 16, j * 16, 32, 48, ItemType<ThwompStatueItem>());
		}

		public override void HitWire(int i, int j) {
			// Find the coordinates of top left tile square through math
			int y = j - Main.tile[i, j].frameY / 18;
			int x = i - Main.tile[i, j].frameX / 18;

			Wiring.SkipWire(x, y);
			Wiring.SkipWire(x, y + 1);
			Wiring.SkipWire(x, y + 2);
			Wiring.SkipWire(x + 1, y);
			Wiring.SkipWire(x + 1, y + 1);
			Wiring.SkipWire(x + 1, y + 2);

			// We add 16 to x to spawn right between the 2 tiles. We also want to right on the ground in the y direction.
			int spawnX = x * 16 + 16;
			int spawnY = (y + 3) * 16;

			/*if (Main.rand.NextFloat() < .95f) // this is 95% chance for item spawn, 5% chance for npc spawn
			{
				// If you want to make a NPC spawning statue, see below.
				if (Wiring.CheckMech(x, y, 60) && Item.MechSpawn(spawnX, spawnY, ItemID.SilverCoin) && Item.MechSpawn(spawnX, spawnY, ItemID.GoldCoin) && Item.MechSpawn(spawnX, spawnY, ItemID.PlatinumCoin)) {
					int id = ItemID.SilverCoin;
					if (Main.rand.NextBool(100)) {
						id++;
						if (Main.rand.NextBool(100)) {
							id++;
						}
					}
					Item.NewItem(spawnX, spawnY - 20, 0, 0, id, 1, false, 0, false);
				}
			}*/
			//else {
			// If you want to make a NPC spawning statue, see below.
			int npcIndex = -1;
			// 30 is the time before it can be used again. NPC.MechSpawn checks nearby for other spawns to prevent too many spawns. 3 in immediate vicinity, 6 nearby, 10 in world.
			if (Wiring.CheckMech(x, y, 30) && NPC.MechSpawn((float)spawnX,(float)spawnY, mod.NPCType("Thwomp"))) {
				npcIndex = NPC.NewNPC(spawnX, spawnY - 4, mod.NPCType("Thwomp"));
			}
			if (npcIndex >= 0) {
				Main.npc[npcIndex].value = 0f;
				Main.npc[npcIndex].npcSlots = 0f;
				// Prevents Loot if NPCID.Sets.NoEarlymodeLootWhenSpawnedFromStatue and !Main.HardMode or NPCID.Sets.StatueSpawnedDropRarity != -1 and NextFloat() >= NPCID.Sets.StatueSpawnedDropRarity or killed by traps.
				// Prevents CatchNPC
				Main.npc[npcIndex].SpawnedFromStatue = true;
			}
			//}
		}
	}
	//Item
	public class ThwompStatueItem : ModItem
	{
		public override void SetStaticDefaults() {
			DisplayName.SetDefault("Thwomp Statue");
		}

		public override void SetDefaults() {
			item.CloneDefaults(ItemID.ArmorStatue);
			item.createTile = TileType<ThwompStatue>();
		}
	}
	//Gen which I'll likely need help with
	public class ThwompWorld : ModWorld
	{
		public override void ModifyWorldGenTasks(List<GenPass> tasks, ref float totalWeight)
		{
			int TrapsIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Traps"));
			if (TrapsIndex != -1) {
				tasks.Insert(TrapsIndex + 1, new PassLegacy("Unmaster Traps", UnmasterTraps));
			}
			/*int TrapsIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Traps"));
			if (TrapsIndex != -1) {
				tasks.Insert(ResetIndex + 1, new PassLegacy("Unmaster Mode Statues", delegate (GenerationProgress progress) {
					progress.Message = "URGH!!";
					// Not necessary, just a precaution.
					if (WorldGen.statueList.Any(point => point.X == TileType<ThwompStatue>())) {
						return;
					}
					// Make space in the statueList array, and then add a Point16 of (TileID, PlaceStyle)
					Array.Resize(ref WorldGen.statueList, WorldGen.statueList.Length + 1);
					for (int i = WorldGen.statueList.Length - 1; i < WorldGen.statueList.Length; i++) {
						WorldGen.statueList[i] = new Point16(TileType<ThwompStatue>(), 0);
						// Do this if you want the statue to spawn with wire and pressure plate
						WorldGen.StatuesWithTraps.Add(i);
					}
				}));
			}*/
		}
		private void UnmasterTraps(GenerationProgress progress) {
			progress.Message = "URGH!!";

			// Computers are fast, so WorldGen code sometimes looks stupid.
			// Here, we want to place a bunch of tiles in the world, so we just repeat until success. It might be useful to keep track of attempts and check for attempts > maxattempts so you don't have infinite loops. 
			// The WorldGen.PlaceTile method returns a bool, but it is useless. Instead, we check the tile after calling it and if it is the desired tile, we know we succeeded.
			for (int k = 0; k < (int)((double)(Main.maxTilesX * Main.maxTilesY) * 6E-05); k++) {
				bool placeSuccessful = false;
				Tile tile;
				int tileToPlace = TileType<ThwompStatue>();
				while (!placeSuccessful) {
					int x = WorldGen.genRand.Next(0, Main.maxTilesX);
					int y = WorldGen.genRand.Next(0, Main.maxTilesY);
					WorldGen.PlaceTile(x, y, tileToPlace);
					tile = Main.tile[x, y];
					placeSuccessful = tile.active() && tile.type == tileToPlace;
				}
			}
		}
	}
	//NPC
	public class Thwomp : ModNPC
    {
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Thwomp");
			Main.npcFrameCount[npc.type] = 3; 
		}
        public override void SetDefaults()
        {
            npc.width = 24;
            npc.height = 35;
            npc.damage = 100;
            npc.defense = 10;
            npc.lifeMax = 100;
            npc.HitSound = SoundID.NPCHit54;
            npc.DeathSound = SoundID.NPCDeath52;
            npc.value = Item.buyPrice(0, 0, 20, 0);
            npc.knockBackResist = 0f;
            npc.aiStyle = -1;
			npc.noGravity = true;
			//npc.noTileCollide = true;
			//banner = npc.type;
			//bannerItem = mod.ItemType("VisageBanner");
        }

		/*public override void NPCLoot()
		{
			Item.NewItem((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, mod.ItemType("BloodBrandBroken"), Main.rand.Next(5, 10));
			if (Main.rand.Next(100) == 0)
			Item.NewItem((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, mod.ItemType("PhantoKey"));
		}*/
		public override void AI()
		{	
			//I only ever-so-slightly know what I'm doing so bear with me here		
			//npc.ai[0] is being used as a state indicator here
			npc.TargetClosest(true);
			Player player = Main.player[npc.target];
			if (npc.HasValidTarget && player.Distance(npc.Center) < 200f && player.Distance(npc.Center) >= 100f && npc.ai[0] < 2 && npc.ai[1] == 0) { //Is the player between this range and that range?
				npc.ai[0] = 1; //enter notice state (just an animation frame change)
				npc.velocity = Vector2.Zero;
				//Main.NewText("he is looking.", 118, 50, 173);
			}
			else if (npc.ai[0] < 2 && npc.ai[1] == 0) {
				npc.ai[0] = 0; //otherwise literally just s
				npc.velocity = Vector2.Zero;
				//Main.NewText("he is not looking.", 118, 50, 173);
			}
			if (npc.HasValidTarget && Math.Abs(player.position.X - npc.position.X)  < 100f && Math.Abs(player.position.Y - npc.position.Y) < 300f && npc.ai[1] == 0 && npc.ai[0] != 3) { //is the player within this range?
				npc.ai[0] = 2; //enter attack state fall
			}
			//attack
			if (npc.ai[0] == 2 && npc.ai[0] != 3)
			{
				//Main.NewText("he is falling.", 118, 50, 173);
				npc.ai[1]++; //just a counter
				/*if (npc.ai[1] >= 1 && npc.ai[1] < 20) {
					//npc.rotation = Main.rand.Next(-1, 1);
					Main.PlaySound(SoundLoader.customSoundType, -1, -1, mod.GetSoundSlot(SoundType.Custom, "Sounds/URGH"));
				}*/
				if (npc.ai[1] == 10){ //if the counter equals 1 (basically, for one frame)
					npc.rotation = 0;
					npc.velocity += new Vector2(0, 10f); //I am literally just giving it downwards velocity for that one frame
				}
				if (npc.velocity == Vector2.Zero && npc.ai[1] >= 11){
					Main.PlaySound(SoundLoader.customSoundType, -1, -1, mod.GetSoundSlot(SoundType.Custom, "Sounds/Koopa/URGH"));
					npc.ai[0] = 3;
					npc.ai[1] = 0;
				}
			}
			
			//rise
			if (npc.ai[0] == 3)
			{
				//Main.NewText("current velocity is " + npc.velocity, 118, 50, 173);
				npc.ai[1]++;
				if (npc.ai[1] == 40) {
					npc.velocity += new Vector2(0, -2f);
				}
				if (npc.velocity.Y == 0.01f && npc.ai[1] >= 40) {
					npc.ai[0] = 0;
					npc.ai[1] = 0;
				}
			}
		}
		public override void FindFrame(int frameHeight) {
			if (npc.ai[0] == 0 || npc.ai[0] == 3 && npc.ai[1] > 40) {
				npc.frame.Y = 0 * frameHeight;
			}
			else if (npc.ai[0] == 1) {
				npc.frame.Y = 1 * frameHeight;
			}
			else if (npc.ai[0] == 2) {
				npc.frame.Y = 2 * frameHeight;
			}
		}
	}
}