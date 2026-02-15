using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace TadSyncLauncher
{
  public sealed class SetupWizardForm : Form
  {
    private readonly Panel _card;
    private readonly Panel _footer;

    private readonly Button _btnBack = new();
    private readonly Button _btnNext = new();
    private readonly Button _btnCancel = new();

    private readonly FlowLayoutPanel _steps = new();

    private int _stepIndex = 0;

    private readonly TextBox _token = new();
    private readonly TextBox _monitor = new();
    private readonly NumericUpDown _ttl = new();

    private readonly DataGridView _dest = new();
    private readonly DataGridView _mapping = new();
    private readonly DataGridView _super = new();

    private readonly CheckBox _guildOnly = new();
    private readonly DataGridView _guildIds = new();

    private BotConfig _cfg = new();

    private Panel _stepPanel = new();

    public SetupWizardForm()
    {
      Text = "TadSync Setup Wizard";
      StartPosition = FormStartPosition.CenterScreen;
      ClientSize = new Size(860, 660);
      MinimumSize = new Size(780, 600);
      FormBorderStyle = FormBorderStyle.Sizable;

      Theme.ApplyForm(this);

      _cfg = JsonUtil.Read<BotConfig>(AppPaths.ConfigPath) ?? new BotConfig();
      _cfg.Presence ??= new PresenceConfig();
      _cfg.SlashRegistration ??= new SlashRegistrationConfig();
      _cfg.SlashRegistration.GuildIds ??= new List<string>();

      _card = Theme.Card(18, 18, ClientSize.Width - 36, ClientSize.Height - 120);
      _card.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
      Controls.Add(_card);

      _footer = new Panel
      {
        Dock = DockStyle.Bottom,
        Height = 92,
        BackColor = Theme.Panel,
        BorderStyle = BorderStyle.FixedSingle,
        Padding = new Padding(16, 16, 16, 16)
      };
      Controls.Add(_footer);

      BuildFooter();
      BuildStepsBar();
      BuildStepHost();

      ApplyTheme();
      RenderStep();
    }

    private void ApplyTheme()
    {
      Theme.StyleTextBox(_token);
      Theme.StyleTextBox(_monitor);
      Theme.StyleNumeric(_ttl);

      Theme.StyleGrid(_dest);
      Theme.StyleGrid(_mapping);
      Theme.StyleGrid(_super);
      Theme.StyleGrid(_guildIds);

      Theme.StyleButton(_btnBack, primary: false);
      Theme.StyleButton(_btnNext, primary: true);
      Theme.StyleButton(_btnCancel, primary: false);
    }

    private void BuildFooter()
    {
      _btnBack.Text = "Back";
      _btnBack.Size = new Size(120, 40);
      _btnBack.Location = new Point(16, 20);
      _btnBack.Click += (_, __) =>
      {
        if (_stepIndex > 0) _stepIndex--;
        RenderStep();
      };

      _btnCancel.Text = "Cancel";
      _btnCancel.Size = new Size(120, 40);
      _btnCancel.Location = new Point(148, 20);
      _btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

      _btnNext.Text = "Next";
      _btnNext.Size = new Size(140, 40);
      _btnNext.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      _btnNext.Location = new Point(_footer.Width - _btnNext.Width - 16, 20);
      _btnNext.Click += (_, __) =>
      {
        if (!ValidateCurrentStep()) return;

        if (_stepIndex < 3)
        {
          _stepIndex++;
          RenderStep();
          return;
        }

        SaveConfig();
        DialogResult = DialogResult.OK;
        Close();
      };

      _footer.Controls.Add(_btnBack);
      _footer.Controls.Add(_btnCancel);
      _footer.Controls.Add(_btnNext);

      _footer.Resize += (_, __) =>
      {
        _btnNext.Location = new Point(_footer.Width - _btnNext.Width - 16, 20);
      };
    }

    private void BuildStepsBar()
    {
      _steps.FlowDirection = FlowDirection.LeftToRight;
      _steps.WrapContents = false;
      _steps.AutoScroll = true;
      _steps.Location = new Point(18, 18);
      _steps.Size = new Size(_card.Width - 36, 46);
      _steps.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
      _steps.BackColor = Color.Transparent;

      _card.Controls.Add(_steps);

      AddStepPill("1) Token");
      AddStepPill("2) Channels");
      AddStepPill("3) Mapping");
      AddStepPill("4) Slash");

      void AddStepPill(string text)
      {
        var pill = new Label
        {
          Text = text,
          AutoSize = true,
          Padding = new Padding(12, 8, 12, 8),
          Margin = new Padding(0, 0, 10, 0),
          BackColor = Theme.Panel,       // ✅ replaced Theme.CardBg
          ForeColor = Theme.Muted,
          BorderStyle = BorderStyle.FixedSingle
        };
        _steps.Controls.Add(pill);
      }
    }

    private void BuildStepHost()
    {
      _stepPanel = new Panel
      {
        Location = new Point(18, 72),
        Size = new Size(_card.Width - 36, _card.Height - 90),
        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
        BackColor = Theme.Bg
      };
      _card.Controls.Add(_stepPanel);
    }

    private void RenderStep()
    {
      _stepPanel.Controls.Clear();

      // Highlight active pill
      for (int i = 0; i < _steps.Controls.Count; i++)
      {
        var pill = (Label)_steps.Controls[i];
        bool active = (i == _stepIndex);

        pill.BackColor = active ? Theme.Accent : Theme.Panel; // ✅ no Theme.CardBg
        pill.ForeColor = active ? Color.White : Theme.Muted;
      }

      _btnBack.Enabled = _stepIndex > 0;
      _btnNext.Text = _stepIndex == 3 ? "Finish" : "Next";

      if (_stepIndex == 0) BuildStepToken();
      else if (_stepIndex == 1) BuildStepChannels();
      else if (_stepIndex == 2) BuildStepMapping();
      else BuildStepSlash();
    }

    private void BuildStepToken()
    {
      _stepPanel.Controls.Add(Title("Discord Token + Basics", 12, 10));
      _stepPanel.Controls.Add(Theme.MutedLabel("Saved to AppData config.json. You can edit later in the control panel.", 12, 44));

      AddLabel("Discord Token", 12, 82);
      _token.Location = new Point(160, 78);
      _token.Size = new Size(_stepPanel.Width - 180, 26);
      _token.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
      _token.UseSystemPasswordChar = true;
      _token.Text = _cfg.DiscordToken ?? "";
      _stepPanel.Controls.Add(_token);

      AddLabel("Monitor Channel ID", 12, 122);
      _monitor.Location = new Point(160, 118);
      _monitor.Size = new Size(260, 26);
      _monitor.Text = _cfg.MonitorChannelId ?? "";
      _stepPanel.Controls.Add(_monitor);

      AddLabel("Gather TTL (minutes)", 450, 122);
      _ttl.Location = new Point(620, 118);
      _ttl.Size = new Size(100, 26);
      _ttl.Minimum = 1;
      _ttl.Maximum = 240;
      _ttl.Value = Math.Clamp(_cfg.Presence?.TtlMinutes ?? 15, 1, 240);
      _stepPanel.Controls.Add(_ttl);

      Theme.StyleTextBox(_token);
      Theme.StyleTextBox(_monitor);
      Theme.StyleNumeric(_ttl);
    }

    private void BuildStepChannels()
    {
      _stepPanel.Controls.Add(Title("Destination Channels + Super Users", 12, 10));
      _stepPanel.Controls.Add(Theme.MutedLabel("Dest Channels receive FollowTo messages. Super Users can run restricted commands.", 12, 44));

      var tabs = new TabControl
      {
        Location = new Point(12, 78),
        Size = new Size(_stepPanel.Width - 24, _stepPanel.Height - 90),
        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
      };
      _stepPanel.Controls.Add(tabs);

      var tabDest = new TabPage("Dest Channels") { BackColor = Theme.Bg };
      var tabSuper = new TabPage("Super Users") { BackColor = Theme.Bg };
      tabs.TabPages.Add(tabDest);
      tabs.TabPages.Add(tabSuper);

      _dest.Location = new Point(12, 12);
      _dest.Size = new Size(tabDest.ClientSize.Width - 24, tabDest.ClientSize.Height - 24);
      _dest.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
      _dest.RowHeadersVisible = false;
      _dest.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
      _dest.AllowUserToAddRows = true;
      _dest.AllowUserToDeleteRows = true;
      _dest.Columns.Clear();
      _dest.Columns.Add("ChannelId", "Channel ID");
      tabDest.Controls.Add(_dest);

      _dest.Rows.Clear();
      foreach (var id in _cfg.BoostDestChannelIds ?? new List<string>())
        _dest.Rows.Add(id);

      _super.Location = new Point(12, 12);
      _super.Size = new Size(tabSuper.ClientSize.Width - 24, tabSuper.ClientSize.Height - 24);
      _super.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
      _super.RowHeadersVisible = false;
      _super.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
      _super.AllowUserToAddRows = true;
      _super.AllowUserToDeleteRows = true;
      _super.Columns.Clear();
      _super.Columns.Add("UserId", "User ID");
      tabSuper.Controls.Add(_super);

      _super.Rows.Clear();
      foreach (var id in _cfg.SuperUsers ?? new List<string>())
        _super.Rows.Add(id);

      Theme.StyleGrid(_dest);
      Theme.StyleGrid(_super);
    }

    private void BuildStepMapping()
    {
      _stepPanel.Controls.Add(Title("Field Mapping", 12, 10));
      _stepPanel.Controls.Add(Theme.MutedLabel("Add FieldName -> Message to Send. Triggered by “Boosted: <FieldName>”.", 12, 44));

      _mapping.Location = new Point(12, 78);
      _mapping.Size = new Size(_stepPanel.Width - 24, _stepPanel.Height - 90);
      _mapping.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
      _mapping.RowHeadersVisible = false;
      _mapping.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
      _mapping.AllowUserToAddRows = true;
      _mapping.AllowUserToDeleteRows = true;
      _mapping.Columns.Clear();
      _mapping.Columns.Add("FieldName", "Field Name");
      _mapping.Columns.Add("TokenToSend", "Message to Send");
      _stepPanel.Controls.Add(_mapping);

      _mapping.Rows.Clear();
      if (_cfg.FieldMapping != null)
      {
        foreach (var kv in _cfg.FieldMapping)
          _mapping.Rows.Add(kv.Key, kv.Value);
      }

      Theme.StyleGrid(_mapping);
    }

    private void BuildStepSlash()
    {
      _stepPanel.Controls.Add(Title("Slash Command Registration", 12, 10));
      _stepPanel.Controls.Add(Theme.MutedLabel("If GuildOnly is enabled, you must provide guild IDs or the bot won’t start.", 12, 44));

      _guildOnly.Text = "GuildOnly (register to specific guilds)";
      _guildOnly.AutoSize = true;
      _guildOnly.Location = new Point(12, 80);
      _guildOnly.ForeColor = Theme.Muted;
      _guildOnly.Checked = _cfg.SlashRegistration?.GuildOnly ?? true;
      _stepPanel.Controls.Add(_guildOnly);

      _guildIds.Location = new Point(12, 112);
      _guildIds.Size = new Size(_stepPanel.Width - 24, _stepPanel.Height - 124);
      _guildIds.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
      _guildIds.RowHeadersVisible = false;
      _guildIds.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
      _guildIds.AllowUserToAddRows = true;
      _guildIds.AllowUserToDeleteRows = true;
      _guildIds.Columns.Clear();
      _guildIds.Columns.Add("GuildId", "Guild ID");
      _stepPanel.Controls.Add(_guildIds);

      _guildIds.Rows.Clear();
      foreach (var id in _cfg.SlashRegistration?.GuildIds ?? new List<string>())
        _guildIds.Rows.Add(id);

      void UpdateEnabled() => _guildIds.Enabled = _guildOnly.Checked;
      _guildOnly.CheckedChanged += (_, __) => UpdateEnabled();
      UpdateEnabled();

      Theme.StyleGrid(_guildIds);
    }

    private bool ValidateCurrentStep()
    {
      if (_stepIndex == 0)
      {
        var token = _token.Text.Trim();
        var monitor = _monitor.Text.Trim();
        if (string.IsNullOrWhiteSpace(token) || token.Contains("PASTE", StringComparison.OrdinalIgnoreCase))
        {
          MessageBox.Show("Token is required.", "Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
          return false;
        }
        if (string.IsNullOrWhiteSpace(monitor))
        {
          MessageBox.Show("MonitorChannelId is required.", "Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
          return false;
        }

        _cfg.DiscordToken = token;
        _cfg.MonitorChannelId = monitor;
        _cfg.Presence.TtlMinutes = (int)_ttl.Value;
        return true;
      }

      if (_stepIndex == 1)
      {
        _cfg.BoostDestChannelIds = CollectOneCol(_dest);
        _cfg.SuperUsers = CollectOneCol(_super);
        if (_cfg.BoostDestChannelIds.Count < 1)
        {
          MessageBox.Show("Add at least one destination channel ID.", "Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
          return false;
        }
        return true;
      }

      if (_stepIndex == 2)
      {
        _cfg.FieldMapping = CollectMap(_mapping);
        if (_cfg.FieldMapping.Count < 1)
        {
          MessageBox.Show("Add at least one field mapping.", "Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
          return false;
        }
        return true;
      }

      _cfg.SlashRegistration ??= new SlashRegistrationConfig();
      _cfg.SlashRegistration.GuildOnly = _guildOnly.Checked;
      _cfg.SlashRegistration.GuildIds = CollectOneCol(_guildIds);

      if (_cfg.SlashRegistration.GuildOnly && _cfg.SlashRegistration.GuildIds.Count < 1)
      {
        MessageBox.Show("GuildOnly is enabled — add at least one Guild ID.", "Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
      }

      return true;
    }

    private void SaveConfig()
    {
      JsonUtil.Write(AppPaths.ConfigPath, _cfg);
    }

    private Label Title(string text, int x, int y)
    {
      return new Label
      {
        Text = text,
        AutoSize = true,
        Font = Theme.TitleFont(this),
        ForeColor = Theme.Text,
        Location = new Point(x, y)
      };
    }

    private void AddLabel(string text, int x, int y)
    {
      _stepPanel.Controls.Add(new Label
      {
        Text = text,
        AutoSize = true,
        Location = new Point(x, y),
        ForeColor = Theme.Muted
      });
    }

    private static List<string> CollectOneCol(DataGridView grid)
    {
      var list = new List<string>();
      foreach (DataGridViewRow row in grid.Rows)
      {
        if (row.IsNewRow) continue;
        var v = Convert.ToString(row.Cells[0].Value)?.Trim();
        if (!string.IsNullOrWhiteSpace(v)) list.Add(v);
      }
      return list;
    }

    private static Dictionary<string, string> CollectMap(DataGridView grid)
    {
      var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      foreach (DataGridViewRow row in grid.Rows)
      {
        if (row.IsNewRow) continue;
        var k = Convert.ToString(row.Cells[0].Value)?.Trim();
        var v = Convert.ToString(row.Cells[1].Value)?.Trim();
        if (string.IsNullOrWhiteSpace(k) || string.IsNullOrWhiteSpace(v)) continue;
        map[k] = v;
      }
      return map;
    }
  }
}
