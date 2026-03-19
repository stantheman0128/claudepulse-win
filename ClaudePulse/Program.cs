using ClaudePulse.UI;

namespace ClaudePulse;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "ClaudePulseWin_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("ClaudePulse is already running.", "ClaudePulse",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }
}
