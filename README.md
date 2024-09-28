# RoR2-SteamTimeline

## Building
`dotnet build`

Building in release configuration `dotnet build -c Release` will hide the steamworks helper child process' console window, debug will show it.

Optionally you can provide a R2modman profile to install the mod to `-p:ProfileName=<name>`
#### Example
```
dotnet build -c Debug -p:ProfileName=Dev
```

## Acknowledgments
* **Facepunch.Steamworks** based on https://github.com/Squalive/Facepunch.Steamworks/
