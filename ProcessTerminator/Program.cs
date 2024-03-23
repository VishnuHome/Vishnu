using System.Diagnostics;

namespace ProcessTerminator
{
    internal class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                MessageBox.Show("ProcessTerminator Process.Id [Delay]: no arguments provided.");
                return 2;
            }
            Process? process = null;
            int delay = 0;
            if (!UInt32.TryParse(args[0], out UInt32 pid))
            {
                MessageBox.Show("ProcessTerminator Process.Id [Delay]: " + args[0] + " is no valid numeric Id.");
                return 3;
            }
            try
            {
                process = Process.GetProcessById((int)pid);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ProcessTerminator Process.Id [Delay]: " + ex.Message);
                return 4;
            }
            if (args.Length > 1 && int.TryParse(args[1], out delay))
            {
                Thread.Sleep(delay);
            }
            try
            {
                process.Kill();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ProcessTerminator Process.Id {pid} [Delay]: " + ex.Message);
                return 5;
            }
            // MessageBox.Show($"ProcessTerminator Process.Id {pid} [Delay]: {delay}");
            return 1;
        }
    }
}
