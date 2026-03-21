using Contracts.ManagerToWorker;
using Manager.Abstractions.Options;
using Manager.Abstractions.Services;
using Manager.Api.Clients;
using Manager.Api.Exceptions;
using Manager.Service;
using Manager.Service.Services;
using Manager.Service.Storages;
using MongoDB.Driver;
using MongoDB.Entities;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB");

var mongoUrl = new MongoUrl(mongoConnectionString);
var settings = MongoClientSettings.FromConnectionString(mongoConnectionString);
DB dbInstance = await DB.InitAsync(mongoUrl.DatabaseName, settings);

builder.Services.AddSingleton(dbInstance);

builder.Services.Configure<WorkerOptions>(
    builder.Configuration.GetSection(WorkerOptions.SectionName));

builder.Services.Configure<TimeoutOptions>(
    builder.Configuration.GetSection(TimeoutOptions.SectionName));

builder.Services.Configure<AlphabetOptions>(
    builder.Configuration.GetSection(AlphabetOptions.SectionName));

builder.Services.Configure<RequestQueueOptions>(
    builder.Configuration.GetSection(RequestQueueOptions.SectionName));

builder.Services.Configure<CacheOptions>(
    builder.Configuration.GetSection(CacheOptions.SectionName));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:3000",
                "http://localhost:3000"
            )
            .AllowAnyMethod()
            .AllowAnyHeader();
        });
});

// Add services to the container.

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilterAttribute>();
});
builder.Services.AddMvc(options =>
{
    options.Filters.Add<ApiExceptionFilterAttribute>();
}).AddXmlFormaterExtensions();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();

builder.Services.AddSingleton<IManager, RequestProcessor>();
builder.Services.AddSingleton<IPlanner, Planner>();
builder.Services.AddSingleton<IRequestFinalizer, RequestFinalizer>();
builder.Services.AddSingleton<IRequestStorage, RequestPersistentStorage>();
builder.Services.AddSingleton<ITaskScheduler, Manager.Service.Services.TaskScheduler>();
builder.Services.AddSingleton<ITaskStorage, WorkerTaskPersistentStorage>();
builder.Services.AddSingleton<IWorkerMonitor, WorkerMonitor>();
builder.Services.AddSingleton<IRequestQueue, RequestQueue>();
builder.Services.AddSingleton<ICrackedHashCache, CrachedHashCache>();
builder.Services.AddSingleton<ITimeoutMonitor<string>, TimeoutMonitor<string>>();
builder.Services.AddSingleton<IEventBus, EventBus>();

builder.Services.AddSingleton<IWorkerApiFactory, WorkerApiFactory>();
builder.Services.AddHttpClient();

builder.Services.AddHostedService<RequestConsumer>();
builder.Services.AddHostedService(sp => (TimeoutMonitor<string>)sp.GetRequiredService<ITimeoutMonitor<string>>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();

app.UseStatusCodePages();

app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.MapControllers();

app.Run();
