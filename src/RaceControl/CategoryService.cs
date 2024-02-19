using System.Timers;
using Npgsql;
using RaceControl.Category;
using RaceControl.Track;
using Serilog;
using Timer = System.Timers.Timer;

namespace RaceControl;

public class CategoryService
{
    /// <summary>
    /// Available racing categories that have a race control parser implemented.
    /// </summary>
    private static readonly Dictionary<string, ICategory> Categories = new()
    {
        { "f1", new Formula1("https://livetiming.formula1.com") }
    };

    /// <summary>
    /// The currently active category.
    /// </summary>
    private ICategory? _activeCategory;

    /// <summary>
    /// The timer used for checkin if there is an active category.
    /// </summary>
    private readonly Timer _timer;

    /// <summary>
    /// The MongoDB client connection to the category calendar database.
    /// </summary>
    private readonly NpgsqlDataSource _pgsqlClient;

    /// <summary>
    /// Event that will be triggered when a category parser has parsed a flag. 
    /// </summary>
    public event Action<FlagData>? OnCategoryFlagChange;

    public CategoryService()
    {
        _pgsqlClient = CreatePgsqlConnection();

        _timer = new Timer(TimeSpan.FromMinutes(1));
        _timer.Elapsed += GetActiveCategory;
    }

    public void Start()
    {
        _timer.Enabled = true;
    }

    /// <summary>
    /// Gets the key of the next active session and sets the correct connector to listen to the
    /// live timing data.
    /// </summary>
    /// <param name="source">The timer source.</param>
    /// <param name="e">Args of the timer event.</param>
    private async void GetActiveCategory(object? source, ElapsedEventArgs e)
    {
        var appendedTime = new TimeSpan(e.SignalTime.Hour, e.SignalTime.Minute + 5, 0);
        var signalTime = (e.SignalTime.Date + appendedTime).ToUniversalTime();
        var calendarItem = await GetCategory(signalTime);
        if (!calendarItem.HasValue || !Categories.TryGetValue(calendarItem.Value.CategoryKey, out var category)) return;

        Log.Information($"[CategoryService] Found active session with key {calendarItem.Value.CategoryKey}");
        _activeCategory = category;
        _activeCategory.OnFlagParsed += data => OnCategoryFlagChange?.Invoke(data);
        _activeCategory.OnSessionFinished += StopActiveCategory;
        _activeCategory.Start(calendarItem.Value.Key);

        _timer.Enabled = false;
    }

    /// <summary>
    /// Creates a async <see cref="NpgsqlConnection"/> to be used later for quering the database.
    /// </summary>
    /// <returns>A <see cref="NpgsqlConnection"/> object.</returns>
    private static NpgsqlDataSource CreatePgsqlConnection()
    {
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (null == databaseUrl)
        {
            Log.Fatal("[CategoryService] 'DATABASE_URL' env variable must be set");
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
                ON c.id = s.category_id
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

    private async void StopActiveCategory()
    {
        await Task.Delay(new TimeSpan(0, 5, 0));

        Log.Information("[CategoryService] Closing the active category");
        _activeCategory?.Stop();
        _activeCategory = null;
        _timer.Enabled = true;
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