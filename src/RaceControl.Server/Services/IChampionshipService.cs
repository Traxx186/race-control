using RaceControl.Data.Dtos;
using RaceControl.Data.Enums;
using RaceControl.Database.Entities;

namespace RaceControl.Server.Services;

public interface IChampionshipService
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
    /// Closes the API connection of the active category.
    /// </summary>
    Task StopActiveCategoryAsync();

    /// <summary>
    /// Adds a new flag to the flag queue.
    /// </summary>
    /// <param name="flagData">flag data to add.</param>
    void EnqueueFlag(FlagDataDto flagData);
}