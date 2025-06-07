using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;

namespace WpfApp2
{
    public class Commands
    {
        public Commands() { }
        public void commandPrompt(string command)
        {
            MessageBox.Show(command);
            Process.Start(new ProcessStartInfo
            {
                FileName = "nircmd.exe",
                Arguments = $"{command}",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }

        public void application_command()
        {

        }

        public void file_command()
        {

        }
    }
}
