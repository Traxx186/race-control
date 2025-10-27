using RaceControl.Categories;
using RaceControl.Database.Entities;
using RaceControl.Track;

namespace RaceControl.Services;

public class CategoryService(ILogger<CategoryService> logger, TrackStatus trackStatus)
{
    /// <summary>
    /// The currently active category.
    /// </summary>
    private ICategory? _activeCategory;
    
    /// <summary>
    /// The currently active session.
    /// </summary>
    private Session? _activeSession;
    
    /// <summary>
    /// If there is already a session active.
    /// </summary>
    public bool HasSessionActive => _activeSession != null;

    /// <summary>
    /// Returns the currently active session, if there is any.
    /// </summary>
    public Session? ActiveSession => _activeSession;
    
    /// <summary>
    /// Starts the API connection of the category based on the given session.
    /// </summary>
    /// <param name="session">The session of the category to start.</param>
    public async Task StartCategoryAsync(Session session)
    {
        _activeSession ??= session;
        
        if (!TryGetCategory(_activeSession.CategoryKey, out var category))
            return;
        
        logger.LogInformation("[Category Service] Starting API connection for session with key {key}", _activeSession.CategoryKey);
        
        _activeCategory = category!;
        _activeCategory.FlagParsed += async (_, args) => await trackStatus.SetActiveFlagAsync(args.FlagData);
        _activeCategory.SessionFinished += async (_, _) => await StopActiveCategoryAsync();
        
        await _activeCategory.StartAsync(_activeSession.Key);
    }

    /// <summary>
    /// Closes the API connection of the active category.
    /// </summary>
    private async Task StopActiveCategoryAsync()
    {
        await Task.Delay(new TimeSpan(0, 0, 30));
        await trackStatus.SetActiveFlagAsync(new FlagData { Flag = Flag.Clear });

        logger.LogInformation("[Category Service] Closing the active category");
        _activeCategory?.Stop();
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
            "f1" => new Formula1(logger, "https://livetiming.formula1.com"),
            "f2" => new Formula2(logger, "https://ltss.fiaformula2.com"),
            "f3" => new Formula3(logger, "https://ltss.fiaformula3.com"),
            _ => null
        };
        
        return category != null;
    }
}