using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

public class ArchiveService : IArchiveService
{
    private readonly string _filesPath;
    private readonly string _outputPath;

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _cacheLocks = new();

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

        var cacheKey = ComputeCacheKey(task.Files);
        var archivePath = Path.Combine(_outputPath, $"{cacheKey}.zip");

        Directory.CreateDirectory(_outputPath);

        var gate = _cacheLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        var createdNew = false;

        try
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                if (!File.Exists(archivePath))
                {
                    createdNew = true;
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
                }
            }
            finally
            {
                gate.Release();
            }

            task.ArchivePath = archivePath;
            task.Status = ArchiveStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            task.Status = ArchiveStatus.Failed;
            task.Error = "Архивирование отменено";
            if (createdNew && File.Exists(archivePath))
                File.Delete(archivePath);
        }
        catch (Exception ex)
        {
            task.Status = ArchiveStatus.Failed;
            task.Error = ex.Message;
            if (createdNew && File.Exists(archivePath))
                File.Delete(archivePath);
        }
    }

    private static string ComputeCacheKey(IEnumerable<string> files)
    {
        var normalized = files
            .Select(f => (f ?? string.Empty).Trim())
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToArray();

        var input = string.Join("\n", normalized);
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}