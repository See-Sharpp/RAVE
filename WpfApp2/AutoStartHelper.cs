using IWshRuntimeLibrary;
using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Windows;

namespace WpfApp2
{
    internal class AutoStartHelper
    {
        static string  exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WpfApp2.exe");
        public static void EnableAutoStart(bool enable,string appName = "RAVE")
        { 
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

        public static void CreateDesktopShortcut()
        {
            string desktopPath=Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string shortcutPath = Path.Combine(desktopPath, "RAVE.lnk");
            MessageBox.Show("in AutoHelper");
            if (!System.IO.File.Exists(shortcutPath))
            {
                MessageBox.Show("creating icon");
                try
                {
                    WshShell shell = new WshShell();
                    IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

                    shortcut.TargetPath = exePath;
                    shortcut.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    shortcut.Description = "RAVE Shortcut";
                    shortcut.IconLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "RAVE2.ico");
                    shortcut.Save();
                    
                }
                catch { }

            }
        }
    }
}
