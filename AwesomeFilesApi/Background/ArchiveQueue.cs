using System.Threading.Channels;

public class ArchiveQueue
{
    private readonly Channel<ArchiveTask> _queue = Channel.CreateUnbounded<ArchiveTask>();

    public async Task Enqueue(ArchiveTask task)
    {
        await _queue.Writer.WriteAsync(task);
    }

    public async Task<ArchiveTask> Dequeue(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}