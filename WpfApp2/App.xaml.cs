using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using WpfApp2.Context;
using WpfApp2.Properties;

namespace WpfApp2
{
    public partial class App : Application
    {


        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var culture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            if (!string.IsNullOrEmpty(Global.deafultScreenShotPath) && !Directory.Exists(Global.deafultScreenShotPath))
            {
                Directory.CreateDirectory(Global.deafultScreenShotPath);
            }
            if (!WpfApp2.Properties.Settings.Default.AutoRegister)
            {
                AutoStartHelper.EnableAutoStart(true);
                WpfApp2.Properties.Settings.Default.AutoRegister = true;
                WpfApp2.Properties.Settings.Default.Save();
            }


        }

        public App()
        {
            // Catch unhandled exceptions in non-UI threads (e.g., Vosk callbacks)
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                File.AppendAllText("fatal.log", $"[Domain] {DateTime.Now} - {e.ExceptionObject}\n");
                MessageBox.Show("Fatal error:\n" + e.ExceptionObject.ToString(), "Unhandled Domain Exception");
            };

            // Catch unhandled exceptions on the UI thread
            DispatcherUnhandledException += (s, e) =>
            {
                File.AppendAllText("fatal.log", $"[Dispatcher] {DateTime.Now} - {e.Exception}\n");
                MessageBox.Show("UI error:\n" + e.Exception.Message, "Unhandled UI Exception");
                e.Handled = true; // Prevent app from crashing
            };

            // Catch unobserved exceptions from Tasks
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                File.AppendAllText("fatal.log", $"[Task] {DateTime.Now} - {e.Exception}\n");
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
