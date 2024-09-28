using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using BepInEx;
using BepInEx.Logging;
using Steamworks;
using SteamTimelineShared;
using Newtonsoft.Json;

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
        string thisPluginPath = Path.Combine(Paths.PluginPath, MyPluginInfo.PLUGIN_GUID.Replace(".", "-"));
        string helperPath = Path.Combine(thisPluginPath, "Helper", "HelperIPC.exe");
        StartHelperProcess(helperPath);

        RoR2.SteamworksClientManager.onLoaded += () =>
        {
            Logger.LogInfo("RoR2 Steamworks loaded");
            SendSteamTimelineCommand("SetTimelineGameMode", (int)TimelineGameMode.LoadingScreen);
        };

        On.RoR2.SceneCatalog.OnActiveSceneChanged += (orig, self, scene) =>
        {
            orig(self, scene);

            Logger.LogInfo($"Scene changed to : {scene.name}");
            if (scene.name == "title")
                SendSteamTimelineCommand("SetTimelineGameMode", (int)TimelineGameMode.Menus);

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

    private void Update()
    {
        helperProcess?.Refresh();
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
        using NamedPipeClientStream pipeClient = new ("SteamworksPipe");
        pipeClient.Connect();
        pipeClient.Write(serializedCommand, 0, serializedCommand.Length);
    }
}
