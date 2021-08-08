# MobilePatcher

MobilePatcher is a program that patches Discord.Net-compatible dlls to display the mobile status.

I made this program because I wanted a way to patch Discord.Net without having to decompile the dlls or build from source.

## How it works

It's (somewhat) simple. MobilePatcher uses [dnlib](https://github.com/0xd4d/dnlib) to add the `$os` and `$browser` properties in [DiscordSocketApiClient.SendIdentifyAsync()](https://github.com/discord-net/Discord.Net/blob/c20086158572acf5fbc3c795769d12b39b127482/src/Discord.Net.WebSocket/DiscordSocketApiClient.cs#L218).

In other words, it changes this:

```c#
var props = new Dictionary<string, string>
{
    ["$device"] = "Discord.Net"
};
```

to this:

```c#
var props = new Dictionary<string, string>
{
    ["$device"] = "Discord.Net"
    ["$os"] = "android" // This property is not required but I added it just in case
    ["$browser"] = "Discord Android"
};
```

This tells Discord that you're using the Android client.

## Usage

- Build the program.

- Move the built file(s) to the same folder `Discord.Net.WebSocket.dll` resides (this is normally your bot's build output folder).

- Execute MobilePatcher.

- Now your bot will have the mobile status :)

  ![Mobile status](https://cdn.discordapp.com/attachments/838832564583661638/874020734035427358/unknown.png)

## Notes

- You'll have to run MobilePatcher every time `Discord.Net.WebSocket.dll` is modified (e.g., recompilations).

  - You can minimize this hassle by using a script that runs MobilePatcher and then runs your bot.

- The mobile status is only displayed when the bot's status is Online.