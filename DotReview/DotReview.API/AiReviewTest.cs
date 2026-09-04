namespace DotReview.API;

public class AiReviewTest
{
    // Test 1: Possible NullReferenceException
    public string GetUserName(string name)
    {
        return name.Trim();
    }

    // Test 2: SQL Injection
    public string GetUserQuery(string username)
    {
        return "SELECT * FROM Users WHERE Name = '" + username + "'";
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