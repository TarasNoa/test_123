/*
using Libr4.IDE.Application.AI.Algorithms;
using Libr4.IDE.Domain.Algorithms;
using Libr4.Shared.Kernel.Results;
using Libr4.IDE.Domain.AI;
using Libr4.Shared.Kernel.Errors;
using Libr4.IDE.Domain;
using System.Text.RegularExpressions;

namespace Libr4.IDE.Application.AI.Algorithms;

public class AIAlgorithmServiceWrapper : IAIAlgorithmService
{
    public AIAlgorithmServiceWrapper()
    {
    }

    public async Task<Result<IntentDetectionResult>> DetectIntentAndEntitiesAsync(string message)
    {
        await Task.Delay(10); // Simulate async processing
        
        try
        {
            var messageLower = message.ToLower();
            
            // Intent classification
            Intent intent;
            if (messageLower.Contains("create task") || messageLower.Contains("new project") || 
               messageLower.Contains("post job") || messageLower.Contains("need help with"))
                intent = Intent.CreateTask;
            else if (messageLower.Contains("find developer") || messageLower.Contains("looking for") || 
                     messageLower.Contains("need freelancer") || messageLower.Contains("hire"))
                intent = Intent.FindFreelancer;
            else if (messageLower.Contains("how much") || messageLower.Contains("price") || 
                     messageLower.Contains("cost") || messageLower.Contains("budget") || 
                     messageLower.Contains("rate"))
                intent = Intent.PricingQuestion;
            else if (messageLower.Contains("skill") || messageLower.Contains("learn") || 
                     messageLower.Contains("improve") || messageLower.Contains("certification"))
                intent = Intent.SkillQuestion;
            else if (messageLower.Contains("payment") || messageLower.Contains("pay") || 
                     messageLower.Contains("invoice") || messageLower.Contains("transaction"))
                intent = Intent.PaymentQuestion;
            else if (messageLower.Contains("help") || messageLower.Contains("how to") || 
                     messageLower.Contains("what is") || messageLower.Contains("explain"))
                intent = Intent.Help;
            else if (IsCodeRequest(message))
                intent = Intent.CodeGeneration;
            else
                intent = Intent.GeneralChat;
            
            // Entity extraction
            var entities = new List<Entity>();
            
            // Extract monetary amounts
            var moneyPattern = @"\$?\d+(?:,\d{3})*(?:\.\d{2})?";
            var moneyMatches = Regex.Matches(message, moneyPattern);
            foreach (Match m in moneyMatches)
            {
                entities.Add(new Entity { Type = "amount", Value = m.Value, Start = m.Index, End = m.Index + m.Length });
            }
            
            // Extract skill mentions
            var commonSkills = new[] { "python", "javascript", "react", "django", "aws", "docker", "typescript", "go", "rust", "c#" };
            foreach (var skill in commonSkills)
            {
                if (messageLower.Contains(skill))
                {
                    var idx = messageLower.IndexOf(skill);
                    entities.Add(new Entity { Type = "skill", Value = skill, Start = idx, End = idx + skill.Length });
                }
            }
            
            var result = new IntentDetectionResult
            {
                Intent = intent,
                Confidence = 0.85f,
                Entities = entities
            };
            
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            return Result.Failure<IntentDetectionResult>(Error.Failure("Algorithm.DetectionFailed", $"Failed to detect intent: {ex.Message}"));
        }
    }

    public async Task<Result<float>> ScoreResponseQualityAsync(string userMessage, string aiResponse, Intent intent)
    {
        await Task.Delay(10); // Simulate async processing
        
        try
        {
            var qualityScore = 0.5f;
            
            // Length appropriateness
            var responseLen = aiResponse.Split().Length;
            if (responseLen >= 20 && responseLen <= 200)
                qualityScore += 0.2f;
            else if (responseLen < 10)
                qualityScore -= 0.2f;
            
            // Relevance
            var userKeywords = new HashSet<string>(userMessage.ToLower().Split());
            var responseKeywords = new HashSet<string>(aiResponse.ToLower().Split());
            var overlap = userKeywords.Intersect(responseKeywords).Count();
            var relevance = (float)overlap / Math.Max(userKeywords.Count, 1);
            qualityScore += relevance * 0.2f;
            
            // Intent fulfillment
            if (intent == Intent.Help && (aiResponse.ToLower().Contains("помощь") || aiResponse.ToLower().Contains("поможет")))
                qualityScore += 0.1f;
            
            // No errors/apologies
            if (!aiResponse.ToLower().Contains("ошибка") && !aiResponse.ToLower().Contains("извините"))
                qualityScore += 0.1f;
            
            var finalScore = Math.Max(0.0f, Math.Min(1.0f, qualityScore));
            return Result.Success(finalScore);
        }
        catch (Exception ex)
        {
            return Result.Failure<float>(Error.Failure("Algorithm.ScoringFailed", $"Failed to score response quality: {ex.Message}"));
        }
    }

    public string InferLanguage(string message, object? context = null)
    {
        var messageLower = message.ToLower();
        
        if (messageLower.Contains("python") || messageLower.Contains("py ") || messageLower.Contains(".py"))
            return "python";
        else if (messageLower.Contains("javascript") || messageLower.Contains(" js ") || messageLower.Contains(".js"))
            return "javascript";
        else if (messageLower.Contains("html"))
            return "html";
        else if (messageLower.Contains("php"))
            return "php";
        else if (context != null)
        {
            var ctxStr = context.ToString().ToLower();
            if (ctxStr.Contains("python") || ctxStr.Contains(".py"))
                return "python";
            else if (ctxStr.Contains("javascript") || ctxStr.Contains(".js"))
                return "javascript";
        }
        
        return "python";
    }

    public bool IsCodeRequest(string message)
    {
        var codeRequestKeywords = new[] { "script", "code", "write", "create", "make", "implement", "функци", "игра", "game", "function", "класс", "class", "программ", "program", "скрипт", "напиши", "написать", "сделай", "создай", "реализуй", "add" };
        var messageLower = message.ToLower().Trim();
        return codeRequestKeywords.Any(kw => messageLower.Contains(kw));
    }
}
*/
