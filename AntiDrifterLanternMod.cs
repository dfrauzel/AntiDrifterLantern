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

		public AntiDrifterLanternConfig Config;

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

			//ServerApi.Event.SaveGameLoaded += Server_OnSaveGameLoaded;
			//ServerApi.Event.GameWorldSave += Server_OnGameWorldSave;

			ServerApi.Event.OnEntitySpawn += Server_OnEntitySpawn;

			ServerApi.RegisterCommand("adl", "Anti-Drifter Lantern utility command.", "[list|id #|clear]", Cmd_Adl);
			ServerApi.RegisterCommand("adlcodes", "Anti-Drifter Lantern spawn codes command.", "[list|add ...|remove|clear|test ...|testlist]", Cmd_AdlCodes);
			ServerApi.RegisterCommand("adlconfig", "Anti-Drifter Lantern configuration command.", "[radius #|fuel [true/false]|fueldur #|storms [true/false]|invslots #|debug [true/false]]", Cmd_AdlConfig);
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

		private void Server_OnEntitySpawn(Entity entity)
		{
			// Always ignore players.
			if (entity.Code.ToString().ToUpperInvariant() == "GAME:PLAYER")
				return;

			// Short-circuit if there aren't even any prevented spawn codes, or lanterns.
			if (Lanterns.Count == 0 || Config.PreventSpawnCodes.Count == 0)
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

		public void AddLantern(BlockEntityAntiDrifterLantern lantern)
        {
			BlockPos atPos = lantern.Pos;
			string posString = BlockPosString(atPos);

			LanternRecord record = Lanterns.FirstOrDefault(_ => _.PosMatches(atPos));
			if (record == null)
			{
				LogDebug($"Registering lantern at position ({posString}).");
				record = new LanternRecord(atPos, lantern);
				Lanterns.Add(record);
			}
			else
			{
				LogDebug($"AddLantern is ignoring position ({posString}), lantern already registered here, probably from stored data.");
			}
		}

		public void RemoveLantern(BlockEntityAntiDrifterLantern lantern)
        {
			BlockPos atPos = lantern.Pos;
			string posString = BlockPosString(atPos);

			LanternRecord record = Lanterns.FirstOrDefault(_ => _.PosMatches(atPos));
			if (record == null)
			{
				LogDebug($"RemoveLantern is ignoring position ({posString}), no lantern registered there. This shouldn't happen.");
			}
			else
			{
				LogDebug($"Unregistering lantern at position ({posString}).");
				Lanterns.Remove(record);
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

			// Manual method (instead of POI)
			int i = 0;
			foreach (LanternRecord lantern in Lanterns)
			{
				LogDebug($"Checking against lantern #{i}, which is at pos {BlockPosString(lantern.Pos)}.");
				if (ShouldPreventSpawn(lantern, entity))
					return true;
				i++;
			}

			LogDebug($"No active lanterns are close enough to the entity to prevent its spawn.");
			return false;
        }

		public bool ShouldPreventSpawn(LanternRecord lantern, Entity entity)
        {
			// Lantern active?
			if (!lantern.LanternEntity.IsActive)
			{
				LogDebug($"This lantern is not active.");
				return false;
			}

			// Lantern is within the configured radius-of-denial of this entity?
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
							list.Add($"Lantern #{i++} has pos {BlockPosString(record.Pos)} and fuel {LanternFuelString(record)}");
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
						player.SendMessage(groupId, $"Lantern id {id} is not valid. Try [list].", EnumChatType.CommandError);
						return;
					}
					LanternRecord lantern = Lanterns[id.Value];
					response =
						$"Lantern #{id} details:\n" +
						$"Pos X {lantern.Pos.X}, Y {lantern.Pos.Y}, Z {lantern.Pos.Z}\n" +
						$"FuelRemaining {LanternFuelString(lantern)}";
					break;

				case "CLEAR":
					response = "Okay, lantern list cleared. A [save] is recommended. Lanterns still in the world will re-register on next load.";
					Lanterns.Clear();
					break;

				//case "SAVE":
				//	response = "Save routine run. Results will be in debug log.";
				//	Server_OnGameWorldSave();
				//	break;

				//case "LOAD":
				//	response = "Load routine run. Results will be in debug log.";
				//	Server_OnSaveGameLoaded();
				//	break;

				case null:
				default:
					player.SendMessage(groupId, $"Invalid syntax for ADL command. Check /help adl", EnumChatType.CommandError);
					return;
			}

			player.SendMessage(groupId, response, EnumChatType.CommandSuccess);
		}

		private void Cmd_AdlCodes(IServerPlayer player, int groupId, CmdArgs args)
		{
			string response = "";

			string verb = args.PopWord() ?? "";

			switch (verb.ToUpperInvariant())
			{
				case "LIST":
					if (Config.PreventSpawnCodes.Count == 0)
						response = "No spawn codes are configured. Lanterns will have no effect.";
					else
					{
						List<string> list = new List<string>();
						int i = 0;
						foreach (string s in Config.PreventSpawnCodes)
							list.Add($"Code #{i++} is '{s}'");
						response = string.Join("\n", list);
					}
					break;

				case "ADD":
					string newcode = args.PopWord() ?? "";
					if (newcode == "")
					{
						player.SendMessage(groupId, $"ADLCodes [add] requires an argument. Eg: /adlcodes add game:drifter-normal", EnumChatType.CommandError);
						return;
					}
					response = $"Okay, '{newcode}' added to the spawn codes list.";
					Config.PreventSpawnCodes.Add(newcode);
					break;

				case "REMOVE":
					string remcode = args.PopWord() ?? "";
					if (remcode == "")
					{
						player.SendMessage(groupId, $"ADLCodes [remove] requires an argument. Eg: /adlcodes remove game:drifter-normal", EnumChatType.CommandError);
						return;
					}
					if (!Config.PreventSpawnCodes.Contains(remcode))
					{
						player.SendMessage(groupId, $"The spawn code list does not contain '{remcode}'.", EnumChatType.CommandError);
						return;
					}
					response = $"Okay, '{remcode}' removed the spawn codes list.";
					Config.PreventSpawnCodes.Remove(remcode);
					break;

				case "CLEAR":
					response = "Okay, spawn code list cleared. Lanterns will have no effect.";
					Config.PreventSpawnCodes.Clear();
					break;

				case "TEST":
					if (ServerApi == null)
						return;

					string testcode = args.PopWord() ?? "";
					if (testcode == "")
					{
						player.SendMessage(groupId, $"ADLCodes [test] requires an argument. Eg: /adlcodes test game:drifter-normal", EnumChatType.CommandError);
						return;
					}
					testcode = testcode.ToLower();
					if (!testcode.Contains(":"))
						testcode = "game:" + testcode;
					bool matched = false;
					foreach(EntityProperties ep in ServerApi.World.EntityTypes)
					{
						if (ep.Code.ToString() == testcode)
						{
							response = $"'{testcode}' is valid.";
							matched = true;
							break;
						}
					}
					if (!matched)
						response = $"'{testcode}' is not valid. No matches in World.EntityTypes.";
					break;

				case "TESTLIST":
					// this preface feature ensures the list entries all have a domain, and saves that change
					for (int i = 0; i < Config.PreventSpawnCodes.Count; i++)
					{
						Config.PreventSpawnCodes[i] = Config.PreventSpawnCodes[i].ToLower();
						if (!Config.PreventSpawnCodes[i].Contains(":"))
							Config.PreventSpawnCodes[i] = "game:" + Config.PreventSpawnCodes[i];
					}
					CoreApi.StoreModConfig(Config, ConfigFilename);
					// different version of the above search in TEST, instead of a standalone function, to avoid multiple searches
					Dictionary<string, EntityProperties> matches = new Dictionary<string, EntityProperties>();
					foreach (string spawncode in Config.PreventSpawnCodes)
						matches[spawncode] = null;
					int matchcount = 0;
					foreach (EntityProperties ep in ServerApi.World.EntityTypes)
					{
						string code = ep.Code.ToString();
						if (matches.ContainsKey(code))
						{
							matches[code] = ep;
							matchcount++;
						}
						if (matchcount == Config.PreventSpawnCodes.Count)
							break;
					}
					response = "Test results for PreventSpawnCodes list:\n";
					foreach (string key in matches.Keys)
					{
						if (matches[key] == null)
							response += $"{key}: INVALID\n";
						else
							response += $"{key} is valid.\n";
					}
					break;

				case null:
				default:
					player.SendMessage(groupId, $"Invalid syntax for ADLCodes command. Check /help adlcodes", EnumChatType.CommandError);
					return;
			}

			player.SendMessage(groupId, response, EnumChatType.CommandSuccess);
		}

		private void Cmd_AdlConfig(IServerPlayer player, int groupId, CmdArgs args)
		{
			string response = "";

			string verb = args.PopWord() ?? "";

			switch (verb.ToUpperInvariant())
			{
				case "RADIUS":
					float? radius = args.PopFloat();
					if (!radius.HasValue) { }						
					else if (radius.Value > 0 && radius.Value < 512)
						Config.PreventSpawnRadius = radius.Value;
					else
					{
						player.SendMessage(groupId, $"Invalid radius. Must be >0 and <512.", EnumChatType.CommandError);
						return;
					}
					response = $"PreventSpawnRadius: {Config.PreventSpawnRadius:#.0}";
					break;

				case "FUEL":
					bool? usefuel = args.PopBool();
					if (!usefuel.HasValue) { }
					else
						Config.RequireFuel = usefuel.Value;
					response = $"RequireFuel: {Config.RequireFuel}";
					break;

				case "FUELDUR":
					double? fueldur = args.PopDouble();
					if (!fueldur.HasValue) { }
					else if (fueldur.Value > 0 && fueldur.Value < 9999)
						Config.FuelDurationHours = fueldur.Value;
					else
					{
						player.SendMessage(groupId, $"Invalid fuel duration. Must be >0 and <9999.", EnumChatType.CommandError);
						return;
					}
					response = $"FuelDurationHours: {Config.FuelDurationHours:#.0}";
					break;

				case "STORMS":
					bool? storms = args.PopBool();
					if (!storms.HasValue) { }
					else
						Config.EnableDuringStorms = storms.Value;
					response = $"EnableDuringStorms: {Config.EnableDuringStorms}";
					break;

				case "INVSLOTS":
					int? invslots = args.PopInt();
					if (!invslots.HasValue) { }
					else if (invslots.Value > 0 && invslots.Value < 17)
						Config.LanternInventorySlots = invslots.Value;
					else
					{
						player.SendMessage(groupId, $"Invalid invslots. Must be >0 and <17.", EnumChatType.CommandError);
						return;
					}
					response = $"LanternInventorySlots: {Config.LanternInventorySlots}. Changes require lanterns to be re-placed.";
					break;

				case "DEBUG":
					bool? deblog = args.PopBool();
					if (!deblog.HasValue) { }
					else
						Config.DebugLogging = deblog.Value;
					response = $"DebugLogging: {Config.DebugLogging}";
					break;

				case null:
				default:
					response = "Current ADL configuration:\n" +
						$"PreventSpawnRadius: {Config.PreventSpawnRadius:#0.0}\n" +
						$"RequireFuel: {Config.RequireFuel}\n" +
						$"FuelDurationHours: {Config.FuelDurationHours:#0.0}\n" +
						$"EnableDuringStorms: {Config.EnableDuringStorms}\n" +
						$"StormFuelEfficiency: {Config.StormFuelEfficiency:#0.0}\n" +
						$"LanternInventorySlots: {Config.LanternInventorySlots}\n" +
						$"DebugLogging: {Config.DebugLogging}";
					break;
			}

			CoreApi.StoreModConfig(Config, ConfigFilename);

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
					// ...
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

		public string LanternFuelString(LanternRecord lantern)
		{
			if (lantern.LanternEntity == null)
				return "???";

			return $"{(lantern.LanternEntity.FuelRemaining*-1):#.0}hr + {lantern.LanternEntity.SpareGears} gears";
		}

		#endregion

	}
}
