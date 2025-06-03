using MahApps.Metro.Controls;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Shapes;
using WpfApp2.Context;
using WpfApp2.Models;

namespace WpfApp2
{
    public partial class Navbar : MetroWindow 
    {
        private NotifyIcon _notifyIcon = null!;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isScanning = false;

        private ApplicationDbContext _context = new ApplicationDbContext();
            readonly string[] excludedPaths = new string[]
            {
                @"C:\Windows",
                @"C:\Program Files",
                @"C:\Program Files (x86)",
                @"C:\ProgramData",
                @"C:\$Recycle.Bin",
                @"C:\Recovery",
                @"C:\System Volume Information",
                @"C:\PerfLogs",
                @"C:\Users\Default",
                @"C:\Users\Default User",
                @"C:\Users\All Users",
                @"C:\Users\Public",
                @"C:\Intel",
                @"C:\Config.Msi",
                @"C:\MSOCache",
                @"C:\OneDriveTemp",
                @"C:\Boot",
                @"C:\DumpStack.log.tmp",
                @"C:\Documents and Settings",
                @"C:\SysReset",               // Windows Reset leftover
                @"C:\Drivers",                // OEM driver folders
                @"C:\OEM",                    // System builder files
                @"C:\$WINDOWS.~BT",           // Windows upgrade leftovers
                @"C:\$WINDOWS.~WS",           // Another upgrade folder
                @"C:\Windows.old",            // Previous OS after upgrade
                @"C:\EFI",                    // EFI System Partition (on FAT32 partition)
                @"C:\Boot",                   // Boot loader files
                @"C:\RecoveryImage",          // On some OEMs like Dell
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
          string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "RAVE2.ico");


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
            System.Windows.MessageBox.Show("Scanning Started");
            NavScanButton.IsEnabled = false;

            
            var spinner = GetScanSpinner();
            if (spinner != null)
            {
                spinner.IsActive = true;
                spinner.Visibility = Visibility.Visible;
            }

            if (_isScanning)
            {
                System.Windows.MessageBox.Show("Scan already in progress. Canceling...");
                _cancellationTokenSource?.Cancel();
                if (spinner != null)
                {
                    spinner.IsActive = false;
                    spinner.Visibility = Visibility.Collapsed;
                }
                return;
            }

            _isScanning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _cancellationTokenSource.Token;

            try
            {
                await Task.Run(() => ParallelScanDirectoryForExe(@"C:\", token));
            }
            catch (OperationCanceledException)
            {
                System.Windows.MessageBox.Show("Scan was canceled.");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Unexpected error: {ex.Message}");
            }
            finally
            {
                _isScanning = false;
                if (spinner != null)
                {
                    spinner.IsActive = false;
                    spinner.Visibility = Visibility.Collapsed;
                }
                _cancellationTokenSource = null;
            }

            NavScanButton.IsEnabled = true;
        }

        private ProgressRing GetScanSpinner()
        {
            // Get the spinner from the button template
            var template = NavScanButton.Template;
            if (template != null)
            {
                return template.FindName("PART_ScanSpinner", NavScanButton) as ProgressRing;
            }
            return null;
        }

        private void ParallelScanDirectoryForExe(string rootPath, CancellationToken token)
        {
            var exeFiles = new ConcurrentBag<(string path, FileInfo info)>();
            var directories = new Stack<string>();
            directories.Push(rootPath);

            while (directories.Count > 0)
            {
                token.ThrowIfCancellationRequested();

                var currentDir = directories.Pop();
                if (IsExcludedPath(currentDir))
                    continue;

                try
                {
                    foreach (var subDir in Directory.GetDirectories(currentDir))
                    {
                        directories.Push(subDir);
                    }

                    var files = Directory.GetFiles(currentDir, "*.exe");

                    Parallel.ForEach(files, new ParallelOptions { CancellationToken = token }, file =>
                    {
                        try
                        {
                            FileInfo fi = new FileInfo(file);
                            exeFiles.Add((file, fi));
                        }
                        catch { }
                    });
                }
                catch { }
            }

            SaveToDatabase(exeFiles.ToList());
        }

        private void SaveToDatabase(List<(string path, FileInfo info)> fileList)
        {
            int userId = Global.UserId ?? 0;
            var user = _context.SignUpDetails.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                System.Windows.MessageBox.Show("User not found.");
                return;
            }

            foreach(var (path,info) in fileList)
            {
                try
                {
                
                    if (_context.ChangeTracker.Entries<AllExes>().Any(e => e.Entity.FilePath == path))
                        continue;

                    if (_context.AllExes.Any(x => x.FilePath == path && x.SignUpDetail.Id == userId))
                        continue;


                    var exes = new AllExes
                    {
                        FilePath = path,
                        SignUpDetail = user,
                        FileName = info.Name,
                        FileSize = FormatFileSize(info.Length),
                        LastWriteTime = info.LastWriteTime,
                        LastAccessTime = info.LastAccessTime,
                        CreatedAt = info.CreationTime
                    };

                    _context.AllExes.Add(exes);
                }
                catch(Exception e)
                {
                    System.Windows.MessageBox.Show($"Failed to process {path}: {e.Message}");
                }
            }

           
            _context.SaveChanges();
            System.Windows.MessageBox.Show($"Saved {fileList.Count} .exe files to database.");

           
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