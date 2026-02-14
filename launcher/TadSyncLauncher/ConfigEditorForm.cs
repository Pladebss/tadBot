using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace TadSyncLauncher
{
  public sealed class ConfigEditorForm : Form
  {
    private BotConfig _cfg;

    private readonly TextBox _token = new();
    private readonly TextBox _monitor = new();
    private readonly NumericUpDown _ttl = new();

    private readonly DataGridView _dest = new();
    private readonly DataGridView _mapping = new();
    private readonly DataGridView _super = new();

    private readonly TabControl _tabs = new();

    public ConfigEditorForm(bool focusToken)
    {
      Text = "Edit TadSync Config";
      StartPosition = FormStartPosition.CenterParent;
      ClientSize = new Size(860, 700);
      FormBorderStyle = FormBorderStyle.Sizable;
      MinimumSize = new Size(780, 620);

      Theme.ApplyForm(this);

      _cfg = JsonUtil.Read<BotConfig>(AppPaths.ConfigPath) ?? new BotConfig();

      BuildUI();
      ApplyTheme();

      Shown += (_, __) =>
      {
        if (focusToken) _token.Focus();
        else _monitor.Focus();
      };
    }

    private void ApplyTheme()
    {
      Theme.StyleTextBox(_token);
      Theme.StyleTextBox(_monitor);
      Theme.StyleNumeric(_ttl);

      Theme.StyleGrid(_dest);
      Theme.StyleGrid(_mapping);
      Theme.StyleGrid(_super);

      _tabs.Appearance = TabAppearance.Normal;
      _tabs.SizeMode = TabSizeMode.Normal;
    }

    private void BuildUI()
    {
      // Top header card
      var header = Theme.Card(18, 18, ClientSize.Width - 36, 120);
      header.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
      Controls.Add(header);

      var title = new Label
      {
        Text = "Config Editor",
        AutoSize = true,
        Font = Theme.TitleFont(this),
        Location = new Point(16, 12),
        ForeColor = Theme.Text
      };
      header.Controls.Add(title);

      header.Controls.Add(Theme.MutedLabel("Edits are saved to AppData config.json. Mapping matches Boosted: <FieldName> anywhere in the message/embed.", 16, 44));

      // Token row
      AddLabelTo(header, "Discord Token", 16, 72);
      _token.Location = new Point(160, 68);
      _token.Size = new Size(header.Width - 176, 26);
      _token.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
      _token.UseSystemPasswordChar = true;
      _token.Text = _cfg.DiscordToken ?? "";
      header.Controls.Add(_token);

      // Middle card: essentials
      var essentials = Theme.Card(18, 150, ClientSize.Width - 36, 70);
      essentials.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
      Controls.Add(essentials);

      AddLabelTo(essentials, "Monitor Channel ID", 16, 18);
      _monitor.Location = new Point(160, 14);
      _monitor.Size = new Size(260, 26);
      _monitor.Text = _cfg.MonitorChannelId ?? "";
      essentials.Controls.Add(_monitor);

      AddLabelTo(essentials, "Gather TTL (minutes)", 450, 18);
      _ttl.Location = new Point(590, 14);
      _ttl.Size = new Size(100, 26);
      _ttl.Minimum = 1;
      _ttl.Maximum = 240;
      _ttl.Value = Math.Clamp(_cfg.Presence?.TtlMinutes ?? 15, 1, 240);
      essentials.Controls.Add(_ttl);

      // Tabs (resizable)
      _tabs.Location = new Point(18, 232);
      _tabs.Size = new Size(ClientSize.Width - 36, ClientSize.Height - 232 - 90);
      _tabs.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
      Controls.Add(_tabs);

      BuildDestTab();
      BuildMappingTab();
      BuildSuperTab();

      // Footer actions (fixed area)
      var footer = new Panel
      {
        Dock = DockStyle.Bottom,
        Height = 80,
        BackColor = Theme.Panel,
        BorderStyle = BorderStyle.FixedSingle,
        Padding = new Padding(14, 14, 14, 14)
      };
      Controls.Add(footer);

      var btnSave = new Button { Text = "Save", Size = new Size(120, 38) };
      var btnCancel = new Button { Text = "Cancel", Size = new Size(120, 38) };

      Theme.StyleButton(btnSave, primary: true);
      Theme.StyleButton(btnCancel, primary: false);

      btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

      btnSave.Location = new Point(footer.Width - btnSave.Width - 16, 18);
      btnCancel.Location = new Point(btnSave.Left - btnCancel.Width - 12, 18);

      btnSave.Click += (_, __) => SaveAndClose();
      btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

      footer.Controls.Add(btnCancel);
      footer.Controls.Add(btnSave);

      footer.Resize += (_, __) =>
      {
        btnSave.Location = new Point(footer.Width - btnSave.Width - 16, 18);
        btnCancel.Location = new Point(btnSave.Left - btnCancel.Width - 12, 18);
      };
    }

    private void BuildDestTab()
    {
      var tab = new TabPage("Dest Channels");
      tab.BackColor = Theme.Bg;
      _tabs.TabPages.Add(tab);

      var hint = Theme.MutedLabel("These channels receive the plaintext FollowTo messages.", 12, 12);
      tab.Controls.Add(hint);

      _dest.Location = new Point(12, 42);
      _dest.Size = new Size(tab.ClientSize.Width - 24, tab.ClientSize.Height - 54);
      _dest.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

      _dest.RowHeadersVisible = false;
      _dest.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
      _dest.AllowUserToAddRows = true;
      _dest.AllowUserToDeleteRows = true;

      _dest.Columns.Clear();
      _dest.Columns.Add("ChannelId", "Channel ID");

      tab.Controls.Add(_dest);

      foreach (var id in _cfg.BoostDestChannelIds ?? new List<string>())
        _dest.Rows.Add(id);
    }

    private void BuildMappingTab()
    {
      var tab = new TabPage("Field Mapping");
      tab.BackColor = Theme.Bg;
      _tabs.TabPages.Add(tab);

      var hint = Theme.MutedLabel("If a message contains Boosted: <FieldName>, TadBot sends the matching Message to Send.", 12, 12);
      tab.Controls.Add(hint);

      _mapping.Location = new Point(12, 42);
      _mapping.Size = new Size(tab.ClientSize.Width - 24, tab.ClientSize.Height - 54);
      _mapping.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

      _mapping.RowHeadersVisible = false;
      _mapping.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
      _mapping.AllowUserToAddRows = true;
      _mapping.AllowUserToDeleteRows = true;

      _mapping.Columns.Clear();
      _mapping.Columns.Add("FieldName", "Field Name");
      _mapping.Columns.Add("TokenToSend", "Message to Send");

      tab.Controls.Add(_mapping);

      if (_cfg.FieldMapping != null)
      {
        foreach (var kv in _cfg.FieldMapping)
          _mapping.Rows.Add(kv.Key, kv.Value);
      }
    }

    private void BuildSuperTab()
    {
      var tab = new TabPage("Super Users");
      tab.BackColor = Theme.Bg;
      _tabs.TabPages.Add(tab);

      var hint = Theme.MutedLabel("Users who can run restricted commands. (They do NOT bypass channel restrictions.)", 12, 12);
      tab.Controls.Add(hint);

      _super.Location = new Point(12, 42);
      _super.Size = new Size(tab.ClientSize.Width - 24, tab.ClientSize.Height - 54);
      _super.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

      _super.RowHeadersVisible = false;
      _super.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
      _super.AllowUserToAddRows = true;
      _super.AllowUserToDeleteRows = true;

      _super.Columns.Clear();
      _super.Columns.Add("UserId", "User ID");

      tab.Controls.Add(_super);

      foreach (var id in _cfg.SuperUsers ?? new List<string>())
        _super.Rows.Add(id);
    }

    private static void AddLabelTo(Control parent, string text, int x, int y)
    {
      parent.Controls.Add(new Label
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

    private void SaveAndClose()
    {
      var token = _token.Text.Trim();
      var monitor = _monitor.Text.Trim();

      if (string.IsNullOrWhiteSpace(token) || token.Contains("PASTE", StringComparison.OrdinalIgnoreCase))
      {
        MessageBox.Show("Token is required.", "Config", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }
      if (string.IsNullOrWhiteSpace(monitor))
      {
        MessageBox.Show("MonitorChannelId is required.", "Config", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }

      _cfg.DiscordToken = token;
      _cfg.MonitorChannelId = monitor;

      _cfg.Presence ??= new PresenceConfig();
      _cfg.Presence.TtlMinutes = (int)_ttl.Value;

      _cfg.BoostDestChannelIds = CollectOneCol(_dest);
      _cfg.SuperUsers = CollectOneCol(_super);
      _cfg.FieldMapping = CollectMap(_mapping);

      if (_cfg.BoostDestChannelIds.Count < 1)
      {
        MessageBox.Show("Add at least one destination channel ID.", "Config", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }
      if (_cfg.FieldMapping.Count < 1)
      {
        MessageBox.Show("Add at least one field mapping.", "Config", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }

      JsonUtil.Write(AppPaths.ConfigPath, _cfg);

      DialogResult = DialogResult.OK;
      Close();
    }
  }
}
