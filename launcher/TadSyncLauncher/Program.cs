using System;
using System.Windows.Forms;

namespace TadSyncLauncher
{
  internal static class Program
  {
    [STAThread]
    static void Main()
    {
      ApplicationConfiguration.Initialize();
      Application.Run(new MainForm());
    }
  }
}
