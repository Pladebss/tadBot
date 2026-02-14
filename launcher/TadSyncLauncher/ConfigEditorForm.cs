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

    public ConfigEditorForm(bool focusToken)
    {
      Text = "Edit TadSync Config";
      StartPosition = FormStartPosition.CenterParent;
      ClientSize = new Size(820, 640);
      FormBorderStyle = FormBorderStyle.FixedDialog;
      MaximizeBox = false;

      _cfg = JsonUtil.Read<BotConfig>(AppPaths.ConfigPath) ?? new BotConfig();

      BuildUI();

      if (focusToken) _token.Focus();
      else _monitor.Focus();
    }

    private void BuildUI()
    {
      int y = 16;

      AddLabel("Discord Token:", 18, y);
      _token.Location = new Point(170, y - 2);
      _token.Size = new Size(620, 24);
      _token.Text = _cfg.DiscordToken ?? "";
      Controls.Add(_token);
      y += 34;

      AddLabel("Monitor Channel ID:", 18, y);
      _monitor.Location = new Point(170, y - 2);
      _monitor.Size = new Size(260, 24);
      _monitor.Text = _cfg.MonitorChannelId ?? "";
      Controls.Add(_monitor);

      AddLabel("TTL Minutes:", 460, y);
      _ttl.Location = new Point(560, y - 2);
      _ttl.Minimum = 1;
      _ttl.Maximum = 240;
      _ttl.Value = Math.Clamp(_cfg.Presence?.TtlMinutes ?? 15, 1, 240);
      Controls.Add(_ttl);

      y += 44;

      var tabs = new TabControl
      {
        Location = new Point(18, y),
        Size = new Size(772, 520)
      };
      Controls.Add(tabs);

      // Dest channels tab
      var tabDest = new TabPage("Dest Channels");
      tabs.TabPages.Add(tabDest);

      _dest.Location = new Point(10, 10);
      _dest.Size = new Size(744, 450);
      _dest.RowHeadersVisible = false;
      _dest.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
      _dest.Columns.Add("ChannelId", "Channel ID");
      _dest.AllowUserToAddRows = true;
      _dest.AllowUserToDeleteRows = true;
      tabDest.Controls.Add(_dest);

      foreach (var id in _cfg.BoostDestChannelIds ?? new List<string>())
        _dest.Rows.Add(id);

      // Mapping tab
      var tabMap = new TabPage("Field Mapping");
      tabs.TabPages.Add(tabMap);

      _mapping.Location = new Point(10, 10);
      _mapping.Size = new Size(744, 450);
      _mapping.RowHeadersVisible = false;
      _mapping.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
      _mapping.Columns.Add("FieldName", "Field Name");
      _mapping.Columns.Add("TokenToSend", "Message to Send");
      _mapping.AllowUserToAddRows = true;
      _mapping.AllowUserToDeleteRows = true;
      tabMap.Controls.Add(_mapping);

      if (_cfg.FieldMapping != null)
      {
        foreach (var kv in _cfg.FieldMapping)
          _mapping.Rows.Add(kv.Key, kv.Value);
      }

      // Super users tab
      var tabSuper = new TabPage("Super Users");
      tabs.TabPages.Add(tabSuper);

      _super.Location = new Point(10, 10);
      _super.Size = new Size(744, 450);
      _super.RowHeadersVisible = false;
      _super.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
      _super.Columns.Add("UserId", "User ID");
      _super.AllowUserToAddRows = true;
      _super.AllowUserToDeleteRows = true;
      tabSuper.Controls.Add(_super);

      foreach (var id in _cfg.SuperUsers ?? new List<string>())
        _super.Rows.Add(id);

      // Buttons
      var btnSave = new Button { Text = "Save", Size = new Size(110, 36), Location = new Point(680, 580) };
      btnSave.Click += (_, __) => SaveAndClose();
      Controls.Add(btnSave);

      var btnCancel = new Button { Text = "Cancel", Size = new Size(110, 36), Location = new Point(560, 580) };
      btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };
      Controls.Add(btnCancel);
    }

    private void AddLabel(string text, int x, int y)
    {
      Controls.Add(new Label { Text = text, AutoSize = true, Location = new Point(x, y) });
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
      if (string.IsNullOrWhiteSpace(token) || token.Contains("PASTE"))
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
