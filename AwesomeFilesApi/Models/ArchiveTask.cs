public class ArchiveTask
{
    public Guid Id { get; set; }
    public List<string> Files { get; set; } = new();
    public ArchiveStatus Status { get; set; }
    public string? ArchivePath { get; set; }
    public string? Error { get; set; }
}