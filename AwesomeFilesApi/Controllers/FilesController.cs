using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Linq;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly string _filesPath;

    public FilesController(IConfiguration config)
    {
        _filesPath = config["AwesomeFiles:FilesPath"]
            ?? throw new ArgumentException("Путь к файлам не настроен");
    }

    [HttpGet]
    public IActionResult GetFiles()
    {
        if (!Directory.Exists(_filesPath))
            return Ok(new List<string>());

        var files = Directory.GetFiles(_filesPath)
                             .Select(Path.GetFileName);

        return Ok(files);
    }
}