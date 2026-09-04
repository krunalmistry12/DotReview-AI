namespace DotReview.API;

public class AiReviewTest
{
    // Test 1: Null-safe
    public string GetUserName(string name)
    {
        return name?.Trim() ?? string.Empty;
    }

    // Test 2: SQL Injection resolved
    public string GetUserQuery(string username)
    {
        // Example: parameterized query should be used with the DB provider.
        return "SELECT * FROM Users WHERE Name = @username";
    }

    // Test 3: Clean code
    public int Add(int a, int b)
    {
        return a + b;
    }

    public int Multiply(int a, int b)
    {
        return a * b;
    }
}