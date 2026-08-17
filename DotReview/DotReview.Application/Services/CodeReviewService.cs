using System.Text.Json;
using DotReview.Application.DTOs;
using DotReview.Application.Interface;

namespace DotReview.Application.Services;

public class CodeReviewService : ICodeReviewService
{
    private readonly IGeminiService _geminiService;
    private readonly IEnumerable<ICodeReviewRule> _rules;
    private readonly ICodeReviewScoringService _scoringService;

    public CodeReviewService(
        IGeminiService geminiService,
        IEnumerable<ICodeReviewRule> rules,
        ICodeReviewScoringService scoringService)
    {
        _geminiService = geminiService;
        _rules = rules;
        _scoringService = scoringService;
    }

    public async Task<CodeReviewResponse> ReviewCodeAsync(
        CodeReviewRequest request)
    {
        // 1. Run deterministic rules
        var ruleViolations = new List<RuleViolationResponse>();

        foreach (var rule in _rules)
        {
            var violation = rule.Check(request.Code);

            if (violation != null)
            {
                ruleViolations.Add(violation);
            }
        }

        // 2. Run AI review
        var aiResponse = await _geminiService.ReviewCodeAsync(
            request.Language,
            request.Code);

        var result = JsonSerializer.Deserialize<CodeReviewResponse>(
            aiResponse,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (result == null)
        {
            throw new InvalidOperationException(
                "Unable to parse AI response.");
        }

        // 3. Convert Rule Engine results
        var ruleIssues = ruleViolations
            .Select(x => new CodeReviewIssueResponse
            {
                RuleId = x.RuleId,
                Severity = x.Severity,
                Category = x.Category,
                LineNumber = x.LineNumber,
                Message = x.Message,
                Explanation = x.Explanation,
                SuggestedFix = x.SuggestedFix
            })
            .ToList();

        // 4. Merge Rule Engine + AI
        var mergedIssues = ruleIssues
            .Concat(result.Issues)
            .ToList();

        // 5. Remove duplicate issues
        var finalIssues = new List<CodeReviewIssueResponse>();

        foreach (var issue in mergedIssues)
        {
            var duplicate = finalIssues.Any(existing =>
                AreDuplicateIssues(existing, issue));

            if (!duplicate)
            {
                finalIssues.Add(issue);
            }
        }

        // 6. Set final issues
        result.Issues = finalIssues;

        // 7. Calculate deterministic score
        result.Score = _scoringService.CalculateScore(
            result.Issues);

        return result;
    }

    private static bool AreDuplicateIssues(
        CodeReviewIssueResponse first,
        CodeReviewIssueResponse second)
    {
        // If both have the same static rule ID,
        // they are definitely duplicates.
        if (!string.IsNullOrWhiteSpace(first.RuleId) &&
            !string.IsNullOrWhiteSpace(second.RuleId) &&
            first.RuleId == second.RuleId)
        {
            return true;
        }

        var firstMessage = NormalizeMessage(first.Message);
        var secondMessage = NormalizeMessage(second.Message);

        // N+1 is a code-level issue.
        // Line number may differ between Rule Engine and AI.
        if (IsNPlusOneIssue(firstMessage) &&
            IsNPlusOneIssue(secondMessage))
        {
            return true;
        }

        // SQL Injection
        if (IsSqlInjectionIssue(firstMessage) &&
            IsSqlInjectionIssue(secondMessage))
        {
            return true;
        }

        // For other issues, use line + category.
        if (first.LineNumber != second.LineNumber)
        {
            return false;
        }

        if (!string.Equals(
                first.Category,
                second.Category,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (IsUnfilteredQueryIssue(firstMessage) &&
            IsUnfilteredQueryIssue(secondMessage))
        {
            return true;
        }

        return false;
    }

    private static string NormalizeMessage(string message)
    {
        return message
            .Trim()
            .ToLowerInvariant();
    }

    private static bool IsNPlusOneIssue(string message)
    {
        return message.Contains("n+1") ||
               message.Contains("n + 1");
    }

    private static bool IsSqlInjectionIssue(string message)
    {
        return message.Contains("sql injection");
    }

    private static bool IsUnfilteredQueryIssue(string message)
    {
        return message.Contains("unfiltered database") ||
               message.Contains("unrestricted data retrieval");
    }
}