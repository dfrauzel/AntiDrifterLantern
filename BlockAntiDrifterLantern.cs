using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace AntiDrifterLantern
{
    public class BlockAntiDrifterLantern : Block
    {
		private AntiDrifterLanternMod AdMod;
		private ICoreAPI CoreApi;
		private ICoreClientAPI CoreClientApi;

        public BlockAntiDrifterLantern()
        {

        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

			AdMod = api.ModLoader.GetModSystem<AntiDrifterLanternMod>();
			CoreApi = api;

			if (api.Side == EnumAppSide.Server)
			{

			}

			CoreClientApi = CoreApi as ICoreClientAPI;
        }

		public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
		{
			return "Working anti-drifter lantern";
		}

		public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
		{
			return $"Remaining fuel ???";
		}

		public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
		{
			return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			//ItemSlot slot = byPlayer.Entity.RightHandItemSlot;
			//if (slot.Empty)
			//{
			//	CoreClientApi?.ShowChatMessage("Empty hand.");
			//	return false;
			//}
			//response += byPlayer.Entity.LeftHandItemSlot.Itemstack.Block.Code
			//response += byPlayer.Entity.RightHandItemSlot.GetStackName();

			return base.OnBlockInteractStart(world, byPlayer, blockSel);

			// stolen from UsefulStuff:BlockRappelAnchor :
			/*
			bool flag = !byPlayer.Entity.Controls.Sneak;
			bool result;
			if (flag)
			{
				result = base.OnBlockInteractStart(world, byPlayer, blockSel);
			}
			else
			{
				BlockPos blockPos = blockSel.Position.DownCopy(1);
				ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
				string a;
				if (activeHotbarSlot == null)
				{
					a = null;
				}
				else
				{
					ItemStack itemstack = activeHotbarSlot.Itemstack;
					if (itemstack == null)
					{
						a = null;
					}
					else
					{
						CollectibleObject collectible = itemstack.Collectible;
						if (collectible == null)
						{
							a = null;
						}
						else
						{
							AssetLocation code = collectible.Code;
							a = ((code != null) ? code.Path : null);
						}
					}
				}
				bool flag2 = a == "rope" && world.BlockAccessor.GetBlock(blockPos).Id == 0;
				if (flag2)
				{
					int num = byPlayer.InventoryManager.ActiveHotbarSlot.StackSize;
					Block block = world.BlockAccessor.GetBlock(base.CodeWithPart("section", 1));
					while (num > 0 && world.BlockAccessor.GetBlock(blockPos).Id == 0)
					{
						world.BlockAccessor.SetBlock(block.Id, blockPos);
						blockPos.Down(1);
						num--;
					}
					byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(byPlayer.InventoryManager.ActiveHotbarSlot.StackSize - num);
					result = true;
				}
				else
				{
					bool flag3 = this.isRope(blockPos) && byPlayer.InventoryManager.ActiveHotbarSlot.Empty;
					if (flag3)
					{
						int num2 = 0;
						while (this.isRope(blockPos))
						{
							num2++;
							blockPos.Down(1);
						}
						bool flag4 = num2 > 0;
						if (flag4)
						{
							ItemStack itemstack2 = new ItemStack(this.api.World.GetItem(new AssetLocation("game:rope")), num2);
							bool flag5 = !byPlayer.InventoryManager.TryGiveItemstack(itemstack2, false);
							if (flag5)
							{
								world.SpawnItemEntity(itemstack2, byPlayer.Entity.SidedPos.XYZ, null);
							}
						}
						while (blockPos.Y < blockSel.Position.Y)
						{
							bool flag6 = this.isRope(blockPos);
							if (flag6)
							{
								world.BlockAccessor.SetBlock(0, blockPos);
							}
							blockPos.Up(1);
						}
						result = true;
					}
					else
					{
						result = true;
					}
				}
			}
			return result;
			*/

		}

	}
}
