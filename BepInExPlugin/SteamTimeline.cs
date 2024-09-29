using Steamworks;

namespace SteamTimeline;

internal static class Timeline {
	internal static void SetTimelineStateDescription(string description, float timeDelta) {
		IPCManager.SendSteamTimelineCommand("SetTimelineStateDescription", description, timeDelta);
	}

	internal static void ClearTimelineStateDescription(float timeDelta) {
		IPCManager.SendSteamTimelineCommand("ClearTimelineStateDescription", timeDelta);
	}

	internal static void AddTimelineEvent(string icon, string title, string description, uint timelinePrority, float startOffset, float duration, TimelineEventClipPriority clipPriority) {
		IPCManager.SendSteamTimelineCommand("AddTimelineEvent", icon, title, description, timelinePrority, startOffset, duration, (uint)clipPriority);
	}

	internal static void SetTimelineGameMode(TimelineGameMode gameMode) {
		IPCManager.SendSteamTimelineCommand("SetTimelineGameMode", (uint)gameMode);
	}
}
