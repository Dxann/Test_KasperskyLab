// See https://aka.ms/new-console-template for more information
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

var baseUrlOption = new Option<string>(new[] { "--base-url", "-b" }, () => "http://localhost:5073", "Базовый URL бэкенда AwesomeFilesApi");

var root = new RootCommand("CLI клиент Awesome Files")
{
    baseUrlOption
};

root.Handler = CommandHandler.Create(() =>
{
    Console.WriteLine("Клиент запущен.");
    Console.WriteLine("Используйте --help для просмотра доступных команд.");
});

var listCmd = new Command("list", "Получить список доступных файлов")
{
};
listCmd.Handler = CommandHandler.Create<string, CancellationToken>(async (baseUrl, cancellationToken) =>
{
    using var http = CreateHttpClient(baseUrl);
    var api = new AwesomeFilesApiClient(http);

    try
    {
        var files = await api.ListFilesAsync(cancellationToken);
        Console.WriteLine(files.Count == 0 ? string.Empty : string.Join(' ', files));
    }
    catch (BackendException ex)
    {
        Console.Error.WriteLine($"Ошибка бэкенда {ex.StatusCode}: {ex.Message}");
        Environment.ExitCode = 1;
    }
});

var createCmd = new Command("create-archive", "Создать задачу архивирования для указанных файлов")
{
    baseUrlOption
};

var createFilesArg = new Argument<string[]>("files")
{
    Arity = ArgumentArity.OneOrMore,
    Description = "Имена файлов для включения в архив"
};
createCmd.AddArgument(createFilesArg);

createCmd.Handler = CommandHandler.Create<string, string[], CancellationToken>(async (baseUrl, files, cancellationToken) =>
{
    var list = files?.ToList() ?? new List<string>();

    using var http = CreateHttpClient(baseUrl);
    var api = new AwesomeFilesApiClient(http);

    try
    {
        var id = await api.CreateArchiveAsync(list, cancellationToken);
        Console.WriteLine($"Задача создания архива запущена, id: {id}");
    }
    catch (BackendException ex)
    {
        Console.Error.WriteLine($"Ошибка бэкенда {ex.StatusCode}: {ex.Message}");
        Environment.ExitCode = 1;
    }
});

var statusCmd = new Command("status", "Получить статус задачи архивирования")
{
    baseUrlOption
};

var statusIdArg = new Argument<string>("id") { Description = "ID задачи (GUID)" };
statusCmd.AddArgument(statusIdArg);

statusCmd.Handler = CommandHandler.Create<string, string, CancellationToken>(async (baseUrl, id, cancellationToken) =>
{
    using var http = CreateHttpClient(baseUrl);
    var api = new AwesomeFilesApiClient(http);

    try
    {
        var status = await api.GetStatusAsync(id, cancellationToken);
        Console.WriteLine(status.ToUserMessage());
    }
    catch (BackendException ex)
    {
        Console.Error.WriteLine($"Ошибка бэкенда {ex.StatusCode}: {ex.Message}");
        Environment.ExitCode = 1;
    }
});

var outOption = new Option<DirectoryInfo?>(new[] { "--out", "-o" }, "Выходная директория для загруженного архива");

var downloadCmd = new Command("download", "Скачать архив для выполненной задачи")
{
    baseUrlOption,
    outOption
};

var downloadIdArg = new Argument<string>("id") { Description = "ID задачи (GUID)" };
downloadCmd.AddArgument(downloadIdArg);

downloadCmd.Handler = CommandHandler.Create<string, string, DirectoryInfo?, CancellationToken>(async (baseUrl, id, @out, cancellationToken) =>
{
    if (@out is null)
        throw new ArgumentException("--out требуется");

    var outDir = @out;

    if (!outDir.Exists)
        outDir.Create();

    using var http = CreateHttpClient(baseUrl);
    var api = new AwesomeFilesApiClient(http);

    try
    {
        var targetPath = Path.Combine(outDir.FullName, $"{id}.zip");
        await api.DownloadArchiveAsync(id, targetPath, cancellationToken);
        Console.WriteLine($"Загружено: {targetPath}");
    }
    catch (BackendException ex)
    {
        Console.Error.WriteLine($"Ошибка бэкенда {ex.StatusCode}: {ex.Message}");
        Environment.ExitCode = 1;
    }
});

var pollIntervalOption = new Option<int>(
    "--poll-interval-ms",
    () => 500,
    "Интервал опроса в миллисекундах");

var timeoutOption = new Option<int>(
    "--timeout-ms",
    () => 60_000,
    "Общее время ожидания в миллисекундах");

var fetchCmd = new Command("fetch", "Создать задачу архивирования, дождаться завершения и скачать архив.")
{
    baseUrlOption,
    outOption,
    pollIntervalOption,
    timeoutOption
};

var fetchFilesArg = new Argument<string[]>("files") { Arity = ArgumentArity.OneOrMore };
fetchCmd.AddArgument(fetchFilesArg);

fetchCmd.Handler = CommandHandler.Create<string, string[], DirectoryInfo?, int, int, CancellationToken>(async (baseUrl, files, @out, pollIntervalMs, timeoutMs, cancellationToken) =>
{
    if (@out is null)
        throw new ArgumentException("--out требуется");

    var outDir = @out;
    var list = files?.ToList() ?? new List<string>();

    if (!outDir.Exists)
        outDir.Create();

    using var http = CreateHttpClient(baseUrl);
    var api = new AwesomeFilesApiClient(http);

    try
    {
        var targetPath = await api.FetchArchiveAsync(
            list,
            outDir,
            TimeSpan.FromMilliseconds(pollIntervalMs),
            TimeSpan.FromMilliseconds(timeoutMs),
            cancellationToken);

        Console.WriteLine($"Загружено: {targetPath}");
    }
    catch (BackendException ex)
    {
        Console.Error.WriteLine($"Ошибка бэкенда {ex.StatusCode}: {ex.Message}");
        Environment.ExitCode = 1;
    }
    catch (TimeoutException)
    {
        Console.Error.WriteLine("Истекло время ожидания архива.");
        Environment.ExitCode = 2;
    }
    catch (InvalidOperationException ex)
    {
        Console.Error.WriteLine(ex.Message);
        Environment.ExitCode = 1;
    }
});

root.AddCommand(listCmd);
root.AddCommand(createCmd);
root.AddCommand(statusCmd);
root.AddCommand(downloadCmd);
root.AddCommand(fetchCmd);

return await root.InvokeAsync(args);

static HttpClient CreateHttpClient(string baseUrl)
{
    var http = new HttpClient
    {
        BaseAddress = new Uri(baseUrl, UriKind.Absolute),
        Timeout = TimeSpan.FromSeconds(100)
    };

    http.DefaultRequestHeaders.UserAgent.ParseAdd("AwesomeFiles.Client/1.0");
    return http;
}
