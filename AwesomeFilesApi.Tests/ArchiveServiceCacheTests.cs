using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class ArchiveServiceCacheTests
{
    [Fact]
    public async Task ProcessAsync_ReusesCachedArchive_ForSameFileSet()
    {
        var root = Path.Combine(Path.GetTempPath(), "AwesomeFilesApiTests", Guid.NewGuid().ToString("N"));
        var filesPath = Path.Combine(root, "Files");
        var archivesPath = Path.Combine(root, "Archives");

        Directory.CreateDirectory(filesPath);
        Directory.CreateDirectory(archivesPath);

        File.WriteAllText(Path.Combine(filesPath, "a.txt"), "a");
        File.WriteAllText(Path.Combine(filesPath, "b.txt"), "b");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AwesomeFiles:FilesPath"] = filesPath,
                ["AwesomeFiles:ArchivesPath"] = archivesPath
            })
            .Build();

        var service = new ArchiveService(config);

        var t1 = new ArchiveTask { Id = Guid.NewGuid(), Files = new List<string> { "a.txt", "b.txt" } };
        await service.ProcessAsync(t1, CancellationToken.None);

        Assert.Equal(ArchiveStatus.Completed, t1.Status);
        Assert.False(string.IsNullOrWhiteSpace(t1.ArchivePath));
        Assert.True(File.Exists(t1.ArchivePath!));

        var lastWrite1 = File.GetLastWriteTimeUtc(t1.ArchivePath!);

        await Task.Delay(25);

        var t2 = new ArchiveTask { Id = Guid.NewGuid(), Files = new List<string> { "b.txt", "a.txt" } };
        await service.ProcessAsync(t2, CancellationToken.None);

        Assert.Equal(ArchiveStatus.Completed, t2.Status);
        Assert.Equal(t1.ArchivePath, t2.ArchivePath);

        var lastWrite2 = File.GetLastWriteTimeUtc(t2.ArchivePath!);
        Assert.Equal(lastWrite1, lastWrite2);
    }
}
