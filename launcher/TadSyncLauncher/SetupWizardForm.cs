using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace TadSyncLauncher
{
  public sealed class SetupWizardForm : Form
  {
    private readonly TextBox _token = new();
    private readonly TextBox _monitor = new();
    private readonly TextBox _dest = new();
    private readonly NumericUpDown _ttl = new();
    private readonly DataGridView _mapping = new();

    public SetupWizardForm()
    {
      Text = "TadSync Setup Wizard";
      StartPosition = FormStartPosition.CenterParent;
      ClientSize = new Size(740, 520);
      FormBorderStyle = FormBorderStyle.FixedDialog;
      MaximizeBox = false;

      AppPaths.EnsureDirs();

      BuildUI();
    }

    private void BuildUI()
    {
      var lbl = new Label
      {
        Text = "First-time Setup",
        AutoSize = true,
        Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
        Location = new Point(18, 14)
      };
      Controls.Add(lbl);

      int y = 60;

      AddLabel("Discord Bot Token:", 18, y);
      _token.Location = new Point(220, y - 2);
      _token.Size = new Size(480, 24);
      Controls.Add(_token);
      y += 38;

      AddLabel("Monitor Channel ID:", 18, y);
      _monitor.Location = new Point(220, y - 2);
      _monitor.Size = new Size(240, 24);
      Controls.Add(_monitor);
      y += 38;

      AddLabel("Destination Channel IDs (comma-separated):", 18, y);
      _dest.Location = new Point(220, y - 2);
      _dest.Size = new Size(480, 24);
      Controls.Add(_dest);
      y += 38;

      AddLabel("Gathering TTL (minutes):", 18, y);
      _ttl.Location = new Point(220, y - 2);
      _ttl.Minimum = 1;
      _ttl.Maximum = 240;
      _ttl.Value = 15;
      Controls.Add(_ttl);
      y += 44;

      var mapTitle = new Label
      {
        Text = "Field Mapping (FieldName -> MessageToSend)",
        AutoSize = true,
        Font = new Font(Font.FontFamily, 10, FontStyle.Bold),
        Location = new Point(18, y)
      };
      Controls.Add(mapTitle);

      y += 24;

      _mapping.Location = new Point(18, y);
      _mapping.Size = new Size(682, 280);
      _mapping.AllowUserToAddRows = true;
      _mapping.AllowUserToDeleteRows = true;
      _mapping.RowHeadersVisible = false;
      _mapping.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
      _mapping.Columns.Add("FieldName", "Field Name");
      _mapping.Columns.Add("TokenToSend", "Message to Send");
      Controls.Add(_mapping);

      // default examples
      _mapping.Rows.Add("Pine Tree", "FollowTo PineTree");
      _mapping.Rows.Add("Bamboo", "FollowTo Bamboo");
      _mapping.Rows.Add("Blue Flower", "FollowTo BlueFlower");

      var btnSave = new Button
      {
        Text = "Finish Setup",
        Size = new Size(140, 36),
        Location = new Point(560, 460)
      };
      btnSave.Click += (_, __) => Finish();
      Controls.Add(btnSave);

      var btnCancel = new Button
      {
        Text = "Cancel",
        Size = new Size(100, 36),
        Location = new Point(450, 460)
      };
      btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };
      Controls.Add(btnCancel);
    }

    private void AddLabel(string text, int x, int y)
    {
      Controls.Add(new Label
      {
        Text = text,
        AutoSize = true,
        Location = new Point(x, y)
      });
    }

    private static List<string> ParseCsvIds(string s)
    {
      var list = new List<string>();
      if (string.IsNullOrWhiteSpace(s)) return list;
      foreach (var part in s.Split(','))
      {
        var t = part.Trim();
        if (t.Length > 0) list.Add(t);
      }
      return list;
    }

    private void Finish()
    {
      var token = _token.Text.Trim();
      var monitor = _monitor.Text.Trim();
      var dest = ParseCsvIds(_dest.Text);

      if (string.IsNullOrWhiteSpace(token) || token.Contains("PASTE"))
      {
        MessageBox.Show("Please paste a real token.", "Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }
      if (string.IsNullOrWhiteSpace(monitor))
      {
        MessageBox.Show("MonitorChannelId is required.", "Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }
      if (dest.Count < 1)
      {
        MessageBox.Show("At least one destination channel ID is required.", "Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }

      var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      foreach (DataGridViewRow row in _mapping.Rows)
      {
        if (row.IsNewRow) continue;
        var k = Convert.ToString(row.Cells[0].Value)?.Trim();
        var v = Convert.ToString(row.Cells[1].Value)?.Trim();
        if (string.IsNullOrWhiteSpace(k) || string.IsNullOrWhiteSpace(v)) continue;
        mapping[k] = v;
      }

      if (mapping.Count < 1)
      {
        MessageBox.Show("Add at least one mapping row.", "Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }

      var cfg = new BotConfig
      {
        DiscordToken = token,
        MonitorChannelId = monitor,
        BoostDestChannelIds = dest,
        FieldMapping = mapping,
        Presence = new PresenceConfig { TtlMinutes = (int)_ttl.Value }
      };

      JsonUtil.Write(AppPaths.ConfigPath, cfg);

      DialogResult = DialogResult.OK;
      Close();
    }
  }
}
