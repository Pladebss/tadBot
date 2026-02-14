using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace TadSyncLauncher
{
  public sealed class SetupWizardForm : Form
  {
    private readonly Panel _content = new();
    private readonly Panel _footer = new();

    private readonly TextBox _token = new();
    private readonly TextBox _monitor = new();
    private readonly TextBox _dest = new();
    private readonly NumericUpDown _ttl = new();
    private readonly DataGridView _mapping = new();

    private readonly Button _btnFinish = new();
    private readonly Button _btnCancel = new();

    public SetupWizardForm()
    {
      Text = "TadSync Setup Wizard";
      StartPosition = FormStartPosition.CenterParent;
      ClientSize = new Size(820, 620);
      FormBorderStyle = FormBorderStyle.Sizable;
      MinimumSize = new Size(760, 560);

      Theme.ApplyForm(this);
      AppPaths.EnsureDirs();

      BuildLayout();
    }

    private void BuildLayout()
    {
      // Scrollable content area
      _content.Dock = DockStyle.Fill;
      _content.AutoScroll = true;
      _content.BackColor = Theme.Bg;
      Controls.Add(_content);

      // Fixed footer (always visible)
      _footer.Dock = DockStyle.Bottom;
      _footer.Height = 72;
      _footer.BackColor = Theme.Panel;
      _footer.Padding = new Padding(14, 12, 14, 12);
      _footer.BorderStyle = BorderStyle.FixedSingle;
      Controls.Add(_footer);

      _btnFinish.Text = "Finish Setup";
      _btnFinish.Size = new Size(140, 36);
      _btnFinish.Anchor = AnchorStyles.Right | AnchorStyles.Top;
      _btnFinish.Location = new Point(_footer.Width - 160, 16);
      _btnFinish.Click += (_, __) => Finish();
      Theme.StyleButton(_btnFinish, primary: true);

      _btnCancel.Text = "Cancel";
      _btnCancel.Size = new Size(110, 36);
      _btnCancel.Anchor = AnchorStyles.Right | AnchorStyles.Top;
      _btnCancel.Location = new Point(_footer.Width - 280, 16);
      _btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };
      Theme.StyleButton(_btnCancel, primary: false);

      _footer.Controls.Add(_btnCancel);
      _footer.Controls.Add(_btnFinish);

      _footer.Resize += (_, __) =>
      {
        _btnFinish.Location = new Point(_footer.Width - _btnFinish.Width - 16, 16);
        _btnCancel.Location = new Point(_btnFinish.Left - _btnCancel.Width - 12, 16);
      };

      // Content inside scroll panel
      int pad = 18;
      int y = 18;

      var title = new Label
      {
        Text = "First-time Setup",
        AutoSize = true,
        Font = Theme.TitleFont(this),
        Location = new Point(pad, y),
        ForeColor = Theme.Text
      };
      _content.Controls.Add(title);

      y += 44;
      _content.Controls.Add(Theme.MutedLabel("Fill this out once. You can edit later from the main control panel.", pad, y));
      y += 28;

      // Card: Essentials
      var card1 = Theme.Card(pad, y, 760, 180);
      _content.Controls.Add(card1);

      card1.Controls.Add(Theme.H2("Essentials", 14, 14, this));
      card1.Controls.Add(Theme.MutedLabel("Token + channels. Destination channels receive the FollowTo messages.", 14, 40));

      int cy = 72;
      AddLabeledText(card1, "Discord Bot Token", _token, 14, cy, 720, mask: true);
      cy += 44;

      AddLabeledText(card1, "Monitor Channel ID", _monitor, 14, cy, 320);
      AddLabeledText(card1, "Dest Channel IDs (comma-separated)", _dest, 350, cy, 384);
      cy += 44;

      AddLabeledNumeric(card1, "Gathering TTL (minutes)", _ttl, 14, cy, 140, 1, 240, 15);

      y += card1.Height + 14;

      // Card: Mapping
      var card2 = Theme.Card(pad, y, 760, 300);
      _content.Controls.Add(card2);

      card2.Controls.Add(Theme.H2("Field Mapping", 14, 14, this));
      card2.Controls.Add(Theme.MutedLabel("If any message/embed contains Boosted: <FieldName>, send the mapped plaintext.", 14, 40));

      _mapping.Location = new Point(14, 72);
      _mapping.Size = new Size(720, 210);
      _mapping.AllowUserToAddRows = true;
      _mapping.AllowUserToDeleteRows = true;
      _mapping.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
      _mapping.Columns.Add("FieldName", "Field Name (matches Boosted: ... text)");
      _mapping.Columns.Add("TokenToSend", "Message to Send (plaintext)");
      Theme.StyleGrid(_mapping);

      // Defaults
      _mapping.Rows.Add("Pine Tree", "FollowTo PineTree");
      _mapping.Rows.Add("Bamboo", "FollowTo Bamboo");
      _mapping.Rows.Add("Blue Flower", "FollowTo BlueFlower");

      card2.Controls.Add(_mapping);

      // Make mapping card stretch nicely with window
      card1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
      card2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

      _content.Resize += (_, __) =>
      {
        int w = _content.ClientSize.Width - (pad * 2) - SystemInformation.VerticalScrollBarWidth;
        if (w < 640) w = 640;

        card1.Width = w;
        card2.Width = w;

        _token.Width = w - 40;
        _mapping.Width = w - 40;

        // adjust right-side dest box
        _dest.Width = Math.Max(220, w - 40 - 350);
      };
    }

    private void AddLabeledText(Panel parent, string label, TextBox tb, int x, int y, int w, bool mask = false)
    {
      var l = new Label
      {
        Text = label,
        AutoSize = true,
        Location = new Point(x, y),
        ForeColor = Theme.Muted
      };
      parent.Controls.Add(l);

      tb.Location = new Point(x, y + 18);
      tb.Size = new Size(w, 24);
      if (mask) tb.UseSystemPasswordChar = true;
      Theme.StyleTextBox(tb);
      parent.Controls.Add(tb);
    }

    private void AddLabeledNumeric(Panel parent, string label, NumericUpDown n, int x, int y, int w, int min, int max, int val)
    {
      var l = new Label
      {
        Text = label,
        AutoSize = true,
        Location = new Point(x, y),
        ForeColor = Theme.Muted
      };
      parent.Controls.Add(l);

      n.Location = new Point(x, y + 18);
      n.Size = new Size(w, 24);
      n.Minimum = min;
      n.Maximum = max;
      n.Value = val;
      Theme.StyleNumeric(n);
      parent.Controls.Add(n);
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

      if (string.IsNullOrWhiteSpace(token))
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
