using IWshRuntimeLibrary;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
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
                    return;
                }

                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key == null)
                    {
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
            catch (SecurityException ex)
            {
                try
                {
                    System.IO.File.AppendAllText("fatal.log", $"[AutoStartHelper:Enable] {DateTime.Now} - {ex}\n");
                }
                catch { /* Swallow logging errors */ }
            }
            catch (Exception ex)
            {
                try
                {
                    System.IO.File.AppendAllText("fatal.log", $"[AutoStartHelper:Enable] {DateTime.Now} - {ex}\n");
                }
                catch { /* Swallow logging errors */ }
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

        public static async Task<String> EnsureNirCmdExistsAsync()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string raveAppFolder = Path.Combine(appDataPath, "RaveApp");
            Directory.CreateDirectory(raveAppFolder);
            string nircmdExePath = Path.Combine(raveAppFolder, "nircmd.exe");

            if(System.IO.File.Exists(nircmdExePath))
            {
                return nircmdExePath;
            }

            string zipUrl = "https://www.nirsoft.net/utils/nircmd.zip";
            string tempZipPath = Path.Combine(raveAppFolder, "nircmd_temp.zip");

            try
            {
                using (var httpClient = new HttpClient())
                {

                    byte[] fileBytes = await httpClient.GetByteArrayAsync(zipUrl);
                    await System.IO.File.WriteAllBytesAsync(tempZipPath, fileBytes);
                }
                ZipFile.ExtractToDirectory(tempZipPath, raveAppFolder);
                System.IO.File.Delete(tempZipPath);

                if (System.IO.File.Exists(nircmdExePath))
                {
                    return nircmdExePath;
                }
                else
                {
                    throw new Exception("NirCmd download or extraction failed.");
                }
            }
            catch (Exception ex)
            {
                try
                {
                    System.IO.File.AppendAllText("fatal.log", $"[AutoStartHelper:EnsureNirCmdExistsAsync] {DateTime.Now} - {ex}\n");
                }
                catch { /* Swallow logging errors */ }
                throw new Exception("Failed to ensure NirCmd exists.", ex);
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
                try
                {
                    System.IO.File.AppendAllText("fatal.log", $"[AutoStartHelper:Enable] {DateTime.Now} - {ex}\n");
                }
                catch { /* Swallow logging errors */ }
            }
        }

        //public static void CreateDesktopShortcut()
        //{
        //    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        //    string shortcutPath = Path.Combine(desktopPath, "RAVE.lnk");
        //    string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";

        //    if (!System.IO.File.Exists(shortcutPath))
        //    {
        //        try
        //        {
        //            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
        //            if (shellType == null)
        //            {
        //                MessageBox.Show("WScript.Shell not found.");
        //                return;
        //            }

        //            dynamic shell = Activator.CreateInstance(shellType)!;
        //            dynamic shortcut = shell.CreateShortcut(shortcutPath);

        //            shortcut.TargetPath = exePath;
        //            shortcut.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
        //            shortcut.Description = "RAVE Shortcut";
        //            shortcut.IconLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "RAVE2.ico");
        //            shortcut.Save();

        //            MessageBox.Show("Shortcut created successfully.");
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show("Failed to create shortcut: " + ex.Message);
        //        }
        //    }
        //}
    }
}