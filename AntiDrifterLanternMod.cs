using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

//[assembly: ModInfo("AntiDrifterLantern", "antidrifterlantern", Version = "1.0.0", Authors = new string[] { "weathersong" })]

namespace AntiDrifterLantern
{
	public class AntiDrifterLanternMod : ModSystem
	{
		const string LogHeader = "ANTI_DRIFTER_LANTERN_1_0";
		const string ConfigFilename = "AntiDrifterLanternConfig.json";

		ICoreAPI CoreApi;
		ICoreServerAPI ServerApi;
		ICoreClientAPI ClientApi;

		AntiDrifterLanternConfig Config;

		//POIRegistry PoiRegistry;
		List<LanternRecord> Lanterns;

        #region STARTUP

        public override void StartPre(ICoreAPI api)
		{
			base.StartPre(api);

			CoreApi = api;
			Config = new AntiDrifterLanternConfig();

			Lanterns = new List<LanternRecord>();

			LoadConfig();
		}

		public override void Start(ICoreAPI api)
		{
			base.Start(api);

			LogDebug("Registering classes...");
			api.RegisterBlockClass("BlockAntiDrifterLantern", typeof(BlockAntiDrifterLantern));
			api.RegisterBlockEntityClass("ADLantern", typeof(BlockEntityAntiDrifterLantern));

        }


		public override void StartServerSide(ICoreServerAPI api)
		{
			base.StartServerSide(api);

			ServerApi = api;
			//PoiRegistry = ServerApi.ModLoader.GetModSystem<POIRegistry>();

			ServerApi.Event.SaveGameLoaded += Server_OnSaveGameLoaded;
			ServerApi.Event.GameWorldSave += Server_OnGameWorldSave;

			ServerApi.Event.OnEntitySpawn += Server_OnEntitySpawn;

			ServerApi.RegisterCommand("adl", "List all current anti-drifter lanterns.", "[list|id #|clear|save|load]", Cmd_Adl);
		}

		public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

			ClientApi = api;
        }

		#endregion

		#region EVENTS

		private void Server_OnSaveGameLoaded()
		{
			LogDebug("OnSaveGameLoaded: Loading stored data for lanterns from SaveGame...");

			try
			{
				byte[] data = ServerApi.WorldManager.SaveGame.GetData($"{Mod.Info.ModID}:Lanterns");
				if (data == null)
				{
					LogDebug("No stored data found. If lanterns already exist, they will use default values.");
				}
				else
				{
					LogDebug("Stored data found. Deserializing...");
					List<LanternRecord> storedLanterns = SerializerUtil.Deserialize<List<LanternRecord>>(data);
					if (storedLanterns == null)
					{
						LogDebug("Stored data was found, but could not be deserialized!");
					}
					else
					{
						LogDebug("Stored data loaded successfully.");
						Lanterns = storedLanterns;
					}
				}
			}
			catch (Exception ex)
			{
				LogDebug($"Exception thrown when trying to load: {ex.Message}");
			}

		}

		private void Server_OnGameWorldSave()
		{
			LogDebug("OnGameWorldSave: Saving lanterns as stored data in the SaveGame...");

			try
			{
				ServerApi.WorldManager.SaveGame.StoreData($"{Mod.Info.ModID}:Lanterns", SerializerUtil.Serialize(Lanterns));
				LogDebug("Data storage complete.");
			}
			catch (Exception ex)
			{
				LogDebug($"Exception thrown when trying to store: {ex.Message}");
			}

		}

		private void Server_OnEntitySpawn(Vintagestory.API.Common.Entities.Entity entity)
		{
			// Always ignore players.
			if (entity.Code.ToString().ToUpperInvariant() == "GAME:PLAYER")
				return;

			LogDebug($"OnEntitySpawn: Checking a newly spawned '{entity.Code}' at pos {EntityPosString(entity.Pos)}, for spawn prevention; radius {Config.PreventSpawnRadius}.");
			if (ShouldPreventSpawn(entity))
            {
				LogDebug("Spawn must be prevented. DIE!");
				entity.Die(EnumDespawnReason.Removed);
			}
		}

		#endregion

		#region MOD_FUNCTIONALITY

		public void AddLantern(BlockPos atPos)
        {
			string posString = BlockPosString(atPos);

			LanternRecord lantern = Lanterns.FirstOrDefault(_ => _.PosMatches(atPos));
			if (lantern == null)
			{
				LogDebug($"Registering lantern at position ({posString}).");
				lantern = new LanternRecord(atPos);
				Lanterns.Add(lantern);
			}
			else
			{
				LogDebug($"AddLantern is ignoring position ({posString}), lantern already registered here, probably from stored data.");
			}
		}

		public void RemoveLantern(BlockPos atPos)
        {
			string posString = BlockPosString(atPos);

			LanternRecord lantern = Lanterns.FirstOrDefault(_ => _.PosMatches(atPos));
			if (lantern == null)
			{
				LogDebug($"RemoveLantern is ignoring position ({posString}), no lantern registered there. This shouldn't happen.");
			}
			else
			{
				LogDebug($"Unregistering lantern at position ({posString}).");
				Lanterns.Remove(lantern);
			}
		}

		public bool ShouldPreventSpawn(Entity entity)
        {
			// Check if the configured list of codes mentions this entity's code.
			if (!Config.PreventSpawnCodes.Exists(_ => _.ToUpperInvariant() == entity.Code.ToString().ToUpperInvariant()))
			{
				LogDebug($"Entity is not on the configured spawn prevention code list. Ignoring.");
				return false;
			}

			// Manual method
			int i = 0;
			foreach (LanternRecord lantern in Lanterns)
			{
				LogDebug($"Checking against lantern #{i}, which is at pos {BlockPosString(lantern.Pos)}.");
				if (ShouldPreventSpawn(lantern, entity))
					return true;
				i++;
			}

			LogDebug($"No lanterns are close enough to the entity to prevent its spawn.");
			return false;
        }

		public bool ShouldPreventSpawn(LanternRecord lantern, Entity entity)
        {
			// Check if the given lantern is within the configured radius-of-denial of this entity.
			float f_distance = entity.Pos.XYZFloat.Distance(lantern.Pos.ToVec3f());
			LogDebug($"Entity is at distance {f_distance} from this lantern.");
			return f_distance <= Config.PreventSpawnRadius;
        }

		#endregion

		#region COMMANDS

		private void Cmd_Adl(IServerPlayer player, int groupId, CmdArgs args)
		{
			string response = "";

			string verb = args.PopWord() ?? "";

			switch (verb.ToUpperInvariant())
			{
				case "LIST":
					if (Lanterns.Count == 0)
						response = "No anti-drifter lanterns are currently registered.";
					else
					{
						List<string> list = new List<string>();
						int i = 0;
						foreach (LanternRecord record in Lanterns)
							list.Add($"Lantern #{i++} has pos {BlockPosString(record.Pos)} and fuel {record.FuelRemaining}.");
						response = string.Join("\n", list);
					}
					break;

				case "ID":
					int? id = args.PopInt();
					if (!id.HasValue)
					{
						player.SendMessage(groupId, $"Invalid syntax for ADL [id #] command.", EnumChatType.CommandError);
						return;
					}
					if (id < 0 || id > Lanterns.Count-1)
					{
						player.SendMessage(groupId, $"Lanter id {id} is not valid. Try [list].", EnumChatType.CommandError);
						return;
					}
					LanternRecord lantern = Lanterns[id.Value];
					response =
						$"Lantern #{id} details:\n" +
						$"Pos X {lantern.Pos.X}, Y {lantern.Pos.Y}, Z {lantern.Pos.Z}\n" +
						$"FuelRemaining {lantern.FuelRemaining}" +
						"";
					break;

				case "CLEAR":
					response = "Okay, lantern list cleared. A [save] is recommended. Lanterns still in the world will re-register on next load.";
					Lanterns.Clear();
					break;

				case "SAVE":
					response = "Save routine run. Results will be in debug log.";
					Server_OnGameWorldSave();
					break;

				case "LOAD":
					response = "Load routine run. Results will be in debug log.";
					Server_OnSaveGameLoaded();
					break;

				case null:
				default:
					player.SendMessage(groupId, $"Invalid syntax for ADL command.", EnumChatType.CommandError);
					return;
			}

			player.SendMessage(groupId, response, EnumChatType.CommandSuccess);

		}

		#endregion

		#region UTIL_FUNCTIONS

		private void LoadConfig()
		{
			try
			{
				Config = CoreApi.LoadModConfig<AntiDrifterLanternConfig>(ConfigFilename);
				if (Config == null)
				{
					LogNotif("No config file found. Using defaults, and creating a default config file.");
					Config = DefaultConfig();
					CoreApi.StoreModConfig(Config, ConfigFilename);
				}
				else
				{
					// Extra sanity checks / warnings on particular values:
					if (string.IsNullOrEmpty(Config.TemporalStormMode))
						Config.GetStormMode();
					else if (!Config.CheckStormModeString())
					{
						LogWarn($"Config: TemporalStormMode '{Config.TemporalStormMode}' isn't valid. Valid modes are: {Config.ValidStormModes}. Default will be used.");
						// This forces the default and saves back the ToString value so that StoreModConfig fixes the problem for next time.
						Config.GetStormMode();
					}
					LogNotif("Config loaded.");
					// In case this was an old version of the config, store again anyway so that it's updated.
					CoreApi.StoreModConfig(Config, ConfigFilename);
				}
			}
			catch (Exception ex)
			{
				LogError($"Problem loading the mod's config file, using defaults. Check the config file for typos! Error details: {ex.Message}");
				Config = DefaultConfig();
			}
		}

		private AntiDrifterLanternConfig DefaultConfig()
		{
			AntiDrifterLanternConfig defaultConfig = new AntiDrifterLanternConfig();
			defaultConfig.ResetToDefaults();

			return defaultConfig;
		}

		public void LogNotif(string msg)
		{
			CoreApi?.Logger.Notification($"[{LogHeader}] {msg}");
		}

		public void LogWarn(string msg)
        {
			CoreApi?.Logger.Warning($"[{LogHeader}] {msg}");
        }

		public void LogError(string msg)
		{
			CoreApi?.Logger.Error($"[{LogHeader}] {msg}");
		}

		public void LogDebug(string msg)
        {
			if (Config.DebugLogging)
				CoreApi?.Logger.Debug($"[{LogHeader}] {msg}");
        }

		public void MessagePlayer(IPlayer toPlayer, string msg)
		{
			ServerApi.SendMessage(toPlayer, GlobalConstants.GeneralChatGroup, msg, EnumChatType.OwnMessage);
		}

		public string BlockPosString(BlockPos pos)
        {
			return $"{pos.X} {pos.Y} {pos.Z}";
        }

		public string EntityPosString(SyncedEntityPos pos)
        {
			return $"{pos.X:#.00} {pos.Y:#.00} {pos.Z:#.00}";
		}

		#endregion

	}
}
