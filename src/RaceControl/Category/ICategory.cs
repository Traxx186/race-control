using RaceControl.Track;

namespace RaceControl.Category;

public interface ICategory
{
    /// <summary>
    /// Event that gets invoked when the active flag of the category has changed.
    /// </summary>
    event Action<FlagData> OnFlagParsed;

    /// <summary>
    /// Event that gets invoked when the a session of the category has finshed.
    /// </summary>
    event Action OnSessionFinished; 

    /// <summary>
    /// Sets up and starts the live timing service related to the category.
    /// </summary>
    void Start(string session);

    /// <summary>
    /// Closes the connection to the live timing service related to the category.
    /// </summary>
    void Stop();
}