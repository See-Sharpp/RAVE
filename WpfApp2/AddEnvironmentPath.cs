using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Windows;

namespace WpfApp2
{
    public class AddEnvironmentPath
    {
        public static void AddNircmd()
        {
            try
            {
                string exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nircmd-x64");
                string systemPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);

                if (systemPath == null)
                {
                    Environment.SetEnvironmentVariable("PATH", exeDir, EnvironmentVariableTarget.User);
                    return;
                }

                if (!systemPath.Split(';').Any(p => p.Trim().Equals(exeDir, StringComparison.OrdinalIgnoreCase)))
                {
                    string newPath = systemPath + ";" + exeDir;
                    Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
                }
            }
            catch (SecurityException)
            {
                MessageBox.Show(
                    "The application does not have permission to modify your system's PATH variable.\n\nPlease try running this application as an Administrator.",
                    "Permission Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An unexpected error occurred while setting up application paths.\n\nDetails: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}