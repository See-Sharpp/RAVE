using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using WpfApp2.Context;
using WpfApp2.Models;
using System.Data.OleDb;


namespace WpfApp2
{
    public partial class Navbar : MetroWindow
    {
        private NotifyIcon _notifyIcon = null!;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isScanning = false;
        private int minFilesize = 1024;

        private ApplicationDbContext _context = new ApplicationDbContext();
        private int totalExes = 0;
        private int totalDocs = 0;
        private List<FileSystemWatcher> _watchers = new();
        readonly string[] excludedPaths = new string[]
            {
                @"C:\Windows",
                @"C:\ProgramData",
                @"C:\$Recycle.Bin",
                @"C:\Recovery",
                @"C:\System Volume Information",
                @"C:\PerfLogs",
                @"C:\Program Files",
                @"C:\Program Files (x86)",
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
                @"C:\SysReset",
                @"C:\Drivers",
                @"C:\OEM",
                @"C:\$WINDOWS.~BT",
                @"C:\$WINDOWS.~WS",
                @"C:\Windows.old",
                @"C:\EFI",
                @"C:\Boot",
                @"C:\RecoveryImage",
            };

        private Commands _commands = new Commands();

        private static readonly List<Regex> ignorePatterns = new()
        {
            new Regex(@"\\Microsoft Visual Studio\\", RegexOptions.IgnoreCase),
            new Regex(@"\\Dev-Cpp\\|\\MinGW\\", RegexOptions.IgnoreCase),
            new Regex(@"\\Windows Kits\\", RegexOptions.IgnoreCase),
            new Regex(@"\\AppData\\Local\\", RegexOptions.IgnoreCase),
            new Regex(@"\\Python\d*", RegexOptions.IgnoreCase),
            new Regex(@"site-packages|venv|_testembed", RegexOptions.IgnoreCase),
            new Regex(@"\\Adobe\\|\\Redist\\|\\Extras\\|\\Installer\\", RegexOptions.IgnoreCase),
            new Regex(@"\\MySQL\\.*\\lib\\Python", RegexOptions.IgnoreCase),
            new Regex(@"\\Common7\\IDE\\CommonExtensions\\Microsoft\\FSharp", RegexOptions.IgnoreCase),
        };


        public Navbar()
        {
            InitializeComponent();
            SetupTrayIcon();

            if (!Properties.Settings.Default.InitialScan)
            {
                InitialScan();
            }
            else
            {
                StartExeWatchers();
            }
            MainContentFrame.Navigate(new Dashboard());




        }
        private static bool IsIgnoredExe(string path)
        {
            foreach (var pattern in ignorePatterns)
            {
                if (pattern.IsMatch(path))
                    return true;
            }
            return false;
        }


        private void StartExeWatchers()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    var watcher = new FileSystemWatcher
                    {
                        Path = drive.RootDirectory.FullName,
                        Filter = "*.exe",
                        IncludeSubdirectories = true,
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite
                    };

                    watcher.Created += OnExeCreated;
                    watcher.Deleted += OnExeDeleted;
                    watcher.Renamed += OnExeRenamed;

                    watcher.EnableRaisingEvents = true;
                    _watchers.Add(watcher);
                }
            }
        }

        private void OnExeCreated(object sender, FileSystemEventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                FileInfo info = new FileInfo(e.FullPath);
                if (!info.Exists || info.Length < minFilesize)
                    return;

                int userId = Global.UserId ?? 0;
                var user = _context.SignUpDetails.FirstOrDefault(u => u.Id == userId);
                if (user == null) return;

                if (_context.AllExes.Any(x => x.FilePath == e.FullPath && x.SignUpDetail.Id == userId))
                    return;

                var versionInfo = FileVersionInfo.GetVersionInfo(e.FullPath);
                string displayName = versionInfo.FileDescription ?? info.Name;

                var exes = new AllExes
                {
                    FilePath = e.FullPath,
                    SignUpDetail = user,
                    FileName = info.Name,
                    DisplayName = displayName,
                    FileSize = FormatFileSize(info.Length),
                    LastWriteTime = info.LastWriteTime,
                    LastAccessTime = info.LastAccessTime,
                    CreatedAt = info.CreationTime
                };

                _context.AllExes.Add(exes);
                _context.SaveChanges();
            }));
        }


        private void OnExeDeleted(object sender, FileSystemEventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                int userId = Global.UserId ?? 0;
                var existing = _context.AllExes.FirstOrDefault(x => x.FilePath == e.FullPath && x.SignUpDetail.Id == userId);
                if (existing != null)
                {
                    _context.AllExes.Remove(existing);
                    _context.SaveChanges();
                }
            });
        }

        private void OnExeRenamed(object sender, RenamedEventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                if (Path.GetExtension(e.OldFullPath)?.ToLower() == ".exe")
                {
                    OnExeDeleted(sender, new FileSystemEventArgs(WatcherChangeTypes.Deleted, Path.GetDirectoryName(e.OldFullPath)!, Path.GetFileName(e.OldFullPath)));
                }

                if (Path.GetExtension(e.FullPath)?.ToLower() == ".exe")
                {
                    OnExeCreated(sender, new FileSystemEventArgs(WatcherChangeTypes.Created, Path.GetDirectoryName(e.FullPath)!, Path.GetFileName(e.FullPath)));
                }
            }));
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
                    if (Global.floatingIcon != null)
                    {
                        Global.floatingIcon.Hide();
                    }

                }
            };

            var contextMenu = new ContextMenuStrip();

            contextMenu.Items.Add("Widgets", null, (s, e) =>
            {
                if (Global.floatingIcon != null)
                {
                    Global.floatingIcon.Show();
                }
            });

            contextMenu.Items.Add("Show", null, (s, e) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal; 
                this.Activate();
                if (Global.floatingIcon != null)
                {
                    Global.floatingIcon.Hide();
                }
               

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
            e.Cancel = true;
            this.Hide();
            if (_notifyIcon != null)
            {
                _notifyIcon.ShowBalloonTip(500, "RAVE", "Running in background", ToolTipIcon.Info);
            }
            try
            {
                if (Global.floatingIcon != null && !Global.logout)
                {
                    Global.floatingIcon.Show();
                }
            }
            catch (Exception ex)
            {
               
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
            var spinner = GetScanSpinner();
            var result = await scanMessage("Scanning files might take some time. Would you like to continue?\r\n\r\n");

            if (result)
            {
                await scanMessageConfirm("Scanning Started...");
                if (spinner != null)
                {
                    spinner.IsActive = true;
                    spinner.Visibility = Visibility.Visible;
                }
                NavScanButton.IsEnabled = false;
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
                await Task.Run(() =>
                {
                    foreach(var driver in DriveInfo.GetDrives())
                    {
                        try
                        {
                            if(driver.IsReady && driver.DriveType== DriveType.Fixed)
                            {
                                ParallelScanDirectoryForFiles(driver.RootDirectory.FullName, token);
                            }
                        }
                        catch (Exception ex)
                        {
                           
                            continue;
                        }
                    }
                    System.Windows.Application.Current.Dispatcher.Invoke(async () =>
                    {
                        await scanMessageConfirm($"Saved {totalExes} exe files and {totalDocs} docs to database.");
                    });
                }
                );
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
            else
            {
                return;
            }
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

        private void ParallelScanDirectoryForFiles(string rootPath, CancellationToken token)
        {
            var exeFiles = new ConcurrentBag<(string path, FileInfo info)>();

            var docFiles = new ConcurrentBag<(string path, FileInfo info)>();

            var directories = new Stack<string>();

            var extensions = new[] { ".exe", ".pdf", ".docx", ".pptx" };
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
                        if (!IsExcludedPath(subDir))
                            directories.Push(subDir);
                    }

                    var files = Directory.GetFiles(currentDir).Where(f => extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase)).ToArray();

                    Parallel.ForEach(files, new ParallelOptions { CancellationToken = token }, file =>
                    {
                        try
                        {
                            if (IsExcludedPath(Path.GetDirectoryName(file)!))
                                return;
                            FileInfo fi = new FileInfo(file);

                            if (IsIgnoredExe(fi.FullName)) return;

                            if (fi.Length < minFilesize) return;

                            var ext = Path.GetExtension(file).ToLower();

                            if(ext == ".exe")
                            {
                                exeFiles.Add((file, fi));
                            }
                            else
                            {
                                docFiles.Add((file, fi));
                            }

                        }
                        catch { }
                    });
                }
                catch { }
            }

            SaveToDatabase(exeFiles.ToList());
            SaveToDatabase(docFiles.ToList());
        }

        private async void SaveToDatabase(List<(string path, FileInfo info)> fileList)
        {
            int userId = Global.UserId ?? 0;
            var user = _context.SignUpDetails.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                System.Windows.MessageBox.Show("User not found.");
                return;
            }

            var (firstPath, _) = fileList[0];

            if (Path.GetExtension(firstPath) == ".exe")
            {

                foreach (var (path, info) in fileList)
                {
                    try
                    {

                        if (_context.ChangeTracker.Entries<AllExes>().Any(e => e.Entity.FilePath == path))
                            continue;

                        if (_context.AllExes.Any(x => x.FilePath == path && x.SignUpDetail.Id == userId))
                            continue;

                        var versionInfo = FileVersionInfo.GetVersionInfo(path);
                        string displayName = versionInfo.FileDescription ?? info.Name;
                        float[] embedding = Commands.GetEmbedding(displayName);
                        string embeddingString = string.Join(",", embedding.Select(x => x.ToString("F4")));
                        var exes = new AllExes
                        {
                            FilePath = path,
                            SignUpDetail = user,
                            FileName = info.Name,
                            DisplayName = displayName,
                            FileSize = FormatFileSize(info.Length),
                            LastWriteTime = info.LastWriteTime,
                            LastAccessTime = info.LastAccessTime,
                            CreatedAt = info.CreationTime,
                            Embedding = embeddingString
                        };


                        _context.AllExes.Add(exes);
                    }
                    catch (Exception e)
                    {
                        System.Windows.MessageBox.Show($"Failed to process {path}: {e.Message}");
                    }
                }
                _context.SaveChanges();
                totalExes += fileList.Count;
            }
            else
            {
                foreach (var (path, info) in fileList)
                {
                    try
                    {

                        if (_context.ChangeTracker.Entries<AllExes>().Any(e => e.Entity.FilePath == path))
                            continue;

                        if (_context.AllExes.Any(x => x.FilePath == path && x.SignUpDetail.Id == userId))
                            continue;

                        var versionInfo = FileVersionInfo.GetVersionInfo(path);
                        string displayName = versionInfo.FileDescription ?? info.Name;
                        float[] embedding = Commands.GetEmbedding(displayName);
                        string embeddingString = string.Join(",", embedding.Select(x => x.ToString("F4")));

                        var docs = new AllDocs
                        {
                            FilePath = path,
                            SignUpDetail = user,
                            FileName = info.Name,
                            DisplayName = displayName,
                            FileSize = FormatFileSize(info.Length),
                            LastWriteTime = info.LastWriteTime,
                            LastAccessTime = info.LastAccessTime,
                            CreatedAt = info.CreationTime,
                            Embedding = embeddingString
                        };


                        _context.AllDocs.Add(docs);
                    }
                    catch (Exception e)
                    {
                        System.Windows.MessageBox.Show($"Failed to process {path}: {e.Message}");
                    }
                }
                _context.SaveChanges();
                totalDocs += fileList.Count;
            }

  
        }

        private bool IsExcludedPath(string path)
        {
            string normalized = Path.GetFullPath(path).TrimEnd('\\');

            foreach (var ex in excludedPaths)
            {
                string excluded = Path.GetFullPath(ex).TrimEnd('\\');

                if (normalized.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
                    return true;
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

        private async void NavLogoutButton_Click(object sender, RoutedEventArgs e)
        {
            await logoutMessage("Are you sure you want to log out?");

        }

        private async Task logoutMessage(string message)
        {
            var metroWindow = Window.GetWindow(this) as MahApps.Metro.Controls.MetroWindow;
            var settings = new MahApps.Metro.Controls.Dialogs.MetroDialogSettings()
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No",
                AnimateShow = true,
                AnimateHide = false
            };


            if (metroWindow != null)
            {
                var result = await metroWindow.ShowMessageAsync("Confirm", message, MessageDialogStyle.AffirmativeAndNegative,settings);
                
                if (result == MessageDialogResult.Affirmative)
                {
                    Global.UserId = null;
                    Global.logout = true;
                    Properties.Settings.Default.RememberMe = false;
                    Properties.Settings.Default.UserId = 0;
                    Properties.Settings.Default.Save();

                    RemoveTrayIcon();
                 
                    await System.Windows.Application.Current.Dispatcher.BeginInvoke(
                                new Action(() =>
                                {
                                    var mainWindow = new MainWindow();
                                    mainWindow.Show();
                                    this.Close();

                                }),
                                System.Windows.Threading.DispatcherPriority.ApplicationIdle
                   );
                }
                else
                {
                    return;
                }
            }

        }

        private async Task<bool> scanMessage(string message)
        {
            
            MahApps.Metro.Controls.MetroWindow metroWindow = null;
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                metroWindow = Window.GetWindow(this) as MahApps.Metro.Controls.MetroWindow;
            });

            if (metroWindow == null)
            {
                return false;
            }

        
            var settings = new MahApps.Metro.Controls.Dialogs.MetroDialogSettings()
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No",
                AnimateShow = true,
                AnimateHide = false
            };

            var dispatcherOp = System.Windows.Application.Current.Dispatcher.InvokeAsync(
                () => metroWindow.ShowMessageAsync("Confirm", message,
                                                  MessageDialogStyle.AffirmativeAndNegative,
                                                  settings)
            );

           
            Task<MessageDialogResult> dialogTask = await dispatcherOp;

         
            MessageDialogResult result = await dialogTask;

            return (result == MessageDialogResult.Affirmative);
        }


        private async Task scanMessageConfirm(string message)
        {
            MahApps.Metro.Controls.MetroWindow metroWindow = null;

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                metroWindow = Window.GetWindow(this) as MahApps.Metro.Controls.MetroWindow;
            });

            if (metroWindow == null)
            {
                return ;
            }

            var dispatcherOp = System.Windows.Application.Current.Dispatcher.InvokeAsync(
               () => metroWindow.ShowMessageAsync("Information", message)
            );

        }

        private async void InitialScan()
        {

            await scanMessageConfirm("Starting initial scan for .exe files. This may take a while depending on the number of files on your system.");
            var spinner = GetScanSpinner();
            if (spinner != null)
            {
                spinner.IsActive = true;
                spinner.Visibility = Visibility.Visible;
            }
            try
            {
                await Task.Run(() =>
                {
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                        {
                            ParallelScanDirectoryForFiles(drive.RootDirectory.FullName, CancellationToken.None);
                        }
                    }
                });
                StartExeWatchers();
                Properties.Settings.Default.InitialScan = true;
                Properties.Settings.Default.Save();
                await System.Windows.Application.Current.Dispatcher.Invoke(async () =>
                {
                    await scanMessageConfirm($"Saved {totalExes} exe files to database.");
                });

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Initial scan failed: {ex.Message}");
            }
            finally
            {
                if (spinner != null)
                {
                    spinner.IsActive = false;
                    spinner.Visibility = Visibility.Collapsed;
                }
            }
        }



    }
}