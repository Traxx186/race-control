using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RaceControl.Console.Categories;
using RaceControl.Console.Options;
using RaceControl.Console.Services;
using Serilog;

var logPath = Path.Join(RaceControlOptions.AppStoragePath, "logs", "log.txt");

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.SetBasePath(Environment.CurrentDirectory);
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile(RaceControlOptions.ConfigFilePath, optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<RaceControlOptions>(builder.Configuration.GetSection(RaceControlOptions.Key));

builder.Services.AddHostedService<BroadcastService>();

// Add the supported racing categories.
builder.Services.AddSingleton<ICategory, Formula1>();

// Configure Serilog
builder.Services.AddSerilog(configuration =>
    configuration
        .WriteTo.Console()
        .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
);

var app = builder.Build();

await app.RunAsync();