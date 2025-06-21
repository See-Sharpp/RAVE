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
using System.Windows.Media.Effects;
using Microsoft.Extensions.Configuration;
using System.Windows.Controls; // Added for Page
using MahApps.Metro.Controls.Dialogs;
using System.Windows.Input;


namespace WpfApp2
{
    public partial class Dashboard : Page 
    {
        private WaveInEvent? waveIn;
        private WaveFileWriter? writer;
        private string outputFilePath = "temp_voice_input.wav";
        private TaskCompletionSource<bool> recordingStoppedTcs = new TaskCompletionSource<bool>(); // Initialize to avoid null reference
        private static string api = null;
        private WakeWordHelper? _wakeWordDetector;

        
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
                llm l1 = new llm(result);
                _wakeWordDetector?.Resume();
            });
        }
        private async void ToggleVoice_Click(object sender, RoutedEventArgs e)
        {
           string result = await HandelVoiceInput(sender, e);
            if (result != null)
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
            CommandInput.AppendText("Start Speaking ...");
            StartRecording();

            await Task.Delay(3000); 

            await StopRecording();

            string result = await TranscribeAsync(outputFilePath);
            CommandInput.Clear();
            CommandInput.AppendText(result);
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
            waveIn.WaveFormat = new WaveFormat(16000, 1);
            recordingStoppedTcs = new TaskCompletionSource<bool>(); // Reset TCS for each new recording
            waveIn.DataAvailable += (s, a) =>
            {
                writer.Write(a.Buffer, 0, a.BytesRecorded);
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
            string command = CommandInput.Text.Trim();
            llm llm1 = new llm(command);
        }
        private void VoiceToggle_Checked(object sender, RoutedEventArgs e)
        {
            VoiceControlPanel.Effect = new BlurEffect { Radius = 3.5 };
            MicEffect.IsEnabled=false;
            SendEffect.IsEnabled=false;
            CommandInput.IsReadOnly=true;
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