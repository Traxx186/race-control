using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RaceControl.Console.Categories;
using RaceControl.Console.Options;
using RaceControl.Data.Dtos;

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
            _logger.LogInformation("[Broadcast Service] Config changed, restart Live timing");

            if (_connection.State == HubConnectionState.Connected)
            {
                await _connection.StopAsync();
                await Task.Delay(5000);
            }

            await _connection.StartAsync();
        });

        var host = optionsMonitor.CurrentValue.BroadcastHost ?? "http://localhost:8080";
        _connection = new HubConnectionBuilder()
            .WithUrl($"{host}/signalr")
            .WithAutomaticReconnect()
            .Build();

        _connection.On<CategoryDto>("CategoryChange", HandleCategoryChange);
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Broadcast Service] Connecting to server");

        await _connection.StartAsync(stoppingToken);
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

        _logger.LogInformation("[Broadcast Service] Starting live timing service for category {category}", categoryDto.Key);
        await category.StartAsync();
    }
}