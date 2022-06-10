# MobilePatcher

MobilePatcher is a small C# class that uses Harmony to patch Discord.Net at runtime to display the mobile status.

I made this because I wanted a way to patch Discord.Net without having to decompile/modify the dlls or build from source.

## How it works

It's (somewhat) simple. MobilePatcher uses [Harmony](https://github.com/pardeike/Harmony) to add the `$os` and `$browser` properties in [DiscordSocketApiClient.SendIdentifyAsync()](https://github.com/discord-net/Discord.Net/blob/275b833205e29244106640af61e9df26d7973d39/src/Discord.Net.WebSocket/DiscordSocketApiClient.cs#L270).

In other words, it changes this:

```c#
var props = new Dictionary<string, string>
{
    ["$device"] = "Discord.Net"
    ["$os"] = Environment.OSVersion.Platform.ToString()
    ["$browser"] = "Discord.Net"
};
```

to this:

```c#
var props = new Dictionary<string, string>
{
    ["$device"] = "Discord.Net"
    ["$os"] = "android"
    ["$browser"] = "Discord Android"
};
```

This tells Discord that you're using the Android client.

## Usage

- Add the [Lib.Harmony](https://www.nuget.org/packages/Lib.Harmony) NuGet package into your project.

- Add the following class to your project:
  ```c#
  using System.Reflection;
  using Discord.WebSocket;
  using HarmonyLib;

  public static class MobilePatcher
  {
      public static void Patch()
      {
          var harmony = new Harmony(nameof(MobilePatcher));
  
          var original = AccessTools.Method("Discord.API.DiscordSocketApiClient:SendGatewayAsync");
          var prefix = typeof(MobilePatcher).GetMethod(nameof(Prefix));
  
          harmony.Patch(original, new HarmonyMethod(prefix));
      }
  
      private static readonly Type _identifyParams =
          typeof(BaseSocketClient).Assembly.GetType("Discord.API.Gateway.IdentifyParams", true)!;
  
      private static readonly PropertyInfo? _property = _identifyParams.GetProperty("Properties");
  
      public static void Prefix(in byte opCode, in object payload)
      {
          if (opCode != 2) // Identify
              return;
  
          if (payload.GetType() != _identifyParams)
              return;
  
          if (_property?.GetValue(payload) is not IDictionary<string, string> props
              || !props.TryGetValue("$device", out string? device)
              || device != "Discord.Net")
              return;
  
          props["$os"] = "android";
          props["$browser"] = "Discord Android";
      }
  }
  ```

- Call `MobilePatcher.Patch()` somewhere in your code before logging-in.

- Now your bot will have the mobile status :)

  ![Mobile status](https://cdn.discordapp.com/attachments/838832564583661638/874020734035427358/unknown.png)

## Notes

- The mobile status is only displayed when the bot's status is Online.