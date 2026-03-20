using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class ArchiveServiceMissingFileTests
{
    [Fact]
    public async Task ProcessAsync_WhenFileMissing_SetsFailedAndError()
    {
        var root = Path.Combine(Path.GetTempPath(), "AwesomeFilesApiTests", Guid.NewGuid().ToString("N"));
        var filesPath = Path.Combine(root, "Files");
        var archivesPath = Path.Combine(root, "Archives");

        Directory.CreateDirectory(filesPath);
        Directory.CreateDirectory(archivesPath);

        File.WriteAllText(Path.Combine(filesPath, "a.txt"), "a");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AwesomeFiles:FilesPath"] = filesPath,
                ["AwesomeFiles:ArchivesPath"] = archivesPath
            })
            .Build();

        var service = new ArchiveService(config);

        var task = new ArchiveTask { Id = Guid.NewGuid(), Files = new List<string> { "a.txt", "missing.txt" } };
        await service.ProcessAsync(task, CancellationToken.None);

        Assert.Equal(ArchiveStatus.Failed, task.Status);
        Assert.False(string.IsNullOrWhiteSpace(task.Error));
        Assert.Null(task.ArchivePath);
    }
}
