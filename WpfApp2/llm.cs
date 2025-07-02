using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using WpfApp2.Context;
using WpfApp2.Models;

namespace WpfApp2
{
    public class llm
    {
        private ApplicationDbContext _context = new ApplicationDbContext();
        private string userInput { get; set; }
        private string processedQuery { get; set; }
        private string apiKey1 { get; set; }
        private string apiKey2 { get; set; }
        private static readonly string apiUrl = "https://api.groq.com/openai/v1/chat/completions";


        public llm(string input)
        {
            this.userInput = input;
         
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("AppSetting.json", optional: true, reloadOnChange: true)
                .Build();

            var api_key1 = config["Groq_Prompt_Api_Key1"] ?? throw new InvalidOperationException("Api key not found");
            var api_key2 = config["Groq_Prompt_Api_Key2"] ?? throw new InvalidOperationException("Api key not found");
            var prompt1 = config["prompt1"] ?? throw new InvalidOperationException("prompt not found");
            var prompt2 = config["prompt2"] ?? throw new InvalidOperationException("prompt not found");
            this.apiKey1 = api_key1;
            this.apiKey2 = api_key2;

 
            try
            {
                processedQuery = Task
                 .Run(() => responceJson2(prompt1+ " " + userInput))
                 .GetAwaiter()
                 .GetResult() 
                  ?? throw new InvalidOperationException("Failed to refine command");

                var fullPrompt = prompt2+ " " + processedQuery;
                Task.Run(() => responceJson(fullPrompt))
                    .GetAwaiter()
                    .GetResult();

            }
            catch(Exception exception)
            {
                Debug.WriteLine(exception);
            }
        }

        public async Task responceJson(string prompt)
        {
            
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey1);

                    var requestBody = new
                    {
                        model = "llama3-70b-8192",
                        messages = new[]
                        {
                        new { role = "user", content = prompt }
                    },
                        temperature = 0.0,
                        max_tokens = 1024,
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    

                    var response = await httpClient.PostAsync(apiUrl, content);
                    string result = await response.Content.ReadAsStringAsync();

                    var jsonResponse = JsonDocument.Parse(result);

                    if (jsonResponse.RootElement.TryGetProperty("choices", out JsonElement choices) && choices.GetArrayLength() > 0)
                    {
                        string? contentString = choices[0].GetProperty("message").GetProperty("content").GetString();

                        if (string.IsNullOrEmpty(contentString))
                        {
                            MessageBox.Show("No content received.");
                            return;
                        }
                        MessageBox.Show("Hello");
                        MessageBox.Show(contentString);
                        var parsedJson = JsonDocument.Parse(contentString);
                        var root = parsedJson.RootElement;

                        var original_user_query = root.GetProperty("original_user_query").GetString();
                        var processed_user_query = root.GetProperty("processed_user_query").GetString();

                        var intent_classification = root.GetProperty("intent_classification");
                        var primary_intent = intent_classification.GetProperty("primary_intent").GetString();
                        var specific_action = intent_classification.GetProperty("specific_action").GetString();

                        var extracted_entities = root.GetProperty("entities");
                        var file_description = extracted_entities.TryGetProperty("file_description", out var fd) ? fd.GetString() : null;
                        var file_type_filter = extracted_entities.TryGetProperty("file_type_filter", out var ft) ? ft.GetString() : null;
                        var time_references = extracted_entities.TryGetProperty("time_references", out var tr) && tr.ValueKind == JsonValueKind.Array
                                              ? tr.EnumerateArray().Select(e => e.GetString()).ToArray()
                                              : Array.Empty<string>();

                        var entities = root.GetProperty("entities");
                        var sources = entities.TryGetProperty("sources", out var s) && s.ValueKind == JsonValueKind.Array
                                      ? s.EnumerateArray().Select(e => e.GetString()).ToArray()
                                      : Array.Empty<string>();
                        var destinations = entities.TryGetProperty("destinations", out var d) && d.ValueKind == JsonValueKind.Array
                                           ? d.EnumerateArray().Select(e => e.GetString()).ToArray()
                                           : Array.Empty<string>();
                        var application = entities.TryGetProperty("application", out var af) ? af.GetString() : null;
                        var file = entities.TryGetProperty("file", out var f) ? f.GetString() : null;
                        var search_query = entities.TryGetProperty("search_query", out var sq) ? sq.GetString() : null;

                        var command_templates = root.TryGetProperty("command_templates", out var ct) && ct.ValueKind == JsonValueKind.Array
                                                ? ct.EnumerateArray().Select(e => e.GetString()).ToArray()
                                                : Array.Empty<string>();


                        var userId = _context.SignUpDetails.FirstOrDefault(u => u.Id == Global.UserId);
                        var entity = new LLM_Detail
                        {
                            SignUpDetail = userId,
                            Expected_json = contentString
                        };

                        _context.LLM_Detail.Add(entity);
                        _context.SaveChanges();

                        //System.Windows.MessageBox.Show("" + original_user_query + "\n" +
                        //    processed_user_query + "\n" +
                        //    primary_intent + "\n" +
                        //    specific_action + "\n" +
                        //    file_description + "\n" +
                        //    file_type_filter + "\n" +
                        //    string.Join(", ", time_references) + "\n" +
                        //    string.Join(", ", sources) + "\n" +
                        //    string.Join(", ", destinations) + "\n" +
                        //    application_or_file + "\n" +
                        //    search_query + "\n" +
                        //    string.Join(", ", command_templates));
                        if (primary_intent != null)
                        {
                            if (primary_intent.ToLower() == "system_control")
                            {
                                if (command_templates[0] != null)
                                {
                                    Commands.systemCommand(command_templates[0], search_query);
                                }
                            }

                            if (primary_intent.ToLower() == "web_browse")
                            {
                                if (command_templates[0] != null)
                                {
                                    Commands.searchBrowser(command_templates[0], search_query);
                                }
                            }

                            if (primary_intent.ToLower() == "application_control")
                            {
                                if (command_templates[0] != null)
                                {

                                    if (application != null)
                                    {
                                        Commands.application_command(application);
                                    }
                                }
                            }

                            if (primary_intent.ToLower() == "file_operation")
                            {
                                if (command_templates[0] != null)
                                {
                                    if (file != null)
                                    {
                                        Commands.file_command(file);
                                    }
                                }
                            }

                        }
                        else
                        {
                            MessageBox.Show("No valid response or unexpected format.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                MessageBox.Show("An error occurred while processing the request. Please try again.");
            }
        }

        public async Task<string?> responceJson2(string prompt)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey2);

                    var requestBody = new
                    {
                        model = "llama3-70b-8192",
                        messages = new[]
                        {
                        new { role = "user", content = prompt}
                    },
                        temperature = 0.0,
                        max_tokens = 1024,
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync(apiUrl, content);
                    string result = await response.Content.ReadAsStringAsync();

                    var jsonResponse = JsonDocument.Parse(result);

                    if (jsonResponse.RootElement.TryGetProperty("choices", out JsonElement choices) && choices.GetArrayLength() > 0)
                    {
                        string? contentString = choices[0].GetProperty("message").GetProperty("content").GetString();

                        if (string.IsNullOrEmpty(contentString))
                        {
                            MessageBox.Show("No content received.");
                            return null;
                        }
                        return contentString.Trim();


                    }
                    }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                MessageBox.Show("An error occurred while processing the request. Please try again.");
                return null;
            }

            return null;
        }

    }
}
