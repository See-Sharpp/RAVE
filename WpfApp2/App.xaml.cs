using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp2
{
    public partial class App : Application
    {
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
        }
    }
}
