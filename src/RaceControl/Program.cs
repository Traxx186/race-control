using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using NodaTime.Serialization.SystemTextJson;
using Quartz;
using RaceControl.Database;
using RaceControl.Hubs;
using RaceControl.Jobs;
using RaceControl.Services;
using RaceControl.Track;
using Serilog;
using Serilog.Settings.Configuration;

// Create a new WebApplication builder and load Environment Variables and the appsettings.json file 
// into the configuration.
var builder = WebApplication.CreateSlimBuilder(args);
builder.Configuration.SetBasePath(Environment.CurrentDirectory);
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// Create a new logger instance with the app configuration 
builder.Services.AddSerilog(configuration =>
    configuration.ReadFrom
        .Configuration(
            builder.Configuration,
            new ConfigurationReaderOptions(ConfigurationAssemblySource.AlwaysScanDllFiles)
        )
);

// Configure JSON serialization options
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<Flag>());
    options.SerializerOptions.Converters.Add(new NodaTimeDefaultJsonConverterFactory());
});

// Load controllers, signalr hubs and services to the web application.
builder.Services.AddSignalR();
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddSingleton<TrackStatus>();
builder.Services.AddSingleton<CategoryService>();

// Create the database connection and add the app database context to the services
builder.Services.AddDbContextPool<RaceControlContext>(opts => opts
    .UseNpgsql(builder.Configuration["DATABASE_URL"], o =>
        o.UseNodaTime()
    ).UseSnakeCaseNamingConvention()
);

// Add all the Quartz jobs with their job trigger to the Quartz service.
builder.Services.AddQuartz(quartz =>
{
    quartz.AddJob<SyncSessionsJob>(opts => opts.WithIdentity(SyncSessionsJob.JobKey));
    quartz.AddTrigger(opts => opts
        .ForJob(SyncSessionsJob.JobKey)
        .WithIdentity("SyncSessionsJob-trigger")
        .WithCronSchedule("0 0 8 ? * THU")
    );

    quartz.AddJob<FetchActiveSessionJob>(opts => opts.WithIdentity(FetchActiveSessionJob.JobKey));
    quartz.AddTrigger(opts => opts
        .ForJob(FetchActiveSessionJob.JobKey)
        .WithIdentity("FetchActiveSessionJob-trigger")
        .WithCronSchedule("0 * * ? * SUN,MON,THU,FRI,SAT")
    );
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// Create a Web Application object from the Web Application Builder.
var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();

// Map controller & related web data
app.MapControllers();
app.MapRazorPages();
app.MapDefaultControllerRoute();
app.MapStaticAssets();

// Map SignalR Hubs to app
app.MapHub<TrackStatusHub>("/track-status");
app.MapHub<SessionHub>("/session");

app.Logger.LogInformation("[Race Control] Starting Application");
await app.RunAsync();