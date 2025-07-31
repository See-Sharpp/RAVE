using IWshRuntimeLibrary;
using Microsoft.Win32;
using System;
using System.IO;
using System.Security;
using System.Windows;

namespace WpfApp2
{
    internal class AutoStartHelper
    {
        private static readonly string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";

        public static void EnableAutoStart(bool enable, string appName = "RAVE")
        {
            try
            {
                if (string.IsNullOrEmpty(exePath))
                {
                    MessageBox.Show("Could not determine the application path. Auto-start feature cannot be changed.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key == null)
                    {
                        MessageBox.Show("Unable to access the registry for auto-start configuration.", "Registry Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (enable)
                    {
                        key.SetValue(appName, $"\"{exePath}\"");
                    }
                    else
                    {
                        key.DeleteValue(appName, false);
                    }
                }
            }
            catch (SecurityException)
            {
                MessageBox.Show("Permission to modify auto-start settings was denied.\n\nPlease try running the application as an administrator.", "Permission Denied", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred while changing auto-start settings.\n\nDetails: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static bool IsAutoStartEnabled(string appName = "RAVE")
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return key?.GetValue(appName) is not null;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void CreateDesktopShortcut()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string shortcutPath = Path.Combine(desktopPath, "RAVE.lnk");

                if (!System.IO.File.Exists(shortcutPath))
                {
                    if (string.IsNullOrEmpty(exePath))
                    {
                        MessageBox.Show("Could not determine the application path. Shortcut was not created.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    WshShell shell = new WshShell();
                    IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

                    shortcut.TargetPath = exePath;
                    shortcut.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    shortcut.Description = "RAVE Shortcut";
                    shortcut.IconLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "RAVE2.ico");
                    shortcut.Save();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create a desktop shortcut.\nThis can happen due to system permissions or a missing component.\n\nDetails: {ex.Message}", "Shortcut Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}