using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.ComponentModel;
using WpfApp2.Models;
using WpfApp2.Context;

namespace WpfApp2
{
    public partial class Navbar : MetroWindow 
    {
        private NotifyIcon _notifyIcon = null!;
        DriveInfo[] drives = DriveInfo.GetDrives();
        List<string> allExeFiles = new List<string>();
        List<List<string>> metadata = new List<List<string>>();

        private ApplicationDbContext _context = new ApplicationDbContext();

        readonly string[] excludedPaths = new string[]
       {
            @"C:\Windows",
            @"C:\Program Files",
            @"C:\Program Files (x86)",
            @"C:\$Recycle.Bin",
            @"C:\Recovery"
       };

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

        private async void NavScanButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Scanning for .exe files in C drive...");

            string startDirectory = @"C:\";

            allExeFiles.Clear();

            try
            {
                ScanDirectoryForExe(startDirectory);
                System.Windows.MessageBox.Show($"Found {allExeFiles.Count} useful .exe files in C drive.");

              
                string Path = @"C:\Users\YASH SOLANKI\AppData\Local\Programs\Microsoft VS Code\Code.exe";


                foreach(var exes in allExeFiles)
                {
                    string filepath = exes;
                    int id = Global.UserId ?? 0; 
                    int index = allExeFiles.IndexOf(exes);
                    string filename = null!;
                    string size = null!;
                    string lastWriteTime = null!;
                    string lastAccessTime = null!;
                    string createdTime = null!;
                    int i = 0;

                    foreach (var meta in metadata[index])
                    {
                        if (i == 0)
                        {
                            size = meta; // File size
                        }
                        else if(i == 1)
                        {
                            filename = meta; // File name
                        }
                        else if (i == 2)
                        {
                            lastWriteTime = meta; // Last write time
                        }
                        else if (i == 3)
                        {
                            lastAccessTime = meta; // Last access time
                        }
                        else if (i == 4)
                        {
                            createdTime = meta; // Creation time
                        }
                        i++;
                    }
                    try
                    {

                        AllExes allExes = new AllExes
                        {

                            FilePath = filepath,
                            SignUpDetail = _context.SignUpDetails.FirstOrDefault(u => u.Id == id),
                            FileName = filename,
                            FileSize = size,
                            LastWriteTime = DateTime.Parse(lastWriteTime),
                            LastAccessTime = DateTime.Parse(lastAccessTime),
                            CreatedAt = DateTime.Parse(createdTime)
                        };

                        _context.AllExes.Add(allExes);
                        await _context.SaveChangesAsync(); // Save to database

                        System.Windows.MessageBox.Show("Database updated");
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Error saving file {exes} to database: {ex.Message}");
                    }

                }
                

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Unexpected error: {ex.Message}");
            }
        }

        private void ScanDirectoryForExe(string path)
        {
            try
            {
                if (IsExcludedPath(path))
                    return;

                foreach (var file in Directory.EnumerateFiles(path, "*.exe"))
                {
                    try
                    {
                        FileInfo fi = new FileInfo(file);

                        allExeFiles.Add(file);
                        List<string> meta = new List<string>();
                        meta.Add(FormatFileSize(fi.Length));
                        meta.Add(fi.Name);
                        meta.Add(fi.LastWriteTime.ToString("yyyy-MM-dd"));
                        meta.Add(fi.LastAccessTime.ToString("yyyy-MM-dd"));
                        meta.Add(fi.CreationTime.ToString("yyyy-MM-dd"));
                        metadata.Add(meta);
                    }
                    catch { /* Skip inaccessible files */ }
                }

                foreach (var dir in Directory.GetDirectories(path))
                {
                    ScanDirectoryForExe(dir);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing {path}: {ex.Message}");
            }
        }

        private bool IsExcludedPath(string path)
        {
            DirectoryInfo dir = new DirectoryInfo(path);

            while (dir != null)
            {
                if (excludedPaths.Any(ex => dir.FullName.Equals(ex, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
                dir = dir.Parent;
            }

            return false;
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
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
            MessageBoxResult result = System.Windows.MessageBox.Show(
            "Are you sure you want to log out?",
            "Confirm Logout",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Global.UserId = null;
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                RemoveTrayIcon();
                this.Close();
            }
        }
    }
}