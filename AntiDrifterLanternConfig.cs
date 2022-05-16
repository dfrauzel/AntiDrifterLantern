using System;
using System.Collections.Generic;
using System.Text;

namespace AntiDrifterLantern
{
	public enum AntiDrifterLanternStormModes
	{
		Unknown,
		NotFunctional,
		BurnsExtraFuel,
		FullyFunctional,
	}

    public class AntiDrifterLanternConfig
    {
		public List<string> PreventSpawnCodes;
		public float PreventSpawnRadius;

		public string TemporalStormMode;
		public bool RequireFuel;
		public int FuelDurationInDays;
        public bool DebugLogging;

		private AntiDrifterLanternStormModes _StormMode;
		internal AntiDrifterLanternStormModes StormMode => GetStormMode();
		internal string ValidStormModes => GetValidStormModes();

		internal AntiDrifterLanternConfig()
		{
			PreventSpawnCodes = new List<string>();
			_StormMode = AntiDrifterLanternStormModes.Unknown;
		}

		internal void ResetToDefaults()
		{
			PreventSpawnRadius = 100f;
			_StormMode = AntiDrifterLanternStormModes.NotFunctional;
			TemporalStormMode = _StormMode.ToString();
			RequireFuel = true;
			FuelDurationInDays = 7;
			DebugLogging = false;

			PreventSpawnCodes.Clear();
			PreventSpawnCodes.Add("game:drifter-normal");
		}

		internal bool CheckStormModeString()
		{
			return Enum.TryParse(TemporalStormMode, true, out AntiDrifterLanternStormModes _);
		}

		internal AntiDrifterLanternStormModes GetStormMode()
		{
			if (_StormMode == AntiDrifterLanternStormModes.Unknown)
			{
				if (!Enum.TryParse(TemporalStormMode, true, out _StormMode))
					_StormMode = AntiDrifterLanternStormModes.NotFunctional;
				TemporalStormMode = _StormMode.ToString();
			}
			return _StormMode;
		}

		private string GetValidStormModes()
		{
			List<string> list = new List<string>();
			foreach (
				AntiDrifterLanternStormModes mode in new AntiDrifterLanternStormModes[]
				{ AntiDrifterLanternStormModes.NotFunctional, AntiDrifterLanternStormModes.BurnsExtraFuel, AntiDrifterLanternStormModes.FullyFunctional }
			)
				list.Add(mode.ToString());

			return string.Join(", ", list);
		}

	}
}
