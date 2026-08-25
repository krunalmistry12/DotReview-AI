using DotReview.Application.DTOs;
using DotReview.Application.Services.Scoring;

namespace DotReview.Tests.Services;

public class CodeReviewScoringServiceTests
{
    private readonly CodeReviewScoringService _service = new();

    [Fact]
    public void Should_Subtract_Correct_Score_For_Severity()
    {
        // Arrange
        var issues = new List<CodeReviewIssueResponse>
        {
            new()
            {
                Severity = "Critical"
            },
            new()
            {
                Severity = "High"
            },
            new()
            {
                Severity = "Medium"
            },
            new()
            {
                Severity = "Low"
            }
        };

        // Act
        var score = _service.CalculateScore(issues);

        // Assert
        // 100 - 30 - 20 - 10 - 5 = 35
        Assert.Equal(35, score);
    }

    [Fact]
    public void Should_Return_100_When_No_Issues()
    {
        // Arrange
        var issues = new List<CodeReviewIssueResponse>();

        // Act
        var score = _service.CalculateScore(issues);

        // Assert
        Assert.Equal(100, score);
    }

    [Fact]
    public void Should_Not_Go_Below_Zero()
    {
        // Arrange
        var issues = Enumerable
            .Repeat(
                new CodeReviewIssueResponse
                {
                    Severity = "Critical"
                },
                10)
            .ToList();

        // Act
        var score = _service.CalculateScore(issues);

        // Assert
        Assert.Equal(0, score);
    }
}