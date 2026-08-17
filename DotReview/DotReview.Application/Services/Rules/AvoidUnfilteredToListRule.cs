using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DotReview.Application.DTOs;
using DotReview.Application.Interface;

namespace DotReview.Application.Services.Rules
{
    public class AvoidUnfilteredToListRule : ICodeReviewRule
    {
        public string RuleId => "EF001";

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

                if (Regex.IsMatch(
                    line,
                    @"db\.\w+\.ToList\s*\(\s*\)"))
                {
                    return new RuleViolationResponse
                    {
                        RuleId = RuleId,
                        Severity = "Medium",
                        Category = "Performance",
                        LineNumber = i + 1,
                        Message =
                            "Potentially unfiltered database query.",
                        Explanation =
                            "ToList() may load all records from the database into memory.",
                        SuggestedFix =
                            "Consider using Where(), Select(), pagination, or another appropriate filter before ToList()."
                    };
                }
            }

            return null;
        }
    }
}
