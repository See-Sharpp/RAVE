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

        public static void systemCommand(string command, string search_query)
        {
            try
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
                    MessageBox.Show("" + current);
                    temp = Path.Combine(Global.deafultScreenShotPath, $"shot{current}.png");
                    Properties.Settings.Default.MediaCounter = current + 1;
                    Properties.Settings.Default.Save();
                    MessageBox.Show("" + Properties.Settings.Default.MediaCounter);

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
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        }

        public static void application_command()
        {

        }

        public static void file_command()
        {

        }

        public static void searchBrowser(string command,string search_query)
        {
            try
            {
                //    string url = "https://www.google.com/search?q=" + Uri.EscapeDataString(search_query);

                string urlPath = command.Replace("{{q}}", Uri.EscapeDataString((search_query)));
                string powershellPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell\\v1.0\\powershell.exe");
                MessageBox.Show(urlPath);
                int urlIndex = command.IndexOf("http");

                if (urlIndex!=-1)
                {
                   
                    string prefix = urlPath.Substring(0, urlIndex);
                    string url = urlPath.Substring(urlIndex);
                    MessageBox.Show(""+url);
                    urlPath = $"{prefix}\"{url}\"";
                }
                MessageBox.Show(urlPath);
                Process.Start(new ProcessStartInfo
                {
                    FileName = powershellPath,
                    Arguments = $"-NoProfile -Command {urlPath}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        }
    }
}
