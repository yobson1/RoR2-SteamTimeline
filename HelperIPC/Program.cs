using System;
using System.IO;
using System.IO.Pipes;
using Newtonsoft.Json;
using Steamworks;
using SteamTimelineShared;
using System.Diagnostics;
using System.Linq;

namespace HelperIPC;

public class SteamHelper {
	private const uint APP_ID = 632360;
	private const string PIPE_NAME = "SteamworksPipe";
	private const string PROCESS_NAME = "Risk of Rain 2";

	static void Main(string[] args) {
		try {
			SteamClient.Init(APP_ID);
			while (MainLoop()) { }
		} finally {
			// Ensure SteamClient is always shut down properly
			SteamClient.Shutdown();
		}
	}

	static bool MainLoop() {
		// If RoR2 ends, we end
		if (!IsGameRunning()) {
			return false;
		}

		try {
			using var pipeServer = new NamedPipeServerStream(PIPE_NAME);
			Console.WriteLine("Waiting for connection...");
			pipeServer.WaitForConnection();

			var command = ReadCommand(pipeServer);

			if (command.Function == "Stop") {
				return false;
			}

			ExecuteCommand(command);
		} catch (IOException) {
			Console.WriteLine("Client disconnected!");
		} catch (JsonException jsonEx) {
			Console.WriteLine($"Error parsing command: {jsonEx.Message}");
		} catch (Exception ex) {
			Console.WriteLine($"An error occurred: {ex.Message}");
		}

		return true;
	}

	// Check if the game process is running
	static bool IsGameRunning() => Process.GetProcessesByName(PROCESS_NAME).Any();

	static SteamTimelineCommand ReadCommand(NamedPipeServerStream pipeServer) {
		using var ms = new MemoryStream();
		pipeServer.CopyTo(ms);
		byte[] receivedData = ms.ToArray();

		// Check if the received data is valid
		if (receivedData.Length == 0) {
			throw new InvalidOperationException("Received empty data from pipe.");
		}

		Console.WriteLine("Processing command");
		string jsonReceived = System.Text.Encoding.UTF8.GetString(receivedData);

		// Deserialize the command, throwing an exception if deserialization fails
		return JsonConvert.DeserializeObject<SteamTimelineCommand>(jsonReceived);
	}

	static void ExecuteCommand(SteamTimelineCommand command) {
		Console.WriteLine($"Received command: {command.Function} with arguments: {string.Join(", ", command.Arguments)}");

		switch (command.Function) {
			case "SetTimelineStateDescription":
				SteamTimeline.SetTimelineStateDescription(
					(string)command.Arguments[0],
					(float)(double)command.Arguments[1]
				);
				break;
			case "ClearTimelineStateDescription":
				SteamTimeline.ClearTimelineStateDescription((float)(double)command.Arguments[0]);
				break;
			case "AddTimelineEvent":
				SteamTimeline.AddTimelineEvent(
					(string)command.Arguments[0],
					(string)command.Arguments[1],
					(string)command.Arguments[2],
					(uint)(long)command.Arguments[3],
					(float)(double)command.Arguments[4],
					(float)(double)command.Arguments[5],
					(TimelineEventClipPriority)(int)(long)command.Arguments[6]
				);
				break;
			case "SetTimelineGameMode":
				SteamTimeline.SetTimelineGameMode((TimelineGameMode)(int)(long)command.Arguments[0]);
				break;
			default:
				Console.WriteLine($"Unknown command: {command.Function}");
				break;
		}
	}
}
