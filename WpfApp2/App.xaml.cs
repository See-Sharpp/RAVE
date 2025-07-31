using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.IO;
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

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            const string name = "RAVE_INSTANCE";
            bool createdNew;

            try
            {
                var mutexSecurity = new MutexSecurity();
                mutexSecurity.AddAccessRule(new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow));
                _mutex = new Mutex(true, $"Global\\{name}", out createdNew);

                if (!createdNew)
                {
                    MessageBox.Show("Another instance of the application is already running.", "Application Already Running", MessageBoxButton.OK, MessageBoxImage.Information);
                    Shutdown();
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"A critical error occurred while checking for other instances of the application. Please restart your computer.\n\nDetails: {ex.Message}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create the default screenshot directory. Screenshots may not save correctly.\n\nPlease check your permissions for the folder.\n\nDetails: {ex.Message}", "Directory Creation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            try
            {
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
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during initial setup (auto-start, shortcut, or settings).\n\nPlease run the application as an administrator to resolve this issue.\n\nDetails: {ex.Message}", "Setup Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try { File.AppendAllText("fatal.log", $"[Domain] {DateTime.Now} - {e.ExceptionObject}\n"); } catch { }
            };

            DispatcherUnhandledException += (s, e) =>
            {
                try { File.AppendAllText("fatal.log", $"[Dispatcher] {DateTime.Now} - {e.Exception}\n"); } catch { }
                e.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                try { File.AppendAllText("fatal.log", $"[Task] {DateTime.Now} - {e.Exception}\n"); } catch { }
                e.SetObserved();
            };

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    context.Database.Migrate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"A critical error occurred while initializing the local database. The application may not function correctly.\n\nDetails: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }

            AddEnvironmentVariable();
        }

        public void AddEnvironmentVariable()
        {
            AddEnvironmentPath.AddNircmd();
        }
    }
}