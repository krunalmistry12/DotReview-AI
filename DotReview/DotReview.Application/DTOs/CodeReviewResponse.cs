using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotReview.Application.DTOs
{
    public class CodeReviewResponse
    {
        public int Score { get; set; }

        public List<CodeReviewIssueResponse> Issues { get; set; } = new();
    }
    public class CodeReviewIssueResponse
    {
        public string? RuleId { get; set; }
        public string Severity { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public int? LineNumber { get; set; }

        public string Message { get; set; } = string.Empty;

        public string Explanation { get; set; } = string.Empty;

        public string SuggestedFix { get; set; } = string.Empty;
    }
}
