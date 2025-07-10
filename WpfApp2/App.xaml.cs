using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using WpfApp2.Context;
using System.Diagnostics;



namespace WpfApp2
{
    public partial class App : Application
    {
        public Mutex _mutex;


        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
               
                const string name = "RAVE_INSTANCE";

                bool createdNew;
                _mutex = new Mutex(true, name, out createdNew);

                if (!createdNew)
                {
                    Shutdown();
                    return;
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            var culture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            if (!string.IsNullOrEmpty(Global.deafultScreenShotPath) && !Directory.Exists(Global.deafultScreenShotPath))
            {
                Directory.CreateDirectory(Global.deafultScreenShotPath);
            }
            MessageBox.Show("in app.xaml.cs");
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
           


        }

        public App()
        {
            
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                System.IO.File.AppendAllText("fatal.log", $"[Domain] {DateTime.Now} - {e.ExceptionObject}\n");
                MessageBox.Show("Fatal error:\n" + e.ExceptionObject.ToString(), "Unhandled Domain Exception");
            };

            // Catch unhandled exceptions on the UI thread
            DispatcherUnhandledException += (s, e) =>
            {
                System.IO.File.AppendAllText("fatal.log", $"[Dispatcher] {DateTime.Now} - {e.Exception}\n");
                MessageBox.Show("UI error:\n" + e.Exception.Message, "Unhandled UI Exception");
                e.Handled = true; // Prevent app from crashing
            };

            // Catch unobserved exceptions from Tasks
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                System.IO.File.AppendAllText("fatal.log", $"[Task] {DateTime.Now} - {e.Exception}\n");
                MessageBox.Show("Task error:\n" + e.Exception.Message, "Unobserved Task Exception");
                e.SetObserved();
            };

            using (var context = new ApplicationDbContext())
            {
                context.Database.Migrate();
            }
            AddEnvironmentVariable();

           
        }

        public void AddEnvironmentVariable()
        {
            AddEnvironmentPath.AddNircmd();
        }
    }
}
