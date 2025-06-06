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
        private string apiKey { get; set; }
        private static readonly string apiUrl = "https://api.groq.com/openai/v1/chat/completions";

        public llm(string input)
        {
            this.userInput = input;
            MessageBox.Show("User Input: " + input);

            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("AppSetting.json", optional: true, reloadOnChange: true)
                .Build();

            var api_key = config["Groq_Prompt_Api_Key"] ?? throw new InvalidOperationException("Api key not found");
            var prompt = config["prompt"] ?? throw new InvalidOperationException("prompt not found");
            this.apiKey = api_key;

            prompt += " " + userInput;

            Task.Run(async () => await responceJson(prompt)).Wait();
        }

        public async Task responceJson(string prompt)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var requestBody = new
                {
                    model = "llama3-70b-8192",
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.2,
                    max_tokens = 1024
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

                    
                    var parsedJson = JsonDocument.Parse(contentString);

                    var root = parsedJson.RootElement;

                    
                    string originalUserQuery = root.GetProperty("original_user_query").GetString() ?? "";
                    string processedUserQuery = root.GetProperty("processed_user_query").GetString() ?? "";

                    
                    var intent = root.GetProperty("intent_classification");
                    string primaryIntent = intent.GetProperty("primary_intent").GetString() ?? "";
                    string specificAction = intent.GetProperty("specific_action").GetString() ?? "";

                    
                    var entities = root.GetProperty("extracted_entities");

                    string fileDescription = entities.GetProperty("file_description").GetString() ?? "null";
                    string fileType = entities.GetProperty("file_type_filter").GetString() ?? "null";
                    string appName = entities.GetProperty("application_name").GetString() ?? "null";
                    string searchQuery = entities.GetProperty("search_engine_query").GetString() ?? "null";
                    string systemTarget = entities.GetProperty("system_component_target").GetString() ?? "null";
                    string systemValue = entities.GetProperty("system_component_value").GetString() ?? "null";
                    string taskDescription = entities.GetProperty("task_description_for_schedule").GetString() ?? "null";
                    string scheduleTime = entities.GetProperty("schedule_datetime_description").GetString() ?? "null";
                    string systemCommand = entities.GetProperty("system_command").GetString() ?? "null";

                    
                    var timeReferences = entities.GetProperty("time_references");
                    string timeRefCombined = string.Join(", ", timeReferences.EnumerateArray().Select(e => e.GetString()));

                    
                    MessageBox.Show($"Original: {originalUserQuery}\nProcessed: {processedUserQuery}\nPrimary Intent: {primaryIntent}\nSpecific Action: {specificAction}");
                    MessageBox.Show($"App: {appName}\nFile Desc: {fileDescription}\nType: {fileType}");
                    MessageBox.Show($"Search Query: {searchQuery}\nSystem Target: {systemTarget}\nSystem Value: {systemValue}");
                    MessageBox.Show($"Task: {taskDescription}\nSchedule Time: {scheduleTime}");
                    MessageBox.Show($"System Cmd: {systemCommand}\nTime Refs: {timeRefCombined}");


                    var entity = new LLM_Detail
                    {
                        Expected_json = contentString
                    };

                    _context.LLM_Detail.Add(entity);

                }
                else
                {
                    MessageBox.Show("No valid response or unexpected format.");
                }
            }
        }
    }
}
