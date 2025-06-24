using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Shapes;
using WpfApp2.Context;


namespace WpfApp2
{

    public class Commands
    {
        private string temp = null; 
        public ApplicationDbContext _context;
        private static readonly tokenizer _tokenizer =
            new tokenizer(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tokenizer", "vocab.txt"));

        private static readonly InferenceSession _session =
            new InferenceSession(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model", "mini-lm.onnx"));

        public Commands() { 
   
        }




        public static void systemCommand(string command, string search_query)
        {
            try
            {
                string temp = null; // Declare a local variable to avoid using the instance field in a static method
                string nircmdPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nircmd-x64", "nircmd.exe");

                if (command.Contains("savescreenshot"))
                {
                    if (Global.deafultScreenShotPath == null)
                    {
                        throw new InvalidOperationException("Global.deafultScreenShotPath is null.");
                    }

                    long current = Properties.Settings.Default.MediaCounter;
                    temp = System.IO.Path.Combine(Global.deafultScreenShotPath, $"shot{current}.png");
                    Properties.Settings.Default.MediaCounter = current + 1;
                    Properties.Settings.Default.Save();

                    temp = System.IO.Path.Combine(Global.deafultScreenShotPath, "shot" + Properties.Settings.Default.MediaCounter + ".png");


                    command = command + " " + '"' + temp + '"';
                    Debug.WriteLine(command);
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = nircmdPath,
                    Arguments = command,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        }

        public static void application_command(string application)
        {

            string connectionString = @"Provider=Search.CollatorDSO;Extended Properties='Application=Windows'";
            string query = $@"
                SELECT TOP 1 System.ItemName, System.ItemPathDisplay
                FROM SYSTEMINDEX
                WHERE System.ItemName LIKE '%{application}%'
            ";

            try
            {
                using (OleDbConnection connection = new OleDbConnection(connectionString))
                {
                    connection.Open();

                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    {
                        using (OleDbDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string name = reader["System.ItemName"].ToString();
                                string path = reader["System.ItemPathDisplay"].ToString();

                                var process = new Process();
                                process.StartInfo.FileName = "cmd.exe";
                                process.StartInfo.Arguments = $"/C start \"\" \"{path}\"";
                                process.StartInfo.UseShellExecute = false; 
                                process.StartInfo.RedirectStandardOutput = true;
                                process.StartInfo.RedirectStandardError = true;
                                process.StartInfo.CreateNoWindow = true;

                                process.Start();

                                string output = process.StandardOutput.ReadToEnd();
                                string error = process.StandardError.ReadToEnd();

                                process.WaitForExit();
                                int exitCode = process.ExitCode;

                                if (exitCode != 0)
                                {
                                    SearchInDatabase(application);
                                }

                            }
                            else
                            {
                                SearchInDatabase(application);
                                //System.Windows.MessageBox.Show($"Application '{application}' not found in the database.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }


        }
        public static void SearchInDatabase(string application)
        {
            try
            {
                using var _context = new ApplicationDbContext();
                char firstChar = application.Trim().ToLower().FirstOrDefault();

                var allExes = _context.AllExes
                               .Where(e => !string.IsNullOrEmpty(e.DisplayName) && !string.IsNullOrEmpty
                               (e.FilePath) && e.DisplayName.ToLower().StartsWith(firstChar.ToString()))  
                               .Select(e => new
                               {
                                   e.DisplayName,
                                   e.FilePath,
                                   e.Embedding
                               })
                               .ToList();

                if (!allExes.Any())
                {
                    MessageBox.Show("No applications found in database.");
                    return;
                }

                var queryEmbedding = GetEmbedding("Sleeping Dogs");
                string bestPath = null;
                string bestName = null;
                double bestScore = -1;

                foreach (var exe in allExes)
                {
                    
                    string? embeddedString = exe.Embedding;
                    if(string.IsNullOrEmpty(embeddedString))
                    {
                        continue; 
                    }
                    float[] candidateEmb = embeddedString.Split(',').Select(s => float.Parse(s)).ToArray();
                    var sim = CosineSimilarity(queryEmbedding, candidateEmb);

                   

                    if (sim > bestScore)
                    {
                        bestScore = sim;
                        bestName = exe.DisplayName;
                        bestPath = exe.FilePath;
                    }
                }

                if (bestPath != null)
                {
                    //MessageBox.Show(
                    //    $"Best match: {bestName}\n" +
                    //    $"Path: {bestPath}\n" +
                    //    $"Similarity: {bestScore:F4}"
                    //);
                    Process.Start("cmd.exe", $"/C start \"\" \"{bestPath}\"");
                }
                else
                {
                    MessageBox.Show("No matching application found.");
                }


            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public static float[] GetEmbedding(string text, int maxLen = 128)
        {

            var (ids, mask, typeIds) = _tokenizer.Encode(text, maxLen);


            var idTensor = new DenseTensor<long>(ids, new[] {1, ids.Length });
            var maskTensor = new DenseTensor<long>(mask, new[] { 1, mask.Length });
            var typeTensor = new DenseTensor<long>(typeIds, new[] { 1, typeIds.Length });

            using var results = _session.Run(new[]
            {
                NamedOnnxValue.CreateFromTensor("input_ids",      idTensor),
                NamedOnnxValue.CreateFromTensor("attention_mask", maskTensor),
                NamedOnnxValue.CreateFromTensor("token_type_ids", typeTensor)
            });


            return ExtractEmbeddingVector(results.First().AsTensor<float>());
        }

        private static float[] ExtractEmbeddingVector(Tensor<float> outputTensor)
        {
            var dims = outputTensor.Dimensions.ToArray();
            if (dims.Length == 3)
            {
                // [1, seqLen, hiddenDim] → take first token
                int hiddenDim = dims[2];
                return outputTensor
                    .ToArray()
                    .Skip(0 * dims[1] * hiddenDim + 0 * hiddenDim)
                    .Take(hiddenDim)
                    .ToArray();
            }
            else if (dims.Length == 2)
            {
                // [1, hiddenDim]
                return outputTensor.ToArray();
            }
            else
            {
                throw new InvalidOperationException($"Unexpected output dimensions: [{string.Join(",", dims)}]");
            }
        }

        private static double CosineSimilarity(float[] a, float[] b)
        {
            double dot = 0, magA = 0, magB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }
            if (magA == 0 || magB == 0) return 0;
            return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
        }

        public static void file_command()
        {

        }

        public static void searchBrowser(string command,string search_query)
        {
            try
            {
                //    string url = "https://www.google.com/search?q=" + Uri.EscapeDataString(search_query);

                string urlPath = command.Replace("{{q}}", Uri.EscapeDataString((search_query)));
                string powershellPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell\\v1.0\\powershell.exe");

                int urlIndex = command.IndexOf("http");

                if (urlIndex!=-1)
                {
                   
                    string prefix = urlPath.Substring(0, urlIndex);
                    string url = urlPath.Substring(urlIndex);
                    urlPath = $"{prefix}\"{url}\"";
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = powershellPath,
                    Arguments = $"-NoProfile -Command {urlPath}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        }
    }
}
