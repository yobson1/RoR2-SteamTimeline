using System;
using System.IO;
using System.IO.Pipes;
using Newtonsoft.Json;
using Steamworks;
using SteamTimelineShared;

namespace HelperIPC;

public class SteamHelper
{
    private const uint APP_ID = 632360;

    static void Main(string[] args)
    {
        SteamClient.Init(APP_ID);

        while (true)
        {
            try
            {
                using NamedPipeServerStream pipeServer = new("SteamworksPipe");
                Console.WriteLine("Waiting for connection...");
                pipeServer.WaitForConnection();

                Console.WriteLine("Connected. Waiting for commands...");

                using MemoryStream ms = new MemoryStream();

                // Read the incoming data into a byte array
                pipeServer.CopyTo(ms);
                byte[] receivedData = ms.ToArray();

                // Check if the received data is valid
                if (receivedData.Length == 0)
                    continue;

                Console.WriteLine("Processing command");

                // Deserialize the data using MessagePack
                string jsonReceived = System.Text.Encoding.UTF8.GetString(receivedData);
                SteamTimelineCommand command = JsonConvert.DeserializeObject<SteamTimelineCommand>(jsonReceived);

                if (command.Function == "Stop")
                    break;

                // Call the appropriate function based on the command
                ExecuteCommand(command);

                ms.SetLength(0); // Clear the MemoryStream for reuse
            }
            catch (IOException)
            {
                // Handle the error: the client disconnected
                Console.WriteLine("Client disconnected. Waiting for a new connection...");
            }
            catch (Exception ex)
            {
                // Handle other potential exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        SteamClient.Shutdown();
    }

    static void ExecuteCommand(SteamTimelineCommand command)
    {
        Console.WriteLine($"Received command: {command.Function} with arguments: {string.Join(", ", command.Arguments)}");

        switch (command.Function)
        {
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
        }
    }
}
