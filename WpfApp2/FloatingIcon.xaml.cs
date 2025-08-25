using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
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
        private string tempRawPath = "temp_raw.raw";
        private string denoisedFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "denoised_output.wav");
        private TaskCompletionSource<bool> recordingStoppedTcs = new TaskCompletionSource<bool>();
        private DateTime _lastRightClickTime = DateTime.MinValue;
        private const int DoubleClickThreshold = 300;


        public FloatingIcon()
        {
            InitializeComponent();
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            this.Left = screenWidth - this.Width - 10;
            this.Top = screenHeight - this.Height - 100;
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("AppSetting.json", optional: true, reloadOnChange: true)
                .Build();
            api = config["Groq_Api_Key"] ?? throw new InvalidOperationException("APIKey not found");

        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(this);
            _isDragging = false;
            (sender as UIElement)?.CaptureMouse();
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var current = e.GetPosition(this);
                if (!_isDragging && (Math.Abs(current.X - _startPoint.X) > 4 || Math.Abs(current.Y - _startPoint.Y) > 4))
                {
                    _isDragging = true;
                    DragMove();
                }
            }
        }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            (sender as UIElement)?.ReleaseMouseCapture();
            if (!_isDragging) _ = OnMicClick();
            _isDragging = false;
        }

        private void Grid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var now = DateTime.Now;
            if ((now - _lastRightClickTime).TotalMilliseconds <= DoubleClickThreshold)
                Global.floatingIcon?.Hide();
            _lastRightClickTime = now;
        }

        private async Task OnMicClick()
        {
            const double durationSeconds = 5;
            const double radius = 36;
            var center = new Point(40, 40);
            Global._wakeWordHelper?.Stop();
            var figure = new PathFigure { StartPoint = new Point(center.X + radius, center.Y) };
            var arc = new ArcSegment { Size = new Size(radius, radius), Point = figure.StartPoint, SweepDirection = SweepDirection.Clockwise };
            figure.Segments.Add(arc);
            var geo = new PathGeometry();
            geo.Figures.Add(figure);
            RecordingArc.Data = geo;
            RecordingArc.Visibility = Visibility.Visible;

            DateTime start = DateTime.Now;
            EventHandler handler = null;
            handler = (s, e) =>
            {
                var prog = Math.Min((DateTime.Now - start).TotalSeconds / durationSeconds, 1);
                var angle = 360 * prog;
                var rad = angle * Math.PI / 180;
                arc.Point = new Point(center.X + radius * Math.Cos(rad), center.Y + radius * Math.Sin(rad));
                arc.IsLargeArc = angle > 180;
                if (prog >= 1)
                {
                    CompositionTarget.Rendering -= handler;
                    RecordingArc.Visibility = Visibility.Collapsed;
                }
            };
            CompositionTarget.Rendering += handler;

            var result = await HandelVoiceInput();
            CompositionTarget.Rendering -= handler;
            RecordingArc.Visibility = Visibility.Collapsed;
            if (!string.IsNullOrEmpty(result))
            {
                _ = new llm(result);
            }
            else
            {

                MessageBox.Show("Your Voice Did Not Match the authorized voice, Try Again" + " in floating icon");
            }
        }

        public async Task<string> HandelVoiceInput()
        {
            StartRecording();
            await Task.Delay(5000).ConfigureAwait(false);
            await StopRecording().ConfigureAwait(false);
            if (File.Exists(denoisedFilePath) && new FileInfo(denoisedFilePath).Length > 0)
                return await TranscribeAsync(denoisedFilePath).ConfigureAwait(false);
            return string.Empty;
        }

        public async Task<string> TranscribeAsync(string path)
        {
            if (Properties.Settings.Default.Speaker_Verification)
            {
                if (Verification.verify())
                {
                    using var http = new HttpClient();
                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api);
                    using var form = new MultipartFormDataContent();
                    var file = new ByteArrayContent(await File.ReadAllBytesAsync(path).ConfigureAwait(false));
                    file.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
                    form.Add(file, "file", Path.GetFileName(path));
                    form.Add(new StringContent("whisper-large-v3"), "model");
                    form.Add(new StringContent("en"), "language");
                    var resp = await http.PostAsync("https://api.groq.com/openai/v1/audio/transcriptions", form).ConfigureAwait(false);
                    var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode) return string.Empty;
                    dynamic obj = JsonConvert.DeserializeObject(json);
                    return obj?.text ?? string.Empty;
                }
                return string.Empty;
            }
            else
            {
                Debug.WriteLine("inside");
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api);
                using var form = new MultipartFormDataContent();
                var file = new ByteArrayContent(await File.ReadAllBytesAsync(path).ConfigureAwait(false));
                file.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
                form.Add(file, "file", Path.GetFileName(path));
                form.Add(new StringContent("whisper-large-v3"), "model");
                form.Add(new StringContent("en"), "language");
                var resp = await http.PostAsync("https://api.groq.com/openai/v1/audio/transcriptions", form).ConfigureAwait(false);
                var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode) return string.Empty;
                dynamic obj = JsonConvert.DeserializeObject(json);
                return obj?.text ?? string.Empty;

            }

        }

        public void StartRecording()
        {
            waveIn?.Dispose();
            if (_tempRawStream != null)
            {
                lock (_tempRawStream) { _tempRawStream.Flush(); _tempRawStream.Dispose(); _tempRawStream = null; }
            }
            if (File.Exists(denoisedFilePath)) File.Delete(denoisedFilePath);
            for (int i = 0; i < 5; i++)
            {
                try { if (File.Exists(tempRawPath)) File.Delete(tempRawPath); break; }
                catch (IOException) { Task.Delay(100).Wait(); }
            }
            _tempRawStream = new FileStream(tempRawPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            waveIn = new WaveInEvent { WaveFormat = new WaveFormat(SAMPLE_RATE, 16, 1) };
            recordingStoppedTcs = new TaskCompletionSource<bool>();
            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.RecordingStopped += WaveIn_RecordingStopped;
            waveIn.StartRecording();
        }

        public async Task StopRecording()
        {
            waveIn?.StopRecording();
            if (recordingStoppedTcs != null) await recordingStoppedTcs.Task.ConfigureAwait(false);
            if (_tempRawStream != null)
            {
                lock (_tempRawStream) { _tempRawStream.Flush(); _tempRawStream.Dispose(); _tempRawStream = null; }
            }
            await DenoiseRawAudioAsync(tempRawPath, denoisedFilePath).ConfigureAwait(false);
            try { if (File.Exists(tempRawPath)) File.Delete(tempRawPath); }
            catch { }
        }

        private async Task DenoiseRawAudioAsync(string inp, string outp)
        {
            await Task.Run(() =>
            {
                var wf = new WaveFormat(SAMPLE_RATE, 16, 1);
                IntPtr st = IntPtr.Zero;
                try
                {
                    st = RNNoise.rnnoise_create();
                    using var raw = new RawSourceWaveStream(File.Open(inp, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), wf);
                    using var w = new WaveFileWriter(outp, wf);
                    var buf = new byte[FRAME_SIZE_BYTES];
                    var inF = new float[FRAME_SIZE];
                    var outF = new float[FRAME_SIZE];
                    int read;
                    while ((read = raw.Read(buf, 0, buf.Length)) == buf.Length)
                    {
                        for (int i = 0; i < FRAME_SIZE; i++) inF[i] = BitConverter.ToInt16(buf, i * BYTES_PER_SAMPLE) / 32768f;
                        RNNoise.rnnoise_process_frame(st, outF, inF);
                        for (int i = 0; i < FRAME_SIZE; i++)
                        {
                            var s = (short)(Math.Clamp(outF[i], -1f, 1f) * 32767);
                            var b = BitConverter.GetBytes(s);
                            buf[i * BYTES_PER_SAMPLE] = b[0];
                            buf[i * BYTES_PER_SAMPLE + 1] = b[1];
                        }
                        w.Write(buf, 0, buf.Length);
                    }
                }
                finally { if (st != IntPtr.Zero) RNNoise.rnnoise_destroy(st); }
            }).ConfigureAwait(false);
        }

        public void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (_tempRawStream == null || e.BytesRecorded == 0) return;
            lock (_tempRawStream)
            {
                _tempRawStream.Write(e.Buffer, 0, e.BytesRecorded);
                _tempRawStream.Flush();
            }
        }

        public void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (_tempRawStream != null)
            {
                lock (_tempRawStream) { _tempRawStream.Flush(); }
            }
            waveIn!.DataAvailable -= WaveIn_DataAvailable;
            waveIn!.RecordingStopped -= WaveIn_RecordingStopped;
            waveIn.Dispose();
            waveIn = null;
            recordingStoppedTcs.TrySetResult(true);
            if (e.Exception != null) recordingStoppedTcs.TrySetException(e.Exception);
        }
    }
}