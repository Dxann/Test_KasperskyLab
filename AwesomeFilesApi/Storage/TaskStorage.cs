public class TaskStorage : ITaskStorage
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, ArchiveTask> _tasks = new();

    public ArchiveTask Create(List<string> files)
    {
        var task = new ArchiveTask
        {
            Id = Guid.NewGuid(),
            Files = files,
            Status = ArchiveStatus.Pending
        };

        _tasks[task.Id] = task;
        return task;
    }

    public ArchiveTask? Get(Guid id)
        => _tasks.TryGetValue(id, out var task) ? task : null;

    public IEnumerable<ArchiveTask> GetAll() => _tasks.Values;
}