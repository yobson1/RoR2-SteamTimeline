using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using BepInEx;
using SteamTimelineShared;
using Newtonsoft.Json;
using UnityEngine;

namespace SteamTimelines;

internal class IPCManager {
	private Process _helperProcess;

	internal void StartHelperProcess() {
		string thisPluginPath = Path.Combine(Paths.PluginPath, MyPluginInfo.PLUGIN_GUID.Replace(".", "-"));
		string helperPath = Path.Combine(thisPluginPath, "Helper", "HelperIPC.exe");

		_helperProcess = new Process {
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
		_helperProcess.Start();
		_helperProcess.EnableRaisingEvents = true;
		_helperProcess.Exited += (s, e) => Application.Quit();
	}

	internal void StopHelperProcess() {
		if (_helperProcess != null && !_helperProcess.HasExited) {
			SendSteamTimelineCommand("Stop");
			_helperProcess.WaitForExit();
			_helperProcess.Dispose();
		}
	}

	internal void SendSteamTimelineCommand(string functionName, params object[] args) {
		var command = new SteamTimelineCommand {
			Function = functionName,
			Arguments = args
		};

		string jsonCommand = JsonConvert.SerializeObject(command);
		Plugin.Logger.LogInfo($"Sending command: {jsonCommand}");
		byte[] serializedCommand = System.Text.Encoding.UTF8.GetBytes(jsonCommand);

		using var pipeClient = new NamedPipeClientStream(".", TimelineConsts.PIPE_NAME, PipeDirection.Out);
		pipeClient.Connect();
		pipeClient.Write(serializedCommand, 0, serializedCommand.Length);
	}
}
