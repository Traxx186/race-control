using RaceControl.Categories;
using RaceControl.Data.Enums;
using RaceControl.Database.Entities;

namespace RaceControl.Services;

public class CategoryService(
    ILogger<CategoryService> logger,
    ITrackStatusService trackStatusService,
    IEnumerable<ICategory> categories) : ICategoryService
{
    /// <summary>
    /// The currently active category.
    /// </summary>
    private ICategory? _activeCategory;

    /// <summary>
    /// The currently active session.
    /// </summary>
    private Session? _activeSession;

    /// <inheritdoc/>
    public bool HasSessionActive => _activeSession != null;

    /// <inheritdoc/>
    public Session? ActiveSession => _activeSession;

    /// <inheritdoc/>
    public async Task StartCategoryAsync(Session session)
    {
        _activeSession ??= session;

        if (!TryGetCategory(_activeSession.CategoryKey, out _activeCategory))
            return;

        logger.LogInformation("[Category Service] Starting API connection for session with key {key}", _activeSession.CategoryKey);

        _activeCategory!.FlagParsed += async (_, args) => await trackStatusService.SetActiveFlagAsync(args.Flag, args.Driver);
        _activeCategory!.SessionFinished += async (_, _) => await StopActiveCategoryAsync();

        await _activeCategory.StartAsync(_activeSession.Key);
    }

    /// <summary>
    /// Closes the API connection of the active category.
    /// </summary>
    private async Task StopActiveCategoryAsync()
    {
        await trackStatusService.SetActiveFlagAsync(Flag.Clear);

        logger.LogInformation("[Category Service] Closing the active category");
        _activeCategory = null;
        _activeSession = null;
    }

    /// <summary>
    /// Creates a new category object based on the given key.
    /// </summary>
    /// <param name="key">Key of the category.</param>
    /// <param name="category">The category object related to the give key.</param>
    /// <returns>If a category object has been found with the given key.</returns>
    private bool TryGetCategory(string key, out ICategory? category)
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