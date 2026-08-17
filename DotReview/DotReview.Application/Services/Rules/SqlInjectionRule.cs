using System.Text.RegularExpressions;
using DotReview.Application.DTOs;
using DotReview.Application.Interface;

namespace DotReview.Application.Services.Rules;

public class SqlInjectionRule : ICodeReviewRule
{
    public string RuleId => "SEC001";

    public RuleViolationResponse? Check(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var lines = code.Split(
            Environment.NewLine,
            StringSplitOptions.None);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            bool containsSqlKeyword =
                Regex.IsMatch(
                    line,
                    @"\b(SELECT|INSERT|UPDATE|DELETE)\b",
                    RegexOptions.IgnoreCase);

            bool containsInterpolation =
                line.Contains("$\"") &&
                line.Contains("{") &&
                line.Contains("}");

            bool containsConcatenation =
                Regex.IsMatch(
                    line,
                    @"[""']\s*\+|\+\s*[""']",
                    RegexOptions.IgnoreCase);

            if (containsSqlKeyword &&
                (containsInterpolation || containsConcatenation))
            {
                return new RuleViolationResponse
                {
                    RuleId = RuleId,
                    Severity = "Critical",
                    Category = "Security",
                    LineNumber = i + 1,
                    Message =
                        "Possible SQL injection vulnerability.",
                    Explanation =
                        "SQL query construction uses string interpolation or concatenation. If dynamic values originate from untrusted input, an attacker may be able to manipulate the SQL query.",
                    SuggestedFix =
                        "Use parameterized queries, EF Core LINQ, or another safe parameterization mechanism instead of building SQL using string interpolation or concatenation."
                };
            }
        }

        return null;
    }
}