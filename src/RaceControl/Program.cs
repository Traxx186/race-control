using Microsoft.EntityFrameworkCore;
using Quartz;
using RaceControl.Database;
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

// Add the services to the web application.
builder.Services.AddControllers();
builder.Services.AddSingleton<TrackStatus>();
builder.Services.AddSingleton<CategoryService>();
builder.Services.AddSingleton<WebsocketService>();

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
        //.WithCronSchedule("0 0 8 ? * THU")
        .WithCronSchedule("0 * * ? * *")
    );

    quartz.AddJob<FetchActiveSessionJob>(opts => opts.WithIdentity(FetchActiveSessionJob.JobKey));
    quartz.AddTrigger(opts => opts
        .ForJob(FetchActiveSessionJob.JobKey)
        .WithIdentity("FetchActiveSessionJob-trigger")
        .WithCronSchedule("0 * * ? * SUN,THU,FRI,SAT")
    );
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// Create a Web Application object from the Web Application Builder.
var app = builder.Build();
app.UseForwardedHeaders();
app.UseWebSockets();
app.MapControllers();

app.Logger.LogInformation("[Race Control] Starting Application");
await app.RunAsync();