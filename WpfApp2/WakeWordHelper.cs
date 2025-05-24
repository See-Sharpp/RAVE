using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WpfApp2
{
    internal class WakeWordHelper
    {
        private readonly InferenceSession session;
        private readonly Action onDetected;
        private readonly int sampleRate = 16000;
        private readonly int frameSize = 512;

        private WaveInEvent waveIn;

        private const int sampleWindow = 1536; // 16x96
        private readonly float[] rollingBuffer = new float[sampleWindow];
        private int rollingIndex = 0;
        private bool bufferFilled = false;

        private DateTime lastDetectionTime = DateTime.MinValue;
        private readonly TimeSpan detectionCooldown = TimeSpan.FromSeconds(2);
        private bool isCurrentlyActive = false;


        public WakeWordHelper(string modelPath, Action onWakeWordDetected)
        {
            session = new InferenceSession(modelPath);
            onDetected = onWakeWordDetected;
        }

        public void Start()
        {
            waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(sampleRate, 16, 1),
                BufferMilliseconds = (int)((double)frameSize / sampleRate * 1000.0)
            };

            waveIn.DataAvailable += OnDataAvailable;
            waveIn.StartRecording();
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            var newSamples = ConvertToFloat(e.Buffer, e.BytesRecorded);

            // Add new samples to rolling buffer
            for (int i = 0; i < newSamples.Length; i++)
            {
                rollingBuffer[rollingIndex] = newSamples[i];
                rollingIndex = (rollingIndex + 1) % sampleWindow;
                if (rollingIndex == 0) bufferFilled = true;
            }

            if (!bufferFilled) return; 

            // Create input audio in the right rolling order
            float[] audioInput = new float[sampleWindow];
            int index = rollingIndex;
            for (int i = 0; i < sampleWindow; i++)
            {
                audioInput[i] = rollingBuffer[index];
                index = (index + 1) % sampleWindow;
            }

            // Reshape to [1, 16, 96]
            var tensorData = new float[1 * 16 * 96];
            for (int i = 0; i < 16; i++)
                for (int j = 0; j < 96; j++)
                    tensorData[i * 96 + j] = audioInput[i * 96 + j];

            var tensor = new DenseTensor<float>(tensorData, new[] { 1, 16, 96 });
            var input = NamedOnnxValue.CreateFromTensor("onnx::Flatten_0", tensor);

            using var results = session.Run(new[] { input });
            var output = results.First().AsEnumerable<float>().ToArray();

            float confidence = output[0];
            //System.Windows.MessageBox.Show($"Confidence: {output[0]:F4}");

            if (confidence > 0.8f && !isCurrentlyActive)
            {
                var now = DateTime.UtcNow;
                if ((now - lastDetectionTime) > detectionCooldown)
                {
                    lastDetectionTime = now;
                    isCurrentlyActive = true;
                 
                    onDetected?.Invoke();

                    Task.Delay(500).ContinueWith(_ =>
                    {
                        Array.Clear(rollingBuffer, 0, rollingBuffer.Length);
                        rollingIndex = 0;
                        bufferFilled = false;
                        isCurrentlyActive = false;
                    });
                }
            }
        }

        private float[] ConvertToFloat(byte[] buffer, int bytesRecorded)
        {
            short[] intData = new short[bytesRecorded / 2];
            Buffer.BlockCopy(buffer, 0, intData, 0, bytesRecorded);
            return intData.Select(i => i / 32768f).ToArray();
        }
    }
}
