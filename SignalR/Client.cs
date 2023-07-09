using System.Net;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using Serilog;

namespace RaceControl.SignalR;

/// <summary>
/// A SignalR client wrapper to easily connect to a SignalR server with the correct hub and args.
/// </summary>
public class Client
{
    /// <summary>
    /// The url to connect to.
    /// </summary>
    private string _url;

    /// <summary>
    /// Subscription arguments.
    /// </summary>
    private string[] _args;

    /// <summary>
    /// The hub name.
    /// </summary>
    private string _hub;

    /// <summary>
    /// The connection object.
    /// </summary>
    private HubConnection? _connection;

    /// <summary>
    /// List of handlers
    /// </summary>
    private List<Tuple<string, string, Action<dynamic>>> _handlers = new();

    private bool _running;

    public bool Running
    {
        set => _running = value;
        get => _running;
    }

    public Client(string url, string hub, string[] args)
    {
        _url = url;
        _hub = hub;
        _args = args;
    }

    /// <summary>
    /// Sets up, connects and processes incoming messages to the given SignalR server.
    /// </summary>
    public async Task Start()
    {
        _running = true;
        while (_running)
        {
            using var connection = new HubConnection(_url);
            connection.TraceWriter = Console.Out;
            connection.TraceLevel = TraceLevels.All;
            connection.CookieContainer = new CookieContainer();
            connection.Error += e => Log.Error($"[SignalR] Error occured: {e.Message}");
            connection.Received += HandleMessage;
            connection.Reconnecting += () => Log.Warning("[SignalR] Reconnecting");
            connection.Reconnected += () => Log.Information("[SignalR] Reconnected");

            var f1Timing = connection.CreateHubProxy(_hub);
            _connection = connection;

            Log.Information($"[SignalR] connected to {_url}");
            await connection.Start();
            await f1Timing.Invoke("Subscribe", _args.ToList());

            Console.Read();
        }
    }

    /// <summary>
    /// Adds a handler to be called when the hub and method equal the incoming message
    /// </summary>
    /// <param name="hub">Name of the hub</param>
    /// <param name="method">Name of the executed method</param>
    /// <param name="handler">Function that will be executed</param>
    public void AddHandler(string hub, string method, Action<dynamic> handler) =>
        _handlers.Add(new Tuple<string, string, Action<dynamic>>(hub, method, handler));

    /// <summary>
    /// Checks if the incoming message can be used to call a handler.
    /// </summary>
    /// <param name="message">The data received from the server</param>
    private void HandleMessage(string message)
    {
        var data = JsonConvert.DeserializeObject<Message>(message);
        if (null == data.A)
            return;
        
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
    }
}