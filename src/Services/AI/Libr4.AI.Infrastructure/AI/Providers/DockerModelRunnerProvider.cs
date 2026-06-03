using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.AI.Providers;

/// <summary>
/// Provider for Docker Model Runner (the local LLM runtime bundled with Docker Desktop).
/// Exposes an OpenAI-compatible API at http://localhost:12434/engines/v1 by default.
/// No API key is required. Models are identified by their full OCI reference
/// (e.g. "docker.io/ai/gemma4:latest") which must match what /engines/v1/models returns.
/// </summary>
public class DockerModelRunnerProvider : IAIProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DockerModelRunnerProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;

    public string ProviderName => "DockerModelRunner";

    public DockerModelRunnerProvider(
        IConfiguration configuration,
        ILogger<DockerModelRunnerProvider> logger,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _endpoint = _configuration["AI:DockerModelRunner:Endpoint"]
                    ?? "http://localhost:12434/engines/v1";
        // Local inference can take a while on the first token; be generous.
        _httpClient.Timeout = TimeSpan.FromMinutes(15);
    }

    public async Task<string> GenerateCompletionAsync(string prompt, string? systemPrompt = null, string? model = null)
    {
        var modelName = SanitizeModelId(model)
            ?? SanitizeModelId(_configuration["AI:DockerModelRunner:DefaultModel"])
            ?? "docker.io/ai/gemma4:latest";

        _logger.LogInformation("DockerModelRunner: Calling model {Model} with prompt length {PromptLength}",
            modelName, prompt?.Length ?? 0);

        var messages = new object[]
        {
            new { role = "system", content = systemPrompt ?? "You are a helpful assistant." },
            new { role = "user", content = prompt }
        };

        var requestBody = new
        {
            model = modelName,
            messages,
            temperature = 0.3,
            // Local models have no streaming-window budget. Allow much larger outputs so
            // code-gen batches don't need aggressive splitting.
            max_tokens = 16000,
            stream = true,
            // Ignored by models without a reasoning channel; disables chain-of-thought on
            // those that honor it (some qwen3 / gemma-thinking builds).
            enable_thinking = false
        };

        var contentBuilder = new StringBuilder();
        var reasoningBuilder = new StringBuilder();
        bool streamCompleted = false;
        var ambientCancellation = AICallCancellationScope.Current;

        try
        {
            var json = JsonSerializer.Serialize(requestBody);
            _logger.LogDebug("DockerModelRunner: Request body (streaming): {RequestBody}", json);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/chat/completions")
            {
                // StringContent with Encoding.UTF8 does NOT prepend a BOM, which is important
                // because Docker Model Runner rejects requests whose body starts with the
                // UTF-8 BOM ("invalid request" / 400).
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Accept.ParseAdd("text/event-stream");

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ambientCancellation);

            _logger.LogInformation("DockerModelRunner: Response status {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("DockerModelRunner: API call failed with status {StatusCode}. Response: {Response}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"DockerModelRunner API call failed: {response.StatusCode} - {errorContent}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ambientCancellation);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            try
            {
                string? line;
                int lineCount = 0;
                int lastLogCount = 0;
                int linesWithoutAnyContent = 0;
                var maxLinesWithoutContent = Math.Clamp(
                    _configuration.GetValue("AI:DockerModelRunner:MaxSseLinesWithoutContent", 6000),
                    500,
                    50000);
                while ((line = await reader.ReadLineAsync().WaitAsync(ambientCancellation)) != null)
                {
                    lineCount++;
                    if (lineCount - lastLogCount >= 100)
                    {
                        _logger.LogDebug("DockerModelRunner: Processed {LineCount} SSE lines, content={ContentLen}", lineCount, contentBuilder.Length);
                        lastLogCount = lineCount;
                    }
                    
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
                        _logger.LogDebug("DockerModelRunner: Skipping non-JSON SSE payload: {Payload}", payload);
                        continue;
                    }

                    if (!chunk.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0) continue;
                    var choice = choices[0];
                    if (choice.TryGetProperty("finish_reason", out var fr)
                        && fr.ValueKind == JsonValueKind.String
                        && !string.IsNullOrEmpty(fr.GetString()))
                    {
                        streamCompleted = true;
                    }
                    if (!choice.TryGetProperty("delta", out var delta)) continue;
                    if (delta.TryGetProperty("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.String)
                    {
                        var chunkContent = contentEl.GetString() ?? string.Empty;
                        contentBuilder.Append(chunkContent);
                        if (!string.IsNullOrEmpty(chunkContent))
                            linesWithoutAnyContent = 0;
                    }
                    if (delta.TryGetProperty("reasoning_content", out var reasoningEl)
                        && reasoningEl.ValueKind == JsonValueKind.String)
                    {
                        var reasoningContent = reasoningEl.GetString() ?? string.Empty;
                        reasoningBuilder.Append(reasoningContent);
                        if (!string.IsNullOrEmpty(reasoningContent))
                            linesWithoutAnyContent = 0;
                    }

                    // Guard against endless streams producing only empty deltas.
                    if (contentBuilder.Length == 0 && reasoningBuilder.Length == 0)
                    {
                        linesWithoutAnyContent++;
                        if (linesWithoutAnyContent >= maxLinesWithoutContent)
                        {
                            throw new IOException(
                                $"DockerModelRunner stream stall: no content for {linesWithoutAnyContent} SSE lines.");
                        }
                    }
                }
                _logger.LogInformation("DockerModelRunner: Stream processing complete. Total lines: {LineCount}", lineCount);
            }
            catch (Exception streamEx) when (streamEx is IOException or HttpRequestException)
            {
                _logger.LogWarning(streamEx,
                    "DockerModelRunner: Stream interrupted after {Chars} chars; returning partial content",
                    contentBuilder.Length);
            }

            var content = contentBuilder.ToString();
            var reasoning = reasoningBuilder.ToString();

            if (content.Length == 0 && reasoning.Length == 0)
            {
                throw new HttpRequestException("DockerModelRunner: Stream produced no content before termination.");
            }

            // If the model exhausted its budget on reasoning and produced no `content`,
            // we used to salvage the entire reasoning channel as the answer. In practice
            // (audit P1-13: live e2e e-commerce run) this caused 70K+ chars of raw
            // chain-of-thought to be returned as the response, polluting downstream
            // stages and turning a "deep think" timeout into a silent quality regression.
            //
            // New behaviour: only salvage when reasoning is small enough to plausibly
            // contain the final answer (≤ 8K chars) AND ends with text that looks like
            // a structured answer (JSON object/array or fenced code block). Anything
            // beyond that is treated as a hard truncation: throw a typed exception so
            // the caller (AIService) can apply its retry/circuit-breaker policy instead
            // of feeding garbage forward.
            const int salvageThreshold = 8_000;
            if (content.Length < 32)
            {
                if (reasoning.Length == 0)
                {
                    throw new HttpRequestException(
                        "DockerModelRunner: model produced no content and no reasoning before stream termination.");
                }

                if (reasoning.Length <= salvageThreshold && LooksLikeStructuredAnswer(reasoning))
                {
                    _logger.LogInformation(
                        "DockerModelRunner: content was {ContentLen} chars; salvaging structured tail of reasoning_content ({ReasoningLen} chars)",
                        content.Length, reasoning.Length);
                    content = reasoning;
                }
                else
                {
                    _logger.LogWarning(
                        "DockerModelRunner: model returned {ReasoningLen} chars of reasoning but no final answer; treating as truncation. content={ContentLen}",
                        reasoning.Length, content.Length);
                    throw new HttpRequestException(
                        $"DockerModelRunner: reasoning-only response ({reasoning.Length} chars) without a closing final answer; raise max_tokens or shorten the prompt.");
                }
            }

            _logger.LogInformation(
                "DockerModelRunner: {Status} response with length {Length} (reasoning={ReasoningLen})",
                streamCompleted ? "Complete" : "Partial (stream truncated)",
                content.Length, reasoning.Length);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DockerModelRunner: Exception during API call");
            throw;
        }
    }

    /// <summary>
    /// Heuristic for "does this reasoning text look like it contains the actual answer
    /// (structured JSON / fenced code block) vs raw chain-of-thought ramble".
    /// We only check the trailing 1KB; the model usually puts the answer at the end.
    /// </summary>
    private static bool LooksLikeStructuredAnswer(string reasoning)
    {
        if (string.IsNullOrWhiteSpace(reasoning)) return false;
        var tail = reasoning.Length > 1024 ? reasoning.Substring(reasoning.Length - 1024) : reasoning;
        var trimmed = tail.TrimEnd();

        // JSON object / array tail.
        if (trimmed.EndsWith('}') || trimmed.EndsWith(']')) return true;
        // Fenced code block close.
        if (trimmed.EndsWith("```")) return true;
        // Explicit "Final answer:" marker some reasoning models emit.
        if (tail.Contains("final answer", StringComparison.OrdinalIgnoreCase)
            || tail.Contains("</answer>", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    public async Task<string> GenerateEmbeddingAsync(string text, string? model = null)
    {
        var modelName = model
            ?? _configuration["AI:DockerModelRunner:EmbeddingModel"]
            ?? throw new InvalidOperationException(
                "DockerModelRunner embeddings require AI:DockerModelRunner:EmbeddingModel to be configured " +
                "(e.g. 'docker.io/ai/nomic-embed-text:latest').");

        var requestBody = new { model = modelName, input = text };
        var json = JsonSerializer.Serialize(requestBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/embeddings")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body);
        return result.GetProperty("data")[0].GetProperty("embedding").ToString() ?? string.Empty;
    }

    public async Task<string> AnalyzeTextAsync(string text, string analysisType, string? model = null)
    {
        var systemPrompt = analysisType switch
        {
            "sentiment"  => "You are a sentiment analysis expert. Analyze the text and return sentiment (positive/negative/neutral) with confidence score (0-1) in JSON format.",
            "complexity" => "You are a task complexity analyst. Analyze the task description and return complexity score (1-10), estimated hours, and required skills in JSON format.",
            "skills"     => "You are a skills extraction expert. Extract all technical and soft skills from the text and return as a JSON array.",
            "risk"       => "You are a risk assessment expert. Analyze the project/task and return risk level (low/medium/high) with explanation in JSON format.",
            _            => "You are a helpful assistant. Analyze the text according to the request."
        };

        var prompt = $"Analyze the following text for {analysisType}: {text}";
        return await GenerateCompletionAsync(prompt, systemPrompt, model);
    }

    public Task<string> ChatAsync(string message, string? systemPrompt = null, string? model = null)
        => GenerateCompletionAsync(message, systemPrompt, model);

    private static string? SanitizeModelId(string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return null;

        return model.Trim().Trim('"', '\'');
    }
}
