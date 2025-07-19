using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NAudio.Wave;
using WpfApp2;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics;
using System.Numerics;



public static class Verification
{
    public static bool verify()
    {
        try
        {
            string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model/voxceleb_ECAPA512_LM.onnx");
            string audioPath1 = "speaker_output.wav";
            string audioPath2 = "denoised_output.wav";

            var session = new InferenceSession(modelPath);

            float[] audio1 = LoadWavAsMonoFloat(audioPath1);
            float[] audio2 = LoadWavAsMonoFloat(audioPath2);

            if (audio1.Length == 0 || audio2.Length == 0)
            {
                
                return false;
            }

            var (feats1, numFrames1, numMels1) = AudioFeatureExtractor.ExtractLogMelSpectrogram(audio1);
            var (feats2, numFrames2, numMels2) = AudioFeatureExtractor.ExtractLogMelSpectrogram(audio2);



            float[] emb1 = GetSpeakerEmbedding(session, feats1, numFrames1, numMels1);
            float[] emb2 = GetSpeakerEmbedding(session, feats2, numFrames2, numMels2);




            double similarity = Commands.CosineSimilarity(emb1, emb2);
            

            if (similarity > 0.75)
                return true;
            else
                return false;


        }
        catch(OnnxRuntimeException onnxEx)
        {
 
            return false;

        }
        catch (Exception ex)
        {

          
            return false;
        }

    }

    static float[] LoadWavAsMonoFloat(string filePath)
    {
        using var reader = new MediaFoundationReader(filePath);

        if (reader.WaveFormat.SampleRate != 16000 || reader.WaveFormat.Channels != 1)
        {
            var resampler = new MediaFoundationResampler(reader, new WaveFormat(16000, 1))
            {
                ResamplerQuality = 60
            };
            var sampleProvider = resampler.ToSampleProvider();
            var floatSamples = new List<float>();
            var buffer = new float[resampler.WaveFormat.SampleRate];
            int read;
            while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
            {
                floatSamples.AddRange(buffer.Take(read));
            }
            return floatSamples.ToArray();
        }
        else
        {
            var sampleProvider = reader.ToSampleProvider();
            var floatSamples = new List<float>();
            var buffer = new float[reader.WaveFormat.SampleRate];
            int read;
            while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
            {
                floatSamples.AddRange(buffer.Take(read));
            }
            return floatSamples.ToArray();
        }

    }

    static float[] GetSpeakerEmbedding(InferenceSession session, float[] features, int numFrames, int numMels)
    {
        var inputTensor = new DenseTensor<float>(new[] { 1, numFrames, numMels });

        for (int frame = 0; frame < numFrames; frame++)
        {
            for (int mel = 0; mel < numMels; mel++)
            {
                inputTensor[0, frame, mel] = features[frame * numMels + mel];
            }
        }

        string inputName = session.InputMetadata.Keys.First();

        var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
            };

        using var results = session.Run(inputs);
        return results.First().AsEnumerable<float>().ToArray();
    }

    public static class AudioFeatureExtractor
    {
        public static (float[] Spectrogram, int NumFrames, int NumMels) ExtractLogMelSpectrogram(float[] audio, int sampleRate = 16000, int numMels = 80, int fftSize = 400, int hopSize = 160)
        {
            double[] hammingWindow = MathNet.Numerics.Window.Hamming(fftSize);

            int numFrames = (audio.Length - fftSize) / hopSize + 1;
            var melSpectrogram = new List<float>();

            for (int i = 0; i < numFrames; i++)
            {
                float[] frame = new float[fftSize];
                Array.Copy(audio, i * hopSize, frame, 0, fftSize);

                var windowed = frame.Select((x, j) => x * (float)hammingWindow[j]).ToArray();

                Complex[] fftInput = windowed.Select(x => new Complex(x, 0)).ToArray();
                Fourier.Forward(fftInput, FourierOptions.Matlab);

                double[] power = fftInput.Take(fftSize / 2 + 1)
                                         .Select(c => c.Magnitude * c.Magnitude)
                                         .ToArray();

                float[] melEnergies = MelFilterBank.Apply(power, sampleRate, fftSize, numMels);
                var logMelEnergies = melEnergies.Select(e => (float)Math.Log(e + 1e-6)).ToArray();

                melSpectrogram.AddRange(logMelEnergies);
            }

            return (melSpectrogram.ToArray(), numFrames, numMels);
        }
    }
    public static class MelFilterBank
    {
        public static float[] Apply(double[] powerSpectrum, int sampleRate, int fftSize, int numMels)
        {
            int numFftBins = fftSize / 2 + 1;
            double fMin = 0;
            double fMax = sampleRate / 2.0;

            double Mel(double f) => 2595 * Math.Log10(1 + f / 700.0);
            double Hz(double m) => 700 * (Math.Pow(10, m / 2595.0) - 1);

            double melMin = Mel(fMin);
            double melMax = Mel(fMax);
            double[] melPoints = new double[numMels + 2];
            double melSpacing = (melMax - melMin) / (numMels + 1);

            for (int i = 0; i < melPoints.Length; i++)
            {
                melPoints[i] = melMin + i * melSpacing;
            }

            double[] hzPoints = melPoints.Select(Hz).ToArray();
            int[] binPoints = hzPoints.Select(f => (int)Math.Floor((fftSize + 1) * f / sampleRate)).ToArray();

            var filterBank = new float[numMels][];
            for (int m = 1; m <= numMels; m++)
            {
                filterBank[m - 1] = new float[numFftBins];
                int startBin = binPoints[m - 1];
                int centerBin = binPoints[m];
                int endBin = binPoints[m + 1];

                for (int k = startBin; k < centerBin; k++)
                {
                    if (centerBin != startBin)
                        filterBank[m - 1][k] = (k - startBin) / (float)(centerBin - startBin);
                }
                for (int k = centerBin; k < endBin; k++)
                {
                    if (endBin != centerBin)
                        filterBank[m - 1][k] = (endBin - k) / (float)(endBin - centerBin);
                }
            }

            float[] melEnergies = new float[numMels];
            for (int m = 0; m < numMels; m++)
            {
                float energy = 0;
                for (int k = 0; k < numFftBins; k++)
                {
                    energy += filterBank[m][k] * (float)powerSpectrum[k];
                }
                melEnergies[m] = energy;
            }

            return melEnergies;
        }
    }


}

