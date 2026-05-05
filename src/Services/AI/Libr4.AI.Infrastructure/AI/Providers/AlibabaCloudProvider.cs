using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.AI.Providers;

public class AlibabaCloudProvider : IAIProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AlibabaCloudProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _endpoint;

    public string ProviderName => "AlibabaCloud";

    public AlibabaCloudProvider(IConfiguration configuration, ILogger<AlibabaCloudProvider> logger, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = _configuration["AI:AlibabaCloud:ApiKey"] ?? throw new InvalidOperationException("AI:AlibabaCloud:ApiKey not configured");
        _endpoint = _configuration["AI:AlibabaCloud:Endpoint"] ?? "https://dashscope.aliyuncs.com/compatible-mode/v1";
        if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
    }

    public async Task<string> GenerateCompletionAsync(string prompt, string? systemPrompt = null, string? model = null)
    {
        var modelName = model ?? _configuration["AI:AlibabaCloud:DefaultModel"] ?? "qwen-plus";

        _logger.LogInformation("AlibabaCloud: Calling model {Model} with prompt length {PromptLength}", modelName, prompt?.Length ?? 0);

        // Quirk: qwen3-max-preview on DashScope collapses output to `{"files":[]}` whenever
        // ANY system message is supplied (verified empirically). Merge the system prompt
        // into the user turn for that family so behavior is normal.
        bool mergeSystemIntoUser = modelName.StartsWith("qwen3-max", StringComparison.OrdinalIgnoreCase);

        object[] messages;
        if (mergeSystemIntoUser && !string.IsNullOrWhiteSpace(systemPrompt))
        {
            var combined = $"{systemPrompt}\n\n===== USER REQUEST =====\n{prompt}";
            messages = new object[] { new { role = "user", content = combined } };
        }
        else
        {
            messages = new object[]
            {
                new { role = "system", content = systemPrompt ?? "You are a helpful assistant." },
                new { role = "user", content = prompt }
            };
        }

        // Use streaming to avoid server-side idle timeout (~30s) on long generations.
        // enable_thinking=false disables the chain-of-thought channel on qwen3 reasoning
        // models (qwen3.6-plus, qwen3.5-plus, qwq-*). Without it those models spend the
        // entire streaming budget in `reasoning_content` and never emit `content`.
        // The flag is ignored by non-thinking models.
        var requestBody = new
        {
            model = modelName,
            messages,
            temperature = 0.7,
            // Modern qwen3 models support up to 32k output; we cap at 16k to give the
            // multi-batch generator enough room to emit complete source files.
            max_tokens = 16000,
            stream = true,
            enable_thinking = false
        };

        var contentBuilder = new StringBuilder();
        var reasoningBuilder = new StringBuilder();
        bool streamCompleted = false;

        try
        {
            _logger.LogDebug("AlibabaCloud: Request body (streaming): {RequestBody}", JsonSerializer.Serialize(requestBody));

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };
            request.Headers.Accept.ParseAdd("text/event-stream");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            _logger.LogInformation("AlibabaCloud: Response status {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("AlibabaCloud: API call failed with status {StatusCode}. Response: {Response}", response.StatusCode, errorContent);
                throw new HttpRequestException($"AlibabaCloud API call failed: {response.StatusCode} - {errorContent}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream, Encoding.UTF8);

            try
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

                    var payload = line.Substring(5).Trim();
                    if (payload == "[DONE]")
                    {
                        streamCompleted = true;
                        break;
                    }
                    if (string.IsNullOrEmpty(payload)) continue;

                    JsonElement chunk;
                    try
                    {
                        chunk = JsonSerializer.Deserialize<JsonElement>(payload);
                    }
                    catch (JsonException)
                    {
                        _logger.LogDebug("AlibabaCloud: Skipping non-JSON SSE payload: {Payload}", payload);
                        continue;
                    }

                    if (!chunk.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0) continue;
                    var choice = choices[0];
                    if (choice.TryGetProperty("finish_reason", out var fr) && fr.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(fr.GetString()))
                    {
                        streamCompleted = true;
                    }
                    if (!choice.TryGetProperty("delta", out var delta)) continue;
                    if (delta.TryGetProperty("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.String)
                    {
                        contentBuilder.Append(contentEl.GetString());
                    }
                    // qwen3 thinking models (qwen3-max-preview, qwq-*, deepseek-r1 family) stream
                    // their chain-of-thought in `reasoning_content`. Capture it so we can fall
                    // back to it if the model never emits a final `content` chunk.
                    if (delta.TryGetProperty("reasoning_content", out var reasoningEl)
                        && reasoningEl.ValueKind == JsonValueKind.String)
                    {
                        reasoningBuilder.Append(reasoningEl.GetString());
                    }
                }
            }
            catch (Exception streamEx) when (streamEx is IOException or HttpRequestException)
            {
                // Server dropped the stream (DashScope 30s budget etc.). Fall through and use what we have.
                _logger.LogWarning(streamEx, "AlibabaCloud: Stream interrupted after {Chars} chars; returning partial content", contentBuilder.Length);
            }

            var content = contentBuilder.ToString();
            var reasoning = reasoningBuilder.ToString();

            if (content.Length == 0 && reasoning.Length == 0)
            {
                throw new HttpRequestException("AlibabaCloud: Stream produced no content before termination.");
            }

            // Thinking models sometimes embed their actual JSON answer inside the reasoning
            // channel when the streaming window is tight. Fall back to reasoning when the
            // content channel is empty or too short to be a plausible answer.
            if (content.Length < 32 && reasoning.Length > content.Length)
            {
                _logger.LogInformation(
                    "AlibabaCloud: content was {ContentLen} chars; falling back to reasoning_content ({ReasoningLen} chars)",
                    content.Length, reasoning.Length);
                content = reasoning;
            }

            _logger.LogInformation(
                "AlibabaCloud: {Status} response with length {Length} (reasoning={ReasoningLen})",
                streamCompleted ? "Complete" : "Partial (stream truncated)",
                content.Length, reasoning.Length);

            // Log the head of very short responses so API-level refusals or error shapes are visible.
            if (content.Length <= 200)
            {
                var head = content.Substring(0, Math.Min(content.Length, 200))
                    .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
                _logger.LogWarning("AlibabaCloud: Short response content (escaped, {Len} chars): {Head}",
                    content.Length, head);
            }

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AlibabaCloud: Exception during API call");
            throw;
        }
    }

    public async Task<string> GenerateEmbeddingAsync(string text, string? model = null)
    {
        var modelName = model ?? _configuration["AI:AlibabaCloud:EmbeddingModel"] ?? "text-embedding-v2";
        
        var requestBody = new
        {
            model = modelName,
            input = text
        };

        var response = await _httpClient.PostAsync(
            $"{_endpoint}/embeddings",
            new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        return result.GetProperty("data")[0].GetProperty("embedding").ToString() ?? string.Empty;
    }

    public async Task<string> AnalyzeTextAsync(string text, string analysisType, string? model = null)
    {
        var systemPrompt = analysisType switch
        {
            "sentiment" => "You are a sentiment analysis expert. Analyze the text and return sentiment (positive/negative/neutral) with confidence score (0-1) in JSON format.",
            "complexity" => "You are a task complexity analyst. Analyze the task description and return complexity score (1-10), estimated hours, and required skills in JSON format.",
            "skills" => "You are a skills extraction expert. Extract all technical and soft skills from the text and return as a JSON array.",
            "risk" => "You are a risk assessment expert. Analyze the project/task and return risk level (low/medium/high) with explanation in JSON format.",
            _ => "You are a helpful assistant. Analyze the text according to the request."
        };

        var prompt = $"Analyze the following text for {analysisType}: {text}";
        return await GenerateCompletionAsync(prompt, systemPrompt, model);
    }

    public async Task<string> ChatAsync(string message, string? systemPrompt = null, string? model = null)
    {
        return await GenerateCompletionAsync(message, systemPrompt, model);
    }
}
