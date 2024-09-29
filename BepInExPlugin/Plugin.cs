using BepInEx;
using BepInEx.Logging;
using Steamworks;
using RoR2;
using UnityEngine.SceneManagement;

namespace SteamTimeline;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
	internal static new ManualLogSource Logger;
	private bool _wasPaused = false;

	private void Awake() {
		Logger = base.Logger;
		Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

		IPCManager.StartHelperProcess();
		GameHooks.SetupHooks();
	}

	private void Update() {
		if (PauseManager.isPaused && !_wasPaused) {
			_wasPaused = true;
			Timeline.SetTimelineGameMode(TimelineGameMode.Menus);
		} else if (!PauseManager.isPaused && _wasPaused) {
			_wasPaused = false;
			GameHooks.SetTimelineGamemodeByScene(SceneManager.GetActiveScene().name);
		}
	}

	private void OnDestroy() {
		IPCManager.StopHelperProcess();
	}
}
