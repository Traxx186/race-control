using RaceControl.Data.Dtos;
using RaceControl.Data.Enums;
using RaceControl.Database.Entities;

namespace RaceControl.Server.Services;

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
    /// List of flag to send to connected clients.
    /// </summary>
    public SortedList<DateTime, FlagDataDto> FlagQueue { get; }

    /// <summary>
    /// Starts the API connection of the category based on the given session.
    /// </summary>
    /// <param name="session">The session of the category to start.</param>
    Task StartCategoryAsync(Session session);

    /// <summary>
    /// Adds a new flag to the flag queue.
    /// </summary>
    /// <param name="flag">flag to add.</param>
    /// <param name="driver">related driver.</param>
    void EnqueueFlag(Flag flag, int? driver);
}