using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IO;

[ApiController]
[Route("api/archive")]
public class ArchiveController : ControllerBase
{
    private readonly ITaskStorage _storage;
    private readonly ArchiveQueue _queue;
    private readonly string _filesPath;

    public ArchiveController(ITaskStorage storage, ArchiveQueue queue, IConfiguration config)
    {
        _storage = storage;
        _queue = queue;
        _filesPath = config["AwesomeFiles:FilesPath"]
            ?? throw new ArgumentException("Путь к файлам не настроен");
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] List<string> files)
    {
        if (files == null || !files.Any())
            return BadRequest("Список файлов пустой");

        var normalized = files
            .Select(f => (f ?? string.Empty).Trim())
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .ToList();

        if (!normalized.Any())
            return BadRequest("Список файлов пустой");

        var invalidNames = normalized
            .Where(f => f.Contains("..") || f.Contains('/') || f.Contains('\\') || Path.GetFileName(f) != f)
            .Distinct()
            .ToList();

        if (invalidNames.Any())
            return BadRequest($"Некорректные имена файлов: {string.Join(", ", invalidNames)}");

        var missingFiles = normalized
            .Where(f => !System.IO.File.Exists(Path.Combine(_filesPath, f)))
            .Distinct()
            .ToList();

        if (missingFiles.Any())
            return BadRequest($"Не найдены файлы: {string.Join(", ", missingFiles)}");

        var task = _storage.Create(normalized);

        await _queue.Enqueue(task);

        return Ok(task.Id);
    }

    [HttpGet("{id}")]
    public IActionResult GetStatus(Guid id)
    {
        var task = _storage.Get(id);

        if (task == null)
            return NotFound();

        return Ok(new
        {
            task.Id,
            task.Status,
            task.Error
        });
    }

    [HttpGet("{id}/download")]
    public IActionResult Download(Guid id)
    {
        var task = _storage.Get(id);

        if (task == null)
            return NotFound();

        if (task.Status != ArchiveStatus.Completed)
            return BadRequest("Архив не готов");

        if (string.IsNullOrWhiteSpace(task.ArchivePath) || !System.IO.File.Exists(task.ArchivePath))
            return StatusCode(StatusCodes.Status500InternalServerError, "Архив отсутствует на диске");

        var stream = System.IO.File.OpenRead(task.ArchivePath);

        return File(stream, "application/zip", $"{id}.zip");
    }
}