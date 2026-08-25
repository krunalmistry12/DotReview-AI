using DotReview.Application.DTOs;
using DotReview.Application.Interface;

namespace DotReview.Application.Services;

public class IssueFingerprintService : IIssueFingerprintService
{
    public string GetFingerprint(CodeReviewIssueResponse issue)
    {
        // Static rules already have a unique identity.
        if (!string.IsNullOrWhiteSpace(issue.RuleId))
        {
            return issue.RuleId.ToUpperInvariant();
        }

        var message = issue.Message.ToLowerInvariant();

        if (message.Contains("n+1"))
        {
            return "EF002";
        }

        if (message.Contains("sql injection"))
        {
            return "SEC001";
        }

        if (message.Contains("unfiltered database") ||
            message.Contains("unrestricted data retrieval"))
        {
            return "EF001";
        }

        // Generic fallback
        return $"{issue.Category}:{issue.LineNumber}:{Normalize(message)}";
    }

    private static string Normalize(string value)
    {
        return string.Join(
            " ",
            value.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries));
    }
}