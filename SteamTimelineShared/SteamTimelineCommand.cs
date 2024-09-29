namespace SteamTimelineShared;

public static class TimelineConsts {
	public const string PIPE_NAME = "SteamworksPipe";
}

public struct SteamTimelineCommand {
	public string Function { get; set; }

	public object[] Arguments { get; set; }
}
