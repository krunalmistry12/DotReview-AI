using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using DotReview.Application.DTOs;
using DotReview.Application.Interface;

namespace DotReview.Application.Services.Rules;

public class NPlusOneQueryRule : ICodeReviewRule
{
    public string RuleId => "EF002";

    public RuleViolationResponse? Check(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var tree = CSharpSyntaxTree.ParseText(code);

        var root = tree.GetRoot();

        var loops = root.DescendantNodes()
            .OfType<ForEachStatementSyntax>();

        foreach (var loop in loops)
        {
            var queryInsideLoop = loop.Statement
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault(IsDatabaseQuery);

            if (queryInsideLoop != null)
            {
                var lineNumber =
                    queryInsideLoop
                        .GetLocation()
                        .GetLineSpan()
                        .StartLinePosition.Line + 1;

                return CreateViolation(lineNumber);
            }
        }

        return null;
    }

    private static bool IsDatabaseQuery(
        InvocationExpressionSyntax invocation)
    {
        var methodName = invocation.Expression
            .ToString();

        return methodName.EndsWith(".ToList") ||
               methodName.EndsWith(".ToListAsync") ||
               methodName.Contains(".Where");
    }

    private static RuleViolationResponse CreateViolation(
        int lineNumber)
    {
        return new RuleViolationResponse
        {
            RuleId = "EF002",
            Severity = "High",
            Category = "Performance",
            LineNumber = lineNumber,
            Message = "Possible N+1 database query.",
            Explanation =
                "A database query appears to execute inside a foreach loop. This can cause a separate database request for each iteration and significantly reduce performance.",
            SuggestedFix =
                "Use Include(), projection, or batch queries to load the required related data before entering the loop."
        };
    }
}