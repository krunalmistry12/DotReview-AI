using DotReview.Application.Services.Rules;

namespace DotReview.Tests.Rules;

public class EF001Tests
{
    [Fact]
    public void Should_Detect_Unfiltered_ToList()
    {
        // Arrange
        var code = """
            var users = db.Users.ToList();
            """;

        var rule = new AvoidUnfilteredToListRule();

        // Act
        var result = rule.Check(code);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("EF001", result.RuleId);
        Assert.Equal("Medium", result.Severity);
        Assert.Equal("Performance", result.Category);
    }

    [Fact]
    public void Should_Not_Detect_Filtered_ToList()
    {
        // Arrange
        var code = """
            var users = db.Users
                .Where(x => x.IsActive)
                .ToList();
            """;

        var rule = new AvoidUnfilteredToListRule();

        // Act
        var result = rule.Check(code);

        // Assert
        Assert.Null(result);
    }
}