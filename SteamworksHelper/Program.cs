using System;
using System.IO;
using System.IO.Pipes;
using Newtonsoft.Json;
using Steamworks;
using SteamTimelineShared;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SteamworksHelper;

public class SteamHelper {
	private const uint APP_ID = 632360;
	private const string PROCESS_NAME = "Risk of Rain 2";

	static bool IsGameRunning() => Process.GetProcessesByName(PROCESS_NAME).Any();

	static async Task Main(string[] args) {
		try {
			SteamClient.Init(APP_ID);
			while (await MainLoop()) { }
		} finally {
			// Ensure SteamClient is always shut down properly
			SteamClient.Shutdown();
		}
	}

	/// <returns><c>true</c> if the loop should continue; <c>false</c> otherwise</returns>
	static async Task<bool> MainLoop() {
		// If RoR2 ends, we end
		if (!IsGameRunning()) {
			return false;
		}

		try {
			using var pipeServer = new NamedPipeServerStream(TimelineConsts.PIPE_NAME, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

			Console.WriteLine("Waiting for connection...");
			var connectionTask = pipeServer.WaitForConnectionAsync();
			var waitTask = Task.Delay(1000);

			var completedTask = await Task.WhenAny(connectionTask, waitTask);

			if (completedTask == connectionTask) {
				var command = ReadCommand(pipeServer);
				if (command.Function == "Stop") {
					return false;
				}
				ExecuteCommand(command);
			}
			// If we get here we timed out and can loop again
			// to check if RoR2 is still running
		} catch (IOException) {
			Console.WriteLine("Client disconnected!");
		} catch (JsonException jsonEx) {
			Console.WriteLine($"Error parsing command: {jsonEx.Message}");
		} catch (Exception ex) {
			Console.WriteLine($"An error occurred: {ex.Message}");
		}

		return true;
	}

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

		// https://partner.steamgames.com/doc/api/ISteamTimeline
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
					(TimelineEventClipPriority)(long)command.Arguments[6]
				);
				break;
			case "SetTimelineGameMode":
				SteamTimeline.SetTimelineGameMode((TimelineGameMode)(long)command.Arguments[0]);
				break;
			default:
				Console.WriteLine($"Unknown command: {command.Function}");
				break;
		}
	}
}
