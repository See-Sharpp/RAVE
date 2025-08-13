using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq; // Added for .Any()
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WpfApp2.Context;

namespace WpfApp2
{
    public partial class App : Application
    {
        public Mutex? _mutex;

        protected async override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string raveAppFolder = Path.Combine(appDataPath, "RaveApp");
            Directory.CreateDirectory(raveAppFolder); 

          
            string nircmdExePath = Path.Combine(raveAppFolder, "nircmd.exe");
            string? nircmdParentFolder = Path.GetDirectoryName(nircmdExePath);

  
            try
            {
                if (File.Exists(nircmdExePath))
                {
                    Global.nircmdPath = nircmdExePath;
                }
                else
                {
                    
                    Global.nircmdPath = await AutoStartHelper.EnsureNirCmdExistsAsync();
                }

                AddDirectoryToUserPath(nircmdParentFolder); 
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to download a required component (NirCmd). The application will now close.\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            const string name = "RAVE_INSTANCE";
            bool createdNew;

            try
            {
                var mutexSecurity = new MutexSecurity();
                mutexSecurity.AddAccessRule(new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow));
                _mutex = new Mutex(true, $"Global\\{name}", out createdNew);

                if (!createdNew)
                {
                    Shutdown();
                    return;
                }
            }
            catch (Exception)
            {
                Shutdown();
                return;
            }

            // 4. Set Culture Info
            var culture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            
            try
            {
              
                if (!string.IsNullOrEmpty(Global.deafultScreenShotPath) && !Directory.Exists(Global.deafultScreenShotPath))
                {
                    Directory.CreateDirectory(Global.deafultScreenShotPath);
                }

               
                if (!WpfApp2.Properties.Settings.Default.AutoRegister || !WpfApp2.Properties.Settings.Default.ShortcutCreated)
                {
                    AutoStartHelper.EnableAutoStart(true);
                    AutoStartHelper.CreateDesktopShortcut();
                    WpfApp2.Properties.Settings.Default.AutoRegister = true;
                    WpfApp2.Properties.Settings.Default.ShortcutCreated = true;
                    WpfApp2.Properties.Settings.Default.Save();
                }

               
                Global.autoOpen = true;
                WpfApp2.Properties.Settings.Default.Is_First = false;
                WpfApp2.Properties.Settings.Default.Save();
                Global.Scanning = WpfApp2.Properties.Settings.Default.Scanning_Time;
                Global.userName = WpfApp2.Properties.Settings.Default.UserName ?? "User";
            }
            catch (Exception)
            {
           
            }
        }

        public App()
        {
           
            AppDomain.CurrentDomain.UnhandledException += (s, e) => LogException(e.ExceptionObject?.ToString());
            DispatcherUnhandledException += (s, e) => { LogException(e.Exception?.ToString()); e.Handled = true; };
            TaskScheduler.UnobservedTaskException += (s, e) => { LogException(e.Exception?.ToString()); e.SetObserved(); };

            
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    context.Database.Migrate();
                }
            }
            catch (Exception ex)
            {
                LogException("CRITICAL: Database migration failed on startup. " + ex.Message);
            }
        }

   
        private void AddDirectoryToUserPath(string directoryPath)
        {
            try
            {
                string pathVariable = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
                var allPaths = pathVariable.Split(';').ToList();

                if (!allPaths.Any(p => p.Trim().Equals(directoryPath, StringComparison.OrdinalIgnoreCase)))
                {
                    string newPath = pathVariable.TrimEnd(';') + ";" + directoryPath;
                    Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
                    Debug.WriteLine($"'{directoryPath}' added to User PATH.");
                }
            }
            catch (Exception ex)
            {
                LogException("Failed to modify PATH variable: " + ex.Message);
            }
        }

        
        private void LogException(string? message)
        {
            if (string.IsNullOrEmpty(message)) return;
            try
            {
                File.AppendAllText("fatal.log", $"[{DateTime.Now}] - {message}\n");
            }
            catch
            {
              
            }
        }
    }
}
