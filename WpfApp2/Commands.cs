using GroqSharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WpfApp2.Context;
using WpfApp2.Models;


namespace WpfApp2
{
    public class Commands
    {
        public static ApplicationDbContext? _context;
        public static SignUpDetail? userId;
        private static readonly tokenizer? _tokenizer;
        private static readonly InferenceSession? _session;
        private static readonly Dictionary<string, float[]> embeddingCache = new Dictionary<string, float[]>();

        static Commands()
        {
            try
            {
                string tokenizerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tokenizer", "vocab.txt");
                string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model", "mini-lm.onnx");

                if (!File.Exists(tokenizerPath) || !File.Exists(modelPath))
                {
                    throw new FileNotFoundException("A required AI model or tokenizer file is missing.");
                }

                _tokenizer = new tokenizer(tokenizerPath);
                _session = new InferenceSession(modelPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"A critical error occurred while loading the AI model files.\nThe application cannot function without them and will now close.\n\nPlease ensure 'tokenizer' and 'model' folders are present and accessible.\n\nDetails: {ex.Message}", "Fatal Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }

        public Commands()
        {
            try
            {
                _context = new ApplicationDbContext();
                userId = _context.SignUpDetails.FirstOrDefault(u => u.Id == Global.UserId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not establish a connection to the database.\nHistory and other features may not work correctly.\n\nDetails: {ex.Message}", "Database Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public static async Task systemCommand(string command, string search_query, string contentString, string content)
        {
            try
            {
                string nircmdPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nircmd-x64", "nircmd.exe");

                if (command.Contains("savescreenshot"))
                {
                    if (string.IsNullOrEmpty(Global.deafultScreenShotPath))
                    {
                        throw new InvalidOperationException("Default screenshot path is not set.");
                    }
                    long current = Properties.Settings.Default.MediaCounter;
                    string temp = Path.Combine(Global.deafultScreenShotPath, $"shot{current}.png");
                    Properties.Settings.Default.MediaCounter = current + 1;
                    Properties.Settings.Default.Save();
                    command += $" \"{temp}\"";
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = nircmdPath,
                    Arguments = command,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (Properties.Settings.Default.History && _context != null)
                {
                    var entity = new LLM_Detail
                    {
                        UserId = (int)Global.UserId,
                        Expected_json = contentString,
                        user_command = content,
                        Status = "Success",
                        CommandTime = DateTime.Now,
                        CommandType = "system_control"
                    };
                    await AddHistoryRecordAsync(_context, entity);
                    if (Global.system_control.Count >= 20) { Global.system_control.Dequeue(); }
                    if (Global.total_commands.Count >= 20) { Global.total_commands.Dequeue(); }
                    Global.system_control.Enqueue(entity);
                    Global.total_commands.Enqueue(entity);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Failed to execute system command.\nEnsure 'nircmd.exe' exists and is accessible.\n\nDetails: {exception.Message}", "Command Execution Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (Properties.Settings.Default.History && _context != null)
                {
                    var entity = new LLM_Detail
                    {
                        UserId = (int)Global.UserId,
                        Expected_json = contentString,
                        user_command = content,
                        Status = "Failed",
                        CommandTime = DateTime.Now,
                        CommandType = "system_control"
                    };
                    await AddHistoryRecordAsync(_context, entity);
                    if (Global.system_control.Count >= 20) { Global.system_control.Dequeue(); }
                    if (Global.total_commands.Count >= 20) { Global.total_commands.Dequeue(); }
                    Global.system_control.Enqueue(entity);
                    Global.total_commands.Enqueue(entity);
                }
            }
        }

        public static async Task application_command(string application, string contentString, string content)
        {
            try
            {
                string connectionString = @"Provider=Search.CollatorDSO;Extended Properties='Application=Windows'";
                string query = $"SELECT TOP 1 System.ItemPathDisplay FROM SYSTEMINDEX WHERE CONTAINS(System.ItemName, '\"{application}*\"')";
                string? path = null;

                using (var connection = new OleDbConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var dbCommand = new OleDbCommand(query, connection))
                    using (var reader = await dbCommand.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            path = reader["System.ItemPathDisplay"]?.ToString();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(path))
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                    if (Properties.Settings.Default.History && _context != null)
                    {
                        var entity = new LLM_Detail
                        {
                            UserId = (int)Global.UserId,
                            Expected_json = contentString,
                            user_command = content,
                            Status = "Success",
                            CommandTime = DateTime.Now,
                            CommandType = "application_control"
                        };
                        await AddHistoryRecordAsync(_context, entity);
                        if (Global.application_control.Count >= 20) { Global.application_control.Dequeue(); }
                        if (Global.total_commands.Count >= 20) { Global.total_commands.Dequeue(); }
                        Global.application_control.Enqueue(entity);
                        Global.total_commands.Enqueue(entity);
                    }
                }
                else
                {
                    await SearchInDatabase(application, contentString, content);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to find '{application}' via Windows Search.\nThis can happen if the service is disabled. Falling back to internal database search.\n\nDetails: {ex.Message}", "Application Search Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                await SearchInDatabase(application, contentString, content);
            }
        }

        public static async Task SearchInDatabase(string application, string contentString, string content)
        {
            try
            {
                using var dbContext = new ApplicationDbContext();
                var allExes = await dbContext.AllExes.Where(e => e.DisplayName != null && e.FilePath != null).ToListAsync();

                if (!allExes.Any())
                {
                    Process.Start("nircmd.exe", "speak text \"Application Not Found\"");
                    return;
                }

                var queryEmbedding = GetEmbedding(application);
                var results = allExes.AsParallel()
                    .Select(exe =>
                    {
                        var embedding = string.IsNullOrEmpty(exe.Embedding)
                            ? GetEmbedding(exe.DisplayName)
                            : exe.Embedding.Split(',').Select(float.Parse).ToArray();
                        double sim = CosineSimilarity(queryEmbedding, embedding);
                        return new { exe.DisplayName, exe.FilePath, sim };
                    })
                    .OrderByDescending(x => x.sim)
                    .FirstOrDefault();

                if (results?.FilePath != null && results.sim > 0.87f)
                {
                    Process.Start(new ProcessStartInfo(results.FilePath) { UseShellExecute = true });
                    if (Properties.Settings.Default.History)
                    {
                        var entity = new LLM_Detail
                        {
                            UserId = (int)Global.UserId,
                            Expected_json = contentString,
                            user_command = content,
                            Status = "Success",
                            CommandTime = DateTime.Now,
                            CommandType = "application_control"
                        };
                        await AddHistoryRecordAsync(dbContext, entity);
                        if (Global.application_control.Count >= 20) { Global.application_control.Dequeue(); }
                        if (Global.total_commands.Count >= 20) { Global.total_commands.Dequeue(); }
                        Global.application_control.Enqueue(entity);
                        Global.total_commands.Enqueue(entity);
                    }
                }
                else
                {
                    Process.Start("nircmd.exe", "speak text \"Application Not Found\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while searching for '{application}' in the database.\n\nDetails: {ex.Message}", "Database Search Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static async Task file_command(string fileName, string contentString, string content)
        {
            try
            {
                await SearchForDocsInDatabase(fileName, contentString, content);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred while processing the file command.\n\nDetails: {ex.Message}", "File Command Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (Properties.Settings.Default.History && _context != null)
                {
                    var entity = new LLM_Detail { /*...Failed History...*/ };
                    await AddHistoryRecordAsync(_context, entity);
                }
            }
        }

        public static async Task SearchForDocsInDatabase(string filename, string contentString, string content)
        {
            try
            {
                using var dbContext = new ApplicationDbContext();
                var allDocs = await dbContext.AllDocs.Where(d => d.DisplayName != null && d.FilePath != null).ToListAsync();

                if (!allDocs.Any())
                {
                    Process.Start("nircmd.exe", "speak text \"File Not Found\"");
                    return;
                }

                float[] queryEmbedding = GetEmbedding(filename.ToLower());
                var results = allDocs.AsParallel()
                    .Select(doc =>
                    {
                        float[] embedding = string.IsNullOrEmpty(doc.Embedding) ? GetEmbedding(doc.DisplayName) : doc.Embedding.Split(',').Select(float.Parse).ToArray();
                        double sim = CosineSimilarity(queryEmbedding, embedding);
                        return new { doc.DisplayName, doc.FilePath, sim };
                    })
                    .OrderByDescending(x => x.sim)
                    .FirstOrDefault();

                if (results?.FilePath != null && results.sim > 0.80f)
                {
                    Process.Start(new ProcessStartInfo(results.FilePath) { UseShellExecute = true });
                    if (Properties.Settings.Default.History)
                    {
                        var entity = new LLM_Detail { /*...Success History...*/ };
                        await AddHistoryRecordAsync(dbContext, entity);
                    }
                }
                else
                {
                    Process.Start("nircmd.exe", "speak text \"File Not Found. Please try again.\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while searching for the file '{filename}'.\n\nDetails: {ex.Message}", "File Search Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static async Task searchBrowser(string command, string search_query, string contentString, string content)
        {
            try
            {
                string urlToOpen = command;
                if (!string.IsNullOrEmpty(search_query))
                {
                    urlToOpen = command.Replace("{{q}}", Uri.EscapeDataString(search_query));
                }

                // Use ShellExecute to open the default browser, which is more reliable.
                Process.Start(new ProcessStartInfo(urlToOpen) { UseShellExecute = true });

                if (Properties.Settings.Default.History && _context != null)
                {
                    var entity = new LLM_Detail
                    {
                        UserId = (int)Global.UserId,
                        Expected_json = contentString,
                        user_command = content,
                        Status = "Success",
                        CommandTime = DateTime.Now,
                        CommandType = "web_browse"
                    };
                    await AddHistoryRecordAsync(_context, entity);
                    if (Global.web_browse.Count >= 20) { Global.web_browse.Dequeue(); }
                    if (Global.total_commands.Count >= 20) { Global.total_commands.Dequeue(); }
                    Global.web_browse.Enqueue(entity);
                    Global.total_commands.Enqueue(entity);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open the browser or perform the search.\n\nDetails: {ex.Message}", "Browser Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (Properties.Settings.Default.History && _context != null)
                {
                    var entity = new LLM_Detail
                    {
                        UserId = (int)Global.UserId,
                        Expected_json = contentString,
                        user_command = content,
                        Status = "Failed",
                        CommandTime = DateTime.Now,
                        CommandType = "web_browse"
                    };
                    await AddHistoryRecordAsync(_context, entity);
                }
            }
        }

        public static float[] GetCachedEmbedding(string text)
        {
            if (embeddingCache.TryGetValue(text, out var cached))
                return cached;

            var embedding = GetEmbedding(text);
            embeddingCache[text] = embedding;
            return embedding;
        }

        public static float[] GetEmbedding(string text, int maxLen = 128)
        {
            try
            {
                if (_tokenizer == null || _session == null)
                {
                    throw new InvalidOperationException("AI models are not initialized.");
                }

                var (ids, mask, typeIds) = _tokenizer.Encode(text, maxLen);
                var idTensor = new DenseTensor<long>(ids, new[] { 1, ids.Length });
                var maskTensor = new DenseTensor<long>(mask, new[] { 1, mask.Length });
                var typeTensor = new DenseTensor<long>(typeIds, new[] { 1, typeIds.Length });

                using var results = _session.Run(new[]
                {
                    NamedOnnxValue.CreateFromTensor("input_ids", idTensor),
                    NamedOnnxValue.CreateFromTensor("attention_mask", maskTensor),
                    NamedOnnxValue.CreateFromTensor("token_type_ids", typeTensor)
                });

                return ExtractEmbeddingVector(results.First().AsTensor<float>());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not generate AI embedding for text: '{text}'.\nThis may indicate a problem with the ONNX model.\n\nDetails: {ex.Message}", "AI Model Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return Array.Empty<float>();
            }
        }

        private static float[] ExtractEmbeddingVector(Tensor<float> outputTensor)
        {
            var dims = outputTensor.Dimensions.ToArray();
            if (dims.Length == 3)
            {
                int hiddenDim = dims[2];
                return outputTensor.ToArray().Take(hiddenDim).ToArray();
            }
            if (dims.Length == 2)
            {
                return outputTensor.ToArray();
            }
            throw new InvalidOperationException($"Unexpected output dimensions: [{string.Join(",", dims)}]");
        }

        public static double CosineSimilarity(float[] a, float[] b)
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

        public static async Task AddHistoryRecordAsync(ApplicationDbContext context, LLM_Detail newDetail)
        {
            try
            {
                if (context == null) return;
                context.LLM_Detail.Add(newDetail);
                await context.SaveChangesAsync();

                const int maxRecords = 500;
                int recordCount = await context.LLM_Detail.CountAsync();
                if (recordCount > maxRecords)
                {
                    int recordsToDeleteCount = recordCount - maxRecords;
                    await context.LLM_Detail
                        .OrderBy(detail => detail.CommandTime)
                        .Take(recordsToDeleteCount)
                        .ExecuteDeleteAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save the command to your history.\n\nDetails: {ex.Message}", "History Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}