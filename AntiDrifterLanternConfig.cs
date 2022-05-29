using System;
using System.Collections.Generic;
using System.Text;

namespace AntiDrifterLantern
{
    public class AntiDrifterLanternConfig
    {
		public List<string> PreventSpawnCodes;
		public float PreventSpawnRadius;

		public bool RequireFuel;
		public double FuelDurationHours;
		public bool EnableDuringStorms;
		public float StormFuelEfficiency;
		public int LanternInventorySlots;
        public bool DebugLogging;

		internal AntiDrifterLanternConfig()
		{
			PreventSpawnCodes = new List<string>();
			
		}

		internal void ResetToDefaults()
		{
			PreventSpawnCodes.Clear();
			PreventSpawnCodes.Add("game:drifter-normal");
			PreventSpawnRadius = 100f;

			RequireFuel = true;
			FuelDurationHours = 168;
			EnableDuringStorms = false;
			StormFuelEfficiency = 1.0f;
			LanternInventorySlots = 4;
			DebugLogging = true;

		}

	}
}
