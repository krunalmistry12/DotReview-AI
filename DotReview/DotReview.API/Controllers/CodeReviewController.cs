using DotReview.Application.DTOs;
using DotReview.Application.Interface;
using Microsoft.AspNetCore.Mvc;

namespace DotReview.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CodeReviewController : ControllerBase
{
    private readonly ICodeReviewService _codeReviewService;

    public CodeReviewController(
        ICodeReviewService codeReviewService)
    {
        _codeReviewService = codeReviewService;
    }

    [HttpPost]
    public async Task<IActionResult> ReviewCode(
        [FromBody] CodeReviewRequest request)
    {
        var result = await _codeReviewService.ReviewCodeAsync(request);

        return Ok(result);
    }
}