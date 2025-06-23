using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WpfApp2.Context;

namespace WpfApp2
{
    public class Commands
    {
        private string temp = null; // Changed to private instance field to fix CS0120
        public ApplicationDbContext _context;

        public Commands() { 
   
        }




        public static void systemCommand(string command, string search_query)
        {
            try
            {
                string temp = null; // Declare a local variable to avoid using the instance field in a static method
                string nircmdPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nircmd-x64", "nircmd.exe");

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

        public static void application_command(string application)
        {

            string connectionString = @"Provider=Search.CollatorDSO;Extended Properties='Application=Windows'";


            string query = $@"
                SELECT TOP 1 System.ItemName, System.ItemPathDisplay
                FROM SYSTEMINDEX
                WHERE System.ItemName LIKE '%{application}%'
            ";
            try
            {
                using (OleDbConnection connection = new OleDbConnection(connectionString))
                {
                    connection.Open();

                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    {
                        using (OleDbDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string name = reader["System.ItemName"].ToString();
                                string path = reader["System.ItemPathDisplay"].ToString();
                                System.Windows.MessageBox.Show($"Found:{path}");
                                Debug.WriteLine(path);
                                Process.Start("cmd.exe", $"/C start \"\" \"{path}\"");


                            }
                            else
                            {
                                SearchInDatabase(application);
                                System.Windows.MessageBox.Show($"Application '{application}' not found in the database.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error: " + ex.Message);
            }


        }
        public static void SearchInDatabase(string application)
        {
            try
            {
                using var _context = new ApplicationDbContext();

                var allExes = _context.AllExes.ToList();

                foreach (var ex in allExes)
                {
                    string? displayName = ex.DisplayName;
                    Debug.WriteLine(displayName);
                }
            }
            catch(Exception e)
            {

            }
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

                int urlIndex = command.IndexOf("http");

                if (urlIndex!=-1)
                {
                   
                    string prefix = urlPath.Substring(0, urlIndex);
                    string url = urlPath.Substring(urlIndex);
                    urlPath = $"{prefix}\"{url}\"";
                }

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
