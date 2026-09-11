using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RaceControl.Console.Categories;
using RaceControl.Console.Options;
using RaceControl.Data.Dtos;
using RaceControl.Data.Events;

namespace RaceControl.Console.Services;

public class BroadcastService : IHostedService
{
    private readonly ILogger<BroadcastService> _logger;
    private readonly HubConnection _connection;
    private readonly IEnumerable<ICategory> _categories;

    public BroadcastService(
        ILogger<BroadcastService> logger,
        IOptionsMonitor<RaceControlOptions> optionsMonitor,
        IEnumerable<ICategory> categories)
    {
        _logger = logger;
        _categories = categories;

        optionsMonitor.OnChange(async _ =>
        {
            if (_connection is null)
                return;

            _logger.LogInformation("[Broadcast Service] Config changed, restart Live timing");

            if (_connection.State == HubConnectionState.Connected)
            {
                await _connection.StopAsync();
                await Task.Delay(5000);
            }

            await _connection.StartAsync();
        });

        var host = new UriBuilder( optionsMonitor.CurrentValue.BroadcastHost ?? "http://localhost:8080")
        {
            Path = "signalr"
        };

        _logger.LogInformation("[Broadcast Service] Setting up connection to race control server {uri}", host.ToString());
        _connection = new HubConnectionBuilder()
            .WithUrl(host.Uri)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<CategoryDto>("CategoryChange", HandleCategoryChange);
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        await _connection.StartAsync(stoppingToken);
        _logger.LogInformation("[Broadcast Service] Connected to server");
    }

    public async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Broadcast Service] Stopping connection to server");

        if (_connection.State == HubConnectionState.Connected)
            await _connection.StopAsync(stoppingToken);
    }

    /// <summary>
    /// Connect to the correct live timing parser based on the active category.
    /// </summary>
    /// <param name="categoryDto">Active category.</param>
    private async Task HandleCategoryChange(CategoryDto categoryDto)
    {
        _logger.LogInformation("[Broadcast Service] Parsing category change message");
        var category = categoryDto.Key switch
        {
            "f1" => _categories.OfType<Formula1>().FirstOrDefault(),
            _ => null
        };

        if (category == null)
            return;

        category.FlagParsed += async (_, args) => await HandleFlagParsedEvent(args);
        category.SessionFinished += async (_, _) => await HandleSessionFinishedEvent();

        _logger.LogInformation("[Broadcast Service] Starting live timing service for category {category}", categoryDto.Key);
        await category.StartAsync();
    }

    /// <summary>
    /// Handle the flag parsed event.
    /// </summary>
    /// <param name="args">Event args</param>
    private async Task HandleFlagParsedEvent(FlagChangedEventArgs args)
    {
        var flagDataDto = new FlagDataDto(args.Flag, args.Driver);

        _logger.LogInformation("[Broadcast Service] Send flag  {flag} to race control server", flagDataDto.Flag);
        await _connection.InvokeAsync("SendFlag", flagDataDto);
    }

    /// <summary>
    /// Handle the session finished event.
    /// </summary>
    private async Task HandleSessionFinishedEvent()
    {
        _logger.LogInformation("[Broadcast Service] Send session finished message to race control server");
        await _connection.InvokeAsync("SessionFinished");
    }
}