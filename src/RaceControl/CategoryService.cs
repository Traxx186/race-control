using RaceControl.Categories;
using RaceControl.Database.Entities;
using RaceControl.Track;

namespace RaceControl;

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
    /// Starts the API connection of the category based on the given session.
    /// </summary>
    /// <param name="session">The session of the category to start.</param>
    public void StartCategory(Session session)
    {
        _activeSession ??= session;
        
        if (!TryGetCategory(_activeSession.CategoryKey, out var category))
            return;
        
        logger.LogInformation("[Category Service] Starting API connection for session with key {key}", _activeSession.CategoryKey);
        _activeCategory = category!;
        _activeCategory.FlagParsed += (_, args) => trackStatus.SetActiveFlag(args.FlagData);
        _activeCategory.SessionFinished += StopActiveCategory;
        _activeCategory.Start(_activeSession.Key);
    }

    /// <summary>
    /// Closes the API connection of the active category.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void StopActiveCategory(object? sender, EventArgs e)
    {
        await Task.Delay(new TimeSpan(0, 1, 0));

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
    private static bool TryGetCategory(string key, out ICategory? category)
    {
        category = key switch
        {
            "f1" => new Formula1("https://livetiming.formula1.com"),
            "f2" => new Formula2("https://ltss.fiaformula2.com"),
            _ => null
        };
        
        return category != null;
    }
}