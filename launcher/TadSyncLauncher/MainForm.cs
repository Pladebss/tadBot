using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace TadSyncLauncher
{
  public sealed class MainForm : Form
  {
    private readonly BotProcessManager _bot = new();
    private LauncherSettings _settings = new();
    private BotConfig? _config;

    private readonly Label _lblBotState = new();
    private readonly Label _lblUptime = new();
    private readonly Label _lblLastBoost = new();
    private readonly Label _lblField = new();
    private readonly Label _lblPresence = new();

    private readonly Button _btnStart = new();
    private readonly Button _btnRestart = new();
    private readonly Button _btnEditToken = new();
    private readonly Button _btnEditConfig = new();

    private readonly CheckBox _chkAutoStart = new();

    private readonly System.Windows.Forms.Timer _uiTimer = new();

    public MainForm()
    {
      Text = "TadSync Control Panel";
      StartPosition = FormStartPosition.CenterScreen;
      ClientSize = new Size(640, 360);
      FormBorderStyle = FormBorderStyle.FixedDialog;
      MaximizeBox = false;

      AppPaths.EnsureDirs();

      LoadSettings();
      LoadOrRunSetupIfNeeded();

      BuildLayout();

      Shown += (_, __) =>
      {
        if (_settings.AutoStartBotOnOpen)
        {
          SafeStartBot();
        }
      };

      _uiTimer.Interval = 1000;
      _uiTimer.Tick += (_, __) => RefreshStatusUI();
      _uiTimer.Start();

      RefreshStatusUI();
    }

    private void BuildLayout()
    {
      var title = new Label
      {
        Text = "TadSync Control Panel",
        Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
        AutoSize = true,
        Location = new Point(18, 16)
      };
      Controls.Add(title);

      int y = 60;

      AddRow("Bot Status:", _lblBotState, y); y += 28;
      AddRow("Bot Uptime:", _lblUptime, y); y += 28;
      AddRow("Last Activated:", _lblLastBoost, y); y += 28;
      AddRow("Current Field:", _lblField, y); y += 28;
      AddRow("Presence:", _lblPresence, y); y += 40;

      _btnStart.Text = "Start Bot";
      _btnStart.Size = new Size(120, 36);
      _btnStart.Location = new Point(18, y);
      _btnStart.Click += (_, __) => SafeStartBot();
      Controls.Add(_btnStart);

      _btnRestart.Text = "Restart Bot";
      _btnRestart.Size = new Size(120, 36);
      _btnRestart.Location = new Point(150, y);
      _btnRestart.Click += (_, __) => SafeRestartBot();
      Controls.Add(_btnRestart);

      _btnEditToken.Text = "Edit Token";
      _btnEditToken.Size = new Size(120, 36);
      _btnEditToken.Location = new Point(282, y);
      _btnEditToken.Click += (_, __) => OpenConfigEditor(focusToken: true);
      Controls.Add(_btnEditToken);

      _btnEditConfig.Text = "Edit Config";
      _btnEditConfig.Size = new Size(120, 36);
      _btnEditConfig.Location = new Point(414, y);
      _btnEditConfig.Click += (_, __) => OpenConfigEditor(focusToken: false);
      Controls.Add(_btnEditConfig);

      y += 54;

      _chkAutoStart.Text = "Auto-start bot when launcher opens";
      _chkAutoStart.AutoSize = true;
      _chkAutoStart.Location = new Point(18, y);
      _chkAutoStart.Checked = _settings.AutoStartBotOnOpen;
      _chkAutoStart.CheckedChanged += (_, __) =>
      {
        _settings.AutoStartBotOnOpen = _chkAutoStart.Checked;
        SaveSettings();
      };
      Controls.Add(_chkAutoStart);

      var btnOpenAppData = new Button
      {
        Text = "Open AppData Folder",
        Size = new Size(160, 28),
        Location = new Point(18, y + 38)
      };
      btnOpenAppData.Click += (_, __) =>
      {
        try { System.Diagnostics.Process.Start("explorer.exe", AppPaths.AppDataRoot); }
        catch { }
      };
      Controls.Add(btnOpenAppData);

      var hint = new Label
      {
        Text = $"Config: {AppPaths.ConfigPath}",
        AutoSize = true,
        Location = new Point(18, ClientSize.Height - 28),
        ForeColor = Color.DimGray
      };
      Controls.Add(hint);
    }

    private void AddRow(string label, Label value, int y)
    {
      var lbl = new Label
      {
        Text = label,
        AutoSize = true,
        Location = new Point(18, y + 2),
        Font = new Font(Font.FontFamily, 10, FontStyle.Bold)
      };
      Controls.Add(lbl);

      value.Text = "—";
      value.AutoSize = true;
      value.Location = new Point(160, y + 2);
      Controls.Add(value);
    }

    private void LoadSettings()
    {
      _settings = JsonUtil.Read<LauncherSettings>(AppPaths.SettingsPath) ?? new LauncherSettings();
      JsonUtil.Write(AppPaths.SettingsPath, _settings);
    }

    private void SaveSettings()
    {
      JsonUtil.Write(AppPaths.SettingsPath, _settings);
    }

    private void LoadOrRunSetupIfNeeded()
    {
      _config = JsonUtil.Read<BotConfig>(AppPaths.ConfigPath);

      if (_config == null || string.IsNullOrWhiteSpace(_config.DiscordToken) || _config.DiscordToken.Contains("PASTE"))
      {
        using var setup = new SetupWizardForm();
        var res = setup.ShowDialog(this);
        if (res != DialogResult.OK)
        {
          // If user closes setup, just close app.
          Close();
          return;
        }

        _config = JsonUtil.Read<BotConfig>(AppPaths.ConfigPath);
      }
    }

    private void OpenConfigEditor(bool focusToken)
    {
      using var editor = new ConfigEditorForm(focusToken);
      var res = editor.ShowDialog(this);
      if (res == DialogResult.OK)
      {
        _config = JsonUtil.Read<BotConfig>(AppPaths.ConfigPath);
      }
    }

    private void SafeStartBot()
    {
      try
      {
        // ensure config exists
        if (!File.Exists(AppPaths.ConfigPath))
        {
          MessageBox.Show("Config not found. Run setup first.", "TadSync", MessageBoxButtons.OK, MessageBoxIcon.Warning);
          return;
        }

        _bot.StartBot();
        RefreshStatusUI();
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Failed to start bot", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void SafeRestartBot()
    {
      try
      {
        _bot.RestartBot();
        RefreshStatusUI();
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Failed to restart bot", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void RefreshStatusUI()
    {
      var status = JsonUtil.Read<BotStatus>(AppPaths.StatusPath);

      var running = _bot.IsRunning;
      _lblBotState.Text = running ? $"Running (PID {(_bot.Pid ?? 0)})" : "Stopped";
      _lblBotState.ForeColor = running ? Color.DarkGreen : Color.DarkRed;

      if (status?.StartedAt != null)
      {
        var started = DateTimeOffset.FromUnixTimeMilliseconds(status.StartedAt.Value);
        var up = DateTimeOffset.UtcNow - started;
        _lblUptime.Text = TimeFmt.FmtSpan(up);
      }
      else
      {
        _lblUptime.Text = "N/A";
      }

      _lblLastBoost.Text = TimeFmt.MinutesAgoFromEpochMs(status?.LastBoostAt);
      _lblField.Text = status?.LastFieldName ?? "N/A";
      _lblPresence.Text = status?.PresenceText ?? "N/A";

      // button enable states
      _btnStart.Enabled = !running;
      _btnRestart.Enabled = true;
    }
  }
}
