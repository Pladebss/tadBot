using System;
using System.Diagnostics;
using System.IO;

namespace TadSyncLauncher
{
  public sealed class BotProcessManager
  {
    private Process? _proc;

    public bool IsRunning => _proc != null && !_proc.HasExited;

    public int? Pid => IsRunning ? _proc!.Id : null;

    public void EnsureBotInstalled()
    {
      AppPaths.EnsureDirs();

      var payload = AppPaths.BotPayloadPathNearLauncher();
      if (!File.Exists(payload))
      {
        throw new FileNotFoundException(
          "botcore.exe payload not found. Expected next to launcher in botcore\\botcore.exe",
          payload
        );
      }

      // Copy payload into LocalAppData app folder
      File.Copy(payload, AppPaths.BotExeInstalledPath, overwrite: true);
    }

    public void StartBot()
    {
      EnsureBotInstalled();

      if (IsRunning) return;

      var psi = new ProcessStartInfo
      {
        FileName = AppPaths.BotExeInstalledPath,
        WorkingDirectory = AppPaths.BotInstallDir,
        UseShellExecute = false,
        CreateNoWindow = false
      };

      // Optional: tell bot where to look (if you decide to use env vars on Node side)
      psi.Environment["TADSYNC_APPDATA"] = AppPaths.AppDataRoot;
      psi.Environment["TADSYNC_LOCALAPPDATA"] = AppPaths.LocalAppDataRoot;

      _proc = Process.Start(psi) ?? throw new Exception("Failed to start botcore.exe");
    }

    public void StopBot()
    {
      if (!IsRunning) return;
      try
      {
        _proc!.Kill(entireProcessTree: true);
      }
      catch { /* ignore */ }
      finally
      {
        try { _proc?.Dispose(); } catch { }
        _proc = null;
      }
    }

    public void RestartBot()
    {
      StopBot();
      StartBot();
    }
  }
}
