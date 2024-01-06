using System.Timers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
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
    private Timer _timer;

    /// <summary>
    /// The MongoDB client connection to the category calendar database.
    /// </summary>
    private MongoClient? _mongoClient;

    /// <summary>
    /// If there is already an session active
    /// </summary>
    private bool _sessionActive = false;

    /// <summary>
    /// Event that will be triggered when a category parser has parsed a flag. 
    /// </summary>
    public event Action<FlagData>? OnCategoryFlagChange;

    public CategoryService()
    {
        _mongoClient = CreateMongoClient();

        _timer = new Timer(TimeSpan.FromMinutes(1));
        _timer.Elapsed += GetActiveCategory;
    }

    public void Start()
    {
        _timer.Enabled = true;
    }

    private void GetActiveCategory(object? source, ElapsedEventArgs e)
    {
        if (_sessionActive)
            return;

        var appendedTime = new TimeSpan(e.SignalTime.Hour, e.SignalTime.Minute + 5, 0);
        var signalTime = (e.SignalTime.Date + appendedTime).ToUniversalTime();
        var calendarItem = GetCategoryKey(signalTime);
        var categoryKey = calendarItem?.Name;
        var session = calendarItem?.Sessions
            .Find(session => session.Value == $"{signalTime:s}Z")
            .Key;

        if (null == categoryKey || !Categories.TryGetValue(categoryKey, out var category)) return;

        Log.Information($"[CategoryService] Found active session with key {categoryKey}");
        _activeCategory = category;
        _activeCategory.OnFlagParsed += data => OnCategoryFlagChange?.Invoke(data);
        _activeCategory.Start(string.Empty);
    }

    private static MongoClient CreateMongoClient()
    {
        var mongoUrl = Environment.GetEnvironmentVariable("MONGODB_URL");
        if (null == mongoUrl)
        {
            Log.Fatal("[CategoryService] 'MONGODB_URL' env variable must be set");
            Environment.Exit(0);
        }

        return new MongoClient(mongoUrl);
    }

    private CalenderItem? GetCategoryKey(DateTime currentTime)
    {
        var calendarCollection = _mongoClient?.GetDatabase("calendar")
            .GetCollection<BsonDocument>(currentTime.Year.ToString());

        var matchBySessionTime = Builders<BsonDocument>.Filter.Eq("sessions.v", "2023-09-17T12:00:00Z");
        var sortByPriority = Builders<BsonDocument>.Sort.Ascending("priority");
        var aggregate = calendarCollection.Aggregate()
            .Unwind("races")
            .Project(new BsonDocument
            {
                { "key", "$key" },
                { "priority", "$priority" },
                { "sessions", new BsonDocument { { "$objectToArray", "$races.sessions" } } },
                { "name", "$races.name" }
            })
            .Match(matchBySessionTime)
            .Sort(sortByPriority)
            .Limit(1);

        var calenderItem = BsonSerializer.Deserialize<CalenderItem>(aggregate.ToBson());
        return calenderItem;
    }

    /// <summary>
    /// Structure for a race weekend entry in the database
    /// </summary>
    private struct CalenderItem
    {
        public string Key;
        public short Priority;
        public List<KeyValuePair<string, string>> Sessions;
        public string Name;
    }
}