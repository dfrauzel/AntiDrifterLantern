using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;

namespace AntiDrifterLantern
{
    public class BlockEntityAntiDrifterLantern : BlockEntity
    {
		//public Vec3d Position => Pos.ToVec3d();
		//public string Type => SpawnMatcher.SpawnMatcherName;

		private AntiDrifterLanternMod AdMod;

		public override void Initialize(ICoreAPI api)
		{
			base.Initialize(api);

			AdMod = Api.ModLoader.GetModSystem<AntiDrifterLanternMod>();

			AddToRegistry();
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

		private void AddToRegistry()
		{
			// PoiRegistry approach
			//if (Api.Side == EnumAppSide.Server)
			//{
			//	Api.ModLoader.GetModSystem<POIRegistry>(true).AddPOI(this);
			//}

			// Homebrew approach
			AdMod?.AddLantern(Pos);
		}

		private void RemoveFromRegistry()
		{
			// PoiRegistry approach
			//if (Api.Side == EnumAppSide.Server)
			//{
			//	Api.ModLoader.GetModSystem<POIRegistry>(true).RemovePOI(this);
			//}

			// Homebrew approach
			AdMod?.RemoveLantern(Pos);

		}

	}
}
