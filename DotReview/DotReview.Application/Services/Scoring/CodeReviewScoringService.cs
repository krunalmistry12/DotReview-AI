using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotReview.Application.DTOs;
using DotReview.Application.Interface;

namespace DotReview.Application.Services.Scoring
{
    public class CodeReviewScoringService : ICodeReviewScoringService
    {
        public int CalculateScore(
            IEnumerable<CodeReviewIssueResponse> issues)
        {
            var score = 100;

            foreach (var issue in issues)
            {
                score -= issue.Severity switch
                {
                    "Critical" => 30,
                    "High" => 20,
                    "Medium" => 10,
                    "Low" => 5,
                    _ => 0
                };
            }

            return Math.Clamp(score, 0, 100);
        }
    }
}
