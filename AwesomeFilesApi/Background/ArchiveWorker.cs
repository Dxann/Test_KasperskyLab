public class ArchiveWorker : BackgroundService
{
    private readonly ArchiveQueue _queue;
    private readonly IArchiveService _service;

    public ArchiveWorker(ArchiveQueue queue, IArchiveService service)
    {
        _queue = queue;
        _service = service;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ArchiveTask? task = null;

            try
            {
                task = await _queue.Dequeue(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (task != null)
            {
                try
                {
                    await _service.ProcessAsync(task, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    task.Status = ArchiveStatus.Failed;
                    task.Error = "Архивирование отменено";
                }
            }
        }
    }
}