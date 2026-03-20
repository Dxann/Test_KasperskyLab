public interface IArchiveService
{
    Task ProcessAsync(ArchiveTask task, CancellationToken cancellationToken = default);
}