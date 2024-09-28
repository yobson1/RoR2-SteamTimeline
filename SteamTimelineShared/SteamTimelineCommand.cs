using Newtonsoft.Json;

namespace SteamTimelineShared;

public struct SteamTimelineCommand
{
    public string Function { get; set; }

    public object[] Arguments { get; set; }
}
