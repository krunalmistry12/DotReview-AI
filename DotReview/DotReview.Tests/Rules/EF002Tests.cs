using DotReview.Application.Services.Rules;

namespace DotReview.Tests.Rules;

public class EF002Tests
{
    [Fact]
    public void Should_Detect_NPlusOne_Query()
    {
        // Arrange
        var code = """
            var users = db.Users.ToList();

            foreach (var user in users)
            {
                var orders = db.Orders
                    .Where(x => x.UserId == user.Id)
                    .ToList();
            }
            """;

        var rule = new NPlusOneQueryRule();

        // Act
        var result = rule.Check(code);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("EF002", result.RuleId);
        Assert.Equal("High", result.Severity);
        Assert.Equal("Performance", result.Category);
    }

    [Fact]
    public void Should_Not_Detect_NPlusOne_Without_Loop()
    {
        // Arrange
        var code = """
            var orders = db.Orders
                .Where(x => x.UserId == userId)
                .ToList();
            """;

        var rule = new NPlusOneQueryRule();

        // Act
        var result = rule.Check(code);

        // Assert
        Assert.Null(result);
    }
}