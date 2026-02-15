using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace TadSyncLauncher
{
  public sealed class BotProcessManager
  {
    private Process? _proc;

    public bool IsRunning => _proc != null && !_proc.HasExited;
    public int Pid => IsRunning ? _proc!.Id : -1;
    private const string BotExeName = "botcore.exe";


    private static string BotExePath => BotcoreBundler.BotInstalledPath;
    private static string BotLogPath => Path.Combine(AppPaths.AppDataRoot, "botcore.log");

    public event Action<int>? BotExited; // exit code

    public void StartBot()
    {
        // Ensure embedded botcore.exe is extracted into AppData
    BotcoreBundler.EnsureExtracted();

      if (IsRunning) return;

      // Ensure config exists
      if (!File.Exists(AppPaths.ConfigPath))
        throw new InvalidOperationException("Config not found. Run setup first.");
        if (!File.Exists(BotExePath))
            {
                var localBot = Path.Combine(AppContext.BaseDirectory, BotExeName);
                if (File.Exists(localBot))
                {
                    Directory.CreateDirectory(AppPaths.AppDataRoot);
                    File.Copy(localBot, BotExePath, overwrite: true);
                }
        }
      // Ensure bot exe exists (launcher should have copied it)
      if (!File.Exists(BotExePath))
        throw new FileNotFoundException($"botcore.exe not found at: {BotExePath} (embedded extract failed)");



      var cfg = JsonUtil.Read<BotConfig>(AppPaths.ConfigPath);
      if (cfg == null || string.IsNullOrWhiteSpace(cfg.DiscordToken))
        throw new InvalidOperationException("Token missing in config. Use Edit Token.");

      Directory.CreateDirectory(AppPaths.AppDataRoot);

      // Fresh log header each launch
      File.AppendAllText(BotLogPath,
        $"\n\n==== Launch {DateTime.Now:yyyy-MM-dd HH:mm:ss} ====\n" +
        $"BotExe: {BotExePath}\n" +
        $"WorkDir: {AppPaths.AppDataRoot}\n");

      var psi = new ProcessStartInfo
      {
        FileName = BotExePath,
        WorkingDirectory = AppPaths.AppDataRoot,
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
      };

      // Set token env var (NO cmd.exe / quoting issues)
      psi.Environment["DISCORD_BOT_TOKEN"] = cfg.DiscordToken.Trim();

      var p = new Process { StartInfo = psi, EnableRaisingEvents = true };

      p.OutputDataReceived += (_, e) =>
      {
        if (e.Data == null) return;
        AppendLog("[OUT] " + e.Data);
      };

      p.ErrorDataReceived += (_, e) =>
      {
        if (e.Data == null) return;
        AppendLog("[ERR] " + e.Data);
      };

      p.Exited += (_, __) =>
      {
        var code = -1;
        try { code = p.ExitCode; } catch { }
        AppendLog($"[EXIT] code={code}");
        BotExited?.Invoke(code);
      };

      if (!p.Start())
        throw new InvalidOperationException($"Failed to start {BotExeName}");

      p.BeginOutputReadLine();
      p.BeginErrorReadLine();

      _proc = p;
    }

    public void RestartBot()
    {
      StopBot();
      StartBot();
    }

    public void StopBot()
    {
      try
      {
        if (_proc == null) return;
        if (_proc.HasExited) { _proc = null; return; }

        // try graceful close first
        try
        {
          _proc.CloseMainWindow();
        }
        catch { }

        // give it a moment
        if (!_proc.WaitForExit(1500))
        {
          try { _proc.Kill(true); } catch { }
          _proc.WaitForExit(1500);
        }
      }
      finally
      {
        _proc = null;
      }
    }

    private static void AppendLog(string line)
    {
      try
      {
        File.AppendAllText(BotLogPath, line + Environment.NewLine, Encoding.UTF8);
      }
      catch { }
    }

    public string GetLogPath() => BotLogPath;
  }
}
