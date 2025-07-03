using System.Windows;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NAudio.Wave;

namespace WpfApp2
{
    public class Verification
    {
        public bool verify()
        {

            string modelPath = "voxceleb_ECAPA512_LM.onnx";
            string audioPath1 = "speaker_output.wav";
            string audioPath2 = "denoised_output.wav";

            var session = new InferenceSession(modelPath);

            // Get raw float audio data
            float[] audio1 = LoadWavAsMonoFloat(audioPath1);
            float[] audio2 = LoadWavAsMonoFloat(audioPath2);

            float[] emb1 = GetSpeakerEmbedding(session, audio1);
            float[] emb2 = GetSpeakerEmbedding(session, audio2);

            // Compare using cosine similarity
            double similarity = CosineSimilarity(emb1, emb2);
            MessageBox.Show(""+similarity);

            if (similarity > 0.75)
                return true;
            else
                return false;
        }

        static float[] LoadWavAsMonoFloat(string filePath)
        {
            using var reader = new AudioFileReader(filePath);
            if (reader.WaveFormat.SampleRate != 16000)
                throw new Exception("Model expects 16kHz sample rate.");

            var floatSamples = new List<float>();
            float[] buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
            int read;

            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                floatSamples.AddRange(buffer.Take(read));

            return floatSamples.ToArray();
        }

        static float[] GetSpeakerEmbedding(InferenceSession session, float[] audioData)
        {
            int sampleLength = audioData.Length;
            var inputTensor = new DenseTensor<float>(new[] { 1, sampleLength });

            for (int i = 0; i < sampleLength; i++)
                inputTensor[0, i] = audioData[i];

            var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input", inputTensor)
        };

            using var results = session.Run(inputs);
            var embedding = results.First().AsEnumerable<float>().ToArray();

            return embedding;
        }

        static double CosineSimilarity(float[] a, float[] b)
        {
            double dot = 0, magA = 0, magB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }
            return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
        }
    }
}
