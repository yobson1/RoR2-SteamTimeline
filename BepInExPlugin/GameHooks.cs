using Steamworks;
using RoR2;
using UnityEngine.SceneManagement;

namespace SteamTimeline;

internal class GameHooks {
	private readonly IPCManager _ipcManager;

	internal GameHooks(IPCManager ipcManager) {
		_ipcManager = ipcManager;
	}

	internal void SetupHooks() {
		SteamworksClientManager.onLoaded += OnSteamworksLoaded;
		On.RoR2.Run.Start += OnRunStart;
		On.RoR2.Run.OnClientGameOver += OnRunEnd;
		On.RoR2.Run.AdvanceStage += OnStageAdvance;
		On.RoR2.InfiniteTowerRun.AdvanceWave += OnWaveAdvance;
		On.RoR2.Run.OnServerBossDefeated += OnBossDefeated;
		On.RoR2.CharacterMaster.OnBodyDeath += OnBodyDeath;
		On.RoR2.TeleporterInteraction.OnInteractionBegin += OnTeleporterActivated;
		On.RoR2.SceneCatalog.OnActiveSceneChanged += OnSceneChanged;
	}

	private void OnSteamworksLoaded() {
		Plugin.Logger.LogInfo("RoR2 Steamworks loaded");
		_ipcManager.SendSteamTimelineCommand("SetTimelineGameMode", (int)TimelineGameMode.LoadingScreen);
	}

	private void OnRunStart(On.RoR2.Run.orig_Start orig, Run self) {
		orig(self);

		var gameModeName = GetGamemodeName(self.gameModeIndex);

		_ipcManager.SendSteamTimelineCommand("SetTimelineGameMode", (int)TimelineGameMode.Playing);
		_ipcManager.SendSteamTimelineCommand("AddTimelineEvent", "steam_timer", "Run Started", $"You started a new {gameModeName} run", 0, 0f, 0f, TimelineEventClipPriority.None);

		if ((int)self.gameModeIndex == 4) // Simulacrum
		{
			_ipcManager.SendSteamTimelineCommand("SetTimelineStateDescription", $"{gameModeName} - Wave 0", 0f);
		} else {
			_ipcManager.SendSteamTimelineCommand("SetTimelineStateDescription", $"{gameModeName} - Stage 1", 0f);
		}

		Plugin.Logger.LogInfo($"Starting new run: {self.gameModeIndex} - {gameModeName}");
	}

	private void OnRunEnd(On.RoR2.Run.orig_OnClientGameOver orig, Run self, RunReport runReport) {
		orig(self, runReport);

		_ipcManager.SendSteamTimelineCommand("ClearTimelineStateDescription", 0f);
		if (runReport.gameEnding.isWin) {
			HandleWinCondition(runReport.gameEnding._cachedName);
		} else {
			_ipcManager.SendSteamTimelineCommand("AddTimelineEvent", "steam_x", "Run Lost", "You lost!", 1, 1f, 0f, TimelineEventClipPriority.Standard);
		}
		Plugin.Logger.LogInfo($"gameEnding: {runReport.gameEnding._cachedName}");
	}

	private void HandleWinCondition(string endingName) {
		switch (endingName) {
			case "MainEnding":
				_ipcManager.SendSteamTimelineCommand("AddTimelineEvent", "steam_crown", "Run Won", "You defeated Mithrix!", 3, 0f, 0f, TimelineEventClipPriority.Featured);
				break;
			case "VoidEnding":
				_ipcManager.SendSteamTimelineCommand("AddTimelineEvent", "steam_crown", "Run Won", "You defeated Voidling and survived the void!", 3, 0f, 0f, TimelineEventClipPriority.Featured);
				break;
			case "ObliterationEnding":
				_ipcManager.SendSteamTimelineCommand("AddTimelineEvent", "steam_crown", "Run Won", "You obliterated at the Obelisk!", 3, 0f, 0f, TimelineEventClipPriority.None);
				break;
			case "RebirthEndingDef":
				_ipcManager.SendSteamTimelineCommand("AddTimelineEvent", "steam_crown", "Run Won", "You defeated the False Son and were reborn!", 3, 0f, 0f, TimelineEventClipPriority.Featured);
				break;
			default:
				_ipcManager.SendSteamTimelineCommand("AddTimelineEvent", "steam_crown", "Run Won", $"You won!", 3, 0f, 0f, TimelineEventClipPriority.Featured);
				break;
		}
	}

	private void OnStageAdvance(On.RoR2.Run.orig_AdvanceStage orig, Run self, SceneDef nextScene) {
		orig(self, nextScene);

		var gameModeName = GetGamemodeName(self.gameModeIndex);
		var currentStage = Run.instance.stageClearCount + 1;
		_ipcManager.SendSteamTimelineCommand("AddTimelineEvent", "steam_flag", $"Stage {currentStage}", $"You reached stage {currentStage}", 0, 0f, 0f, TimelineEventClipPriority.None);
		_ipcManager.SendSteamTimelineCommand("SetTimelineStateDescription", $"{gameModeName} - Stage {currentStage}", 0f);
	}

	private void OnWaveAdvance(On.RoR2.InfiniteTowerRun.orig_AdvanceWave orig, InfiniteTowerRun self) {
		orig(self);
		var wave = self.waveIndex;
		_ipcManager.SendSteamTimelineCommand("AddTimelineEvent", "steam_combat", $"Wave {wave}", $"You reached wave {wave}", 0, 0f, 0f, TimelineEventClipPriority.None);
		_ipcManager.SendSteamTimelineCommand("SetTimelineStateDescription", $"Simulacrum - Wave {wave}", 0f);
	}

	private void OnBossDefeated(On.RoR2.Run.orig_OnServerBossDefeated orig, Run self, BossGroup boss) {
		orig(self, boss);
		_ipcManager.SendSteamTimelineCommand("AddTimelineEvent", "steam_attack", "Boss Defeated", $"You defeated {boss.bestObservedName}!", 1, 0f, 0f, TimelineEventClipPriority.Standard);
	}

	private void OnBodyDeath(On.RoR2.CharacterMaster.orig_OnBodyDeath orig, CharacterMaster self, CharacterBody characterBody) {
		orig(self, characterBody);
		if (!characterBody.master || !characterBody.master.playerCharacterMasterController) return;
		NetworkUser netUser = characterBody.master.playerCharacterMasterController.networkUser;
		if (!netUser) return;

		string deathMessage = netUser.isLocalPlayer ? "You died" : $"{netUser.userName} died";
		TimelineEventClipPriority clipPriority = netUser.isLocalPlayer ? TimelineEventClipPriority.Featured : TimelineEventClipPriority.Standard;
		uint timelinePriority = netUser.isLocalPlayer ? 2u : 1u;
		_ipcManager.SendSteamTimelineCommand("AddTimelineEvent", "steam_death", "Death", deathMessage, timelinePriority, 0f, 0f, clipPriority);
	}

	private void OnTeleporterActivated(On.RoR2.TeleporterInteraction.orig_OnInteractionBegin orig, TeleporterInteraction self, Interactor interactor) {
		orig(self, interactor);
		Plugin.Logger.LogInfo($"Teleporter activated: {self.activationState}");
		if (self.activationState == TeleporterInteraction.ActivationState.Idle) {
			_ipcManager.SendSteamTimelineCommand("AddTimelineEvent", "steam_effect", "Teleporter activated", "The teleporter was activated", 0, 0f, 0f, TimelineEventClipPriority.None);
		}
	}

	private void OnSceneChanged(On.RoR2.SceneCatalog.orig_OnActiveSceneChanged orig, Scene self, Scene newScene) {
		orig(self, newScene);
		Plugin.Logger.LogInfo($"Scene changed to : {newScene.name}");
		SetTimelineGamemodeByScene(newScene.name);
	}

	internal void SetTimelineGamemodeByScene(string sceneName) {
		TimelineGameMode gameMode = sceneName switch {
			"title" or "crystalworld" or "eclipseworld" or "infinitetowerworld" or "logbook" => TimelineGameMode.Menus,
			"lobby" => TimelineGameMode.Staging,
			"intro" or "loadingbasic" or "splash" => TimelineGameMode.LoadingScreen,
			_ => TimelineGameMode.Playing
		};

		_ipcManager.SendSteamTimelineCommand("SetTimelineGameMode", (int)gameMode);

		if (gameMode == TimelineGameMode.Menus) {
			_ipcManager.SendSteamTimelineCommand("ClearTimelineStateDescription", 0f);
		}
	}

	private string GetGamemodeName(GameModeIndex gameModeIndex) {
		return Language.GetString(GameModeCatalog.GetGameModePrefabComponent(gameModeIndex).nameToken);
	}
}
