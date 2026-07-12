using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BussinessLayer.Services
{
    public class RAGEvaluationService : IRAGEvaluationService
    {
        private readonly IRetrievalService _retrievalService;
        private readonly ILLMService _llmService;
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private const string ModelName = "gemini-2.5-flash";

        public RAGEvaluationService(IRetrievalService retrievalService, ILLMService llmService, IConfiguration config)
        {
            _retrievalService = retrievalService;
            _llmService = llmService;
            _apiKey = config["Gemini:ApiKey"]
                ?? throw new InvalidOperationException("Thiếu Gemini:ApiKey trong appsettings.json");
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(120);
        }

        public async Task<RAGEvaluationResultDto> EvaluateAsync(string question, string expectedAnswer, long? subjectId = null)
        {
            var result = new RAGEvaluationResultDto
            {
                Question = question,
                ExpectedAnswer = expectedAnswer
            };

            var sw = Stopwatch.StartNew();

            // 1. Retrieve Contexts
            var contexts = await _retrievalService.RetrieveAsync(question, subjectId);
            result.RetrievedContexts = contexts.Select(c => c.Chunk.Content).ToList();

            // 2. Generate Answer
            var (generatedAnswer, _) = await _llmService.GenerateAnswerAsync(question, contexts);
            sw.Stop();
            
            result.GeneratedAnswer = generatedAnswer;
            result.LatencyMilliseconds = sw.ElapsedMilliseconds;

            // 3. Evaluate using LLM-as-a-Judge
            var judgeResult = await CallJudgeLLMAsync(question, expectedAnswer, generatedAnswer, result.RetrievedContexts);
            
            result.FaithfulnessScore = judgeResult.Faithfulness;
            result.AnswerRelevanceScore = judgeResult.AnswerRelevance;
            result.ContextRelevanceScore = judgeResult.ContextRelevance;
            result.Reasoning = judgeResult.Reasoning;

            return result;
        }

        private async Task<(int Faithfulness, int AnswerRelevance, int ContextRelevance, string Reasoning)> 
            CallJudgeLLMAsync(string question, string expectedAnswer, string generatedAnswer, System.Collections.Generic.List<string> contexts)
        {
            var systemPrompt = @"You are an impartial AI judge evaluating the quality of an AI-generated answer in a RAG (Retrieval-Augmented Generation) system.
You will be provided with:
1. The User's Question
2. The Expected Answer (Ground Truth)
3. The Retrieved Contexts
4. The AI's Generated Answer

Evaluate the generated answer on three criteria, providing a score from 1 to 5 for each (1 is worst, 5 is best):
- Faithfulness: Does the generated answer rely entirely on the provided contexts? (5 = entirely faithful, no hallucination; 1 = completely hallucinated).
- Answer Relevance: Does the generated answer effectively address the user's question, similar to the expected answer? (5 = perfect answer, 1 = irrelevant).
- Context Relevance: Do the retrieved contexts contain the necessary information to answer the question? (5 = highly relevant, 1 = irrelevant).

Output exactly in this JSON format (do not include markdown blocks like ```json):
{
  ""Faithfulness"": [score],
  ""AnswerRelevance"": [score],
  ""ContextRelevance"": [score],
  ""Reasoning"": ""[Detailed explanation for the scores]""
}";

            string contextStr = string.Join("\n---\n", contexts);
            string prompt = $@"
User's Question: {question}
Expected Answer: {expectedAnswer}
Retrieved Contexts: 
{contextStr}
Generated Answer: {generatedAnswer}
";

            var payload = new
            {
                systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new { temperature = 0.1, responseMimeType = "application/json" }
            };

            var body = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{ModelName}:generateContent?key={_apiKey}";

            try
            {
                var httpRes = await _httpClient.PostAsync(url, body);
                if (httpRes.IsSuccessStatusCode)
                {
                    var json = await httpRes.Content.ReadAsStringAsync();
                    var doc = JsonDocument.Parse(json);
                    var text = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    if (!string.IsNullOrEmpty(text))
                    {
                        var parsed = JsonDocument.Parse(text).RootElement;
                        int f = parsed.TryGetProperty("Faithfulness", out var fProp) ? fProp.GetInt32() : 0;
                        int ar = parsed.TryGetProperty("AnswerRelevance", out var arProp) ? arProp.GetInt32() : 0;
                        int cr = parsed.TryGetProperty("ContextRelevance", out var crProp) ? crProp.GetInt32() : 0;
                        string reasoning = parsed.TryGetProperty("Reasoning", out var rProp) ? rProp.GetString() ?? "" : "";
                        
                        return (f, ar, cr, reasoning);
                    }
                }
            }
            catch (Exception ex)
            {
                return (0, 0, 0, $"Error during evaluation: {ex.Message}");
            }

            return (0, 0, 0, "Failed to get evaluation from LLM Judge.");
        }
    }
}
