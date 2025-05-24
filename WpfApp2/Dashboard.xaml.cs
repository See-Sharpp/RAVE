using MahApps.Metro.Controls;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Extensions.Configuration;

namespace WpfApp2
{
    public partial class Dashboard : MetroWindow
    {
        DriveInfo[] drives = DriveInfo.GetDrives();
        List<string> allExeFiles = new List<string>();
        List<List<string>> metadata = new List<List<string>>();

        private NotifyIcon _notifyIcon=null!;
        private WaveInEvent? waveIn;
        private WaveFileWriter? writer;
        private string outputFilePath = "temp_voice_input.wav";
        private TaskCompletionSource<bool> recordingStoppedTcs;
        private static string api = null;
        private WakeWordHelper? _wakeWordDetector;

        readonly string[] excludedPaths = new string[]
        {
            @"C:\Windows",
            @"C:\Program Files",
            @"C:\Program Files (x86)",
            $@"C:\Users\{Environment.UserName}\AppData",
            @"C:\$Recycle.Bin",
            @"C:\Recovery"
        };

       

        public Dashboard()
        {
            InitializeComponent();
            SetupTrayIcon();
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("AppSetting.json", optional: true, reloadOnChange: true)
                .Build();

            api = config["Groq_Api_Key"] ?? throw new InvalidOperationException("APIKey not found in configuration.");

            _wakeWordDetector = new WakeWordHelper("model/HEY_RAVE.onnx", OnWakeWordDetected);
            Task.Run(() => _wakeWordDetector.Start()); 


        }

        private void OnWakeWordDetected()
        {
            Dispatcher.Invoke(() =>
            {
                System.Windows.MessageBox.Show("Hey Rave Detected!");
               // ToggleVoice_Click(this, new RoutedEventArgs());
            });
        }



        private void SetupTrayIcon()
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "RAVE2.ico");
            _notifyIcon = new NotifyIcon
            {
                Icon = new Icon(iconPath), 
                Visible = true,
                Text = "RAVE"
            };
        
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
                this.WindowState = WindowState.Maximized;
                this.Activate();
            });
            contextMenu.Items.Add("Exit", null, (s, e) =>
            {
                _notifyIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            });
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
           e.Cancel = true;
            this.Hide(); 
            _notifyIcon.ShowBalloonTip(500, "RAVE", "Running in background", ToolTipIcon.Info);
        }

        private async void  ToggleVoice_Click(object sender, RoutedEventArgs e)
        {
            
            CommandInput.Clear();
            CommandInput.AppendText("Start Speaking ...");
            StartRecording();

            await Task.Delay(5000); // Simulate 5 seconds of recording

            await StopRecording();

            string result = await TranscribeAsync(outputFilePath);
            CommandInput.Clear();
            CommandInput.AppendText(result);
            System.Windows.MessageBox.Show(result);

        }

        private async Task<string> TranscribeAsync(string audioFilePath)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",api);


            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(File.ReadAllBytes(audioFilePath));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
            form.Add(fileContent, "file", "temp_voice_input.wav");
            form.Add(new StringContent("whisper-large-v3-turbo"), "model");

            var response = await httpClient.PostAsync("https://api.groq.com/openai/v1/audio/transcriptions", form);
            var responseJson = await response.Content.ReadAsStringAsync();

            dynamic result = JsonConvert.DeserializeObject(responseJson);
            return result?.text;
        }

        private void StartRecording()
        {

            if (waveIn != null)
            {
                waveIn.DataAvailable -= WaveIn_DataAvailable;
                waveIn.RecordingStopped -= WaveIn_RecordingStopped;
                waveIn.StopRecording();
                waveIn.Dispose();
                waveIn = null;
            }

            if(writer != null)
            {
                writer.Dispose();
                writer = null;
            }
            if (File.Exists(outputFilePath))
            {
                try
                {
                    File.Delete(outputFilePath);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error deleting existing file: {ex.Message}");
                }
            }

            waveIn = new WaveInEvent();
            waveIn.WaveFormat = new WaveFormat(16000, 1); 
            recordingStoppedTcs = new TaskCompletionSource<bool>();
            waveIn.DataAvailable += (s, a) =>
            {
                writer.Write(a.Buffer, 0, a.BytesRecorded);
            };
            waveIn.RecordingStopped += (s, a) =>
            {
                if (writer != null) { 
                    writer?.Dispose();
                    writer = null;
                }
                waveIn?.Dispose();
                waveIn = null;
                recordingStoppedTcs?.TrySetResult(true);
            };

            writer = new WaveFileWriter(outputFilePath, waveIn.WaveFormat);
            waveIn.StartRecording();
        }


        private async Task StopRecording()
        {
            if (waveIn != null)
            {
                waveIn?.StopRecording();
                if (recordingStoppedTcs != null)
                {
                    await recordingStoppedTcs.Task.ConfigureAwait(false);
                }
            }
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            writer?.Write(e.Buffer, 0, e.BytesRecorded);
        }

        private void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            writer?.Dispose();
            writer = null;

            waveIn?.Dispose();
            waveIn = null;

            recordingStoppedTcs.SetResult(true);
        }


        private void CommandInput_GotFocus(object sender, RoutedEventArgs e)
        {
            // UI feedback on focus
        }

        private void CommandInput_LostFocus(object sender, RoutedEventArgs e)
        {
            // UI feedback on loss of focus
        }

        private void SendCommand_Click(object sender, RoutedEventArgs e)
        {
            string command = CommandInput.Text;
            System.Windows.MessageBox.Show("Scanning for .exe files in C drive...");

            string startDirectory = @"C:\";

            allExeFiles.Clear();

            try
            {
                ScanDirectoryForExe(startDirectory);
                System.Windows.MessageBox.Show($"Found {allExeFiles.Count} useful .exe files in C drive.");

                // Example command match
                string Path = @"C:\Assassin's Creed\Assassins.Creed.Brotherhood-SKIDROW\autorun.exe";
                if (allExeFiles.Contains(Path, StringComparer.OrdinalIgnoreCase))
                {
                    System.Windows.MessageBox.Show("AC found.");
                    int index = allExeFiles.IndexOf(Path);
                    foreach (var data in metadata[index])
                    {
                        System.Windows.MessageBox.Show("" + data);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("AC not found.");
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

                foreach (var file in Directory.EnumerateFiles(path,"*.exe"))
                {
                    try
                    {
                        FileInfo fi = new FileInfo(file);
                       
                        allExeFiles.Add(file);
                        List<string> meta = new List<string>();
                        meta.Add(FormatFileSize(fi.Length));
                        meta.Add(fi.Name);
                        meta.Add(fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
                        meta.Add(fi.LastAccessTime.ToString("yyyy-MM-dd HH:mm:ss"));
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

    }
}
