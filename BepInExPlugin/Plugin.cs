using System.IO.Pipes;
using System.Diagnostics;
using BepInEx;
using BepInEx.Logging;
using Steamworks;
using SteamTimelineShared;
using Newtonsoft.Json;
using RoR2;

namespace SteamTimelines;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    private Process helperProcess;

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        // Start the helper process
        string thisPluginPath = System.IO.Path.Combine(Paths.PluginPath, MyPluginInfo.PLUGIN_GUID.Replace(".", "-"));
        string helperPath = System.IO.Path.Combine(thisPluginPath, "Helper", "HelperIPC.exe");
        StartHelperProcess(helperPath);

        SteamworksClientManager.onLoaded += () =>
        {
            Logger.LogInfo("RoR2 Steamworks loaded");
            SendSteamTimelineCommand("SetTimelineGameMode", (int)TimelineGameMode.LoadingScreen);
        };

        // Track run start
        On.RoR2.Run.Start += (orig, self) =>
        {
            orig(self);
            SendSteamTimelineCommand("AddTimelineEvent", "steam_timer", "Run Started", "You started a new run", 0, 0f, 0f, TimelineEventClipPriority.None);
            SendSteamTimelineCommand("SetTimelineStateDescription", $"Stage 1", 0f);
        };

        // Track run end
        On.RoR2.Run.OnClientGameOver += (orig, self, runReport) =>
        {
            orig(self, runReport);

            SendSteamTimelineCommand("ClearTimelineStateDescription", 0f);
            if (runReport.gameEnding.isWin)
            {
                switch (runReport.gameEnding._cachedName)
                {
                    case "MainEnding":
                        SendSteamTimelineCommand("AddTimelineEvent", "steam_crown", "Run Won", "You deafeated Mithrix!", 3, 0f, 12f, TimelineEventClipPriority.Featured);
                        break;
                    case "VoidEnding":
                        SendSteamTimelineCommand("AddTimelineEvent", "steam_crown", "Run Won", "You deafeated Voidling and survived the void!", 3, 0f, 12f, TimelineEventClipPriority.Featured);
                        break;
                    case "ObliterationEnding":
                        SendSteamTimelineCommand("AddTimelineEvent", "steam_crown", "Run Won", "You obliterated at the Obelisk!", 3, 0f, 0f, TimelineEventClipPriority.None);
                        break;
                    case "RebirthEndingDef":
                        SendSteamTimelineCommand("AddTimelineEvent", "steam_crown", "Run Won", "You deafeated the False Son and were reborn!", 3, 0f, 0f, TimelineEventClipPriority.None);
                        break;
                    default:
                        SendSteamTimelineCommand("AddTimelineEvent", "steam_crown", "Run Won", $"You won!", 3, 0f, 12f, TimelineEventClipPriority.Featured);
                        break;
                }
            }
            else
            {
                SendSteamTimelineCommand("AddTimelineEvent", "steam_death", "Run Lost", $"You lost!", 3, 0f, 12f, TimelineEventClipPriority.Standard);
            }
            Logger.LogInfo($"gameEnding: {runReport.gameEnding._cachedName}");
        };

        // Track stage advances
        On.RoR2.Run.AdvanceStage += (orig, self, stage) =>
        {
            orig(self, stage);

            if (stage.stageOrder > 1)
            {
                var currentStage = Run.instance.stageClearCount + 1;
                SendSteamTimelineCommand("AddTimelineEvent", "steam_flag", $"Stage {currentStage}", $"You reached stage {currentStage}", 0, 0f, 0f, TimelineEventClipPriority.None);
                SendSteamTimelineCommand("SetTimelineStateDescription", $"Stage {currentStage}", 0f);
            }
        };

        // Track boss kill
        On.RoR2.Run.OnServerBossDefeated += (orig, self, boss) =>
        {
            orig(self, boss);
            SendSteamTimelineCommand("AddTimelineEvent", "steam_attack", "Boss Defeated", $"You defeated {boss.bestObservedName}!", 1, 0f, 12f, TimelineEventClipPriority.Standard);
        };

        // Track player death
        On.RoR2.CharacterMaster.OnBodyDeath += (orig, self, masterbody) =>
        {
            orig(self, masterbody);
            if (masterbody.isLocalPlayer)
                SendSteamTimelineCommand("AddTimelineEvent", "steam_death", "Death", "You died", 2, 0f, 12f, TimelineEventClipPriority.Featured);
            else if (masterbody.master && masterbody.master.playerCharacterMasterController)
            {
                NetworkUser netUser = masterbody.master.playerCharacterMasterController.networkUser;
                if (netUser)
                    SendSteamTimelineCommand("AddTimelineEvent", "steam_death", "Death", $"{netUser.userName} died", 1, 0f, 12f, TimelineEventClipPriority.Standard);
            }
        };

        // Track teleporter activation
        On.RoR2.TeleporterInteraction.OnInteractionBegin += (orig, self, interactor) =>
        {
            orig(self, interactor);
            Logger.LogInfo($"Teleporter activated: {self.activationState}");
            if (self.activationState == TeleporterInteraction.ActivationState.Idle)
            {
                SendSteamTimelineCommand("AddTimelineEvent", "steam_effect", "Teleporter activated", "The teleporter was activated", 0, 0f, 0f, TimelineEventClipPriority.None);
            }
        };

        // Track scene changes for timeline game mode
        On.RoR2.SceneCatalog.OnActiveSceneChanged += (orig, self, scene) =>
        {
            orig(self, scene);

            Logger.LogInfo($"Scene changed to : {scene.name}");
            switch (scene.name)
            {
                case "title":
                    SendSteamTimelineCommand("SetTimelineGameMode", (int)TimelineGameMode.Menus);
                    SendSteamTimelineCommand("ClearTimelineStateDescription", 0f);
                    break;
                case "lobby":
                    SendSteamTimelineCommand("SetTimelineGameMode", (int)TimelineGameMode.Staging);
                    break;
                case "crystalworld":
                    SendSteamTimelineCommand("SetTimelineGameMode", (int)TimelineGameMode.Menus);
                    break;
                case "eclipseworld":
                    SendSteamTimelineCommand("SetTimelineGameMode", (int)TimelineGameMode.Menus);
                    break;
                case "infinitetowerworld":
                    SendSteamTimelineCommand("SetTimelineGameMode", (int)TimelineGameMode.Menus);
                    break;
                case "intro":
                    SendSteamTimelineCommand("SetTimelineGameMode", (int)TimelineGameMode.LoadingScreen);
                    break;
                case "loadingbasic":
                    SendSteamTimelineCommand("SetTimelineGameMode", (int)TimelineGameMode.LoadingScreen);
                    break;
                case "logbook":
                    SendSteamTimelineCommand("SetTimelineGameMode", (int)TimelineGameMode.Menus);
                    break;
                case "splash":
                    SendSteamTimelineCommand("SetTimelineGameMode", (int)TimelineGameMode.LoadingScreen);
                    break;
                default:
                    SendSteamTimelineCommand("SetTimelineGameMode", (int)TimelineGameMode.Playing);
                    break;
            }
        };
    }

    private void StartHelperProcess(string helperPath)
    {
        // Start the helper application process
        helperProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = helperPath,
                UseShellExecute = false,
                // Create window if debug mode is enabled
#if DEBUG
                CreateNoWindow = false
#else
                CreateNoWindow = true
#endif
            }
        };
        helperProcess.Start();
    }

    private void OnDestroy()
    {
        if (helperProcess != null && !helperProcess.HasExited)
        {
            SendSteamTimelineCommand("Stop");
            helperProcess.WaitForExit();
            helperProcess.Dispose();
        }
    }

    internal void SendSteamTimelineCommand(string functionName, params object[] args)
    {
        // Create the command structure
        SteamTimelineCommand command = new SteamTimelineCommand
        {
            Function = functionName,
            Arguments = args
        };

        string jsonCommand = JsonConvert.SerializeObject(command);
        Logger.LogInfo($"Sending command: {jsonCommand}");
        byte[] serializedCommand = System.Text.Encoding.UTF8.GetBytes(jsonCommand);

        // Send the serialized data over the named pipe
        using NamedPipeClientStream pipeClient = new("SteamworksPipe");
        pipeClient.Connect();
        pipeClient.Write(serializedCommand, 0, serializedCommand.Length);
    }
}
