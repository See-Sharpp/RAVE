using NAudio.Wave;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using Newtonsoft.Json;
using System.Windows.Media.Effects;
using Microsoft.Extensions.Configuration;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using WpfApp2.Context;
using WpfApp2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WpfApp2
{
    public partial class Dashboard : Page
    {
        private WaveInEvent? waveIn;
        private string denoisedFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "denoised_output.wav");
        private TaskCompletionSource<bool>? recordingStoppedTcs;
        private static string? api;
        private ApplicationDbContext _context;
        private IntPtr _rnnoiseState = IntPtr.Zero;
        private readonly List<byte> _unprocessedBuffer = new List<byte>();
        private string tempDenoisedPath = "temp_denoised.raw";
        private FileStream? _tempDenoisedStream;

        private const int SAMPLE_RATE = 48000;
        private const int FRAME_SIZE = 480;
        private const int BYTES_PER_SAMPLE = 2;
        private const int FRAME_SIZE_BYTES = FRAME_SIZE * BYTES_PER_SAMPLE;

        public Dashboard()
        {
            try
            {
                InitializeComponent();

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("AppSetting.json", optional: false, reloadOnChange: true)
                    .Build();

                api = config["Groq_Api_Key"] ?? throw new InvalidOperationException("Groq API Key not found in AppSetting.json.");
                _context = new ApplicationDbContext();

                if (!Properties.Settings.Default.Is_First)
                {
                    Global.web_browse = new Queue<LLM_Detail>(_context.LLM_Detail.Where(x => x.CommandType == "web_browse").OrderByDescending(x => x.CommandTime).Take(20));
                    Global.file_operation = new Queue<LLM_Detail>(_context.LLM_Detail.Where(x => x.CommandType == "file_operation").OrderByDescending(x => x.CommandTime).Take(20));
                    Global.application_control = new Queue<LLM_Detail>(_context.LLM_Detail.Where(x => x.CommandType == "application_control").OrderByDescending(x => x.CommandTime).Take(20));
                    Global.system_control = new Queue<LLM_Detail>(_context.LLM_Detail.Where(x => x.CommandType == "system_control").OrderByDescending(x => x.CommandTime).Take(20));
                    Global.total_commands = new Queue<LLM_Detail>(_context.LLM_Detail.OrderByDescending(x => x.CommandTime).Take(20));

                    Properties.Settings.Default.Is_First = true;
                    Properties.Settings.Default.Save();
                }

                LoadHistoryData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"A critical error occurred while initializing the dashboard.\n\nDetails: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void ToggleVoice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string result = await HandelVoiceInput();
                if (!string.IsNullOrEmpty(result))
                {
                    CommandInput.Clear();
                    CommandInput.AppendText(result);
                    SendCommand_Click(this, new RoutedEventArgs());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during the voice command process.\n\nDetails: {ex.Message}", "Voice Error", MessageBoxButton.OK, MessageBoxImage.Error);
                CommandInput.Clear();
                CommandInput.AppendText("Error processing voice input.");
            }
        }

        public async Task<string> HandelVoiceInput()
        {
            CommandInput.Clear();
            CommandInput.AppendText("Listening...");
            StartRecording();

            await Task.Delay(5000); // Record for 5 seconds

            await StopRecording();

            if (File.Exists(denoisedFilePath) && new FileInfo(denoisedFilePath).Length > 0)
            {
                CommandInput.Clear();
                CommandInput.AppendText("Transcribing...");
                return await TranscribeAsync(denoisedFilePath);
            }
            else
            {
                CommandInput.Clear();
                CommandInput.AppendText("No voice input detected or audio file is empty.");
                return string.Empty;
            }
        }

        public async Task<string> TranscribeAsync(string audioFilePath)
        {
            try
            {
                if (Properties.Settings.Default.Speaker_Verification)
                {
                    if (!Verification.verify())
                    {
                        CommandInput.AppendText("Your voice did not match the authorized voice. Please try again.");
                        return string.Empty;
                    }
                }

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api);

                using var form = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(audioFilePath));
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
                form.Add(fileContent, "file", Path.GetFileName(audioFilePath));
                form.Add(new StringContent("whisper-large-v3"), "model");
                form.Add(new StringContent("en"), "language");

                var response = await httpClient.PostAsync("https://api.groq.com/openai/v1/audio/transcriptions", form);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"API request failed: {response.ReasonPhrase}\n{responseJson}");
                }

                dynamic? result = JsonConvert.DeserializeObject(responseJson);
                return result?.text ?? string.Empty;
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"A network error occurred while transcribing audio.\nPlease check your internet connection.\n\nDetails: {ex.Message}", "Transcription Network Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty;
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Failed to parse the response from the transcription service.\n\nDetails: {ex.Message}", "Transcription Response Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred during transcription.\n\nDetails: {ex.Message}", "Transcription Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty;
            }
        }

        public void StartRecording()
        {
            try
            {
                waveIn?.Dispose();
                _tempDenoisedStream?.Dispose();

                if (File.Exists(denoisedFilePath)) File.Delete(denoisedFilePath);
                if (File.Exists(tempDenoisedPath)) File.Delete(tempDenoisedPath);

                _tempDenoisedStream = new FileStream(tempDenoisedPath, FileMode.Create, FileAccess.Write);
                waveIn = new WaveInEvent { WaveFormat = new WaveFormat(SAMPLE_RATE, 16, 1) };

                recordingStoppedTcs = new TaskCompletionSource<bool>();

                waveIn.DataAvailable += WaveIn_DataAvailable;
                waveIn.RecordingStopped += WaveIn_RecordingStopped;

                _rnnoiseState = RNNoise.rnnoise_create();
                waveIn.StartRecording();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start audio recording.\nPlease ensure a microphone is connected and not in use by another application.\n\nDetails: {ex.Message}", "Recording Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task StopRecording()
        {
            try
            {
                waveIn?.StopRecording();

                if (recordingStoppedTcs != null)
                {
                    await recordingStoppedTcs.Task;
                }

                ProcessAndAmplifyAudio();
            }
            finally
            {
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
                catch (IOException ex)
                {
                    // Silently log failure to delete temp file, as it's not critical.
                    Trace.WriteLine($"Could not delete temp file: {ex.Message}");
                }
            }
        }

        public void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            try
            {
                if (_tempDenoisedStream == null || e.BytesRecorded == 0 || _rnnoiseState == IntPtr.Zero) return;

                _unprocessedBuffer.AddRange(e.Buffer.Take(e.BytesRecorded));

                byte[] inputBytes = new byte[FRAME_SIZE_BYTES];
                float[] inputFloat = new float[FRAME_SIZE];
                float[] outputFloat = new float[FRAME_SIZE];

                while (_unprocessedBuffer.Count >= FRAME_SIZE_BYTES)
                {
                    for (int i = 0; i < FRAME_SIZE_BYTES; i++) inputBytes[i] = _unprocessedBuffer[i];
                    _unprocessedBuffer.RemoveRange(0, FRAME_SIZE_BYTES);

                    for (int i = 0; i < FRAME_SIZE; i++)
                    {
                        inputFloat[i] = BitConverter.ToInt16(inputBytes, i * BYTES_PER_SAMPLE) / 32768f;
                    }

                    RNNoise.rnnoise_process_frame(_rnnoiseState, outputFloat, inputFloat);

                    for (int i = 0; i < FRAME_SIZE; i++)
                    {
                        short outSample = (short)(Math.Clamp(outputFloat[i], -1f, 1f) * 32767);
                        byte[] sampleBytes = BitConverter.GetBytes(outSample);
                        inputBytes[i * BYTES_PER_SAMPLE] = sampleBytes[0];
                        inputBytes[i * BYTES_PER_SAMPLE + 1] = sampleBytes[1];
                    }
                    _tempDenoisedStream.Write(inputBytes, 0, FRAME_SIZE_BYTES);
                }
            }
            catch (Exception ex)
            {
                // Log errors on this background thread silently to avoid crashing the app.
                Trace.WriteLine($"Error during audio data processing: {ex.Message}");
            }
        }

        public void ProcessAndAmplifyAudio()
        {
            try
            {
                if (!File.Exists(tempDenoisedPath)) return;

                const float gainFactor = 1.5f;
                var waveFormat = new WaveFormat(SAMPLE_RATE, 16, 1);

                using (var rawReader = new RawSourceWaveStream(File.OpenRead(tempDenoisedPath), waveFormat))
                using (var finalWriter = new WaveFileWriter(denoisedFilePath, waveFormat))
                {
                    var sampleProvider = rawReader.ToSampleProvider();
                    float[] buffer = new float[1024];
                    int read;
                    while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        for (int i = 0; i < read; i++)
                        {
                            buffer[i] = Math.Clamp(buffer[i] * gainFactor, -1.0f, 1.0f);
                        }
                        finalWriter.WriteSamples(buffer, 0, read);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to process the recorded audio.\nThe final audio file may be corrupt or missing.\n\nDetails: {ex.Message}", "Audio Processing Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        public void SendCommand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string command = CommandInput.Text.Trim();
                if (!string.IsNullOrEmpty(command))
                {
                    llm l1 = new llm(command);
                    CommandInput.Clear();
                    LoadHistoryData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to process command.\n\nDetails: {ex.Message}", "Command Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SendCommand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendCommand_Click(sender, e);
            }
        }

        public void LoadHistoryData()
        {
            try
            {
                CommandHistoryList.ItemsSource = new List<LLM_Detail>(Global.total_commands.Reverse());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load command history.\n\nDetails: {ex.Message}", "UI Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public void CommandInput_GotFocus(object sender, RoutedEventArgs e) { }
        public void CommandInput_LostFocus(object sender, RoutedEventArgs e) { }
        public void VoiceToggle_Checked() { }
        public void VoiceToggle_Unchecked() { }
        private void onLoad(object sender, RoutedEventArgs e) { }
    }
}