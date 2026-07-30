using Microsoft.AspNetCore.SignalR;
using RaceControl.Data.Dtos;
using RaceControl.Data.Enums;
using RaceControl.Hubs;

namespace RaceControl.Services;

public sealed class TrackStatusService(
    ILogger<TrackStatusService> logger,
    IHubContext<TrackStatusHub, ITrackStatusHubClient> trackStatusHubContext) : ITrackStatusService
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
    public async Task SetActiveFlagAsync(Flag flag, int? driver = null)
    {
        logger.LogInformation("[Track Status] New flag received");
        if (OverrideFlags.Contains(flag))
        {
            logger.LogInformation("[Track Status] Received override flag {flag}, sending flag and updating track status", flag);

            ActiveFlag = flag;
            await trackStatusHubContext.Clients.All.FlagChange(new FlagDataDto(ActiveFlag, driver));

            return;
        }

        // If given flag is the same as the active flag, or the active flag is
        // None. Do not try to set the given flag.
        if (flag == ActiveFlag || flag == Flag.None)
            return;

        var newFlagPrio = FlagPriority.GetValueOrDefault(flag);
        var currentFlagPrio = FlagPriority.GetValueOrDefault(ActiveFlag);
        if (flag == Flag.Clear && newFlagPrio == InformationFlagPriority)
        {
            logger.LogInformation("[Track Status] Received information flag, sending flag data but not updating track status");
            await trackStatusHubContext.Clients.All.FlagChange(new FlagDataDto(flag, driver));

            return;
        }

        logger.LogInformation("[Track Status] Received status flag");
        if (newFlagPrio < currentFlagPrio)
        {
            logger.LogInformation("[Track Status] New received flag has lower priority, ignoring flag");
            return;
        }

        logger.LogInformation("[Track Status] New received flag with higher priority, updating track status");
        ActiveFlag = flag;

        await trackStatusHubContext.Clients.All.FlagChange(new FlagDataDto(ActiveFlag, driver));
    }

    /// <inheritdoc/>
    public static bool TryParseFlag(string? input, out Flag flag)
    {
        flag = input switch
        {
            "BLACK AND WHITE" => Flag.BlackWhite,
            "BLUE" => Flag.Blue,
            "CHEQUERED" => Flag.Chequered,
            "CLEAR" or "GREEN" => Flag.Clear,
            "CODE 60" => Flag.Code60,
            "DOUBLE YELLOW" => Flag.DoubleYellow,
            "FULL COURSE YELLOW" => Flag.Fyc,
            "RED" => Flag.Red,
            "SAFETY CAR" => Flag.SafetyCar,
            "SLIPPERY SURFACE" => Flag.Surface,
            "VIRTUAL SAFETY CAR" => Flag.Vsc,
            "YELLOW" => Flag.Yellow,
            _ => Flag.None
        };

        return flag != Flag.None;
    }
}