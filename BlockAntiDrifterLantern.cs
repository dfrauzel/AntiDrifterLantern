using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

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
			return Lang.Get("antidrifterlantern:block-adlantern-up");
		}

		//public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
		//{
		//	return $"Remaining fuel ???";
		//}

		//public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
		//{
		//	return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
		//}

		//public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		//{
		//	if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityAntiDrifterLantern blockEntityAdLantern)
		//	{
		//		return blockEntityAdLantern.OnBlockInteractStart(world, byPlayer, blockSel);
		//	}
		//	return base.OnBlockInteractStart(world, byPlayer, blockSel);
		//}

	}
}
