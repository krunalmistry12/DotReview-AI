using DotReview.Application.DTOs;
using DotReview.Application.Interface;
using DotReview.API.Services;

namespace DotReview.API.Background;

public class ReviewWorker : BackgroundService
{
    private readonly IReviewQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReviewWorker> _logger;

    public ReviewWorker(
        IReviewQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<ReviewWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Code review background worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _queue.DequeueAsync(
                    stoppingToken);

                _logger.LogInformation(
                    "Processing PR #{PullRequestNumber} for {Owner}/{Repository}",
                    job.PullRequestNumber,
                    job.Owner,
                    job.Repository);

                using var scope =
                    _scopeFactory.CreateScope();

                var githubService =
                    scope.ServiceProvider
                        .GetRequiredService<GitHubService>();

                var codeReviewService =
                    scope.ServiceProvider
                        .GetRequiredService<ICodeReviewService>();

                // Get PR diff
                var diff =
                    await githubService.GetPullRequestDiffAsync(
                        job.Owner,
                        job.Repository,
                        job.PullRequestNumber);

                if (string.IsNullOrWhiteSpace(diff))
                {
                    _logger.LogInformation(
                        "No diff found for PR #{PullRequestNumber}.",
                        job.PullRequestNumber);

                    continue;
                }

                // Temporary limit for testing
                var codeToReview =
                    diff.Length > 20000
                        ? diff[..20000]
                        : diff;

                var reviewRequest =
                    new CodeReviewRequest
                    {
                        Language = "C#",
                        Code = codeToReview
                    };

                // Existing review pipeline
                var reviewResult =
                    await codeReviewService.ReviewCodeAsync(
                        reviewRequest);

                _logger.LogInformation(
                    "PR #{PullRequestNumber} reviewed successfully. Score: {Score}, Issues: {IssueCount}",
                    job.PullRequestNumber,
                    reviewResult.Score,
                    reviewResult.Issues.Count);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while processing code review job.");
            }
        }

        _logger.LogInformation(
            "Code review background worker stopped.");
    }
}