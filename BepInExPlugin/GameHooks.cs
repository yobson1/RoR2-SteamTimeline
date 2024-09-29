using Steamworks;
using RoR2;
using UnityEngine.SceneManagement;

namespace SteamTimeline;

internal static class GameHooks {
	internal static void SetupHooks() {
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

	private static void OnSteamworksLoaded() {
		Plugin.Logger.LogInfo("RoR2 Steamworks loaded");
		Timeline.SetTimelineGameMode(TimelineGameMode.LoadingScreen);
	}

	private static void OnRunStart(On.RoR2.Run.orig_Start orig, Run self) {
		orig(self);

		var gameModeName = GetGamemodeName(self.gameModeIndex);

		Timeline.SetTimelineGameMode(TimelineGameMode.Playing);
		Timeline.AddTimelineEvent("steam_timer", "Run Started", $"You started a new {gameModeName} run", 0, 0f, 0f, TimelineEventClipPriority.None);


		if ((int)self.gameModeIndex == 4) // Simulacrum
		{
			Timeline.SetTimelineStateDescription($"{gameModeName} - Wave 0", 0f);
		} else {
			Timeline.SetTimelineStateDescription($"{gameModeName} - Stage 1", 0f);
		}

		Plugin.Logger.LogInfo($"Starting new run: {self.gameModeIndex} - {gameModeName}");
	}

	private static void OnRunEnd(On.RoR2.Run.orig_OnClientGameOver orig, Run self, RunReport runReport) {
		orig(self, runReport);

		Timeline.ClearTimelineStateDescription(0f);
		if (runReport.gameEnding.isWin) {
			HandleWinCondition(runReport.gameEnding._cachedName);
		} else {
			Timeline.AddTimelineEvent("steam_x", "Run Lost", "You lost!", 1, 1f, 0f, TimelineEventClipPriority.Standard);
		}
		Plugin.Logger.LogInfo($"gameEnding: {runReport.gameEnding._cachedName}");
	}

	private static void HandleWinCondition(string endingName) {
		switch (endingName) {
			case "MainEnding":
				Timeline.AddTimelineEvent("steam_crown", "Run Won", "You defeated Mithrix!", 3, 0f, 0f, TimelineEventClipPriority.Featured);
				break;
			case "VoidEnding":
				Timeline.AddTimelineEvent("steam_crown", "Run Won", "You defeated Voidling and survived the void!", 3, 0f, 0f, TimelineEventClipPriority.Featured);
				break;
			case "ObliterationEnding":
				Timeline.AddTimelineEvent("steam_crown", "Run Won", "You obliterated at the Obelisk!", 3, 0f, 0f, TimelineEventClipPriority.None);
				break;
			case "RebirthEndingDef":
				Timeline.AddTimelineEvent("steam_crown", "Run Won", "You defeated the False Son and were reborn!", 3, 0f, 0f, TimelineEventClipPriority.Featured);
				break;
			default:
				Timeline.AddTimelineEvent("steam_crown", "Run Won", $"You won!", 3, 0f, 0f, TimelineEventClipPriority.Featured);
				break;
		}
	}

	private static void OnStageAdvance(On.RoR2.Run.orig_AdvanceStage orig, Run self, SceneDef nextScene) {
		orig(self, nextScene);

		var gameModeName = GetGamemodeName(self.gameModeIndex);
		var currentStage = Run.instance.stageClearCount + 1;
		Timeline.AddTimelineEvent("steam_flag", $"Stage {currentStage}", $"You reached stage {currentStage}", 0, 0f, 0f, TimelineEventClipPriority.None);
		Timeline.SetTimelineStateDescription($"{gameModeName} - Stage {currentStage}", 0f);
	}

	private static void OnWaveAdvance(On.RoR2.InfiniteTowerRun.orig_AdvanceWave orig, InfiniteTowerRun self) {
		orig(self);
		var wave = self.waveIndex;
		Timeline.AddTimelineEvent("steam_combat", $"Wave {wave}", $"You reached wave {wave}", 0, 0f, 0f, TimelineEventClipPriority.None);
		Timeline.SetTimelineStateDescription($"Simulacrum - Wave {wave}", 0f);
	}

	private static void OnBossDefeated(On.RoR2.Run.orig_OnServerBossDefeated orig, Run self, BossGroup boss) {
		orig(self, boss);
		Timeline.AddTimelineEvent("steam_attack", "Boss Defeated", $"You defeated {boss.bestObservedName}!", 1, 0f, 0f, TimelineEventClipPriority.Standard);
	}

	private static void OnBodyDeath(On.RoR2.CharacterMaster.orig_OnBodyDeath orig, CharacterMaster self, CharacterBody characterBody) {
		orig(self, characterBody);
		if (!characterBody.master || !characterBody.master.playerCharacterMasterController) return;
		NetworkUser netUser = characterBody.master.playerCharacterMasterController.networkUser;
		if (!netUser) return;

		string deathMessage = netUser.isLocalPlayer ? "You died" : $"{netUser.userName} died";
		TimelineEventClipPriority clipPriority = netUser.isLocalPlayer ? TimelineEventClipPriority.Featured : TimelineEventClipPriority.Standard;
		uint timelinePriority = netUser.isLocalPlayer ? 2u : 1u;
		Timeline.AddTimelineEvent("steam_death", "Death", deathMessage, timelinePriority, 0f, 0f, clipPriority);
	}

	private static void OnTeleporterActivated(On.RoR2.TeleporterInteraction.orig_OnInteractionBegin orig, TeleporterInteraction self, Interactor interactor) {
		orig(self, interactor);
		Plugin.Logger.LogInfo($"Teleporter activated: {self.activationState}");
		if (self.activationState == TeleporterInteraction.ActivationState.Idle) {
			Timeline.AddTimelineEvent("steam_effect", "Teleporter activated", "The teleporter was activated", 0, 0f, 0f, TimelineEventClipPriority.None);
		}
	}

	private static void OnSceneChanged(On.RoR2.SceneCatalog.orig_OnActiveSceneChanged orig, Scene self, Scene newScene) {
		orig(self, newScene);
		Plugin.Logger.LogInfo($"Scene changed to : {newScene.name}");
		SetTimelineGamemodeByScene(newScene.name);
	}

	internal static void SetTimelineGamemodeByScene(string sceneName) {
		TimelineGameMode gameMode = sceneName switch {
			"title" or "crystalworld" or "eclipseworld" or "infinitetowerworld" or "logbook" => TimelineGameMode.Menus,
			"lobby" => TimelineGameMode.Staging,
			"intro" or "loadingbasic" or "splash" => TimelineGameMode.LoadingScreen,
			_ => TimelineGameMode.Playing
		};

		Timeline.SetTimelineGameMode(gameMode);

		if (gameMode == TimelineGameMode.Menus) {
			Timeline.ClearTimelineStateDescription(0f);
		}
	}

	private static string GetGamemodeName(GameModeIndex gameModeIndex) {
		return Language.GetString(GameModeCatalog.GetGameModePrefabComponent(gameModeIndex).nameToken);
	}
}
