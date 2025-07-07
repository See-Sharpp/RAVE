using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Windows;

namespace WpfApp2
{
    internal class AutoStartHelper
    {
        public static void EnableAutoStart(bool enable,string appName = "RAVE")
        {
            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WpfApp2.exe");
            MessageBox.Show(exePath);
            RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);

            if (enable)
            {

                key?.SetValue(appName, $"\"{exePath}\"");
            }
            else
            {
                key?.DeleteValue(appName, false);
            }
        }
        
        public static bool IsAutoStartEnabled(string appName = "RAVE")
        {
            using var value = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
            return value?.GetValue(appName) is not null;
        }
    }
}
