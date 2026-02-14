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
      ClientSize = new Size(760, 420);
      MinimumSize = new Size(740, 400);

      Theme.ApplyForm(this);
      AppPaths.EnsureDirs();

      LoadSettings();
      LoadOrRunSetupIfNeeded();

      BuildLayout();

      Shown += (_, __) =>
      {
        if (_settings.AutoStartBotOnOpen)
          SafeStartBot();
      };

      _uiTimer.Interval = 1000;
      _uiTimer.Tick += (_, __) => RefreshStatusUI();
      _uiTimer.Start();

      RefreshStatusUI();
    }

    private void BuildLayout()
    {
      var header = Theme.Card(18, 18, ClientSize.Width - 36, 72);
      header.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
      Controls.Add(header);

      var title = new Label
      {
        Text = "TadSync Control Panel",
        AutoSize = true,
        Font = Theme.TitleFont(this),
        Location = new Point(16, 12),
        ForeColor = Theme.Text
      };
      header.Controls.Add(title);

      header.Controls.Add(Theme.MutedLabel("Manage bot + config. Live status updates from AppData.", 16, 44));

      var statusCard = Theme.Card(18, 104, ClientSize.Width - 36, 190);
      statusCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
      Controls.Add(statusCard);

      statusCard.Controls.Add(Theme.H2("Live Status", 16, 14, this));

      int y = 52;
      AddRow(statusCard, "Bot Status:", _lblBotState, y); y += 28;
      AddRow(statusCard, "Bot Uptime:", _lblUptime, y); y += 28;
      AddRow(statusCard, "Last Activated:", _lblLastBoost, y); y += 28;
      AddRow(statusCard, "Current Field:", _lblField, y); y += 28;
      AddRow(statusCard, "Presence:", _lblPresence, y);

      var actions = Theme.Card(18, 308, ClientSize.Width - 36, 92);
      actions.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
      Controls.Add(actions);

      actions.Controls.Add(Theme.H2("Actions", 16, 14, this));

      _btnStart.Text = "Start Bot";
      _btnStart.Size = new Size(120, 34);
      _btnStart.Location = new Point(16, 44);
      _btnStart.Click += (_, __) => SafeStartBot();
      Theme.StyleButton(_btnStart, primary: true);
      actions.Controls.Add(_btnStart);

      _btnRestart.Text = "Restart Bot";
      _btnRestart.Size = new Size(120, 34);
      _btnRestart.Location = new Point(148, 44);
      _btnRestart.Click += (_, __) => SafeRestartBot();
      Theme.StyleButton(_btnRestart);
      actions.Controls.Add(_btnRestart);

      _btnEditToken.Text = "Edit Token";
      _btnEditToken.Size = new Size(120, 34);
      _btnEditToken.Location = new Point(280, 44);
      _btnEditToken.Click += (_, __) => OpenConfigEditor(focusToken: true);
      Theme.StyleButton(_btnEditToken);
      actions.Controls.Add(_btnEditToken);

      _btnEditConfig.Text = "Edit Config";
      _btnEditConfig.Size = new Size(120, 34);
      _btnEditConfig.Location = new Point(412, 44);
      _btnEditConfig.Click += (_, __) => OpenConfigEditor(focusToken: false);
      Theme.StyleButton(_btnEditConfig);
      actions.Controls.Add(_btnEditConfig);

      _chkAutoStart.Text = "Auto-start bot when launcher opens";
      _chkAutoStart.AutoSize = true;
      _chkAutoStart.Location = new Point(552, 50);
      _chkAutoStart.Checked = _settings.AutoStartBotOnOpen;
      _chkAutoStart.CheckedChanged += (_, __) =>
      {
        _settings.AutoStartBotOnOpen = _chkAutoStart.Checked;
        SaveSettings();
      };
      Theme.StyleCheck(_chkAutoStart);
      actions.Controls.Add(_chkAutoStart);

      var btnOpenAppData = new Button
      {
        Text = "Open AppData",
        Size = new Size(120, 30),
        Location = new Point(actions.Width - 136, 12),
        Anchor = AnchorStyles.Top | AnchorStyles.Right
      };
      btnOpenAppData.Click += (_, __) =>
      {
        try { System.Diagnostics.Process.Start("explorer.exe", AppPaths.AppDataRoot); } catch { }
      };
      Theme.StyleButton(btnOpenAppData);
      actions.Controls.Add(btnOpenAppData);
    }

    private void AddRow(Panel parent, string label, Label value, int y)
    {
      var lbl = new Label
      {
        Text = label,
        AutoSize = true,
        Location = new Point(16, y),
        ForeColor = Theme.Muted
      };
      parent.Controls.Add(lbl);

      value.Text = "—";
      value.AutoSize = true;
      value.Location = new Point(160, y);
      value.ForeColor = Theme.Text;
      parent.Controls.Add(value);
    }

    private void LoadSettings()
    {
      _settings = JsonUtil.Read<LauncherSettings>(AppPaths.SettingsPath) ?? new LauncherSettings();
      JsonUtil.Write(AppPaths.SettingsPath, _settings);
    }

    private void SaveSettings() => JsonUtil.Write(AppPaths.SettingsPath, _settings);

    private void LoadOrRunSetupIfNeeded()
    {
      _config = JsonUtil.Read<BotConfig>(AppPaths.ConfigPath);
      if (_config == null || string.IsNullOrWhiteSpace(_config.DiscordToken))
      {
        using var setup = new SetupWizardForm();
        var res = setup.ShowDialog(this);
        if (res != DialogResult.OK) { Close(); return; }
        _config = JsonUtil.Read<BotConfig>(AppPaths.ConfigPath);
      }
    }

    private void OpenConfigEditor(bool focusToken)
    {
      using var editor = new ConfigEditorForm(focusToken);
      var res = editor.ShowDialog(this);
      if (res == DialogResult.OK)
        _config = JsonUtil.Read<BotConfig>(AppPaths.ConfigPath);
    }

    private void SafeStartBot()
    {
      try
      {
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
      _lblBotState.Text = running ? $"Running (PID {_bot.Pid})" : "Stopped";
      _lblBotState.ForeColor = running ? Theme.Green : Theme.Red;

      if (status?.StartedAt != null)
      {
        var started = DateTimeOffset.FromUnixTimeMilliseconds(status.StartedAt.Value);
        var up = DateTimeOffset.UtcNow - started;
        _lblUptime.Text = TimeFmt.FmtSpan(up);
      }
      else _lblUptime.Text = "N/A";

      _lblLastBoost.Text = TimeFmt.MinutesAgoFromEpochMs(status?.LastBoostAt);
      _lblField.Text = status?.LastFieldName ?? "N/A";
      _lblPresence.Text = status?.PresenceText ?? "N/A";

      _btnStart.Enabled = !running;
    }
  }
}
