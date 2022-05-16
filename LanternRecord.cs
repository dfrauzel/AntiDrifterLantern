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
		public int FuelRemaining;

		// Should only be used by the Deserializer.
		public LanternRecord()
		{
			Pos = new BlockPos();
		}

		// New lantern plonked down.
        public LanternRecord(BlockPos isAtPos, int fuelRemaining = 0)
        {
			Pos = new BlockPos(isAtPos.X, isAtPos.Y, isAtPos.Z);

			FuelRemaining = fuelRemaining;
        }

		public bool PosMatches(BlockPos pos)
		{
			return Pos.X == pos.X && Pos.Y == pos.Y && Pos.Z == pos.Z;
		}

	}
}
