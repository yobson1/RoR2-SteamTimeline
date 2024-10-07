# RoR2-SteamTimeline
Hooks many different game events and updates the steam recording timeline

## Building
`dotnet publish SteamTimelines.sln`

Building in release configuration `-c Release` will hide the steamworks helper child process' console window, debug will show it.

Optionally you can provide a R2modman profile to install the mod to `-p:ProfileName=<name>`\
The mod will be automatically packaged if published in release mode.
#### Example
```
git clone https://github.com/yobson1/RoR2-SteamTimeline.git
cd RoR2-SteamTimeline
dotnet build SteamTimelines.sln -c Debug -p:ProfileName=Dev
```

## Acknowledgments
* **Facepunch.Steamworks** based on https://github.com/Squalive/Facepunch.Steamworks/
* **Icon art by Gib**: [twitter](https://twitter.com/gibpip) [reddit](https://www.reddit.com/user/Gibbyyuh/)
