using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfApp2
{
    public partial class FloatingIcon : Window
    {
        private Point _startPoint;
        private bool _isDragging = false;
        private WaveInEvent? waveIn;
        private FileStream? _tempRawStream;
        private static string? api;
        private const int SAMPLE_RATE = 48000;
        private const int FRAME_SIZE = 480;
        private const int BYTES_PER_SAMPLE = 2;
        private const int FRAME_SIZE_BYTES = FRAME_SIZE * BYTES_PER_SAMPLE;
        private string tempRawPath = Path.Combine(Path.GetTempPath(), "temp_raw.raw");
        private string denoisedFilePath = Path.Combine(Path.GetTempPath(), "denoised_output.wav");
        private TaskCompletionSource<bool>? recordingStoppedTcs;
        private DateTime _lastRightClickTime = DateTime.MinValue;
        private const int DoubleClickThreshold = 300;

        public FloatingIcon()
        {
            try
            {
                InitializeComponent();
                this.Left = SystemParameters.PrimaryScreenWidth - this.Width - 10;
                this.Top = SystemParameters.PrimaryScreenHeight - this.Height - 100;

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("AppSetting.json", optional: false, reloadOnChange: true)
                    .Build();

                api = config["Groq_Api_Key"] ?? throw new InvalidOperationException("Groq API Key not found in AppSetting.json.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"A critical error occurred while initializing the floating icon.\n\nDetails: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close(); // Close the window if initialization fails
            }
        }

        private async Task OnMicClick()
        {
            // Stop wake word detection while processing manual command
            Global._wakeWordHelper?.Stop();

            // UI animation logic
            var figure = new PathFigure { StartPoint = new Point(40 + 36, 40) };
            var arc = new ArcSegment { Size = new Size(36, 36), Point = figure.StartPoint, SweepDirection = SweepDirection.Clockwise };
            figure.Segments.Add(arc);
            RecordingArc.Data = new PathGeometry { Figures = { figure } };
            RecordingArc.Visibility = Visibility.Visible;

            try
            {
                string result = await HandelVoiceInput();
                if (!string.IsNullOrEmpty(result))
                {
                    // Process the command
                    _ = new llm(result);
                }
                else
                {
                    // This case is handled inside TranscribeAsync if verification fails
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during the voice command process.\n\nDetails: {ex.Message}", "Voice Command Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Ensure UI is always cleaned up
                RecordingArc.Visibility = Visibility.Collapsed;
                // Optionally restart wake word detection
                if (Properties.Settings.Default.WakeWord)
                {
                    Global._wakeWordHelper?.Start();
                }
            }
        }

        public async Task<string> HandelVoiceInput()
        {
            StartRecording();
            await Task.Delay(5000);
            await StopRecording();

            if (File.Exists(denoisedFilePath) && new FileInfo(denoisedFilePath).Length > 0)
            {
                return await TranscribeAsync(denoisedFilePath);
            }
            return string.Empty;
        }

        public async Task<string> TranscribeAsync(string path)
        {
            try
            {
                if (Properties.Settings.Default.Speaker_Verification && !Verification.verify())
                {
                    MessageBox.Show("Your voice did not match the authorized voice. Please try again.", "Verification Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return string.Empty;
                }

                using var http = new HttpClient();
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api);

                using var form = new MultipartFormDataContent();
                var fileBytes = await File.ReadAllBytesAsync(path);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");

                form.Add(fileContent, "file", Path.GetFileName(path));
                form.Add(new StringContent("whisper-large-v3"), "model");
                form.Add(new StringContent("en"), "language");

                var response = await http.PostAsync("https://api.groq.com/openai/v1/audio/transcriptions", form);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"API request failed: {response.ReasonPhrase}\n{json}");
                }

                dynamic? obj = JsonConvert.DeserializeObject(json);
                return obj?.text ?? string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to transcribe audio.\nPlease check your internet connection and API key.\n\nDetails: {ex.Message}", "Transcription Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty;
            }
        }

        public void StartRecording()
        {
            try
            {
                waveIn?.Dispose();
                _tempRawStream?.Dispose();

                if (File.Exists(denoisedFilePath)) File.Delete(denoisedFilePath);
                if (File.Exists(tempRawPath)) File.Delete(tempRawPath);

                _tempRawStream = new FileStream(tempRawPath, FileMode.Create, FileAccess.Write);
                waveIn = new WaveInEvent { WaveFormat = new WaveFormat(SAMPLE_RATE, 16, 1) };

                recordingStoppedTcs = new TaskCompletionSource<bool>();
                waveIn.DataAvailable += WaveIn_DataAvailable;
                waveIn.RecordingStopped += WaveIn_RecordingStopped;
                waveIn.StartRecording();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start audio recording.\nPlease ensure a microphone is connected and not in use.\n\nDetails: {ex.Message}", "Recording Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task StopRecording()
        {
            try
            {
                waveIn?.StopRecording();
                if (recordingStoppedTcs != null) await recordingStoppedTcs.Task;
            }
            finally
            {
                _tempRawStream?.Dispose();
                _tempRawStream = null;

                // Denoising happens after the raw stream is fully written and closed.
                await DenoiseRawAudioAsync(tempRawPath, denoisedFilePath);

                try
                {
                    if (File.Exists(tempRawPath)) File.Delete(tempRawPath);
                }
                catch (IOException) { /* Silently ignore, temp file will be cleaned up later */ }
            }
        }

        private async Task DenoiseRawAudioAsync(string inp, string outp)
        {
            await Task.Run(() =>
            {
                IntPtr st = IntPtr.Zero;
                try
                {
                    if (!File.Exists(inp)) return;

                    st = RNNoise.rnnoise_create();
                    var wf = new WaveFormat(SAMPLE_RATE, 16, 1);
                    using var rawReader = new RawSourceWaveStream(File.OpenRead(inp), wf);
                    using var writer = new WaveFileWriter(outp, wf);

                    var buffer = new byte[FRAME_SIZE_BYTES];
                    var inFloat = new float[FRAME_SIZE];
                    var outFloat = new float[FRAME_SIZE];

                    while (rawReader.Read(buffer, 0, buffer.Length) == buffer.Length)
                    {
                        for (int i = 0; i < FRAME_SIZE; i++) inFloat[i] = BitConverter.ToInt16(buffer, i * 2) / 32768f;
                        RNNoise.rnnoise_process_frame(st, outFloat, inFloat);
                        for (int i = 0; i < FRAME_SIZE; i++)
                        {
                            var s = (short)(Math.Clamp(outFloat[i], -1f, 1f) * 32767);
                            var b = BitConverter.GetBytes(s);
                            buffer[i * 2] = b[0];
                            buffer[i * 2 + 1] = b[1];
                        }
                        writer.Write(buffer, 0, buffer.Length);
                    }
                }
                catch (Exception ex)
                {
                    // This runs on a background thread, so we dispatch the error to the UI thread.
                    Application.Current.Dispatcher.Invoke(() =>
                        MessageBox.Show($"Failed to denoise audio.\n\nDetails: {ex.Message}", "Audio Processing Error", MessageBoxButton.OK, MessageBoxImage.Error));
                }
                finally
                {
                    if (st != IntPtr.Zero) RNNoise.rnnoise_destroy(st);
                }
            });
        }

        public void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            try
            {
                _tempRawStream?.Write(e.Buffer, 0, e.BytesRecorded);
            }
            catch (ObjectDisposedException) { /* Stream was closed, ignore */ }
            catch (Exception ex) { Trace.WriteLine($"Error writing to temp audio stream: {ex.Message}"); }
        }

        public void WaveIn_RecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                MessageBox.Show($"An error occurred during recording: {e.Exception.Message}", "Recording Error", MessageBoxButton.OK, MessageBoxImage.Error);
                recordingStoppedTcs?.TrySetException(e.Exception);
            }
            else
            {
                recordingStoppedTcs?.TrySetResult(true);
            }

            waveIn?.Dispose();
            waveIn = null;
        }

        // --- UI Event Handlers for Dragging and Right-Click ---
        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(this);
            _isDragging = false;
            (sender as UIElement)?.CaptureMouse();
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && (sender as UIElement)?.IsMouseCaptured == true)
            {
                Point currentPos = e.GetPosition(this);
                if (Math.Abs(currentPos.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(currentPos.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _isDragging = true;
                    DragMove();
                }
            }
        }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            (sender as UIElement)?.ReleaseMouseCapture();
            if (!_isDragging)
            {
                _ = OnMicClick();
            }
            _isDragging = false;
        }

        private void Grid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var now = DateTime.Now;
            if ((now - _lastRightClickTime).TotalMilliseconds <= DoubleClickThreshold)
            {
                this.Hide();
            }
            _lastRightClickTime = now;
        }
    }
}