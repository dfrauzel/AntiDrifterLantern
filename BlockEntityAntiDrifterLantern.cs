using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace AntiDrifterLantern
{
    public class BlockEntityAntiDrifterLantern : BlockEntityOpenableContainer
    {

		public override string InventoryClassName => "adlantern";

		// General properties
		private AntiDrifterLanternMod adMod;
		private SystemTemporalStability tsMod;
		private bool unlimitedFuel;
		private Random rnd;
		//private BlockAntiDrifterLantern ownBlock;
		//private int quantitySlots;
		private ICoreClientAPI capi;

		public bool IsActive => GetIsActive();

		public double FuelUntilTotalHours;
		public double FuelRemaining => Api.World.Calendar.TotalHours - FuelUntilTotalHours;
		public bool OutOfFuel => !unlimitedFuel && Api.World.Calendar.TotalHours > FuelUntilTotalHours;
		public int SpareGears => GetSpareGears();

		// Sounds
		//private ILoadedSound ambientSound;
		private static readonly AssetLocation lanternOpen = new AssetLocation("sounds/block/hopperopen");
		public override AssetLocation OpenSound => lanternOpen;
		public override AssetLocation CloseSound => null;

		// Vfx
		private static SimpleParticleProperties smoke;
		private static SimpleParticleProperties spark;

		// Inventory
		protected InventoryGeneric inventory;
		public override InventoryBase Inventory => inventory;

		public BlockEntityAntiDrifterLantern()
		{
			rnd = new Random();

			smoke = new SimpleParticleProperties(
				3f,										// minQuantity
				10f,									// maxQuantity
				ColorUtil.ToRgba(150, 25, 217, 255),	// color (adjusted in OnGameTick)
				new Vec3d(),							// minPos (set in tick)
				new Vec3d(),							// maxPos
				new Vec3f(-0.2f, 1f, -0.2f),			// minVelocity
				new Vec3f(0.2f, 2f, 0.2f),				// maxVelocity
				0.5f,									// lifeLength
				0.1f,									// gravityEffect
				0.2f,									// minSize
				0.3f,									// maxSize
				EnumParticleModel.Quad
			)
			{
				SelfPropelled = true,
				OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -255f),				
				SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 2f)
			};
			spark = new SimpleParticleProperties(
				1f,										// minQuantity
				5f,										// maxQuantity
				ColorUtil.ToRgba(255, 255, 255, 255),	// color
				new Vec3d(),							// minPos (set in tick)
				new Vec3d(),							// maxPos
				new Vec3f(),							// minVelocity
				new Vec3f(),							// maxVelocity
				0.1f,									// lifeLength
				10f,									// gravityEffect
				0.1f,									// minSize
				0.2f,									// maxSize
				EnumParticleModel.Cube
			)
			{
				SelfPropelled = false,
			};

		}

		public override void Initialize(ICoreAPI api)
		{

			capi = api as ICoreClientAPI;
			adMod = api.ModLoader.GetModSystem<AntiDrifterLanternMod>();
			tsMod = api.ModLoader.GetModSystem<SystemTemporalStability>(true);
			if (adMod != null && adMod.Config != null)
				unlimitedFuel = !adMod.Config.RequireFuel;

			InitInventory();
			AddToRegistry();

			//bool flag = this.ambientSound == null && api.Side == EnumAppSide.Client;
			//if (flag)
			//{
			//	this.ambientSound = ((IClientWorldAccessor)api.World).LoadSound(new SoundParams
			//	{
			//		Location = new AssetLocation("game:sounds/environment/fire.ogg"),
			//		ShouldLoop = true,
			//		Position = this.Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
			//		DisposeOnFinish = false,
			//		Volume = 0.3f,
			//		Range = 8f
			//	});
			//	bool lit = this.Lit;
			//	if (lit)
			//	{
			//		this.ambientSound.Start();
			//	}
			//}

			// Initialize here after inventory is defined but before registering for listeners.
			base.Initialize(api);

			// Fuel consumption, and vfx
			RegisterGameTickListener(new Action<float>(OnGameTick), 500, 0);

		}

		private void InitInventory()
		{
			if (inventory == null)
			{
				int numSlots = adMod == null ? 4 : adMod.Config.LanternInventorySlots;
				if (numSlots < 1)
					numSlots = 4;
				inventory = new InventoryGeneric(numSlots, null, null, null);

				inventory.OnInventoryClosed += OnInvClosed;
				inventory.OnInventoryOpened += OnInvOpened;
				inventory.SlotModified += OnSlotModified;
			}
		}

		#region EVENTS

		private void OnGameTick(float deltaTime)
		{
			// Fuel
			if (OutOfFuel)
			{
				if (!TryAddFuel())
				{
					//ILoadedSound loadedSound = this.ambientSound;
					//if (loadedSound != null)
					//{
					//	loadedSound.Stop();
					//}
					return;
				}
			}

			// Vfx
			if (Api.Side == EnumAppSide.Client)
			{
				smoke.MinPos.Set(Pos.X + 0.375, Pos.Y + 0.625f, Pos.Z + 0.375); // "root" of particle spawn
				smoke.AddPos.Set(0.25, 0.0, 0.25); // randomized delta of root
				smoke.Color = ColorUtil.ToRgba( 150, 25 + rnd.Next(25), 200 + rnd.Next(55), 255 );
				Api.World.SpawnParticles(smoke);

				if (rnd.Next(3) == 0)
				{
					spark.MinPos.Set(Pos.X + 0.5f, Pos.Y + 0.2f, Pos.Z + 0.5f); // "root" of particle spawn
					if (rnd.Next(2) == 0)
						spark.Color = ColorUtil.ToRgba(0 + rnd.Next(256), 77, 25, 0);
					else
						spark.Color = ColorUtil.ToRgba(0 + rnd.Next(256), 255, 0, 0);
					spark.MinVelocity.Set(4f * (rnd.Next(2) == 0 ? -1 : 1), 12f, 4f * (rnd.Next(2) == 0 ? -1 : 1));
					Api.World.SpawnParticles(spark);
				}
			}

		}

		public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
		{
			if (Api.World is IServerWorldAccessor)
			{
				byte[] data;
				using (MemoryStream memoryStream = new MemoryStream())
				{
					BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
					binaryWriter.Write("BlockEntityAntiDrifterLanternDialog");
					binaryWriter.Write( Lang.Get("antidrifterlantern:lanterncontents") );
					binaryWriter.Write(4);
					TreeAttribute treeAttribute = new TreeAttribute();
					inventory.ToTreeAttributes(treeAttribute);
					treeAttribute.ToBytes(binaryWriter);
					data = memoryStream.ToArray();
				}
				((ICoreServerAPI)Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, Pos.X, Pos.Y, Pos.Z, 5000, data);
				byPlayer.InventoryManager.OpenInventory(inventory);
				MarkDirty();
			}
			return true;
		}

		public override void OnReceivedServerPacket(int packetid, byte[] data)
		{
			base.OnReceivedServerPacket(packetid, data);
		}

		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
		{
			InitInventory();
			base.FromTreeAttributes(tree, worldForResolving);			
			FuelUntilTotalHours = tree.GetDouble("FuelUntilTotalHours", 0.0);
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
			base.ToTreeAttributes(tree);

			tree.SetDouble("FuelUntilTotalHours", FuelUntilTotalHours);
		}

		private void OnInvOpened(IPlayer player)
		{
			inventory.PutLocked = false;
		}

		private void OnInvClosed(IPlayer player)
		{
			GuiDialogBlockEntityInventory invDialog = this.invDialog;
			if (invDialog != null)
			{
				invDialog.Dispose();
			}
		}

		private void OnSlotModified(int slotId)
		{
			IWorldChunk chunkAtBlockPos = Api.World.BlockAccessor.GetChunkAtBlockPos(Pos);
			if (chunkAtBlockPos == null)
			{
				return;
			}
			chunkAtBlockPos.MarkModified();

		}

		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
		{
			if (tsMod != null && adMod != null && adMod.Config != null && tsMod.StormData.nowStormActive)
			{
				string stormLine = "Storm active! ";
				if (!adMod.Config.EnableDuringStorms)
					stormLine += "Operation blocked!";
				else
					stormLine += $"Operating at {adMod.Config.StormFuelEfficiency:#.0}x fuel efficiency!";
				dsc.AppendLine(stormLine);
			}

			if (unlimitedFuel)
			{
				dsc.AppendLine($"No fuel required.");
			}
			else
			{
				double hoursRemain = FuelUntilTotalHours - Api.World.Calendar.TotalHours;
				if (hoursRemain <= 0)
				{
					dsc.AppendLine($"Fuel empty. Lantern inactive.");
				}
				else
				{
					string hours = hoursRemain >= 2 ? $"{hoursRemain:#} hours" : (hoursRemain >= 1 ? "1 hour" : "less than 1 hour");
					int gears = GetSpareGears();
					dsc.AppendLine($"Fuel for {hours} with {gears} spare gear{Plural(gears)}.");
				}
			}

			//base.GetBlockInfo(forPlayer, dsc);
		}

		public override void OnBlockBroken(IPlayer byPlayer = null)
		{
			if (Api.World is IServerWorldAccessor)
			{
				Vec3d position = Pos.ToVec3d().Add(0.5, 0.5, 0.5);
				foreach (ItemSlot itemSlot in inventory)
				{
					if (itemSlot.Itemstack != null)
					{
						Api.World.SpawnItemEntity(itemSlot.Itemstack, position, null);
						itemSlot.Itemstack = null;
						itemSlot.MarkDirty();
					}
				}
			}
			base.OnBlockBroken(byPlayer);
		}

		public override void OnBlockRemoved()
		{
			base.OnBlockRemoved();

			RemoveFromRegistry();
		}

		public override void OnBlockUnloaded()
		{
			base.OnBlockUnloaded();

			RemoveFromRegistry();
		}

		#endregion

		private bool TryAddFuel()
		{
			for (int i = 0; i < inventory.Count; i++)
			{
				ItemSlot slot = inventory[i];
				if (slot != null && slot.Itemstack?.Class == EnumItemClass.Item && slot.Itemstack.Item.Code.Path == "gear-temporal")
				{
					double addHours = adMod == null ? 168 : adMod.Config.FuelDurationHours;
					if (addHours <= 0)
						addHours = 7;
					FuelUntilTotalHours = Api.World.Calendar.TotalHours + addHours;
					slot.TakeOut(1);
					slot.MarkDirty();
					return true;
				}
			}
			return false;
		}

		private void Msg(string msg)
		{
			capi?.ShowChatMessage(msg);
		}

		private string Plural(int count)
		{
			return count == 1 ? "" : "s";
		}

		private bool GetIsActive()
		{
			// Storm?
			if (tsMod != null && adMod != null && adMod.Config != null)
			{
				if (tsMod.StormData.nowStormActive && !adMod.Config.EnableDuringStorms)
					return false;
			}

			// Fuel?
			return !OutOfFuel;
		}

		private int GetSpareGears()
		{
			int gears = 0;
			foreach (ItemSlot itemSlot in Inventory)
			{
				Item item = itemSlot.Itemstack?.Item;
				if (item != null && item.Code.Path == "gear-temporal")
					gears++;
			}

			return gears;
		}

		private void AddToRegistry()
		{
			// PoiRegistry approach
			//if (Api.Side == EnumAppSide.Server)
			//{
			//	Api.ModLoader.GetModSystem<POIRegistry>(true).AddPOI(this);
			//}

			// Homebrew approach
			adMod?.AddLantern(this);
		}

		private void RemoveFromRegistry()
		{
			// PoiRegistry approach
			//if (Api.Side == EnumAppSide.Server)
			//{
			//	Api.ModLoader.GetModSystem<POIRegistry>(true).RemovePOI(this);
			//}

			// Homebrew approach
			adMod?.RemoveLantern(this);

		}

	}
}
