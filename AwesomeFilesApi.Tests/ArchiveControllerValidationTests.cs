using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

public class ArchiveControllerValidationTests
{
    [Fact]
    public void Create_WhenInvalidFileNames_ReturnsBadRequest()
    {
        var root = Path.Combine(Path.GetTempPath(), "AwesomeFilesApiTests", Guid.NewGuid().ToString("N"));
        var filesPath = Path.Combine(root, "Files");
        Directory.CreateDirectory(filesPath);
        File.WriteAllText(Path.Combine(filesPath, "ok.txt"), "ok");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AwesomeFiles:FilesPath"] = filesPath
            })
            .Build();

        var controller = new ArchiveController(new TaskStorage(), new ArchiveQueue(), config);

        var result = controller.Create(new List<string> { "..\\evil.txt", "ok.txt" }).GetAwaiter().GetResult();

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Некорректные", bad.Value?.ToString());
    }
}
