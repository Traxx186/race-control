using RaceControl.Console.Events;

namespace RaceControl.Console.Categories;

public interface IChampionship
{
    /// <summary>
    /// Event that gets invoked when a message is received from the live timing API.
    /// </summary>
    event EventHandler<MessageEventArgs> MessageReceived;

    /// <summary>
    /// Sets up and starts the live timing service related to the category.
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Closes the connection to the live timing service related to the category.
    /// </summary>
    Task StopAsync();
}