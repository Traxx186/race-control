using System.Timers;
using MongoDB.Bson;
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
    private static readonly Dictionary<string, ICategory> _categories = new()
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
    /// Event that will be triggered when a category parser has parsed a flag. 
    /// </summary>
    public event Action<FlagData>? OnCategoryFlagChange;

    public CategoryService()
    {
        _mongoClient = CreateMongoClient();
        
        _timer = new Timer(TimeSpan.FromMinutes(1));
        _timer.Elapsed += GetActiveCategory;
        
        _activeCategory = new Formula1("https://livetiming.formula1.com");
        _activeCategory.OnFlagParsed += data => OnCategoryFlagChange?.Invoke(data);
    }

    public void Start()
    {
        _activeCategory?.Start();
        //_timer.Enabled = true;
    }

    private void GetActiveCategory(object? source, ElapsedEventArgs e)
    {
        var appendedTime = new TimeSpan(e.SignalTime.Hour, e.SignalTime.Minute + 5, 0);
        var signalTime = (e.SignalTime.Date + appendedTime).ToUniversalTime();
        var categoryKey = GetCategoryKey(signalTime);
    }

    private MongoClient CreateMongoClient()
    {
        var mongoUrl = Environment.GetEnvironmentVariable("MONGODB_URL");
        if (null == mongoUrl)
        {
            Log.Error("[CategoryService] 'MONGODB_URL' env variable must be set");
            Environment.Exit(0);
        }

        return new MongoClient(mongoUrl);
    }

    private string? GetCategoryKey(DateTime currentTime)
    {
        var calendarCollection = _mongoClient?.GetDatabase("calendar")
            .GetCollection<BsonDocument>(currentTime.Year.ToString());

        var matchBySessionTime = Builders<BsonDocument>.Filter.Eq("sessions.v", currentTime.ToString("s") + "Z");
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

        var results = aggregate.ToList();
        return results[0]["key"]?.ToString();
    }
}