using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Vintagestory.API.MathTools;
using ProtoBuf;

namespace AntiDrifterLantern
{
	[ProtoContract]
    public class LanternRecord
    {
		[ProtoMember(1)]
		public BlockPos Pos;

		[ProtoMember(2)]
		public int FuelTicksRemaining;

		internal BlockEntityAntiDrifterLantern LanternEntity;

		//public int FuelRemaining => GetFuelRemaining();

		// Should only be used by the Deserializer.
		public LanternRecord()
		{
			Pos = new BlockPos();
		}

        public LanternRecord(BlockPos isAtPos, BlockEntityAntiDrifterLantern lantern)
        {
			Pos = new BlockPos(isAtPos.X, isAtPos.Y, isAtPos.Z);

			LanternEntity = lantern;

			FuelTicksRemaining = 0;
        }

		public bool PosMatches(BlockPos pos)
		{
			return Pos.X == pos.X && Pos.Y == pos.Y && Pos.Z == pos.Z;
		}

	}
}
