using System.IO.Compression;

public class ArchiveService : IArchiveService
{
    private readonly string _filesPath;
    private readonly string _outputPath;

    public ArchiveService(IConfiguration config)
    {
        _filesPath = config["AwesomeFiles:FilesPath"] 
            ?? throw new ArgumentException("Путь к файлам не настроен");

        _outputPath = config["AwesomeFiles:ArchivesPath"] 
            ?? throw new ArgumentException("Путь к архивам не настроен");
    }

    public async Task ProcessAsync(ArchiveTask task, CancellationToken cancellationToken = default)
    {
        task.Status = ArchiveStatus.InProgress;

        var missingFiles = task.Files
            .Where(f => !File.Exists(Path.Combine(_filesPath, f)))
            .ToList();

        if (missingFiles.Any())
        {
            task.Status = ArchiveStatus.Failed;
            task.Error = $"Не найдены файлы: {string.Join(", ", missingFiles)}";
            return;
        }

        try
        {
            Directory.CreateDirectory(_outputPath);
            var archivePath = Path.Combine(_outputPath, $"{task.Id}.zip");

            await Task.Run(() =>
            {
                using var zip = ZipFile.Open(archivePath, ZipArchiveMode.Create);
                foreach (var file in task.Files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var fullPath = Path.Combine(_filesPath, file);
                    zip.CreateEntryFromFile(fullPath, file);
                }
            }, cancellationToken);

            task.ArchivePath = archivePath;
            task.Status = ArchiveStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            task.Status = ArchiveStatus.Failed;
            task.Error = "Архивирование отменено";
            if (!string.IsNullOrWhiteSpace(task.ArchivePath) && File.Exists(task.ArchivePath))
                File.Delete(task.ArchivePath);
        }
        catch (Exception ex)
        {
            task.Status = ArchiveStatus.Failed;
            task.Error = ex.Message;
            if (!string.IsNullOrWhiteSpace(task.ArchivePath) && File.Exists(task.ArchivePath))
                File.Delete(task.ArchivePath);
        }
    }
}