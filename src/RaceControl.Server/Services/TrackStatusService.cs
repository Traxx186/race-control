using Microsoft.AspNetCore.SignalR;
using RaceControl.Data.Dtos;
using RaceControl.Data.Enums;
using RaceControl.Server.Hubs;

namespace RaceControl.Server.Services;

public sealed class TrackStatusService(
    ILogger<TrackStatusService> logger,
    IHubContext<RaceControlHub, IRaceControlHubClient> raceHubContext) : ITrackStatusService
{
    private const int InformationFlagPriority = 0;

    /// <summary>
    /// Flag with their given priority. Flags with priority 0 are information flags
    /// </summary>
    private static readonly Dictionary<Flag, short> FlagPriority = new()
    {
        { Flag.BlackWhite, InformationFlagPriority },
        { Flag.Blue, InformationFlagPriority },
        { Flag.Surface, InformationFlagPriority },
        { Flag.Yellow, 2 },
        { Flag.DoubleYellow, 3 },
        { Flag.Vsc, 4 },
        { Flag.Code60, 4 },
        { Flag.Fyc, 4 },
        { Flag.SafetyCar, 5 },
        { Flag.Red, 6 }
    };

    /// <summary>
    /// Flags that override the other race flags.
    /// </summary>
    private static readonly Flag[] OverrideFlags = [Flag.Clear, Flag.Chequered];

    /// <inheritdoc/>
    public Flag ActiveFlag { get; private set; } = Flag.Clear;

    /// <inheritdoc/>
    public async Task SetActiveFlagAsync(FlagDataDto flagData)
    {
        logger.LogInformation("[Track Status] New flag received");
        if (OverrideFlags.Contains(flagData.Flag))
        {
            logger.LogInformation("[Track Status] Received override flag {flag}, sending flag and updating track status", flagData.Flag);

            ActiveFlag = flagData.Flag;
            await raceHubContext.Clients.All.FlagChange(flagData);

            return;
        }

        // If given flag is the same as the active flag, ignore the flag change.
        if (flagData.Flag == ActiveFlag)
            return;

        var newFlagPrio = FlagPriority.GetValueOrDefault(flagData.Flag);
        var currentFlagPrio = FlagPriority.GetValueOrDefault(ActiveFlag);
        if (ActiveFlag == Flag.Clear && newFlagPrio == InformationFlagPriority)
        {
            logger.LogInformation("[Track Status] Received information flag, sending flag data but not updating track status");
            await raceHubContext.Clients.All.FlagChange(flagData);

            return;
        }

        logger.LogInformation("[Track Status] Received status flag");
        if (newFlagPrio < currentFlagPrio)
        {
            logger.LogInformation("[Track Status] New received flag has lower priority, ignoring flag");
            return;
        }

        logger.LogInformation("[Track Status] New received flag with higher priority, updating track status");
        ActiveFlag = flagData.Flag;

        await raceHubContext.Clients.All.FlagChange(flagData);
    }
}