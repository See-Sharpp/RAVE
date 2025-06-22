using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using System.Windows.Media.Effects;
using Microsoft.Extensions.Configuration;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfApp2
{
    public partial class Dashboard : Page
    {
        private WaveInEvent? waveIn;
        private WaveFileWriter? _denoisedWriter;
        private string denoisedFilePath = "denoised_output.wav";

        
        private TaskCompletionSource<bool> recordingStoppedTcs = new TaskCompletionSource<bool>();

   
        private static string? api;
        private WakeWordHelper? _wakeWordDetector;

        private IntPtr _rnnoiseState = IntPtr.Zero;
        private readonly List<byte> _unprocessedBuffer = new List<byte>();


        private string tempDenoisedPath = "temp_denoised.raw";
        private FileStream? _tempDenoisedStream;


        public Dashboard()
        {
            InitializeComponent();

            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("AppSetting.json", optional: true, reloadOnChange: true)
                .Build();

            api = config["Groq_Api_Key"] ?? throw new InvalidOperationException("APIKey not found in configuration.");
        }
        

        private async void OnWakeWordDetected()
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                _wakeWordDetector?.Pause();
                System.Windows.MessageBox.Show("Hey Jarvis Detected!");
                string? result = await HandelVoiceInput(this, new RoutedEventArgs());
                if (!string.IsNullOrEmpty(result))
                {
                    llm l1 = new llm(result);

                }
                _wakeWordDetector?.Resume();
            });
        }

        private async void ToggleVoice_Click(object sender, RoutedEventArgs e)
        {
            string result = await HandelVoiceInput(sender, e);
            if (!string.IsNullOrEmpty(result))
            {
                CommandInput.Clear();
                CommandInput.AppendText(result);
                SendCommand_Click(this, new RoutedEventArgs());
            }
            else
            {
                CommandInput.Clear();
                CommandInput.AppendText("No voice input detected.");
            }
        }
        

        private async Task<string> HandelVoiceInput(object sender, RoutedEventArgs e)
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
                CommandInput.Clear();
                CommandInput.AppendText(result);
                return result;
            }
            else
            {
                CommandInput.Clear();
                CommandInput.AppendText("Denoised audio file was not created or is empty.");
                return string.Empty;
            }
        }

        private async Task<string> TranscribeAsync(string audioFilePath)
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

        private void StartRecording()
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

            
            _tempDenoisedStream = new FileStream(tempDenoisedPath, FileMode.Create, FileAccess.Write);

            
            waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 16, 1) 
            };

            recordingStoppedTcs = new TaskCompletionSource<bool>();

            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.RecordingStopped += WaveIn_RecordingStopped;

            
            _rnnoiseState = RNNoise.rnnoise_create();

            waveIn.StartRecording();
        }


        private async Task StopRecording()
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

        
        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (_tempDenoisedStream == null || e.BytesRecorded == 0) return;

            
            _unprocessedBuffer.AddRange(e.Buffer.Take(e.BytesRecorded));

            
            const int frameSize = 160;   
            const int bytesPerSample = 2;
            const int frameSizeBytes = frameSize * bytesPerSample;

            
            float[] inputFloatSamples = new float[frameSize];
            float[] outputFloatSamples = new float[frameSize];
            byte[] processedFrameBytes = new byte[frameSizeBytes];

           
            while (_unprocessedBuffer.Count >= frameSizeBytes)
            {
                
                byte[] frameToProcess = _unprocessedBuffer.Take(frameSizeBytes).ToArray();

                
                for (int i = 0; i < frameSize; i++)
                {
                    short sample = BitConverter.ToInt16(frameToProcess, i * bytesPerSample);
                    inputFloatSamples[i] = sample / 32768f; 
                }
                RNNoise.rnnoise_process_frame(_rnnoiseState, outputFloatSamples, inputFloatSamples);

                
                for (int i = 0; i < frameSize; i++)
                {
                    
                    short outSample = (short)(Math.Clamp(outputFloatSamples[i], -1f, 1f) * 32767);
                    byte[] sampleBytes = BitConverter.GetBytes(outSample);
                    processedFrameBytes[i * bytesPerSample] = sampleBytes[0];
                    processedFrameBytes[i * bytesPerSample + 1] = sampleBytes[1];
                }

                
                _tempDenoisedStream.Write(processedFrameBytes, 0, frameSizeBytes);

                
                _unprocessedBuffer.RemoveRange(0, frameSizeBytes);
            }
        }

        
        private void ProcessAndAmplifyAudio()
        {
            if (!File.Exists(tempDenoisedPath)) return;

            
            const float gainFactor = 2.0f;

            
            var waveFormat = new WaveFormat(16000, 16, 1);

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

        private void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            
            if (_unprocessedBuffer.Count > 0 && _tempDenoisedStream != null)
            {
                
                int bytesToPad = (160 * 2) - _unprocessedBuffer.Count;
                _unprocessedBuffer.AddRange(new byte[bytesToPad]);

                
                byte[] finalFrame = _unprocessedBuffer.ToArray();
                float[] inSamples = new float[160];
                float[] outSamples = new float[160];
                for (int i = 0; i < 160; i++)
                {
                    inSamples[i] = BitConverter.ToInt16(finalFrame, i * 2) / 32768f;
                }
                RNNoise.rnnoise_process_frame(_rnnoiseState, outSamples, inSamples);
                byte[] finalBytes = new byte[320];
                for (int i = 0; i < 160; i++)
                {
                    short outSample = (short)(Math.Clamp(outSamples[i], -1f, 1f) * 32767);
                    var sampleBytes = BitConverter.GetBytes(outSample);
                    finalBytes[i * 2] = sampleBytes[0];
                    finalBytes[i * 2 + 1] = sampleBytes[1];
                }
                _tempDenoisedStream.Write(finalBytes, 0, finalBytes.Length);
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

        
        
        private void CommandInput_GotFocus(object sender, RoutedEventArgs e) { }
        private void CommandInput_LostFocus(object sender, RoutedEventArgs e) { }
        private void SendCommand_Click(object sender, RoutedEventArgs e)
        {
            string command = CommandInput.Text.Trim();
            if (!string.IsNullOrEmpty(command))
            {
                
            }
        }
        private void VoiceToggle_Checked(object sender, RoutedEventArgs e)
        {
            VoiceControlPanel.Effect = new BlurEffect { Radius = 3.5 };
            MicEffect.IsEnabled = false;
            SendEffect.IsEnabled = false;
            CommandInput.IsReadOnly = true;
            CommandInput.Cursor = Cursors.Arrow;
            _wakeWordDetector = new WakeWordHelper("model/hey_jarvis_v0.1.onnx", OnWakeWordDetected);
            
            Task.Run(() => _wakeWordDetector.Start());
        }

        private void VoiceToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            VoiceControlPanel.Effect = null;
            MicEffect.IsEnabled = true;
            SendEffect.IsEnabled = true;
            CommandInput.IsReadOnly = false;
            CommandInput.Cursor = Cursors.IBeam;
            if (_wakeWordDetector != null)
            {
                _wakeWordDetector.Stop();
                _wakeWordDetector = null;
            }
        }
    }
}
