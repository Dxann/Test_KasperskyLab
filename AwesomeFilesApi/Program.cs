using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Awesome Files API", Version = "v1" });
});

builder.Services.AddSingleton<ITaskStorage, TaskStorage>();
builder.Services.AddSingleton<ArchiveQueue>();
builder.Services.AddSingleton<IArchiveService, ArchiveService>();
builder.Services.AddHostedService<ArchiveWorker>();

var sqlitePath = builder.Configuration["RequestLogging:SqlitePath"] ?? "Logs/requests.db";
builder.Services.AddDbContext<LoggingDbContext>(options =>
    options.UseSqlite($"Data Source={sqlitePath}"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LoggingDbContext>();
    Directory.CreateDirectory(Path.GetDirectoryName(sqlitePath) ?? ".");
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Awesome Files API v1"));

app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    await next();
    sw.Stop();

    Console.WriteLine($"{context.Request.Method} {context.Request.Path} -> {context.Response.StatusCode}");

    try
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LoggingDbContext>();
        db.RequestLogs.Add(new RequestLog
        {
            Timestamp = DateTimeOffset.UtcNow,
            Method = context.Request.Method,
            Path = context.Request.Path,
            StatusCode = context.Response.StatusCode,
            DurationMs = sw.ElapsedMilliseconds
        });
        await db.SaveChangesAsync();
    }
    catch
    {
    }
});

app.MapControllers();

app.Run();