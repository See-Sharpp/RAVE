using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;

namespace WpfApp2
{
    public class Commands
    {
        private string temp = null; // Changed to private instance field to fix CS0120

        public Commands() { }

        public static void systemCommand(string command)
        {
            string temp = null; // Declare a local variable to avoid using the instance field in a static method
            string nircmdPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nircmd-x64", "nircmd.exe");
            string fixedStr = "nircmd.exe setsysvolume 0";
            if (command.Contains("savescreenshot"))
            {
                if (Global.deafultScreenShotPath == null)
                {
                    throw new InvalidOperationException("Global.deafultScreenShotPath is null.");
                }

                long current = Properties.Settings.Default.MediaCounter;
                temp = Path.Combine(Global.deafultScreenShotPath, $"shot{current}.png");
                Properties.Settings.Default.MediaCounter = current + 1;
                Properties.Settings.Default.Save();

                temp = Path.Combine(Global.deafultScreenShotPath, "shot" + Properties.Settings.Default.MediaCounter + ".png");


                command = command + " " + '"' + temp + '"';
                System.Windows.MessageBox.Show(command);
                Debug.WriteLine(command);
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = nircmdPath,
                Arguments = command,
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }

        public static void application_command()
        {

        }

        public static void file_command()
        {

        }
    }
}
