using ControlzEx.Standard;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WpfApp2
{
    public class llm
    {
        private string userInput { get; set; }
        private string apiKey { get; set; }
        private static readonly string apiUrl = "https://api.groq.com/openai/v1/chat/completions";
        public llm(string input)
        {
            this.userInput = input;
            System.Windows.MessageBox.Show(input);
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

                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(apiUrl, content);
                string result = await response.Content.ReadAsStringAsync();
                var jsonResponce = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(result);

                if (jsonResponce.TryGetProperty("choices", out JsonElement choices) && choices.GetArrayLength() > 0)
                {
                    string? contentString = choices[0].GetProperty("message").GetProperty("content").GetString();

                    var parsedJson = JsonSerializer.Deserialize<JsonElement>(contentString);
                    var entities = parsedJson.GetProperty("extracted_entities");

                    System.Windows.MessageBox.Show(entities.GetProperty("application_name").GetString());




                }
                else
                {
                    System.Windows.MessageBox.Show("No response from the API or unexpected response format.");
                }
            }
        }
    }
}
