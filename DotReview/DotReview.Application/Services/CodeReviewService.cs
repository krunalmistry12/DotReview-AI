using System.Text.Json;
using DotReview.Application.DTOs;
using DotReview.Application.Interface;

namespace DotReview.Application.Services;

public class CodeReviewService : ICodeReviewService
{
    private readonly IGeminiService _geminiService;
    private readonly IEnumerable<ICodeReviewRule> _rules;
    private readonly ICodeReviewScoringService _scoringService;
    private readonly IIssueFingerprintService _fingerprintService;

    public CodeReviewService(
        IGeminiService geminiService,
        IEnumerable<ICodeReviewRule> rules,
        ICodeReviewScoringService scoringService,
        IIssueFingerprintService fingerprintService)
    {
        _geminiService = geminiService;
        _rules = rules;
        _scoringService = scoringService;
        _fingerprintService = fingerprintService;
    }

    public async Task<CodeReviewResponse> ReviewCodeAsync(
        CodeReviewRequest request)
    {
        // 1. Run deterministic/static rules
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

        // 3. Convert static rule violations
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

        // 4. Merge static rules + AI issues
        var mergedIssues = ruleIssues
            .Concat(result.Issues)
            .ToList();

        // 5. Remove duplicate issues using fingerprints
        var finalIssues = mergedIssues
            .GroupBy(issue =>
                _fingerprintService.GetFingerprint(issue))
            .Select(group => group.First())
            .ToList();

        // 6. Set final issues
        result.Issues = finalIssues;

        // 7. Calculate deterministic score
        result.Score = _scoringService.CalculateScore(
            result.Issues);

        // 8. Return final review
        return result;
    }
}