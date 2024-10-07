using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using BepInEx;
using SteamTimelineShared;
using Newtonsoft.Json;
using UnityEngine;

namespace SteamTimeline;

internal static class IPCManager {
	private static Process? s_helperProcess;

	internal static void StartHelperProcess() {
		string thisPluginPath = Path.Combine(Paths.PluginPath, MyPluginInfo.PLUGIN_GUID.Replace(".", "-"));
		string helperPath = Path.Combine(thisPluginPath, "Helper", "SteamworksHelper.exe");

		if (!File.Exists(helperPath)) {
			Plugin.Logger!.LogFatal($"Helper file not found: {helperPath}");
			return;
		}

		s_helperProcess = new Process {
			StartInfo = new ProcessStartInfo {
				FileName = helperPath,
				UseShellExecute = false,
				// Create window if building in debug configuration
#if DEBUG
				CreateNoWindow = false
#else
				CreateNoWindow = true
#endif
			}
		};
		s_helperProcess.Start();
		s_helperProcess.EnableRaisingEvents = true;
		s_helperProcess.Exited += (s, e) => {
			Plugin.Logger!.LogFatal($"Helper process exited unexpectedly! Exit code: {s_helperProcess.ExitCode}");
			Application.Quit();
		};

		Plugin.Logger!.LogInfo("SteamworksHelper started");
	}

	internal static void StopHelperProcess() {
		Plugin.Logger!.LogInfo("Stopping SteamworksHelper");
		if (s_helperProcess != null && !s_helperProcess.HasExited) {
			SendSteamTimelineCommand("Stop");
			s_helperProcess.WaitForExit();
			s_helperProcess.Dispose();
		} else {
			Plugin.Logger!.LogWarning("Failed to stop helper: process already exited");
		}
	}

	internal static void SendSteamTimelineCommand(string functionName, params object[] args) {
		var command = new SteamTimelineCommand {
			Function = functionName,
			Arguments = args
		};

		string jsonCommand = JsonConvert.SerializeObject(command);
		Plugin.Logger!.LogDebug($"Sending command: {jsonCommand}");
		byte[] serializedCommand = System.Text.Encoding.UTF8.GetBytes(jsonCommand);

		using var pipeClient = new NamedPipeClientStream(".", TimelineConsts.PIPE_NAME, PipeDirection.Out);
		pipeClient.Connect();
		pipeClient.Write(serializedCommand, 0, serializedCommand.Length);
	}
}
