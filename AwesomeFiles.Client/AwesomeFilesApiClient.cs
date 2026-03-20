using System.Net.Http.Json;
using System.Text.Json;

public sealed class AwesomeFilesApiClient
{
    private readonly HttpClient _http;

    public AwesomeFilesApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<string>> ListFilesAsync(CancellationToken cancellationToken)
    {
        using var resp = await _http.GetAsync("/api/files", cancellationToken);
        await EnsureSuccessAsync(resp, cancellationToken);

        var files = await resp.Content.ReadFromJsonAsync<List<string>>(cancellationToken: cancellationToken);
        IReadOnlyList<string> result = files ?? (IReadOnlyList<string>)Array.Empty<string>();
        return result;
    }

    public async Task<string> CreateArchiveAsync(IReadOnlyList<string> files, CancellationToken cancellationToken)
    {
        using var resp = await _http.PostAsJsonAsync("/api/archive", files, cancellationToken);
        await EnsureSuccessAsync(resp, cancellationToken);

        var idText = (await resp.Content.ReadAsStringAsync(cancellationToken)).Trim().Trim('"');
        return idText;
    }

    public async Task<ArchiveTaskStatus> GetStatusAsync(string id, CancellationToken cancellationToken)
    {
        using var resp = await _http.GetAsync($"/api/archive/{Uri.EscapeDataString(id)}", cancellationToken);
        await EnsureSuccessAsync(resp, cancellationToken);

        var json = await resp.Content.ReadAsStringAsync(cancellationToken);
        var status = JsonSerializer.Deserialize<ArchiveTaskStatus>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (status is null)
            throw new InvalidOperationException("Неожиданный ответ бэкенда.");

        return status;
    }

    public async Task DownloadArchiveAsync(string id, string targetPath, CancellationToken cancellationToken)
    {
        using var resp = await _http.GetAsync($"/api/archive/{Uri.EscapeDataString(id)}/download", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(resp, cancellationToken);

        var dir = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        await using var src = await resp.Content.ReadAsStreamAsync(cancellationToken);
        await using var dst = File.Create(targetPath);
        await src.CopyToAsync(dst, cancellationToken);
    }

    public async Task<string> FetchArchiveAsync(
        IReadOnlyList<string> files,
        DirectoryInfo outDir,
        TimeSpan pollInterval,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var id = await CreateArchiveAsync(files, cancellationToken);

        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (DateTimeOffset.UtcNow > deadline)
                throw new TimeoutException("Истекло время ожидания архива.");

            var status = await GetStatusAsync(id, cancellationToken);

            if (status.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                break;

            if (status.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(!string.IsNullOrWhiteSpace(status.Error) ? status.Error : "Failed");

            await Task.Delay(pollInterval, cancellationToken);
        }

        var targetPath = Path.Combine(outDir.FullName, $"{id}.zip");
        await DownloadArchiveAsync(id, targetPath, cancellationToken);
        return targetPath;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        string body;
        try
        {
            body = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            body = string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(body))
            throw new BackendException((int)response.StatusCode, body);

        throw new BackendException((int)response.StatusCode, response.ReasonPhrase ?? "Ошибка бэкенда");
    }
}

public sealed class BackendException : Exception
{
    public int StatusCode { get; }

    public BackendException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

public sealed record ArchiveTaskStatus
{
    public string Id { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Error { get; init; }

    public string ToUserMessage()
    {
        if (Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            return "Архив создан.";

        if (Status.Equals("Pending", StringComparison.OrdinalIgnoreCase) || Status.Equals("InProgress", StringComparison.OrdinalIgnoreCase))
            return "Процесс выполняется, пожалуйста, подождите...";

        if (Status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(Error))
                return $"Ошибка: {Error}";
            return "Ошибка.";
        }

        return $"Статус: {Status}";
    }
}
