using Refit;
using Contracts.WorkerToManager;
using Microsoft.Extensions.Options;
using Serilog;
using Worker.Abstractions.Options;
using Worker.Abstractions;
using Worker.Service;
using Common.Options;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<ManagerOptions>(
    builder.Configuration.GetSection(ManagerOptions.SectionName));

builder.Services.Configure<TaskQueueRabbitMQOptions>(
    builder.Configuration.GetSection(TaskQueueRabbitMQOptions.SectionName));

builder.Services.Configure<ResponseQueueRabbitMQOptions>(
    builder.Configuration.GetSection(ResponseQueueRabbitMQOptions.SectionName));

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddMvc().AddXmlFormaterExtensions();

builder.Services.AddSingleton<IWorker, Scheduler>();
builder.Services.AddSingleton<IFinalizer, ResponsePublisher>();
builder.Services.AddSingleton<IExecutor, Executor>();

builder.Services.AddHostedService<CrackRequestConsumer>();

builder.Services.AddRefitClient<IManagerApi>()
    .ConfigureHttpClient((serviceProvider, httpClient) =>
    {
        var managerOptions = serviceProvider.GetRequiredService<IOptions<ManagerOptions>>().Value;

        httpClient.BaseAddress = managerOptions.Uri;

        httpClient.Timeout = TimeSpan.FromSeconds(15);

        httpClient.DefaultRequestHeaders.Add("User-Agent", "Worker-Service");
        
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
