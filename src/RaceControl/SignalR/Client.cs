using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNet.SignalR.Client;
using Serilog;

namespace RaceControl.SignalR;

/// <summary>
/// A SignalR client wrapper to easily connect to a SignalR server with the correct hub and args.
/// </summary>
public sealed class Client(string url, string hub, object[] args)
{
    /// <summary>
    /// The url to connect to.
    /// </summary>
    private readonly string _url = url;

    /// <summary>
    /// Subscription arguments.
    /// </summary>
    private readonly object[] _args = args;

    /// <summary>
    /// The hub name.
    /// </summary>
    private readonly string _hub = hub;

    /// <summary>
    /// The protocol version to be used
    /// </summary>
    private readonly Version? _version;
    
    /// <summary>
    /// Custom endpoint if API doesn't use /signalr.
    /// </summary>
    private readonly string _customEndpoint = string.Empty;

    /// <summary>
    /// If the connection uses the default endpoint.
    /// </summary>
    private readonly bool _useDefaultEndpoint = true;

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
    private bool Running { set; get; }

    public Client(string url, string hub, object[] args, Version version)
        : this(url, hub, args)
    {
          _version = version;
    }

    public Client(string url, string hub, object[] args, Version version, string customEndpoint)
        : this(url, hub, args, version)
    {
        _customEndpoint = customEndpoint;
        _useDefaultEndpoint = false;
    }

    /// <summary>
    /// Sets up, connects and processes incoming messages to the given SignalR server.
    /// </summary>
    public async Task StartAsync(string method)
    {
        var uriBuilder = new UriBuilder(_url);
        if (!_useDefaultEndpoint)
            uriBuilder.Path = _customEndpoint;
        
        Running = true;
        while (Running)
        { 
            using var connection = new HubConnection(uriBuilder.ToString(), useDefaultUrl: _useDefaultEndpoint);
#if DEBUG
            connection.TraceWriter = Console.Out;
            connection.TraceLevel = TraceLevels.All;
#endif
            connection.CookieContainer = new CookieContainer();
            connection.Error += e => Log.Error("[SignalR] Error occured: {error}", e.Message);
            connection.Received += HandleMessage;
            connection.Reconnecting += () => Log.Information("[SignalR] Reconnecting");
            connection.Reconnected += () => Log.Information("[SignalR] Reconnected");
            
            if (null != _version)
                connection.Protocol = _version;

            var hubProxy = connection.CreateHubProxy(_hub);
            _connection = connection;

            Log.Information("[SignalR] Connecting to {url}", _url);
            await connection.Start();
            
            if (_url.Contains("formula1"))
                await hubProxy.Invoke(method, _args.ToList());
            else
                await hubProxy.Invoke(method, _args);
            
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
        if (data?.A == null)
            return;

        Log.Information("[SignalR] New message received");
        var handlers = _handlers.Where(x => x.Item1 == data.H && x.Item2 == data.M);
        foreach (var handler in handlers)
            handler.Item3.Invoke(data.A);
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