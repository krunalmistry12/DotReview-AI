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

                var diffParser =
                    scope.ServiceProvider
                        .GetRequiredService<GitHubDiffParser>();

                // 1. Get PR diff
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

                _logger.LogInformation(
                    "PR #{PullRequestNumber} diff received. Length: {DiffLength}",
                    job.PullRequestNumber,
                    diff.Length);

                // 2. Extract only C# changed code
                var codeToReview =
                    diffParser.ParseCSharpDiff(diff);

                if (string.IsNullOrWhiteSpace(codeToReview))
                {
                    _logger.LogInformation(
                        "No C# changes found for PR #{PullRequestNumber}.",
                        job.PullRequestNumber);

                    continue;
                }

                _logger.LogInformation(
                    "C# code extracted for PR #{PullRequestNumber}. Length: {CodeLength}",
                    job.PullRequestNumber,
                    codeToReview.Length);

                // 3. Temporary safety limit
                if (codeToReview.Length > 20000)
                {
                    codeToReview =
                        codeToReview[..20000];

                    _logger.LogInformation(
                        "C# code was limited to 20000 characters for PR #{PullRequestNumber}.",
                        job.PullRequestNumber);
                }

                // 4. Create review request
                var reviewRequest =
                    new CodeReviewRequest
                    {
                        Language = "C#",
                        Code = codeToReview
                    };

                // 5. Existing review pipeline
                var reviewResult =
                    await codeReviewService.ReviewCodeAsync(
                        reviewRequest);

                // 6. Log result
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