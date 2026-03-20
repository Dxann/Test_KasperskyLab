using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

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

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Awesome Files API v1"));

app.Use(async (context, next) =>
{
    await next();
    Console.WriteLine($"{context.Request.Method} {context.Request.Path} -> {context.Response.StatusCode}");
});

app.MapControllers();

app.Run();