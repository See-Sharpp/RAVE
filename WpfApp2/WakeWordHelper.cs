using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NAudio.Wave;
using NWaves.FeatureExtractors;
using NWaves.FeatureExtractors.Options;
using NWaves.Windows;
using System.Diagnostics;


namespace WpfApp2
{
    internal class WakeWordHelper
    {
        private readonly InferenceSession session;
        private readonly Action onDetected;
        private readonly int sampleRate = 16000;
        private readonly int frameSize = 1536;

        private WaveInEvent waveIn;
        private const int sampleWindow = 16000;
        private readonly float[] rollingBuffer = new float[sampleWindow];
        private int rollingIndex = 0;
        private bool bufferFilled = false;

        private DateTime lastDetectionTime = DateTime.MinValue;
        private readonly TimeSpan detectionCooldown = TimeSpan.FromSeconds(2);
        private bool isCurrentlyActive = false;

        private readonly FilterbankExtractor fbExtractor;

        float max = 0f;

        public WakeWordHelper(string modelPath, Action onWakeWordDetected)
        {
            session = new InferenceSession(modelPath);
            onDetected = onWakeWordDetected;

            var fbOptions = new FilterbankOptions
            {
                SamplingRate = sampleRate,
                FeatureCount = 96,            
                FrameDuration = 0.025,
                HopDuration = 0.010,
                FilterBankSize = 96,           
                Window = WindowType.Hamming,
                NonLinearity = NonLinearityType.LogE
            };

            fbExtractor = new FilterbankExtractor(fbOptions);
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

            for (int i = 0; i < newSamples.Length; i++)
            {
                rollingBuffer[rollingIndex] = newSamples[i];
                rollingIndex = (rollingIndex + 1) % sampleWindow;
                if (rollingIndex == 0) bufferFilled = true;
            }

            if (!bufferFilled) return;

            float[] audioInput = new float[sampleWindow];
            int idx = rollingIndex;
            for (int i = 0; i < sampleWindow; i++)
            {
                audioInput[i] = rollingBuffer[idx];
                idx = (idx + 1) % sampleWindow;
            }

            var features = fbExtractor.ComputeFrom(audioInput).ToList();
            if (features.Count < 16) return;

            var sliced = features.Take(16).ToArray();
            var tensorData = sliced.SelectMany(f => f).ToArray(); 

            var tensor = new DenseTensor<float>(tensorData, new[] { 1, 16, 96 });
            var input = NamedOnnxValue.CreateFromTensor("x.1", tensor);

            using var results = session.Run(new[] { input });
            var rawScore = results.First().AsEnumerable<float>().First();
            float probability = 1f / (1f + MathF.Exp(-rawScore));
            float wakeWordProbability = 1f - probability;

            Debug.WriteLine($"Raw score: {rawScore}, Probability: {wakeWordProbability}");
            max = Math.Max(max, probability);
            Debug.WriteLine($"Max Probability: {max}");

            if (probability > 0.69f && !isCurrentlyActive)
            {
                var now = DateTime.UtcNow;
                if (now - lastDetectionTime > detectionCooldown)
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
