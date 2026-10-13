using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNet.SignalR.Client;
using Serilog;

namespace RaceControl.Server.SignalR;

/// <summary>
/// A SignalR client wrapper to easily connect to a SignalR server with the correct hub and args.
/// </summary>
public sealed class Client(string url, string hub) : IDisposable
{
    /// <summary>
    /// The url to connect to.
    /// </summary>
    private readonly string _url = url;

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
    /// Proxy of the connected hub.
    /// </summary>
    private IHubProxy? _proxy;

    /// <summary>
    /// List of handlers.
    /// </summary>
    private readonly List<(string, string, Action<JsonArray>)> _handlers = [];

    /// <summary>
    /// If the SignalR service is active.
    /// </summary>
    public bool Running { get; private set; }

    public event Action<Exception>? Error;

    public Client(string url, string hub, Version version)
        : this(url, hub)
    {
          _version = version;

          InitHubConnection();
    }

    public Client(string url, string hub, Version version, string customEndpoint)
        : this(url, hub, version)
    {
        _customEndpoint = customEndpoint;
        _useDefaultEndpoint = false;

        InitHubConnection();
    }

    /// <summary>
    /// Connects to the given SignalR server.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        Log.Information("[SignalR] Connecting to {url}", _url);

        await Task.WhenAny(_connection.Start(), Task.Delay(Timeout.Infinite, cancellationToken));
    }

    /// <summary>
    /// Adds a handler to be called when the hub and method equal the incoming message.
    /// </summary>
    /// <param name="hub">Name of the hub.</param>
    /// <param name="method">Name of the executed method.</param>
    /// <param name="handler">Function that will be executed.</param>
    public void AddHandler(string hub, string method, Action<JsonArray> handler) =>
        _handlers.Add((hub, method, handler));

    public IDisposable On(string method, Action handler) => _proxy.On(method, handler);
    public IDisposable On<T>(string method, Action<T> handler) => _proxy.On(method, handler);

    /// <summary>
    /// Disconnects the SignalR client.
    /// </summary>
    public void Stop()
    {
        Dispose();
    }

    public async Task InvokeAsync(string method, CancellationToken cancellationToken = default,
        params object[] args)
    {
        ThrowIfNotConnected();

        // Invoke the given method. If cancellation token is called when the invocation is still in progress, it will
        // be terminated.
        var invokeTask = _proxy.Invoke(method, args);
        await Task.WhenAny(invokeTask, Task.Delay(Timeout.Infinite, cancellationToken));
    }

    /// <summary>
    /// Disposes the SignalR client.
    /// </summary>
    public void Dispose()
    {
        Log.Information("[SignalR] Disconnecting client");

        _connection?.Stop();
        _connection?.Dispose();
        _connection = null;

        Running = false;
    }

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
    /// Initialize the SignalR connection hub object.
    /// </summary>
    private void InitHubConnection()
    {
        var uriBuilder = new UriBuilder(_url);
        if (!_useDefaultEndpoint)
            uriBuilder.Path = _customEndpoint;

        _connection = new HubConnection(uriBuilder.ToString(), useDefaultUrl: _useDefaultEndpoint);
        _connection.CookieContainer = new CookieContainer();
        _connection.Received += HandleMessage;
        _connection.Closed += () => Log.Information("[SignalR] Hub connection closed");
        _connection.Reconnecting += () => Log.Information("[SignalR] Reconnecting");
        _connection.Reconnected += () => Log.Information("[SignalR] Reconnected");
        _connection.Error += e =>
        {
            Log.Error("[SignalR] Error occured: {error}", e.Message);
            Error?.Invoke(e);
        };

#if DEBUG
        _connection.TraceWriter = Console.Out;
        _connection.TraceLevel = TraceLevels.All;
#endif

        if (null != _version)
            _connection.Protocol = _version;

        _proxy = _connection.CreateHubProxy(_hub);
    }

    /// <summary>
    /// Throw a <see cref="InvalidOperationException"/> if the SignalR client is not connected to a server.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    private void ThrowIfNotConnected()
    {
        if (_connection is null || _connection.State != ConnectionState.Connected)
            throw new InvalidOperationException($"HubConnection is not connected, expected Connected.");
    }
}