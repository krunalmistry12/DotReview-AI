using DotReview.Application.DTOs;
using DotReview.Application.Interface;
using DotReview.Application.Services;
using Moq;

namespace DotReview.Tests.Services;

public class CodeReviewServiceTests
{
    [Fact]
    public async Task Should_Merge_Static_Rule_And_AI_Issues()
    {
        // Arrange

        var geminiMock = new Mock<IGeminiService>();
        var scoringMock = new Mock<ICodeReviewScoringService>();
        var fingerprintMock = new Mock<IIssueFingerprintService>();
        var ruleMock = new Mock<ICodeReviewRule>();

        var code = """
            var users = db.Users.ToList();
            """;

        var aiResponse = """
            {
              "score": 0,
              "issues": [
                {
                  "ruleId": null,
                  "severity": "High",
                  "category": "Security",
                  "lineNumber": 1,
                  "message": "Potential security issue",
                  "explanation": "AI detected a security problem.",
                  "suggestedFix": "Use a safer implementation."
                }
              ]
            }
            """;

        var ruleViolation = new RuleViolationResponse
        {
            RuleId = "EF001",
            Severity = "Medium",
            Category = "Performance",
            LineNumber = 1,
            Message = "Potentially unfiltered database query.",
            Explanation = "ToList() may load all records.",
            SuggestedFix = "Use filtering or pagination."
        };

        geminiMock
            .Setup(x => x.ReviewCodeAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(aiResponse);

        ruleMock
            .Setup(x => x.Check(code))
            .Returns(ruleViolation);

        fingerprintMock
            .Setup(x => x.GetFingerprint(
                It.IsAny<CodeReviewIssueResponse>()))
            .Returns((CodeReviewIssueResponse issue) =>
                issue.RuleId ?? issue.Message);

        scoringMock
            .Setup(x => x.CalculateScore(
                It.IsAny<IEnumerable<CodeReviewIssueResponse>>()))
            .Returns(70);

        var rules = new List<ICodeReviewRule>
        {
            ruleMock.Object
        };

        var service = new CodeReviewService(
            geminiMock.Object,
            rules,
            scoringMock.Object,
            fingerprintMock.Object);

        var request = new CodeReviewRequest
        {
            Language = "csharp",
            Code = code
        };

        // Act

        var result = await service.ReviewCodeAsync(request);

        // Assert

        Assert.NotNull(result);

        Assert.Equal(70, result.Score);

        Assert.Equal(2, result.Issues.Count);

        Assert.Contains(
            result.Issues,
            x => x.RuleId == "EF001");

        Assert.Contains(
            result.Issues,
            x => x.Category == "Security");

        geminiMock.Verify(
            x => x.ReviewCodeAsync(
                "csharp",
                code),
            Times.Once);

        ruleMock.Verify(
            x => x.Check(code),
            Times.Once);

        scoringMock.Verify(
            x => x.CalculateScore(
                It.IsAny<IEnumerable<CodeReviewIssueResponse>>()),
            Times.Once);
    }


    [Fact]
    public async Task Should_Remove_Duplicate_Issues_Using_Fingerprint()
    {
        // Arrange

        var geminiMock = new Mock<IGeminiService>();
        var scoringMock = new Mock<ICodeReviewScoringService>();
        var fingerprintMock = new Mock<IIssueFingerprintService>();
        var ruleMock = new Mock<ICodeReviewRule>();

        var code = """
            foreach (var user in users)
            {
                var orders = db.Orders
                    .Where(x => x.UserId == user.Id)
                    .ToList();
            }
            """;

        var aiResponse = """
            {
              "score": 0,
              "issues": [
                {
                  "ruleId": null,
                  "severity": "High",
                  "category": "Performance",
                  "lineNumber": 1,
                  "message": "N+1 query problem detected",
                  "explanation": "AI detected an N+1 query.",
                  "suggestedFix": "Use Include()."
                }
              ]
            }
            """;

        var ruleViolation = new RuleViolationResponse
        {
            RuleId = "EF002",
            Severity = "High",
            Category = "Performance",
            LineNumber = 4,
            Message = "Possible N+1 database query.",
            Explanation = "Database query inside loop.",
            SuggestedFix = "Use Include()."
        };

        geminiMock
            .Setup(x => x.ReviewCodeAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(aiResponse);

        ruleMock
            .Setup(x => x.Check(code))
            .Returns(ruleViolation);

        // Both static EF002 and AI N+1
        // receive the same fingerprint.
        fingerprintMock
            .Setup(x => x.GetFingerprint(
                It.IsAny<CodeReviewIssueResponse>()))
            .Returns("EF002");

        scoringMock
            .Setup(x => x.CalculateScore(
                It.IsAny<IEnumerable<CodeReviewIssueResponse>>()))
            .Returns(80);

        var rules = new List<ICodeReviewRule>
        {
            ruleMock.Object
        };

        var service = new CodeReviewService(
            geminiMock.Object,
            rules,
            scoringMock.Object,
            fingerprintMock.Object);

        var request = new CodeReviewRequest
        {
            Language = "csharp",
            Code = code
        };

        // Act

        var result = await service.ReviewCodeAsync(request);

        // Assert

        Assert.NotNull(result);

        // Static EF002 + AI N+1 should become one issue.
        Assert.Single(result.Issues);

        Assert.Equal(
            "EF002",
            result.Issues[0].RuleId);

        Assert.Equal(80, result.Score);
    }
}