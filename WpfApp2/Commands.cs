﻿using GroqSharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Data.OleDb;
using System.Diagnostics;
using System.Windows.Forms;
using WpfApp2.Context;
using WpfApp2.Models;


namespace WpfApp2
{

    public class Commands
    {
        public static ApplicationDbContext _context;
        public static SignUpDetail userId;
        private static readonly tokenizer _tokenizer =
            new tokenizer(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tokenizer", "vocab.txt"));


        private static readonly InferenceSession _session =
            new InferenceSession(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model", "mini-lm.onnx"));

        private static readonly Dictionary<string, float[]> embeddingCache = new Dictionary<string, float[]>();

        public Commands() {
            _context = new ApplicationDbContext();
            userId = _context.SignUpDetails.FirstOrDefault(u => u.Id == Global.UserId);
        }

        public static void systemCommand(string command, string search_query,string contentString,string content)
        {
            try
            {
                string temp = null; 
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
                    Arguments = contentString,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                if (Properties.Settings.Default.History)
                {
                    var entity = new LLM_Detail
                    {
                        SignUpDetail = userId,
                        Expected_json = contentString,
                        user_command = content,
                        Status = "Success",
                        CommandTime = DateTime.Now,
                        CommandType = "system_control"
                    };

                    _context.LLM_Detail.Add(entity);
                    _context.SaveChanges();

                    if (Global.system_control.Count >= 30)
                    {
                        Global.system_control.Dequeue();
                    }
                    if (Global.total_commands.Count >= 30)
                    {
                        Global.total_commands.Dequeue();
                    }
                    Global.system_control.Enqueue(entity);
                    Global.total_commands.Enqueue(entity);
                }
            }
            catch (Exception exception)
            {
                if (Properties.Settings.Default.History)
                {
                    var entity = new LLM_Detail
                    {
                        SignUpDetail = userId,
                        Expected_json = contentString,
                        user_command = content,
                        Status = "Failed",
                        CommandTime = DateTime.Now,
                        CommandType = "system_control"
                    };
                    _context.LLM_Detail.Add(entity);
                    _context.SaveChanges();
                    if (Global.system_control.Count >= 30)
                    {
                        Global.system_control.Dequeue();
                    }
                    if (Global.total_commands.Count >= 30)
                    {
                        Global.total_commands.Dequeue();
                    }
                    Global.system_control.Enqueue(entity);
                    Global.total_commands.Enqueue(entity);
                }
                Debug.WriteLine(exception);
            }
        }

        public static void application_command(string application,string contentString,string content)
        {
            string com= $"nircmd.exe speak text \"Opening {application}\'";
            
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
                                Process.Start("cmd.exe", "/c " + com);
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
                                    SearchInDatabase(application,contentString,content);
                                }
                                else
                                {
                                    if (Properties.Settings.Default.History)
                                    {
                                        var entity = new LLM_Detail
                                        {
                                            SignUpDetail = userId,
                                            Expected_json = contentString,
                                            user_command = content,
                                            Status = "Success",
                                            CommandTime = DateTime.Now,
                                            CommandType = "application_control"
                                        };
                                        _context.LLM_Detail.Add(entity);
                                        _context.SaveChanges();
                                        if (Global.application_control.Count >= 30)
                                        {
                                            Global.application_control.Dequeue();
                                        }
                                        if (Global.total_commands.Count >= 30)
                                        {
                                            Global.total_commands.Dequeue();
                                        }
                                        Global.application_control.Enqueue(entity);
                                        Global.total_commands.Enqueue(entity);
                                    }
                                }

                            }
                            else
                            {
                                SearchInDatabase(application,contentString,content);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Properties.Settings.Default.History)
                {
                    var entity = new LLM_Detail
                    {
                        SignUpDetail = userId,
                        Expected_json = contentString,
                        user_command = content,
                        Status = "Failed",
                        CommandTime = DateTime.Now,
                        CommandType = "application_control"
                    };
                    _context.LLM_Detail.Add(entity);
                    _context.SaveChanges();
                    if (Global.application_control.Count >= 30)
                    {
                        Global.application_control.Dequeue();
                    }
                    if (Global.total_commands.Count >= 30)
                    {
                        Global.total_commands.Dequeue();
                    }
                    Global.application_control.Enqueue(entity);
                    Global.total_commands.Enqueue(entity);
                }
                Debug.WriteLine(ex.Message);
            }


        }

        public static void SearchInDatabase(string application,string contentString,string content)
        {
            try
            {
                string com = $"nircmd.exe speak text \"Opening {application}\'";

                using var _context = new ApplicationDbContext();
                char firstChar = application.Trim().ToLower().FirstOrDefault();

                var allExes = _context.AllExes
                     .Where(e => e.DisplayName != null && e.FilePath != null &&
                                 EF.Functions.Like(e.DisplayName, $"{firstChar}%"))
                     .Select(e => new { e.DisplayName, e.FilePath, e.Embedding })
                     .ToList();


                if (!allExes.Any())
                {
                    if (Properties.Settings.Default.History)
                    {
                        var entity = new LLM_Detail
                        {
                            SignUpDetail = userId,
                            Expected_json = contentString,
                            user_command = content,
                            Status = "Failed",
                            CommandTime = DateTime.Now,
                            CommandType = "application_control"
                        };
                        _context.LLM_Detail.Add(entity);
                        _context.SaveChanges();
                        if (Global.application_control.Count >= 30)
                        {
                            Global.application_control.Dequeue();
                        }
                        if (Global.total_commands.Count >= 30)
                        {
                            Global.total_commands.Dequeue();
                        }
                        Global.application_control.Enqueue(entity);
                        Global.total_commands.Enqueue(entity);
                    }
                    Process.Start("cmd.exe", "/c nircmd.exe speak text \"Application Not Found\"");
                    return;
                }

                var queryEmbedding = GetCachedEmbedding(application);
                string bestPath = null;
                string bestName = null;
                double bestScore = -1;

              
                var results = allExes
                .AsParallel()
                .Select(exe =>
                {
                    float[] embedding;
                    if (string.IsNullOrEmpty(exe.Embedding))
                        embedding = GetEmbedding(exe.DisplayName);
                    else
                        embedding = exe.Embedding.Split(',').Select(float.Parse).ToArray();

                    double sim = CosineSimilarity(queryEmbedding, embedding);

                    return new { exe.DisplayName, exe.FilePath, sim };
                })
                .OrderByDescending(x => x.sim)
                .FirstOrDefault();

                Debug.WriteLine(results.DisplayName + " " + results.FilePath, results.sim);

                if (results?.FilePath != null && results.sim > 0.87f)
                {
                    if (Properties.Settings.Default.History)
                    {
                        var entity = new LLM_Detail
                        {
                            SignUpDetail = userId,
                            Expected_json = contentString,
                            user_command = content,
                            Status = "Success",
                            CommandTime = DateTime.Now,
                            CommandType = "application_control"
                        };
                        _context.LLM_Detail.Add(entity);
                        _context.SaveChanges();
                        if (Global.application_control.Count >= 30)
                        {
                            Global.application_control.Dequeue();
                        }
                        if (Global.total_commands.Count >= 30)
                        {
                            Global.total_commands.Dequeue();
                        }
                        Global.application_control.Enqueue(entity);
                        Global.total_commands.Enqueue(entity);
                    }
                    Process.Start("cmd.exe", "/c " + com);
                    Process.Start("cmd.exe", $"/C start \"\" \"{results.FilePath}\"");
                }
                else
                {
                    if (Properties.Settings.Default.History)
                    {
                        var entity = new LLM_Detail
                        {
                            SignUpDetail = userId,
                            Expected_json = contentString,
                            user_command = content,
                            Status = "Failed",
                            CommandTime = DateTime.Now,
                            CommandType = "application_control"
                        };
                        _context.LLM_Detail.Add(entity);
                        _context.SaveChanges();
                        if (Global.application_control.Count >= 30)
                        {
                            Global.application_control.Dequeue();
                        }
                        if (Global.total_commands.Count >= 30)
                        {
                            Global.total_commands.Dequeue();
                        }
                        Global.application_control.Enqueue(entity);
                        Global.total_commands.Enqueue(entity);
                    }
                    Process.Start("cmd.exe", "/c nircmd.exe speak text \"Application Not Found\"");
                }
                
            }
            catch (Exception e)
            {
                if (Properties.Settings.Default.History)
                {
                    var entity = new LLM_Detail
                    {
                        SignUpDetail = userId,
                        Expected_json = contentString,
                        user_command = content,
                        Status = "Failed",
                        CommandTime = DateTime.Now,
                        CommandType = "application_control"
                    };
                    _context.LLM_Detail.Add(entity);
                    _context.SaveChanges();
                    if (Global.application_control.Count >= 30)
                    {
                        Global.application_control.Dequeue(); 
                    }
                    Global.application_control.Enqueue(entity);
                    if (Global.total_commands.Count >= 30)
                    {
                        Global.total_commands.Dequeue();
                    }
                    Global.total_commands.Enqueue(entity);
                }
                Debug.WriteLine(e.Message);
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
                
                int hiddenDim = dims[2];
                return outputTensor
                    .ToArray()
                    .Skip(0 * dims[1] * hiddenDim + 0 * hiddenDim)
                    .Take(hiddenDim)
                    .ToArray();
            }
            else if (dims.Length == 2)
            { 
                return outputTensor.ToArray();
            }
            else
            {
                throw new InvalidOperationException($"Unexpected output dimensions: [{string.Join(",", dims)}]");
            }
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

        public static void file_command(string fileName,string contentString,string content)
        {
            try
            {
                MessageBox.Show(fileName);
                string[] supportedTypes = { "pdf", "txt", "ppt", "docx" };
                string[] arr = fileName.Trim().Split(".");
                string fileType = arr[arr.Length - 1].ToLower();
                MessageBox.Show(fileType);

                MessageBox.Show("in " + fileType);
                SearchForDocsInDatabase(fileName,contentString,content);
            }
            catch(Exception e)
            {
                if (Properties.Settings.Default.History)
                {
                    var entity = new LLM_Detail
                    {
                        SignUpDetail = userId,
                        Expected_json = contentString,
                        user_command = content,
                        Status = "Failed",
                        CommandTime = DateTime.Now,
                        CommandType = "file_operation"
                    };
                    _context.LLM_Detail.Add(entity);
                    _context.SaveChanges();
                    if (Global.file_operation.Count >= 30)
                    {
                        Global.file_operation.Dequeue();
                    }
                    Global.file_operation.Enqueue(entity);
                    if (Global.total_commands.Count >= 30)
                    {
                        Global.total_commands.Dequeue();
                    }
                    Global.total_commands.Enqueue(entity);
                }
                Debug.WriteLine(e.Message);
            }
        }

        public static void SearchForDocsInDatabase(string filename,string contentString,string content)
        {
            try
            {
                using var _context = new ApplicationDbContext();
                float[] queryEmbedding = GetEmbedding(filename);

                var allDocs = _context.AllDocs.Where(d => d.DisplayName != null && d.FilePath != null).Select(d => new { d.DisplayName, d.FilePath, d.Embedding }).ToList();
                if (!allDocs.Any())
                {
                    if (Properties.Settings.Default.History)
                    {
                        var entity = new LLM_Detail
                        {
                            SignUpDetail = userId,
                            Expected_json = contentString,
                            user_command = content,
                            Status = "Failed",
                            CommandTime = DateTime.Now,
                            CommandType = "file_operation"
                        };
                        _context.LLM_Detail.Add(entity);
                        _context.SaveChanges();
                        if (Global.file_operation.Count >= 30)
                        {
                            Global.file_operation.Dequeue();
                        }
                        Global.file_operation.Enqueue(entity);
                        if (Global.total_commands.Count >= 30)
                        {
                            Global.total_commands.Dequeue();
                        }
                        Global.total_commands.Enqueue(entity);
                    }
                    Process.Start("cmd.exe", "/c nircmd.exe speak text \"File Not Found, Please Try Again.\" ");
                    return;
                }
                
                var results = allDocs
                .AsParallel()
                .Select(exe =>
                {
                    float[] embedding;
                    if (string.IsNullOrEmpty(exe.Embedding))
                        embedding = GetEmbedding(exe.DisplayName);
                    else
                        embedding = exe.Embedding.Split(',').Select(float.Parse).ToArray();

                    double sim = CosineSimilarity(queryEmbedding, embedding);

                    return new { exe.DisplayName, exe.FilePath, sim };
                })
                .OrderByDescending(x => x.sim)
                .FirstOrDefault();

                Debug.WriteLine(results.DisplayName + " " + results.FilePath, results.sim);

                MessageBox.Show(""+results.sim);

                if (results?.FilePath != null && results.sim > 0.80f)
                {
                    MessageBox.Show(
                        $"Best match: {results.DisplayName}\n" +
                        $"Path: {results.FilePath}\n" +
                        $"Similarity: {results.sim:F4}"
                    );
                    if (Properties.Settings.Default.History)
                    {
                        var entity = new LLM_Detail
                        {
                            SignUpDetail = userId,
                            Expected_json = contentString,
                            user_command = content,
                            Status = "Success",
                            CommandTime = DateTime.Now,
                            CommandType = "file_operation"
                        };
                        _context.LLM_Detail.Add(entity);
                        _context.SaveChanges();
                        if (Global.file_operation.Count >= 30)
                        {
                            Global.file_operation.Dequeue();
                        }
                        if (Global.total_commands.Count >= 30)
                        {
                            Global.total_commands.Dequeue();
                        }
                        Global.file_operation.Enqueue(entity);
                        Global.total_commands.Enqueue(entity);
                    }
                    Process.Start("cmd.exe", $"/C start \"\" \"{results.FilePath}\"");
                }
                else
                {
                    Process.Start("cmd.exe", "/c nircmd.exe speak text \"File Not Found. Ensure you said correct Name and Try Again.\"s ");
                    if (Properties.Settings.Default.History)
                    {
                        var entity = new LLM_Detail
                        {
                            SignUpDetail = userId,
                            Expected_json = contentString,
                            user_command = content,
                            Status = "Failed",
                            CommandTime = DateTime.Now,
                            CommandType = "file_operation"
                        };
                        _context.LLM_Detail.Add(entity);
                        _context.SaveChanges();
                        if (Global.file_operation.Count >= 30)
                        {
                            Global.file_operation.Dequeue();
                        }
                        if (Global.total_commands.Count >= 30)
                        {
                            Global.total_commands.Dequeue();
                        }
                        Global.file_operation.Enqueue(entity);
                        Global.total_commands.Enqueue(entity);
                    }
                    MessageBox.Show("No matching application found.");
                }


                
            }
            catch(Exception ex)
            {
                if (Properties.Settings.Default.History)
                {
                    var entity = new LLM_Detail
                    {
                        SignUpDetail = userId,
                        Expected_json = contentString,
                        user_command = content,
                        Status = "Failed",
                        CommandTime = DateTime.Now,
                        CommandType = "file_operation"
                    };
                    _context.LLM_Detail.Add(entity);
                    _context.SaveChanges();
                    if (Global.file_operation.Count >= 30)
                    {
                        Global.file_operation.Dequeue();
                    }
                    if (Global.total_commands.Count >= 30)
                    {
                        Global.total_commands.Dequeue();
                    }
                    Global.file_operation.Enqueue(entity);
                    Global.total_commands.Enqueue(entity);
                }
                Debug.WriteLine(ex.Message);
            }

        }

        public static void searchBrowser(string command,string search_query,string contentString,string content)
        {
            try
            {

                MessageBox.Show(command);
                MessageBox.Show(search_query);
                //string url = "https://www.google.com/search?q=" + Uri.EscapeDataString(search_query);
                string powershellPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell\\v1.0\\powershell.exe");

                if (search_query == null)
                {

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = powershellPath,
                        Arguments = $"-NoProfile -Command {command}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                    if (Properties.Settings.Default.History)
                    {
                        var entity1 = new LLM_Detail
                        {
                            SignUpDetail = userId,
                            Expected_json = contentString,
                            user_command = content,
                            Status = "Success",
                            CommandTime = DateTime.Now,
                            CommandType = "web_browse"
                        };
                        _context.LLM_Detail.Add(entity1);
                        _context.SaveChanges();
                        if (Global.web_browse.Count >= 30)
                        {
                            Global.web_browse.Dequeue();
                        }
                        Global.web_browse.Enqueue(entity1);
                        if (Global.total_commands.Count >= 30)
                        {
                            Global.total_commands.Dequeue();
                        }
                        Global.total_commands.Enqueue(entity1);
                    }
                    return;
                }

                string urlPath = command.Replace("{{q}}", Uri.EscapeDataString((search_query)));
                MessageBox.Show(urlPath);


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
                if (Properties.Settings.Default.History)
                {
                    var entity = new LLM_Detail
                    {
                        SignUpDetail = userId,
                        Expected_json = contentString,
                        user_command = content,
                        Status = "Success",
                        CommandTime = DateTime.Now,
                        CommandType = "web_browse"
                    };
                    _context.LLM_Detail.Add(entity);
                    _context.SaveChanges();
                    if (Global.web_browse.Count >= 30)
                    {
                        Global.web_browse.Dequeue();
                    }
                    if (Global.total_commands.Count >= 30)
                    {
                        Global.total_commands.Dequeue();
                    }
                    Global.web_browse.Enqueue(entity);
                    Global.total_commands.Enqueue(entity);
                }
            }
            catch (Exception exception)
            {
                if (Properties.Settings.Default.History)
                {
                    var entity = new LLM_Detail
                    {
                        SignUpDetail = userId,
                        Expected_json = contentString,
                        user_command = content,
                        Status = "Failed",
                        CommandTime = DateTime.Now,
                        CommandType = "web_browse"
                    };
                    _context.LLM_Detail.Add(entity);
                    _context.SaveChanges();
                    if (Global.web_browse.Count >= 30)
                    {
                        Global.web_browse.Dequeue();
                    }
                    if (Global.total_commands.Count >= 30)
                    {
                        Global.total_commands.Dequeue();
                    }
                    Global.web_browse.Enqueue(entity);
                    Global.total_commands.Enqueue(entity);
                }
                Debug.WriteLine(exception);
            }
        }
    }
}
