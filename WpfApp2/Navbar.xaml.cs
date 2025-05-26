using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.ComponentModel;

namespace WpfApp2
{
    public partial class Navbar : MetroWindow 
    {
        private NotifyIcon _notifyIcon = null!; 

        public Navbar()
        {
            InitializeComponent();
            SetupTrayIcon(); 
            MainContentFrame.Navigate(new Dashboard()); // Set initial page
            System.Windows.MessageBox.Show(""+Global.UserId);
        }

        private void SetupTrayIcon()
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "RAVE2.ico");

            // Check if the icon file exists
            if (!File.Exists(iconPath))
            {
                // Handle the case where the icon is not found.
                System.Windows.MessageBox.Show($"Warning: RAVE2.ico not found at {iconPath}. Tray icon might not display correctly.",
                                               "Icon Missing", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                _notifyIcon = new NotifyIcon { Visible = true, Text = "RAVE" }; // Create with default icon
            }
            else
            {
                _notifyIcon = new NotifyIcon
                {
                    Icon = new Icon(iconPath),
                    Visible = true,
                    Text = "RAVE"
                };
            }

            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                }
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show", null, (s, e) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal; 
                this.Activate();
            });
            contextMenu.Items.Add("Exit", null, (s, e) =>
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose(); 
                System.Windows.Application.Current.Shutdown();
            });
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

      
        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true; // Prevent the window from actually closing
            this.Hide();     // Hide the window
            if (_notifyIcon != null)
            {
                _notifyIcon.ShowBalloonTip(500, "RAVE", "Running in background", ToolTipIcon.Info);
            }
        }

        private void NavHomeButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentFrame.Navigate(new Dashboard());
        }

        private void NavHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentFrame.Navigate(new History());
        }

        private void NavScanButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void NavHowItWorksButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentFrame.Navigate(new HowItWorks());
        }


        public void RemoveTrayIcon()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null!;
            }
        }

        private void NavLogoutButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Are you sure to LogOut ? ");
            Global.UserId = null;
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            RemoveTrayIcon();
            this.Close();
        }
    }
}