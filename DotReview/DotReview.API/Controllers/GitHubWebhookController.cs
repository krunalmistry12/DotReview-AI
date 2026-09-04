using System.Text.Json;
using DotReview.API.Services;
using DotReview.Application.DTOs;
using DotReview.Application.Interface;
using Microsoft.AspNetCore.Mvc;

namespace DotReview.API.Controllers;

[ApiController]
[Route("api/github")]
public class GitHubWebhookController : ControllerBase
{
    private readonly GitHubService _githubService;
    private readonly ICodeReviewService _codeReviewService;

    public GitHubWebhookController(
        GitHubService githubService,
        ICodeReviewService codeReviewService)
    {
        _githubService = githubService;
        _codeReviewService = codeReviewService;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> ReceiveWebhook()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(body))
        {
            return BadRequest(new
            {
                message = "Webhook payload is empty."
            });
        }

        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;

            // GitHub PR action
            var action =
                root.TryGetProperty("action", out var actionElement)
                    ? actionElement.GetString()
                    : null;

            // Pull request number
            var number =
                root.TryGetProperty("pull_request", out var prObj) &&
                prObj.TryGetProperty("number", out var numberElement)
                    ? numberElement.GetInt32()
                    : 0;

            // Repository name
            var repositoryName =
                root.TryGetProperty("repository", out var repository) &&
                repository.TryGetProperty("full_name", out var fullName)
                    ? fullName.GetString()
                    : null;

            // PR title
            var title =
                prObj.ValueKind != JsonValueKind.Undefined &&
                prObj.TryGetProperty("title", out var titleElement)
                    ? titleElement.GetString()
                    : null;

            // Only process opened/synchronize PR events
            if (action != "opened" && action != "synchronize")
            {
                return Ok(new
                {
                    message = "Webhook received but no code review required.",
                    action,
                    pullRequestNumber = number
                });
            }

            if (string.IsNullOrWhiteSpace(repositoryName))
            {
                return BadRequest(new
                {
                    message = "Repository name not found."
                });
            }

            if (number <= 0)
            {
                return BadRequest(new
                {
                    message = "Pull request number not found."
                });
            }

            var repositoryParts = repositoryName.Split('/');

            if (repositoryParts.Length != 2)
            {
                return BadRequest(new
                {
                    message = "Invalid repository name."
                });
            }

            var owner = repositoryParts[0];
            var repo = repositoryParts[1];

            // 1. Get PR diff from GitHub
            var diff = await _githubService.GetPullRequestDiffAsync(
                owner,
                repo,
                number);

            if (string.IsNullOrWhiteSpace(diff))
            {
                return Ok(new
                {
                    message = "PR received but no code changes found.",
                    action,
                    pullRequestNumber = number,
                    repository = repositoryName,
                    title
                });
            }

            // 2. Create code review request
            var reviewRequest = new CodeReviewRequest
            {
                Language = "C#",
                Code = diff
            };

            // 3. Run existing code review pipeline
            var reviewResult =
                await _codeReviewService.ReviewCodeAsync(reviewRequest);

            // 4. Return review result for testing
            return Ok(new
            {
                message = "Pull request reviewed successfully.",
                action,
                pullRequestNumber = number,
                repository = repositoryName,
                title,
                diffLength = diff.Length,
                review = reviewResult
            });
        }
        catch (JsonException)
        {
            return BadRequest(new
            {
                message = "Invalid JSON payload."
            });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(500, new
            {
                message = "Failed to get PR diff from GitHub.",
                error = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new
            {
                message = "Code review failed.",
                error = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Unexpected error occurred.",
                error = ex.Message
            });
        }
    }
}