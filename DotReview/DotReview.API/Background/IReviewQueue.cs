using System.Threading.Channels;

namespace DotReview.API.Background;

public interface IReviewQueue
{
    ValueTask QueueAsync(ReviewJob job);

    ValueTask<ReviewJob> DequeueAsync(
        CancellationToken cancellationToken);
}

public record ReviewJob(
    string Owner,
    string Repository,
    int PullRequestNumber);