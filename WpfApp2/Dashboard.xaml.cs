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

namespace WpfApp2
{
    public partial class Dashboard : Page
    {
        private WaveInEvent? waveIn;
        private WaveFileWriter? _denoisedWriter;
        private string denoisedFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "denoised_output.wav");


        private TaskCompletionSource<bool> recordingStoppedTcs = new TaskCompletionSource<bool>();


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
            InitializeComponent();



            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("AppSetting.json", optional: true, reloadOnChange: true)
                .Build();

            api = config["Groq_Api_Key"] ?? throw new InvalidOperationException("APIKey not found in configuration.");
            _context = new ApplicationDbContext();

            if (!WpfApp2.Properties.Settings.Default.Is_First)
            {
                Global.web_browse = new Queue<LLM_Detail>(_context.LLM_Detail.Where(x => x.CommandType == "web_browse").OrderByDescending(x => x.CommandTime).Take(30));
                Global.file_operation = new Queue<LLM_Detail>(_context.LLM_Detail.Where(x => x.CommandType == "file_operation").OrderByDescending(x => x.CommandTime).Take(30));
                Global.application_control = new Queue<LLM_Detail>(_context.LLM_Detail.Where(x => x.CommandType == "application_control").OrderByDescending(x => x.CommandTime).Take(30));
                Global.system_control = new Queue<LLM_Detail>(_context.LLM_Detail.Where(x => x.CommandType == "system_control").OrderByDescending(x => x.CommandTime).Take(30));
                Global.total_commands = new Queue<LLM_Detail>(_context.LLM_Detail.OrderByDescending(x => x.CommandTime).Take(30));

                WpfApp2.Properties.Settings.Default.Is_First = true;
                Properties.Settings.Default.Save();
            }
        }

       


        public async void ToggleVoice_Click(object sender, RoutedEventArgs e)
        {
            string result = await HandelVoiceInput(sender, e);
            if (!string.IsNullOrEmpty(result))
            {
                CommandInput.Clear();
                CommandInput.AppendText(result);
                SendCommand_Click(this, new RoutedEventArgs());
            }
            else if(string.IsNullOrEmpty(result))
            {
                CommandInput.Clear();
                CommandInput.AppendText("No voice input detected");
            }
        }

        public async Task<string> HandelVoiceInput(object sender, RoutedEventArgs e)
        {
            CommandInput.Clear();
            CommandInput.AppendText("Listening...");
            StartRecording();


            await Task.Delay(5000);


            await StopRecording();

            if (File.Exists(denoisedFilePath) && new FileInfo(denoisedFilePath).Length > 0)
            {
                CommandInput.Clear();
                CommandInput.AppendText("Transcribing...");
                string result = await TranscribeAsync(denoisedFilePath);
                if (result != "error")
                {
                    CommandInput.Clear();
                    CommandInput.AppendText(result);
                }
                return result;
            }
            else
            {
                CommandInput.Clear();
                CommandInput.AppendText("Denoised audio file was not created or is empty.");
                return string.Empty;
            }
        }

        public async Task<string> TranscribeAsync(string audioFilePath)
        {
            if (Properties.Settings.Default.Speaker_Verification)
            {
                if (Verification.verify())
                {
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

                        MessageBox.Show($"Error during transcription: {response.ReasonPhrase}\n{responseJson}");
                        return string.Empty;
                    }

                    dynamic result = JsonConvert.DeserializeObject(responseJson);
                    return result?.text ?? string.Empty;
                }
                CommandInput.AppendText("Your Voice Did Not Match the authorized voice, Try Again");
                return "error";
            }
            else
            {
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

                    MessageBox.Show($"Error during transcription: {response.ReasonPhrase}\n{responseJson}");
                    return string.Empty;
                }

                dynamic result = JsonConvert.DeserializeObject(responseJson);
                return result?.text ?? string.Empty;
            }
        }

        public void StartRecording()
        {
            // Cleanup existing resources
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

            // Debug: Show temp file path
            string fullTempPath = Path.GetFullPath(tempDenoisedPath);

            _tempDenoisedStream = new FileStream(tempDenoisedPath, FileMode.Create, FileAccess.Write);

            // Use 48kHz for RNNoise compatibility
            waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(SAMPLE_RATE, 16, 1)
            };

            recordingStoppedTcs = new TaskCompletionSource<bool>();

            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.RecordingStopped += WaveIn_RecordingStopped;


            _rnnoiseState = RNNoise.rnnoise_create();

            waveIn.StartRecording();
        }

        public async Task StopRecording()
        {
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
        }

        public void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (_tempDenoisedStream == null || e.BytesRecorded == 0) return;


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
            // Debug: Check if temp file exists

            if (!File.Exists(tempDenoisedPath)) return;

            const float gainFactor = 1.5f;
            var waveFormat = new WaveFormat(SAMPLE_RATE, 16, 1);

            try
            {
                // Debug: Show full paths
                string fullTempPath = Path.GetFullPath(tempDenoisedPath);
                string fullDenoisedPath = Path.GetFullPath(denoisedFilePath);

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

                    // Debug: Show processing results
                }

                // Debug: Check if final file was created
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
            // Process any remaining data with proper padding
            if (_unprocessedBuffer.Count > 0 && _tempDenoisedStream != null)
            {
                // Calculate how many bytes we need to pad to reach a full frame
                int remainingBytes = _unprocessedBuffer.Count;
                if (remainingBytes < FRAME_SIZE_BYTES)
                {
                    // Pad with zeros to complete the frame
                    int bytesToPad = FRAME_SIZE_BYTES - remainingBytes;
                    _unprocessedBuffer.AddRange(new byte[bytesToPad]);
                }

                // Process the final frame
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
                    // Only write the original data length, not the padded portion
                    int bytesToWrite = Math.Min(remainingBytes, FRAME_SIZE_BYTES);
                    _tempDenoisedStream.Write(finalBytes, 0, bytesToWrite);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error writing final frame: {ex.Message}");
                }
                _tempDenoisedStream.Write(finalBytes, 0, finalBytes.Length);
            }
            _unprocessedBuffer.Clear();

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

        public void CommandInput_GotFocus(object sender, RoutedEventArgs e) { }
        public void CommandInput_LostFocus(object sender, RoutedEventArgs e) { }
        public void SendCommand_Click(object sender, RoutedEventArgs e)
        {
            string command = CommandInput.Text.Trim();
            if (!string.IsNullOrEmpty(command))
            {
                llm l1 = new llm(command);
                CommandInput.Clear();
            }
        }   

        private void SendCommand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string command = CommandInput.Text.Trim();
                if (!string.IsNullOrEmpty(command))
                {
                    llm l1 = new llm(command);
                    CommandInput.Clear();
                }
            }
        }

        public void VoiceToggle_Checked()
        {
            VoiceControlPanel.Effect = new BlurEffect { Radius = 3.5 };
            MicEffect.IsEnabled = false;
            SendEffect.IsEnabled = false;
            CommandInput.IsReadOnly = true;
            CommandInput.Cursor = Cursors.Arrow;

        }

        public void VoiceToggle_Unchecked()
        {
            VoiceControlPanel.Effect = null;
            MicEffect.IsEnabled = true;
            SendEffect.IsEnabled = true;
            CommandInput.IsReadOnly = false;
            CommandInput.Cursor = Cursors.IBeam;

        }

        private void onLoad(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.WakeWord == true)
            {
                VoiceToggle_Checked();
            }
            else
            {
                VoiceToggle_Unchecked();
            }
        }
    }
}