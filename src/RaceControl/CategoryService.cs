using Npgsql;
using RaceControl.Category;
using RaceControl.Track;

namespace RaceControl;

public class CategoryService : BackgroundService
{
    /// <summary>
    /// The currently active category.
    /// </summary>
    private ICategory? _activeCategory;

    /// <summary>
    /// The MongoDB client connection to the category calendar database.
    /// </summary>
    private readonly NpgsqlDataSource _pgsqlClient;

    /// <summary>
    /// Event that will be triggered when a category parser has parsed a flag. 
    /// </summary>
    public event EventHandler<FlagDataEventArgs>? CategoryFlagChange;

    /// <summary>
    /// If there is a current session active.
    /// </summary>
    private bool _sessionActive;

    /// <summary>
    /// The category service logger.
    /// </summary>
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(ILogger<CategoryService> logger)
    {
        _pgsqlClient = CreatePgsqlConnection();
        _logger = logger;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[CategoryService] Category service started");
        GetActiveCategory();

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while(await timer.WaitForNextTickAsync(stoppingToken))
        {
            // Wait for the next loop if there is an session active.
            if(_sessionActive)
                continue;

            _logger.LogInformation("[CategoryService] Search for an active category");
            GetActiveCategory();
        }
    }

    /// <summary>
    /// Gets the key of the next active session and sets the correct connector to listen to the
    /// live timing data.
    /// </summary>
    private async void GetActiveCategory()
    {
        var signalTime = DateTime.Now.AddMinutes(5).ToUniversalTime();
        var calendarItem = await GetCategory(new DateTime(signalTime.Year, signalTime.Month, signalTime.Day, signalTime.Hour, signalTime.Minute, 0));
        if (!calendarItem.HasValue) 
            return;

        var category = GetCategory(calendarItem.Value.CategoryKey);
        if (category == null)
            return;

        _logger.LogInformation($"[CategoryService] Found active session with key {calendarItem.Value.CategoryKey}");
        _activeCategory = category;
        _activeCategory.FlagParsed += (_, args) => OnCategoryFlagChange(args.FlagData);
        _activeCategory.SessionFinished += StopActiveCategory;
        _activeCategory.Start(calendarItem.Value.Key);
        _sessionActive = true;
    }

    /// <summary>
    /// Invokes the Category Flag Change event.
    /// </summary>
    /// <param name="flagData">The flag data send in the event.</param>
    protected virtual void OnCategoryFlagChange(FlagData flagData)
    {
        var args = new FlagDataEventArgs() { FlagData = flagData };

        CategoryFlagChange?.Invoke(this, args);
    }

    /// <summary>
    /// Creates a async <see cref="NpgsqlConnection"/> to be used later for querying the database.
    /// </summary>
    /// <returns>A <see cref="NpgsqlConnection"/> object.</returns>
    private NpgsqlDataSource CreatePgsqlConnection()
    {
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (null == databaseUrl)
        {
            _logger.LogCritical("[CategoryService] 'DATABASE_URL' env variable must be set");
            Environment.Exit(0);
        }

        var dataSource = NpgsqlDataSource.Create(databaseUrl);
        return dataSource;
    }

    /// <summary>
    /// Queries the database to find of there is a session that will start at the given
    /// time (UTC). 
    /// </summary>
    /// <param name="currentTime">The current time (UTC).</param>
    /// <returns>If there is an active session, else null.</returns>
    private async Task<RaceSession?> GetCategory(DateTime currentTime)
    {
        var query = @$"
            SELECT s.name as session_name, 
                s.key as session_key, 
                c.key as category_key, 
                c.priority as category_priority
            FROM session s
            INNER JOIN category c
                ON c.key = s.category_key
            WHERE s.time = @p1
            ORDER BY category_priority ASC
            LIMIT 1
        ";

        await using var connection = await _pgsqlClient.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(query, connection)
        {
            Parameters = { new("p1", currentTime) }
        };

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            return new RaceSession()
            {
                CategoryKey = (string)reader["category_key"],
                Priority = (short)reader["category_priority"],
                Name = (string)reader["session_name"],
                Key = (string)reader["session_key"]
            };
        }

        return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void StopActiveCategory(object? sender, EventArgs e)
    {
        await Task.Delay(new TimeSpan(0, 1, 0));

        _logger.LogInformation("[CategoryService] Closing the active category");
        _activeCategory?.Stop();
        _activeCategory = null;
        _sessionActive = false;
    }

    /// <summary>
    /// Creates a new category object based on the given key.
    /// </summary>
    /// <param name="key">Key of the category.</param>
    /// <returns>A instance of the category.</returns>
    private ICategory? GetCategory(string key)
    {
        return key switch
        {
            "f1" => new Formula1("https://livetiming.formula1.com"),
            "f2" => new Formula2("https://ltss.fiaformula2.com"),
            _ => null,
        };
    }

    /// <summary>
    /// Structure for a session entry in the database
    /// </summary>
    private struct RaceSession
    {
        /// <summary>
        /// A identification key for the race category e.g. f1, f2.
        /// </summary>
        public string CategoryKey;
        /// <summary>
        /// The priority of the race category
        /// </summary>
        public short Priority;
        /// <summary>
        /// The name of the active race weekend.
        /// </summary>
        public string Name;
        /// <summary>
        /// The key of the active session  e.g. fp1, gp.
        /// </summary>
        public string Key;
    }
}