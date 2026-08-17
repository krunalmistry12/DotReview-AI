using DotReview.Application.Interface;
using Google.GenAI;

namespace DotReview.AI.Services;

public class GeminiService : IGeminiService
{
    private readonly Client _client;

    public GeminiService()
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "GEMINI_API_KEY is not configured.");
        }

        _client = new Client(apiKey: apiKey);
    }

    public async Task<string> ReviewCodeAsync(
    string language,
    string code)
    {
        var prompt = $$"""
    You are an expert {{language}} code reviewer.

    Analyze the following code for:

    1. Security
    2. Performance
    3. Code quality
    4. Best practices

    Return ONLY valid JSON.

    Do NOT use markdown.
    Do NOT use ```json.
    Do NOT add any explanation outside the JSON.

    Use exactly this structure:

    {
      "score": 0,
      "issues": [
        {
          "severity": "Low",
          "category": "Performance",
          "lineNumber": 1,
          "message": "Short issue description",
          "explanation": "Detailed explanation",
          "suggestedFix": "Recommended fix"
        }
      ]
    }

    Severity must be one of:
    Low, Medium, High, Critical

    Category must be one of:
    Security, Performance, CodeQuality, BestPractices

    Score must be between 0 and 100.

    Code to review:

    {{code}}
    """;

        var response = await _client.Models.GenerateContentAsync(
            model: "gemini-2.5-flash",
            contents: prompt);

        return response.Text ?? string.Empty;
    }
}