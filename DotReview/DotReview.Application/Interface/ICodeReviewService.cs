using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotReview.Application.DTOs;

namespace DotReview.Application.Interface
{
    public interface ICodeReviewService
    {
        Task<CodeReviewResponse> ReviewCodeAsync(
            CodeReviewRequest request);
    }
}
