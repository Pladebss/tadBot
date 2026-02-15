using System;
using System.IO;
using System.Reflection;

namespace TadSyncLauncher
{
  public static class BotcoreBundler
  {
    public const string BotExeName = "botcore.exe";
    private const string ResourceName = "TadSyncLauncher.Assets.botcore.exe";

    public static string BotInstalledPath => Path.Combine(AppPaths.AppDataRoot, BotExeName);

    public static void EnsureExtracted()
    {
      Directory.CreateDirectory(AppPaths.AppDataRoot);

      // Already installed
      if (File.Exists(BotInstalledPath))
        return;

      using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
      if (stream == null)
        throw new Exception($"Embedded botcore resource missing: {ResourceName}");

      // Write atomically (avoid half-written files)
      var tmp = BotInstalledPath + ".tmp";
      using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
        stream.CopyTo(fs);

      if (File.Exists(BotInstalledPath)) File.Delete(BotInstalledPath);
      File.Move(tmp, BotInstalledPath);
    }

    public static bool IsEmbeddedPresent()
    {
      return Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName) != null;
    }
  }
}
