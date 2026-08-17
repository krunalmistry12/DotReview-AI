using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotReview.Domain.Entities
{
    public class CodeReviewIssue
    {
        public Guid Id { get; set; }

        public string Severity { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public int? LineNumber { get; set; }

        public string Message { get; set; } = string.Empty;

        public string Explanation { get; set; } = string.Empty;

        public string SuggestedFix { get; set; } = string.Empty;
    }
}
