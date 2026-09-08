using System.Threading.Channels;

namespace DotReview.API.Background;

public class ReviewQueue : IReviewQueue
{
    private readonly Channel<ReviewJob> _queue;

    public ReviewQueue()
    {
        _queue = Channel.CreateUnbounded<ReviewJob>();
    }

    public async ValueTask QueueAsync(ReviewJob job)
    {
        await _queue.Writer.WriteAsync(job);
    }

    public async ValueTask<ReviewJob> DequeueAsync(
        CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}