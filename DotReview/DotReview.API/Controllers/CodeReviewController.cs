using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace DotReview.API.Controllers;

[ApiController]
[Route("api/github/webhook")]
public class GitHubWebhookController : ControllerBase
{
    [HttpPost]
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

            var action = root.TryGetProperty("action", out var actionElement)
                ? actionElement.GetString()
                : null;

            var number = root.TryGetProperty("number", out var numberElement)
                ? numberElement.GetInt32()
                : 0;

            var repositoryName =
                root.TryGetProperty("repository", out var repository) &&
                repository.TryGetProperty("full_name", out var fullName)
                    ? fullName.GetString()
                    : null;

            var title =
                root.TryGetProperty("pull_request", out var pullRequest) &&
                pullRequest.TryGetProperty("title", out var titleElement)
                    ? titleElement.GetString()
                    : null;

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
            return BadRequest(new
            {
                message = "Invalid JSON payload."
            });
        }
    }
}