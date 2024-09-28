using BepInEx;
using BepInEx.Logging;
using Steamworks;
using RoR2;
using UnityEngine.SceneManagement;

namespace SteamTimelines;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
	internal static new ManualLogSource Logger;
	private GameHooks _gameHooks;
	private IPCManager _ipcManager;
	private bool _wasPaused = false;

	private void Awake() {
		Logger = base.Logger;
		Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

		_ipcManager = new IPCManager();
		_gameHooks = new GameHooks(_ipcManager);

		_ipcManager.StartHelperProcess();
		_gameHooks.SetupHooks();
	}

	private void Update() {
		if (PauseManager.isPaused && !_wasPaused) {
			_wasPaused = true;
			_ipcManager.SendSteamTimelineCommand("SetTimelineGameMode", (int)TimelineGameMode.Menus);
		} else if (!PauseManager.isPaused && _wasPaused) {
			_wasPaused = false;
			_gameHooks.SetTimelineGamemodeByScene(SceneManager.GetActiveScene().name);
		}
	}

	private void OnDestroy() {
		_ipcManager.StopHelperProcess();
	}
}
