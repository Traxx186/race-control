using RaceControl.Categories;
using RaceControl.Database;
using RaceControl.Database.Entities;
using RaceControl.Track;

namespace RaceControl;

public class CategoryService(ILogger<CategoryService> logger, RaceControlContext dbContext, TrackStatus trackStatus)
    : BackgroundService
{
    /// <summary>
    /// The currently active category.
    /// </summary>
    private ICategory? _activeCategory;

    /// <summary>
    /// If there is a current session active.
    /// </summary>
    private bool _sessionActive;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[CategoryService] Category service started");
        GetActiveCategory();

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // Wait for the next loop if there is a session active.
            if (_sessionActive)
                continue;

            // If the current day of week is not between thursday and sunday, do nothing
            // to reduce checks.
            if (DateTime.Now.DayOfWeek < DayOfWeek.Thursday && DateTime.Now.DayOfWeek != DayOfWeek.Sunday)
                continue;

            logger.LogInformation("[CategoryService] Search for an active category");
            GetActiveCategory();
        }
    }

    /// <summary>
    /// Gets the key of the next active session and sets the correct connector to listen to the
    /// live timing data.
    /// </summary>
    private void GetActiveCategory()
    {
        var signalTime = DateTime.Now.AddMinutes(5).ToUniversalTime();
        var calendarItem = GetCategory(new DateTime(signalTime.Year, signalTime.Month, signalTime.Day, signalTime.Hour,
            signalTime.Minute, 0));
        if (null == calendarItem)
            return;

        var category = GetCategory(calendarItem.CategoryKey);
        if (category == null)
            return;

        logger.LogInformation("[CategoryService] Found active session with key {key}", calendarItem.CategoryKey);
        _activeCategory = category;
        _activeCategory.FlagParsed += (_, args) => trackStatus.SetActiveFlag(args.FlagData);
        _activeCategory.SessionFinished += StopActiveCategory;
        _activeCategory.Start(calendarItem.Key);
        _sessionActive = true;
    }

    /// <summary>
    /// Queries the database to find of there is a session that will start at the given time (UTC). 
    /// </summary>
    /// <param name="currentTime">The current time (UTC).</param>
    /// <returns>If there is an active session, else null.</returns>
    private Session? GetCategory(DateTime currentTime)
    {
        return dbContext.Sessions
            .SingleOrDefault(s => s.Time == currentTime);
    }

    /// <summary>
    /// Closes the API connection of the active category.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void StopActiveCategory(object? sender, EventArgs e)
    {
        await Task.Delay(new TimeSpan(0, 1, 0));

        logger.LogInformation("[CategoryService] Closing the active category");
        _activeCategory?.Stop();
        _activeCategory = null;
        _sessionActive = false;
    }

    /// <summary>
    /// Creates a new category object based on the given key.
    /// </summary>
    /// <param name="key">Key of the category.</param>
    /// <returns>A instance of the category.</returns>
    private static ICategory? GetCategory(string key)
    {
        return key switch
        {
            "f1" => new Formula1("https://livetiming.formula1.com"),
            "f2" => new Formula2("https://ltss.fiaformula2.com"),
            _ => null
        };
    }
}