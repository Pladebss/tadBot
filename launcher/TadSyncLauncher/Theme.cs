using System.Drawing;
using System.Windows.Forms;

namespace TadSyncLauncher
{
  public static class Theme
  {
    // Dark palette
    public static readonly Color Bg = Color.FromArgb(18, 18, 22);
    public static readonly Color Panel = Color.FromArgb(24, 24, 30);
    public static readonly Color Panel2 = Color.FromArgb(30, 30, 38);

    public static readonly Color Text = Color.FromArgb(235, 235, 245);
    public static readonly Color Muted = Color.FromArgb(160, 160, 175);

    public static readonly Color Accent = Color.FromArgb(105, 170, 255);
    public static readonly Color Green = Color.FromArgb(60, 210, 120);
    public static readonly Color Red = Color.FromArgb(235, 85, 90);

    public static readonly Color Border = Color.FromArgb(55, 55, 70);
    public static readonly Color GridLine = Color.FromArgb(50, 50, 64);

    public static Font TitleFont(Control c) => new Font(c.Font.FontFamily, 16, FontStyle.Bold);
    public static Font H2Font(Control c) => new Font(c.Font.FontFamily, 11, FontStyle.Bold);

    public static void ApplyForm(Form f)
    {
      f.BackColor = Bg;
      f.ForeColor = Text;
      f.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
    }

    public static void StyleButton(Button b, bool primary = false)
    {
      b.FlatStyle = FlatStyle.Flat;
      b.FlatAppearance.BorderSize = 1;
      b.FlatAppearance.BorderColor = Border;
      b.ForeColor = Text;
      b.BackColor = primary ? Color.FromArgb(45, 90, 170) : Panel2;
      b.Cursor = Cursors.Hand;
      b.Padding = new Padding(2);
    }

    public static void StyleTextBox(TextBox t)
    {
      t.BorderStyle = BorderStyle.FixedSingle;
      t.BackColor = Panel2;
      t.ForeColor = Text;
    }

    public static void StyleNumeric(NumericUpDown n)
    {
      n.BackColor = Panel2;
      n.ForeColor = Text;
      n.BorderStyle = BorderStyle.FixedSingle;
    }

    public static void StyleCheck(CheckBox c)
    {
      c.ForeColor = Text;
    }

    public static void StyleGrid(DataGridView g)
    {
      g.BackgroundColor = Panel2;
      g.BorderStyle = BorderStyle.FixedSingle;

      g.EnableHeadersVisualStyles = false;
      g.ColumnHeadersDefaultCellStyle.BackColor = Panel;
      g.ColumnHeadersDefaultCellStyle.ForeColor = Text;
      g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Panel;
      g.ColumnHeadersDefaultCellStyle.SelectionForeColor = Text;

      g.DefaultCellStyle.BackColor = Panel2;
      g.DefaultCellStyle.ForeColor = Text;
      g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(55, 70, 110);
      g.DefaultCellStyle.SelectionForeColor = Text;

      g.GridColor = GridLine;
      g.RowHeadersVisible = false;
      g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
      g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

      g.AllowUserToResizeRows = false;
      g.RowTemplate.Height = 28;
    }

    public static Panel Card(int x, int y, int w, int h)
    {
      return new Panel
      {
        Location = new Point(x, y),
        Size = new Size(w, h),
        BackColor = Panel,
        BorderStyle = BorderStyle.FixedSingle
      };
    }

    public static Label H2(string text, int x, int y, Control parent)
    {
      return new Label
      {
        Text = text,
        AutoSize = true,
        Location = new Point(x, y),
        Font = H2Font(parent),
        ForeColor = Text
      };
    }

    public static Label MutedLabel(string text, int x, int y)
    {
      return new Label
      {
        Text = text,
        AutoSize = true,
        Location = new Point(x, y),
        ForeColor = Muted
      };
    }
  }
}
