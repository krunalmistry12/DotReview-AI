using DotReview.Application.DTOs;
using DotReview.Application.Services;

namespace DotReview.Tests.Services;

public class IssueFingerprintServiceTests
{
    private readonly IssueFingerprintService _service = new();

    [Fact]
    public void Should_Return_EF002_For_NPlusOne_Issue()
    {
        var issue = new CodeReviewIssueResponse
        {
            RuleId = null,
            Severity = "High",
            Category = "Performance",
            LineNumber = 1,
            Message = "N+1 query problem detected",
            Explanation = "Database query inside loop.",
            SuggestedFix = "Use Include()."
        };

        var fingerprint = _service.GetFingerprint(issue);

        Assert.Equal("EF002", fingerprint);
    }

    [Fact]
    public void Should_Return_RuleId_For_Static_Rule()
    {
        var issue = new CodeReviewIssueResponse
        {
            RuleId = "SEC001",
            Severity = "Critical",
            Category = "Security",
            LineNumber = 1,
            Message = "Possible SQL injection vulnerability."
        };

        var fingerprint = _service.GetFingerprint(issue);

        Assert.Equal("SEC001", fingerprint);
    }

    [Fact]
    public void Should_Return_EF001_For_Unfiltered_Query()
    {
        var issue = new CodeReviewIssueResponse
        {
            RuleId = null,
            Severity = "Medium",
            Category = "Performance",
            LineNumber = 1,
            Message = "Potentially unfiltered database query."
        };

        var fingerprint = _service.GetFingerprint(issue);

        Assert.Equal("EF001", fingerprint);
    }
}