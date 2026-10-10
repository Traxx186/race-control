using RaceControl.Server.Championships;
using RaceControl.Data.Dtos;
using RaceControl.Data.Enums;
using RaceControl.Database.Entities;

namespace RaceControl.Server.Services;

public sealed class ChampionshipService(
    ILogger<ChampionshipService> logger,
    ITrackStatusService trackStatusService,
    IEnumerable<IChampionship> categories) : IChampionshipService
{
    /// <summary>
    /// The currently active category.
    /// </summary>
    private IChampionship? _activeChampionship;

    /// <summary>
    /// The currently active session.
    /// </summary>
    private Session? _activeSession;

    /// <inheritdoc/>
    public SortedList<DateTime, FlagDataDto> FlagQueue { get; } = new();

    /// <inheritdoc/>
    public bool HasSessionActive => _activeSession != null;

    /// <inheritdoc/>
    public Session? ActiveSession => _activeSession;

    /// <inheritdoc/>
    public IChampionship? ActiveChampionship => _activeChampionship;

    /// <inheritdoc/>
    public async Task StartCategoryAsync(Session session)
    {
        _activeSession ??= session;

        if (!TryGetCategory(_activeSession.ChampionshipId, out _activeChampionship))
            return;

        logger.LogInformation("[Category Service] Starting API connection for session with key {key}", _activeSession.ChampionshipId);

        _activeChampionship!.FlagParsed += (_, args) => EnqueueFlag(new FlagDataDto(args.Flag, args.Driver));
        _activeChampionship!.SessionFinished += async (_, _) => await StopActiveCategoryAsync();

        await _activeChampionship.StartAsync();
    }

    /// <inheritdoc/>
    public async Task StopActiveCategoryAsync()
    {
        await trackStatusService.SetActiveFlagAsync(new FlagDataDto(Flag.Clear));

        logger.LogInformation("[Category Service] Closing the active category");
        FlagQueue.Clear();
        _activeChampionship = null;
        _activeSession = null;
    }

    /// <inheritdoc/>
    public void EnqueueFlag(FlagDataDto flagData)
    {
        if (flagData.Flag == Flag.None)
        {
            logger.LogInformation("[Category Service] Ignore invalid flag");
            return;
        }

        logger.LogInformation("[Category Service] Append flag {flag} to queue", flagData.Flag);
        FlagQueue.Add(DateTime.UtcNow, flagData);
    }

    /// <summary>
    /// Creates a new category object based on the given key.
    /// </summary>
    /// <param name="key">Key of the category.</param>
    /// <param name="category">The category object related to the give key.</param>
    /// <returns>If a category object has been found with the given key.</returns>
    private bool TryGetCategory(string key, out IChampionship? category)
    {
        category = key switch
        {
            "f1" => categories.OfType<Formula1>().FirstOrDefault(),
            "f2" => categories.OfType<Formula2>().FirstOrDefault(),
            "f3" => categories.OfType<Formula3>().FirstOrDefault(),
            _ => null
        };

        return category != null;
    }
}