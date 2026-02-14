using System;
using System.IO;

namespace TadSyncLauncher
{
  public static class AppPaths
  {
    public const string Brand = "TadSync";

    public static string AppDataRoot =>
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Brand);

    public static string LocalAppDataRoot =>
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Brand);

    public static string ConfigPath => Path.Combine(AppDataRoot, "config.json");
    public static string SettingsPath => Path.Combine(AppDataRoot, "settings.json");

    public static string DataDir => Path.Combine(AppDataRoot, "data");
    public static string LogsDir => Path.Combine(AppDataRoot, "logs");

    public static string StatusPath => Path.Combine(DataDir, "status.json");

    public static string BotInstallDir => Path.Combine(LocalAppDataRoot, "app");
    public static string BotExeInstalledPath => Path.Combine(BotInstallDir, "botcore.exe");

    public static void EnsureDirs()
    {
      Directory.CreateDirectory(AppDataRoot);
      Directory.CreateDirectory(DataDir);
      Directory.CreateDirectory(LogsDir);

      Directory.CreateDirectory(LocalAppDataRoot);
      Directory.CreateDirectory(BotInstallDir);
    }

    // payload path: alongside launcher in executable\botcore\botcore.exe
    public static string BotPayloadPathNearLauncher()
    {
      var launcherDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
      return Path.Combine(launcherDir, "botcore", "botcore.exe");
    }
  }
}
