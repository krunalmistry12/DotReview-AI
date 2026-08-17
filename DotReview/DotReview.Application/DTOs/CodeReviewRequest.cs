using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotReview.Application.DTOs
{
    public class CodeReviewRequest
    {
        public string Language { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;
    }
}
