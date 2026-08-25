using DotReview.Application.Services.Rules;

namespace DotReview.Tests.Rules;

public class SEC001Tests
{
    [Fact]
    public void Should_Detect_SqlInjection()
    {
        // Arrange
        var code = """
            var query = $"SELECT * FROM Users WHERE Id = {id}";
            """;

        var rule = new SqlInjectionRule();

        // Act
        var result = rule.Check(code);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SEC001", result.RuleId);
        Assert.Equal("Critical", result.Severity);
        Assert.Equal("Security", result.Category);
    }

    [Fact]
    public void Should_Not_Detect_Parameterized_Query()
    {
        // Arrange
        var code = """
            var query = "SELECT * FROM Users WHERE Id = @id";
            """;

        var rule = new SqlInjectionRule();

        // Act
        var result = rule.Check(code);

        // Assert
        Assert.Null(result);
    }
}