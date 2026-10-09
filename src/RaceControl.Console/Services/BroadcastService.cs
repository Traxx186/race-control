using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RaceControl.Console.Championships;
using RaceControl.Console.Events;
using RaceControl.Console.Options;
using RaceControl.Data.Dtos;

namespace RaceControl.Console.Services;

public class BroadcastService : IHostedService
{
    private readonly ILogger<BroadcastService> _logger;
    private readonly HubConnection _connection;
    private readonly IEnumerable<IChampionship> _championships;

    private IChampionship? _currentChampionship;

    public BroadcastService(
        ILogger<BroadcastService> logger,
        IOptionsMonitor<RaceControlOptions> optionsMonitor,
        IEnumerable<IChampionship> championships)
    {
        _logger = logger;
        _championships = championships;

        optionsMonitor.OnChange(async _ =>
        {
            if (_connection is null)
                return;

            _logger.LogInformation("[Broadcast Service] Config changed, restart Live timing");

            if (_connection.State == HubConnectionState.Connected)
            {
                if (_currentChampionship is not null)
                    await _currentChampionship.StopAsync();

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

    /// <summary>
    /// Connect to the set race control server.
    /// </summary>
    /// <param name="stoppingToken">The token to monitor for cancellation requests.</param>
    public async Task StartAsync(CancellationToken stoppingToken)
    {
        await _connection.StartAsync(stoppingToken);
        _logger.LogInformation("[Broadcast Service] Connected to server");
    }

    /// <summary>
    /// Disconnect from the set race control server.
    /// </summary>
    /// <param name="stoppingToken">The token to monitor for cancellation requests.</param>
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
        _currentChampionship = categoryDto.Key switch
        {
            "f1" => _championships.OfType<Formula1>().FirstOrDefault(),
            _ => null
        };

        if (_currentChampionship == null)
            return;

        _currentChampionship.MessageReceived += async (_, args) => await HandleFlagParsedEvent(args);

        _logger.LogInformation("[Broadcast Service] Starting live timing service for category {category}", categoryDto.Key);
        await _currentChampionship.StartAsync();
    }

    /// <summary>
    /// Handle the message received event.
    /// </summary>
    /// <param name="args">Event args</param>
    private async Task HandleFlagParsedEvent(MessageEventArgs args)
    {
        var topic = args.Topic;
        var data = args.Message;

        _logger.LogInformation("[Broadcast Service] Send message {topic} to race control server", topic);
        switch (topic)
        {
            case "RaceControlMessage":
                await _connection.InvokeAsync("RaceControlMessage", data);
                break;
            case "SessionStatus":
                await _connection.InvokeAsync("SessionStatus", data);
                break;
            case "TrackStatus":
                await _connection.InvokeAsync("TrackStatusMessage", data);
                break;
            default:
                throw new ArgumentException($"Topic {topic} is not supported");
        }
    }
}