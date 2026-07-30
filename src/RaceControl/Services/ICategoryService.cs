using RaceControl.Database.Entities;

namespace RaceControl.Services;

public interface ICategoryService
{
    /// <summary>
    /// If there is already a session active.
    /// </summary>
    bool HasSessionActive { get; }

    /// <summary>
    /// Returns the currently active session, if there is any.
    /// </summary>
    Session? ActiveSession { get; }

    /// <summary>
    /// Starts the API connection of the category based on the given session.
    /// </summary>
    /// <param name="session">The session of the category to start.</param>
    Task StartCategoryAsync(Session session);
}