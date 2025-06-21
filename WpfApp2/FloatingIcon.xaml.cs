using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace WpfApp2
{
    public partial class FloatingIcon : Window
    {
        private Point _startPoint;
        private bool _isDragging = false;

        private WaveInEvent? waveIn;
        private WaveFileWriter? writer;
        private string outputFilePath = "temp_voice_input.wav";
        private TaskCompletionSource<bool> recordingStoppedTcs = new TaskCompletionSource<bool>(); 
        private static string api = null;

        public FloatingIcon()
        {
            InitializeComponent();

            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var screenWidth = SystemParameters.PrimaryScreenWidth;

            double right = 10;
            double bottom = 100;

            this.Left = screenWidth-this.Width-right;
            this.Top = screenHeight - this.Height - bottom;

            var config = new ConfigurationBuilder()
              .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
              .AddJsonFile("AppSetting.json", optional: true, reloadOnChange: true)
              .Build();



            api = config["Groq_Api_Key"] ?? throw new InvalidOperationException("APIKey not found in configuration.");

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
                Point currentPoint = e.GetPosition(this);
                if (!_isDragging && (Math.Abs(currentPoint.X - _startPoint.X) > 4 || Math.Abs(currentPoint.Y - _startPoint.Y) > 4))
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
                OnMicClick();
            }

            _isDragging = false;
        }

        private async Task OnMicClick()
        {

            const double durationSeconds = 4;
            const double radius = 36;
            Point center = new Point(40, 40);


            if (Global._wakeWordHelper != null)
            {
                Global._wakeWordHelper.Stop();
                Global._wakeWordHelper = null;
            }

            await Dispatcher.InvokeAsync(async () =>
            {
                
                string? result = await HandelVoiceInput(this, new RoutedEventArgs());

                llm l1 = new llm(result);
            });



            var figure = new PathFigure { StartPoint = new Point(center.X + radius, center.Y) };
            var arcSegment = new ArcSegment
            {
                Size = new Size(radius, radius),
                Point = figure.StartPoint,
                IsLargeArc = false,
                SweepDirection = SweepDirection.Clockwise
            };

            figure.Segments.Add(arcSegment);
            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            RecordingArc.Data = geometry;
            RecordingArc.Visibility = Visibility.Visible;

          
            DateTime startTime = DateTime.Now;
            EventHandler handler = null;

            handler = (s, e) =>
            {
                var elapsed = (DateTime.Now - startTime).TotalSeconds;
                var progress = Math.Min(elapsed / durationSeconds, 1);
                var angle = 360 * progress;
                var radians = angle * Math.PI / 180;

               
                Point endPoint = new Point(
                    center.X + radius * Math.Cos(radians),
                    center.Y + radius * Math.Sin(radians)
                );

                arcSegment.Point = endPoint;
                arcSegment.IsLargeArc = angle > 180;

                if (progress >= 1)
                {
                    CompositionTarget.Rendering -= handler;
                    RecordingArc.Visibility = Visibility.Collapsed;
                }
            };

            CompositionTarget.Rendering += handler;

        }

        private async Task<string> HandelVoiceInput(object sender, RoutedEventArgs e)
        {
          
           
            StartRecording();

            await Task.Delay(4000);

            await StopRecording();

            string result = await TranscribeAsync(outputFilePath);
            // System.Windows.MessageBox.Show(result);
            return result;
        }

        private async Task<string> TranscribeAsync(string audioFilePath)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api);

            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(File.ReadAllBytes(audioFilePath));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
            form.Add(fileContent, "file", "temp_voice_input.wav");
            form.Add(new StringContent("whisper-large-v3-turbo"), "model");
            form.Add(new StringContent("en"), "language");

            var response = await httpClient.PostAsync("https://api.groq.com/openai/v1/audio/transcriptions", form);
            response.EnsureSuccessStatusCode();
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

            if (writer != null)
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
            waveIn.WaveFormat = new WaveFormat(16000, 16,1);
            recordingStoppedTcs = new TaskCompletionSource<bool>(); // Reset TCS for each new recording
            waveIn.DataAvailable += (s, a) =>
            {
                int bytesPerSample = 2; // 16-bit
                int sampleCount = a.BytesRecorded / bytesPerSample;
                byte[] processedBuffer = new byte[a.BytesRecorded];

                const float noiseThreshold = 0.02f;  
                const float gainFactor = 3.0f;

                int offset = 0;

                for (int i = 0; i < sampleCount; i++)
                {
                    short sample = BitConverter.ToInt16(a.Buffer, i * bytesPerSample);
                    float amplitude = Math.Abs(sample / 32768f);

                    if (amplitude > noiseThreshold)
                    {
                        // Amplify sample
                        float amplifiedSample = sample * gainFactor;

                        // Clamp to 16-bit range
                        if (amplifiedSample > 32767f) amplifiedSample = 32767f;
                        if (amplifiedSample < -32768f) amplifiedSample = -32768f;

                        short outSample = (short)amplifiedSample;

                        processedBuffer[offset++] = (byte)(outSample & 0xFF);
                        processedBuffer[offset++] = (byte)((outSample >> 8) & 0xFF);
                    }
                    else
                    {
                        // Silence below threshold
                        processedBuffer[offset++] = 0;
                        processedBuffer[offset++] = 0;
                    }
                }

                writer.Write(processedBuffer, 0, offset);
            };
            waveIn.RecordingStopped += (s, a) =>
            {
                if (writer != null)
                {
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


    }
}
