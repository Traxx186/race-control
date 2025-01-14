using RaceControl.Track;

namespace RaceControl.Categories;

public interface ICategory : IDisposable
{
    /// <summary>
    /// Event that gets invoked when the active flag of the category has changed.
    /// </summary>
    event EventHandler<FlagDataEventArgs> FlagParsed;

    /// <summary>
    /// Event that gets invoked when a session of the category has finished.
    /// </summary>
    event EventHandler SessionFinished; 

    /// <summary>
    /// Sets up and starts the live timing service related to the category.
    /// </summary>
    void Start(string session);

    /// <summary>
    /// Closes the connection to the live timing service related to the category.
    /// </summary>
    void Stop();
}