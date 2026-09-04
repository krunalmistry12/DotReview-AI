using System.Text.Json;
using DotReview.API.Background;
using Microsoft.AspNetCore.Mvc;

namespace DotReview.API.Controllers;

[ApiController]
[Route("api/github")]
public class GitHubWebhookController : ControllerBase
{
    private readonly IReviewQueue _reviewQueue;

    public GitHubWebhookController(
        IReviewQueue reviewQueue)
    {
        _reviewQueue = reviewQueue;
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
                root.TryGetProperty(
                    "action",
                    out var actionElement)
                    ? actionElement.GetString()
                    : null;

            // Pull request number
            var number =
                root.TryGetProperty(
                    "pull_request",
                    out var prObj) &&
                prObj.TryGetProperty(
                    "number",
                    out var numberElement)
                    ? numberElement.GetInt32()
                    : 0;

            // Repository full name
            var repositoryName =
                root.TryGetProperty(
                    "repository",
                    out var repository) &&
                repository.TryGetProperty(
                    "full_name",
                    out var fullName)
                    ? fullName.GetString()
                    : null;

            // PR title
            string? title = null;

            if (prObj.ValueKind != JsonValueKind.Undefined &&
                prObj.TryGetProperty(
                    "title",
                    out var titleElement))
            {
                title = titleElement.GetString();
            }

            // Only process opened and synchronize events
            if (action != "opened" &&
                action != "synchronize")
            {
                return Ok(new
                {
                    message =
                        "Webhook received. No review required.",
                    action
                });
            }

            // Validate repository
            if (string.IsNullOrWhiteSpace(repositoryName))
            {
                return BadRequest(new
                {
                    message = "Repository name not found."
                });
            }

            // Validate PR number
            if (number <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Pull request number not found."
                });
            }

            // Split owner/repository
            var repositoryParts =
                repositoryName.Split('/');

            if (repositoryParts.Length != 2)
            {
                return BadRequest(new
                {
                    message = "Invalid repository name."
                });
            }

            var owner = repositoryParts[0];
            var repo = repositoryParts[1];

            // Add job to background queue
            await _reviewQueue.QueueAsync(
                new ReviewJob(
                    owner,
                    repo,
                    number));

            // Return immediately to GitHub
            return Ok(new
            {
                message =
                    "Pull request received and queued for review.",
                action,
                pullRequestNumber = number,
                repository = repositoryName,
                title
            });
        }
        catch (JsonException)
        {
            return BadRequest(new
            {
                message = "Invalid JSON payload."
            });
        }
    }
}