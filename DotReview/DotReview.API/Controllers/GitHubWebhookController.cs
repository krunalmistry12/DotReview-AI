using System.Text.Json;
using DotReview.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotReview.API.Controllers;

[ApiController]
[Route("api/github")]
public class GitHubWebhookController : ControllerBase
{
    private readonly GitHubService _githubService;

    public GitHubWebhookController(GitHubService githubService)
    {
        _githubService = githubService;
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

            var action =
                root.TryGetProperty("action", out var actionElement)
                    ? actionElement.GetString()
                    : null;

            var number =
                root.TryGetProperty("pull_request", out var prObj) &&
                prObj.TryGetProperty("number", out var numEl)
                    ? numEl.GetInt32()
                    : 0;

            var repositoryName =
                root.TryGetProperty("repository", out var repository) &&
                repository.TryGetProperty("full_name", out var fullName)
                    ? fullName.GetString()
                    : null;

            var title =
                prObj.ValueKind != JsonValueKind.Undefined &&
                prObj.TryGetProperty("title", out var titleElement)
                    ? titleElement.GetString()
                    : null;

            string? diff = null;

            if ((action == "opened" || action == "synchronize") &&
                !string.IsNullOrWhiteSpace(repositoryName) &&
                number > 0)
            {
                var repositoryParts = repositoryName.Split('/');

                if (repositoryParts.Length == 2)
                {
                    var owner = repositoryParts[0];
                    var repo = repositoryParts[1];

                    diff = await _githubService.GetPullRequestDiffAsync(
                        owner,
                        repo,
                        number);
                }
            }

            return Ok(new
            {
                message = "Pull request webhook received",
                action,
                pullRequestNumber = number,
                repository = repositoryName,
                title,
                diffLength = diff?.Length ?? 0,
                diff = diff
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
    }
}