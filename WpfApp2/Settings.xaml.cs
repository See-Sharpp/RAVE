using ControlzEx.Standard;
using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfApp2
{
    public partial class Settings : Page
    {
        private DispatcherTimer countdownTimer;
        private DispatcherTimer recordingTimer;
        private DispatcherTimer visualizerTimer;
        private int countdownValue = 5;
        private int recordingTimeLeft = 5;



        private WaveInEvent? waveIn;
        private WaveFileWriter? _denoisedWriter;
        private string denoisedFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "speaker_output.wav");
        private TaskCompletionSource<bool> recordingStoppedTcs = new TaskCompletionSource<bool>();

        private IntPtr _rnnoiseState = IntPtr.Zero;
        private readonly List<byte> _unprocessedBuffer = new List<byte>();
        private string tempDenoisedPath = "temp_speaker.raw";
        private FileStream? _tempDenoisedStream;
        private float currentAudioLevel = 0f;
        private const int SAMPLE_RATE = 48000;
        private const int FRAME_SIZE = 480;  
        private const int BYTES_PER_SAMPLE = 2;
        private const int FRAME_SIZE_BYTES = FRAME_SIZE * BYTES_PER_SAMPLE;
        private List<float> audioLevels = new List<float>();
        private const int MAX_BARS = 50;
        private readonly object audioLevelLock = new object();

    
        private static string? api;

        public Settings()
        {
            InitializeComponent();
            SpeakerVerificationToggle.IsChecked = Properties.Settings.Default.Speaker_Verification;
            WakeWordToggle.IsChecked = Properties.Settings.Default.WakeWord;
            DarkModeToggle.IsChecked = Properties.Settings.Default.Dark_Mode;
            SaveHistoryToggle.IsChecked = Properties.Settings.Default.History;

            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("AppSetting.json", optional: true, reloadOnChange: true)
                .Build();

            api = config["Groq_Api_Key"] ?? throw new InvalidOperationException("APIKey not found in configuration.");

         
        }

        

        private void WakeWordToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (!Properties.Settings.Default.WakeWord)
            {
                WakeWordManager.WakeWordDetected += OnWakeWordDetected;
                WakeWordManager.Start();

                Properties.Settings.Default.WakeWord = true;
                Properties.Settings.Default.Save();
            }
        }

        private void WakeWordToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.WakeWord)
            {
                WakeWordManager.WakeWordDetected -= OnWakeWordDetected;
                WakeWordManager.Stop();

                Properties.Settings.Default.WakeWord = false;
                Properties.Settings.Default.Save();
            }
        }

        public async void OnWakeWordDetected()
        {
            await Dispatcher.InvokeAsync(async () =>
            {
             
                System.Windows.MessageBox.Show("Hey Jarvis Detected!");
                string? result = await HandelVoiceInput(this, new RoutedEventArgs());
                if (!string.IsNullOrEmpty(result))
                {
                    llm l1 = new llm(result);

                }
                
            });
        }

        public async Task<string> HandelVoiceInput(object sender, RoutedEventArgs e)
        {
           
            StartRecording();


            await Task.Delay(5000);


            await StopRecording();

            if (File.Exists(denoisedFilePath) && new FileInfo(denoisedFilePath).Length > 0)
            {
             
                string result = await TranscribeAsync(denoisedFilePath);
            
                return result;
            }
            else
            {
                return string.Empty;
            }
        }

        public async Task<string> TranscribeAsync(string audioFilePath)
        {

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api);

            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(audioFilePath));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
            form.Add(fileContent, "file", System.IO.Path.GetFileName(audioFilePath));
            form.Add(new StringContent("whisper-large-v3"), "model");
            form.Add(new StringContent("en"), "language");

            var response = await httpClient.PostAsync("https://api.groq.com/openai/v1/audio/transcriptions", form);

            var responseJson = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {

                MessageBox.Show($"Error during transcription: {response.ReasonPhrase}\n{responseJson}");
                return string.Empty;
            }

            dynamic result = JsonConvert.DeserializeObject(responseJson);
            return result?.text ?? string.Empty;
        }
       


        private void AutoListenToggle_Checked(object sender, RoutedEventArgs e)
        {
            
        }

        private void AutoListenToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            
        }

        private void DarkModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (!Properties.Settings.Default.Dark_Mode)
            {
                Properties.Settings.Default.Dark_Mode = true;
                Properties.Settings.Default.Save();
            }

        }

        private void DarkModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.Dark_Mode)
            {
                Properties.Settings.Default.Dark_Mode = false;
                Properties.Settings.Default.Save();
            }
        }

        private void SaveHistoryToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (!Properties.Settings.Default.History)
            {
                Properties.Settings.Default.History = true;
                Properties.Settings.Default.Save();
            }
        }

        private void SaveHistoryToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.History) { 
                Properties.Settings.Default.History = false;
                Properties.Settings.Default.Save();
            }
        }

        private async void CountdownTimer_Tick(object sender, EventArgs e)
        {
            countdownValue--;
            if (countdownValue > 0)
            {
                CountdownText.Text = countdownValue.ToString();
            }
            else
            {
                countdownTimer.Stop();
                CountdownPanel.Visibility = Visibility.Collapsed;
                RecordingPanel.Visibility = Visibility.Visible;
                StartRecording();

                recordingTimeLeft = 5;
                recordingTimer = new DispatcherTimer();
                recordingTimer.Interval = TimeSpan.FromSeconds(1);
                recordingTimer.Tick += RecordingTimer_Tick;
                recordingTimer.Start();

                await Task.Delay(5000);
                StopRecording();
            }
        }

        private void RecordingTimer_Tick(object sender, EventArgs e)
        {
            recordingTimeLeft--;
            RecordingTimeText.Text = $"00:0{Math.Max(0, recordingTimeLeft)}";

            if (recordingTimeLeft <= 0)
            {
                recordingTimer?.Stop();
            }
        }

        private void StartRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            InitialMessagePanel.Visibility = Visibility.Collapsed;
            CountdownPanel.Visibility = Visibility.Visible;
            countdownValue = 5;
            CountdownText.Text = countdownValue.ToString();

            countdownTimer = new DispatcherTimer();
            countdownTimer.Interval = TimeSpan.FromSeconds(1);
            countdownTimer.Tick += CountdownTimer_Tick;
            countdownTimer.Start();
        }

        private async void SpeakerVerificationToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (!Properties.Settings.Default.Speaker_Verification)
            {
                Properties.Settings.Default.Speaker_Verification = true;
                Properties.Settings.Default.Save();
                RecordingOverlay.Visibility = Visibility.Visible;
                InitialMessagePanel.Visibility = Visibility.Visible;
                CountdownPanel.Visibility = Visibility.Collapsed;
                RecordingPanel.Visibility = Visibility.Collapsed;
                PlaybackPanel.Visibility = Visibility.Collapsed;
            }
        }
        
        private async void SpeakerVerificationToggle_Unchecked(object sender, RoutedEventArgs e)
        {
           
            if (Properties.Settings.Default.Speaker_Verification)
            {
               
                await Task.Delay(100);
                var dialog = new SpeakerVerificationPass();
                bool? result = dialog.ShowDialog(); 

                if(result == true)
                {

                    Properties.Settings.Default.Speaker_Verification = false;
                    Properties.Settings.Default.Save();
                    SpeakerVerificationToggle.IsChecked = false;
                }
                else
                {
                    SpeakerVerificationToggle.IsChecked = true;
                }
                await Task.Delay(200);
            }
        }

        public void StartRecording()
        {
            
            waveIn?.Dispose();
            _denoisedWriter?.Dispose();
            _tempDenoisedStream?.Dispose();

            try
            {
                if (File.Exists(denoisedFilePath)) File.Delete(denoisedFilePath);
                if (File.Exists(tempDenoisedPath)) File.Delete(tempDenoisedPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting existing file: {ex.Message}");
                return;
            }

            string fullTempPath = System.IO.Path.GetFullPath(tempDenoisedPath);
            _tempDenoisedStream = new FileStream(tempDenoisedPath, FileMode.Create, FileAccess.Write);

            
            waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(SAMPLE_RATE, 16, 1)
            };

            recordingStoppedTcs = new TaskCompletionSource<bool>();

            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.RecordingStopped += WaveIn_RecordingStopped;

            _rnnoiseState = RNNoise.rnnoise_create();

            
            lock (audioLevelLock)
            {
                audioLevels.Clear();
                currentAudioLevel = 0f;
            }

            
            visualizerTimer = new DispatcherTimer();
            visualizerTimer.Interval = TimeSpan.FromMilliseconds(50);
            visualizerTimer.Tick += VisualizerTimer_Tick;
            visualizerTimer.Start();

            waveIn.StartRecording();
        }

        private void VisualizerTimer_Tick(object sender, EventArgs e)
        {
            UpdateAudioVisualizer();
        }

        private void UpdateAudioVisualizer()
        {
            Dispatcher.Invoke(() =>
            {
                AudioVisualizerCanvas.Children.Clear();

                lock (audioLevelLock)
                {
                    if (audioLevels.Count == 0) return;

                    double canvasWidth = AudioVisualizerCanvas.ActualWidth;
                    double canvasHeight = AudioVisualizerCanvas.ActualHeight;

                    if (canvasWidth <= 0 || canvasHeight <= 0) return;

                    double barWidth = canvasWidth / MAX_BARS;
                    int startIndex = Math.Max(0, audioLevels.Count - MAX_BARS);

                    for (int i = startIndex; i < audioLevels.Count; i++)
                    {
                        float level = audioLevels[i];
                        double barHeight = Math.Max(2, level * canvasHeight * 0.8);

                        Rectangle bar = new Rectangle
                        {
                            Width = Math.Max(1, barWidth - 1),
                            Height = barHeight,
                            Fill = GetBarBrush(level)
                        };

                        Canvas.SetLeft(bar, (i - startIndex) * barWidth);
                        Canvas.SetBottom(bar, (canvasHeight - barHeight) / 2);

                        AudioVisualizerCanvas.Children.Add(bar);
                    }
                }
            });
        }

        private Brush GetBarBrush(float level)
        {
            
            if (level < 0.1f)
            {
                return new SolidColorBrush(Color.FromRgb(99, 102, 241)); 
            }
            else if (level < 0.3f)
            {
                return new SolidColorBrush(Color.FromRgb(34, 197, 94)); 
            }
            else
            {
                return new SolidColorBrush(Color.FromRgb(239, 68, 68));
            }
        }

        public async Task StopRecording()
        {
            visualizerTimer?.Stop();
            recordingTimer?.Stop();

            waveIn?.StopRecording();
            if (recordingStoppedTcs != null)
            {
                await recordingStoppedTcs.Task.ConfigureAwait(false);
            }

            ProcessAndAmplifyAudio();
            _tempDenoisedStream?.Dispose();
            _tempDenoisedStream = null;

            if (_rnnoiseState != IntPtr.Zero)
            {
                RNNoise.rnnoise_destroy(_rnnoiseState);
                _rnnoiseState = IntPtr.Zero;
            }

            try
            {
                if (File.Exists(tempDenoisedPath))
                {
                    File.Delete(tempDenoisedPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not delete temp file: {ex.Message}");
            }

            Dispatcher.Invoke(() =>
            {
                RecordingPanel.Visibility = Visibility.Collapsed;
                ShowPlaybackPanel();
            });
        }

        private void ShowPlaybackPanel()
        {
            PlaybackPanel.Visibility = Visibility.Visible;
        }

        private void PlayRecordedAudio(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(denoisedFilePath))
                {
                    var player = new System.Media.SoundPlayer(denoisedFilePath);
                    player.Play();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error playing audio: {ex.Message}");
            }
        }

        private void RestartRecording(object sender, RoutedEventArgs e)
        {
            RecordingOverlay.Visibility = Visibility.Collapsed;
            SpeakerVerificationToggle.IsChecked = false;
        }

        private void AcceptRecording(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Voice sample saved successfully!");
            RecordingOverlay.Visibility = Visibility.Collapsed;
        }

        public void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (_tempDenoisedStream == null || e.BytesRecorded == 0) return;

            
            float maxLevel = 0f;
            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                if (i + 1 < e.BytesRecorded)
                {
                    short sample = BitConverter.ToInt16(e.Buffer, i);
                    float level = Math.Abs(sample) / 32768f;
                    maxLevel = Math.Max(maxLevel, level);
                }
            }

            lock (audioLevelLock)
            {
                currentAudioLevel = maxLevel;
                audioLevels.Add(maxLevel);

                
                if (audioLevels.Count > MAX_BARS * 2)
                {
                    audioLevels.RemoveRange(0, audioLevels.Count - MAX_BARS);
                }
            }

            _unprocessedBuffer.AddRange(e.Buffer.Take(e.BytesRecorded));

            float[] inputFloatSamples = new float[FRAME_SIZE];
            float[] outputFloatSamples = new float[FRAME_SIZE];
            byte[] processedFrameBytes = new byte[FRAME_SIZE_BYTES];

            while (_unprocessedBuffer.Count >= FRAME_SIZE_BYTES)
            {
                byte[] frameToProcess = _unprocessedBuffer.Take(FRAME_SIZE_BYTES).ToArray();

                for (int i = 0; i < FRAME_SIZE; i++)
                {
                    short sample = BitConverter.ToInt16(frameToProcess, i * BYTES_PER_SAMPLE);
                    inputFloatSamples[i] = sample / 32768f;
                }

                RNNoise.rnnoise_process_frame(_rnnoiseState, outputFloatSamples, inputFloatSamples);

                for (int i = 0; i < FRAME_SIZE; i++)
                {
                    short outSample = (short)(Math.Clamp(outputFloatSamples[i], -1f, 1f) * 32767);
                    byte[] sampleBytes = BitConverter.GetBytes(outSample);
                    processedFrameBytes[i * BYTES_PER_SAMPLE] = sampleBytes[0];
                    processedFrameBytes[i * BYTES_PER_SAMPLE + 1] = sampleBytes[1];
                }

                try
                {
                    _tempDenoisedStream.Write(processedFrameBytes, 0, FRAME_SIZE_BYTES);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error writing processed frame: {ex.Message}");
                }

                _unprocessedBuffer.RemoveRange(0, FRAME_SIZE_BYTES);
            }
        }

        public void ProcessAndAmplifyAudio()
        {
            if (!File.Exists(tempDenoisedPath)) return;

            const float gainFactor = 1.5f;
            var waveFormat = new WaveFormat(SAMPLE_RATE, 16, 1);

            try
            {
                string fullTempPath = System.IO.Path.GetFullPath(tempDenoisedPath);
                string fullDenoisedPath = System.IO.Path.GetFullPath(denoisedFilePath);

                using (var rawReader = new RawSourceWaveStream(File.OpenRead(tempDenoisedPath), waveFormat))
                using (var finalWriter = new WaveFileWriter(denoisedFilePath, waveFormat))
                {
                    var sampleProvider = rawReader.ToSampleProvider();
                    float[] buffer = new float[1024];
                    int read;
                    int totalSamplesWritten = 0;

                    while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        for (int i = 0; i < read; i++)
                        {
                            buffer[i] = Math.Clamp(buffer[i] * gainFactor, -1.0f, 1.0f);
                        }

                        finalWriter.WriteSamples(buffer, 0, read);
                        totalSamplesWritten += read;
                    }
                }

                if (File.Exists(denoisedFilePath))
                {
                    long fileSize = new FileInfo(denoisedFilePath).Length;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR in ProcessAndAmplifyAudio: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        public void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            
            if (_unprocessedBuffer.Count > 0 && _tempDenoisedStream != null)
            {
                int remainingBytes = _unprocessedBuffer.Count;
                if (remainingBytes < FRAME_SIZE_BYTES)
                {
                    int bytesToPad = FRAME_SIZE_BYTES - remainingBytes;
                    _unprocessedBuffer.AddRange(new byte[bytesToPad]);
                }

                byte[] finalFrame = _unprocessedBuffer.Take(FRAME_SIZE_BYTES).ToArray();
                float[] inSamples = new float[FRAME_SIZE];
                float[] outSamples = new float[FRAME_SIZE];

                for (int i = 0; i < FRAME_SIZE; i++)
                {
                    short sample = BitConverter.ToInt16(finalFrame, i * BYTES_PER_SAMPLE);
                    inSamples[i] = sample / 32768f;
                }

                RNNoise.rnnoise_process_frame(_rnnoiseState, outSamples, inSamples);

                byte[] finalBytes = new byte[FRAME_SIZE_BYTES];
                for (int i = 0; i < FRAME_SIZE; i++)
                {
                    short outSample = (short)(Math.Clamp(outSamples[i], -1f, 1f) * 32767);
                    var sampleBytes = BitConverter.GetBytes(outSample);
                    finalBytes[i * BYTES_PER_SAMPLE] = sampleBytes[0];
                    finalBytes[i * BYTES_PER_SAMPLE + 1] = sampleBytes[1];
                }

                try
                {
                    int bytesToWrite = Math.Min(remainingBytes, FRAME_SIZE_BYTES);
                    _tempDenoisedStream.Write(finalBytes, 0, bytesToWrite);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error writing final frame: {ex.Message}");
                }
            }

            _unprocessedBuffer.Clear();
            waveIn?.Dispose();
            waveIn = null;
            _tempDenoisedStream?.Flush();
            _tempDenoisedStream?.Close();
            recordingStoppedTcs?.TrySetResult(true);

            if (e.Exception != null)
            {
                MessageBox.Show($"An error occurred during recording: {e.Exception.Message}");
                recordingStoppedTcs?.TrySetException(e.Exception);
            }
        }

        private void SensitivitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SensitivityValue != null)
            {
                SensitivityValue.Text = $"{(int)e.NewValue}%";
            }
        }
        public double VoiceSensitivity => SensitivitySlider?.Value ?? 50;
    }
}
