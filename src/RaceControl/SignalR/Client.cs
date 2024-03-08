using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNet.SignalR.Client;
using Serilog;

namespace RaceControl.SignalR;

/// <summary>
/// A SignalR client wrapper to easily connect to a SignalR server with the correct hub and args.
/// </summary>
public sealed class Client
{
    /// <summary>
    /// The url to connect to.
    /// </summary>
    private readonly string _url;

    /// <summary>
    /// Subscription arguments.
    /// </summary>
    private readonly string[] _args;

    /// <summary>
    /// The hub name.
    /// </summary>
    private readonly string _hub;

    /// <summary>
    /// The protocol version to be used
    /// </summary>
    private readonly Version? _version;
    
    /// <summary>
    /// The connection object.
    /// </summary>
    private HubConnection? _connection;

    /// <summary>
    /// List of handlers.
    /// </summary>
    private readonly List<(string, string, Action<JsonArray>)> _handlers = [];
    
    /// <summary>
    /// If the SignalR service is active.
    /// </summary>
    public bool Running { private set; get; }
    
    public Client(string url, string hub, string[] args)
    {
        _url = url;
        _hub = hub;
        _args = args;
    }

    public Client(string url, string hub, string[] args, Version version)
        : this(url, hub, args)
    {
          _version = version;
    }

    /// <summary>
    /// Sets up, connects and processes incoming messages to the given SignalR server.
    /// </summary>
    public async void Start(string method)
    {
        Running = true;
        while (Running)
        { 
            using var connection = new HubConnection(_url);
//#if DEBUG
            connection.TraceWriter = Console.Out;
            connection.TraceLevel = TraceLevels.All;
//#endif
            connection.CookieContainer = new();
            connection.Error += e => Log.Error($"[SignalR] Error occured: {e.Message}");
            connection.Received += HandleMessage;
            connection.Reconnecting += () => Log.Information("[SignalR] Reconnecting");
            connection.Reconnected += () => Log.Information("[SignalR] Reconnected");
            connection.DeadlockErrorTimeout  = TimeSpan.FromSeconds(60);

            if (null != _version)
                connection.Protocol = _version;

            var f1Timing = connection.CreateHubProxy(_hub);
            _connection = connection;

            Log.Information($"[SignalR] Connecting to {_url}");
            await connection.Start();
            await f1Timing.Invoke(method, _args.ToList());

            Console.Read();   
        }
    }

    /// <summary>
    /// Adds a handler to be called when the hub and method equal the incoming message.
    /// </summary>
    /// <param name="hub">Name of the hub.</param>
    /// <param name="method">Name of the executed method.</param>
    /// <param name="handler">Function that will be executed.</param>
    public void AddHandler(string hub, string method, Action<JsonArray> handler) =>
        _handlers.Add((hub, method, handler));

    /// <summary>
    /// Checks if the incoming message can be used to call a handler.
    /// </summary>
    /// <param name="message">The data received from the server.</param>
    private void HandleMessage(string message)
    {
        var data = JsonSerializer.Deserialize<Message>(message);
        if (null == data || null == data.A)
            return;

        Log.Information($"[SignalR] New message received");
        _handlers.Where(x => x.Item1 == data.H && x.Item2 == data.M)
            .ToList()
            .ForEach(x => x.Item3.Invoke(data.A));
    }

    /// <summary>
    /// Disconnects the SignalR client.
    /// </summary>
    public void Stop()
    {
        _connection?.Dispose();
        _connection = null;
        
        Running = false;
    }
}