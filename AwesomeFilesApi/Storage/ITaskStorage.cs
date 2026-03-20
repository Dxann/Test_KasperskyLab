public interface ITaskStorage
{
    ArchiveTask Create(List<string> files);
    ArchiveTask? Get(Guid id);
    IEnumerable<ArchiveTask> GetAll();
}