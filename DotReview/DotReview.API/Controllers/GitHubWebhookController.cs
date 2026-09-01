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
            return BadRequest(new { message = "Webhook payload is empty." });
        }

        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;

            var action = root.TryGetProperty("action", out var actionElement) ? actionElement.GetString() : null;

            // Note: In GitHub PR payloads, the top-level 'number' property doesn't always exist 
            // (it's inside the 'pull_request' object). Safely extract it:
            var number = root.TryGetProperty("pull_request", out var prObj) && prObj.TryGetProperty("number", out var numEl)
                ? numEl.GetInt32()
                : 0;

            var repositoryName = root.TryGetProperty("repository", out var repository) &&
                                 repository.TryGetProperty("full_name", out var fullName)
                ? fullName.GetString()
                : null;

            var title = prObj.ValueKind != JsonValueKind.Undefined && prObj.TryGetProperty("title", out var titleElement)
                ? titleElement.GetString()
                : null;
            if ((action == "opened" || action == "synchronize") &&
    !string.IsNullOrWhiteSpace(repositoryName) &&
    number > 0)
            {
                var repositoryParts = repositoryName.Split('/');

                if (repositoryParts.Length == 2)
                {
                    var owner = repositoryParts[0];
                    var repo = repositoryParts[1];

                    var diff = await _githubService.GetPullRequestDiffAsync(
                        owner,
                        repo,
                        number);

                    Console.WriteLine("===== PR DIFF =====");
                    Console.WriteLine(diff);
                    Console.WriteLine("==================");
                }
            }
            return Ok(new
            {
                message = "Pull request webhook received",
                action,
                pullRequestNumber = number,
                repository = repositoryName,
                title
            });
        }
        catch (JsonException)
        {
            return BadRequest(new { message = "Invalid JSON payload." });
        }
    }
}