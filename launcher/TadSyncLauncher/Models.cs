using System;
using System.Collections.Generic;

namespace TadSyncLauncher
{
  public sealed class LauncherSettings
  {
    public bool AutoStartBotOnOpen { get; set; } = false;
  }

  public sealed class SlashRegistration
  {
    public bool GuildOnly { get; set; } = true;
    public List<string> GuildIds { get; set; } = new();
  }

  public sealed class PresenceConfig
  {
    public int TtlMinutes { get; set; } = 15;
    public string IdleStatus { get; set; } = "dnd";
    public string IdleActivity { get; set; } = "Standing By...";
    public string GatheringStatus { get; set; } = "online";
    public string GatheringActivityTemplate { get; set; } = "Gathering: {field}";
  }

  public sealed class BotConfig
  {
    public string DiscordToken { get; set; } = "";
    public string MonitorChannelId { get; set; } = "";
    public List<string> BoostDestChannelIds { get; set; } = new();
    public Dictionary<string, string> FieldMapping { get; set; } = new();
    public List<string> SuperUsers { get; set; } = new();

    public PresenceConfig Presence { get; set; } = new();
    public SlashRegistration SlashRegistration { get; set; } = new();
  }

  public sealed class BotStatus
  {
    public int? Pid { get; set; }
    public long? StartedAt { get; set; }          // epoch ms
    public int? UptimeSec { get; set; }
    public string? DiscordStatus { get; set; }
    public string? PresenceText { get; set; }
    public long? LastBoostAt { get; set; }        // epoch ms
    public string? LastFieldName { get; set; }
  }

  public static class TimeFmt
  {
    public static string FmtSpan(TimeSpan ts)
    {
      if (ts.TotalSeconds < 0) ts = TimeSpan.Zero;
      return $"{(int)ts.TotalDays:00}d {ts.Hours:00}h {ts.Minutes:00}m {ts.Seconds:00}s";
    }

    public static string MinutesAgoFromEpochMs(long? epochMs)
    {
      if (epochMs == null) return "N/A";
      var dt = DateTimeOffset.FromUnixTimeMilliseconds(epochMs.Value);
      var mins = (DateTimeOffset.UtcNow - dt).TotalMinutes;
      if (mins < 0) mins = 0;
      return $"{(int)Math.Floor(mins)} min";
    }
  }
}
