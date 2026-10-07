using RaceControl.Data.Dtos.LiveTimingDtos;
using RaceControl.Data.Enums;
using RaceControl.Data.Events;

namespace RaceControl.Server.Championships;

public class Formula1(ILogger<Formula1> logger) : IChampionship
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public bool Connected => true;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event EventHandler<FlagChangedEventArgs>? FlagParsed;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event EventHandler? SessionFinished;

    /// <summary>
    /// <inheritdoc/>
    /// Live timing is handled by client module.
    /// </summary>
    public Task StartAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// <inheritdoc/>
    /// Live timing is handled by client module.
    /// </summary>
    public Task StopAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void ParseRaceControlMessage(RaceControlMessageDto raceControlMessage)
    {
        logger.LogInformation("[Formula 1] Parsing race control message");

        // Checks if the slippery surface flag is shown.
        if (raceControlMessage.Message.Contains("slippery", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("[Formula 1] Parsed race control message to {flag}", Flag.Surface);
            OnFlagParsed(Flag.Surface);

            return;
        }

        // Checks if a standing start announcement is made.
        if (raceControlMessage.Category.Equals("other", StringComparison.OrdinalIgnoreCase) &&
            raceControlMessage.Message.Contains("standing start", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("[Formula 1] Parsed race control message to {flag}", Flag.StandingStart);
            OnFlagParsed(Flag.StandingStart);

            return;
        }

        // Checks if a rolling start announcement is made.
        if (raceControlMessage.Category.Equals("other", StringComparison.OrdinalIgnoreCase) &&
            raceControlMessage.Message.Contains("rolling start", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("[Formula 1] Parsed race control message to {flag}", Flag.RollingStart);
            OnFlagParsed(Flag.RollingStart);

            return;
        }

        // Skip the message if the message category is related to a flag.
        if (!raceControlMessage.Category.Equals("flag", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("[Formula 1] Race control message ignored");
            return;
        }

        // Checks if the flag message contains a valid flag and if the flag should be ignored.
        if (!TryParseFlag(raceControlMessage.Flag, out var flag))
        {
            logger.LogWarning("[Formula 1] Could not parse flag '{flag}'", raceControlMessage.Flag);
            return;
        }

        if (!int.TryParse(raceControlMessage.RacingNumber, out var driver))
            driver = 0;

        OnFlagParsed(flag, driver == 0 ? null : driver);
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void ParseTrackStatusMessage(TrackStatusMessageDto trackStatusMessage)
    {
        logger.LogInformation("[Formula 1] Parsing track status message");
        if (!short.TryParse(trackStatusMessage.Status, out var status))
        {
            logger.LogError("[Formula 1] Invalid track status message received");
            return;
        }

        var flag = status switch
        {
            1 => Flag.Clear,
            2 => Flag.Yellow,
            4 => Flag.SafetyCar,
            5 => Flag.Red,
            6 => Flag.Vsc,
            _ => Flag.None
        };

        OnFlagParsed(flag);
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Task ParseSessionStatusMessageAsync(SessionStatusMessageDto sessionStatusMessage)
    {
        logger.LogInformation("[Formula 1] Parsing session status message");

        if (!sessionStatusMessage.Status.Equals("finalised", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("[Formula 1] Session status message ignored");
            return Task.CompletedTask;
        }

        logger.LogInformation("[Formula 1] Session finalised, stopping live timing");
        OnSessionFinished();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Invokes the FlagPares event with the required arguments
    /// </summary>
    /// <param name="flag">The parsed flag.</param>
    /// <param name="driverNumber">The number of the driver for whom the flag is intended.</param>
    private void OnFlagParsed(Flag flag, int? driverNumber = null)
    {
        var args = new FlagChangedEventArgs { Flag = flag, Driver = driverNumber};
        FlagParsed?.Invoke(this, args);
    }

    /// <summary>
    /// Invokes the SessionFinished event.
    /// </summary>
    private void OnSessionFinished()
    {
        SessionFinished?.Invoke(this, EventArgs.Empty);

        // clear event handlers
        SessionFinished = null;
        FlagParsed = null;
    }

    /// <summary>
    /// Converts the input string to a <see cref="Flag"/>.
    /// </summary>
    /// <param name="input">The string representing a flag.</param>
    /// <param name="flag">
    /// When this method returns <see langword="true"/>, the related <see cref="Flag"/> item.
    /// Else <code>Flag.None</code> will be returned.
    /// </param>
    /// <returns>If the flag could be parsed.</returns>
    private static bool TryParseFlag(string? input, out Flag flag)
    {
        flag = input switch
        {
            "BLACK AND WHITE" => Flag.BlackWhite,
            "BLUE" => Flag.Blue,
            "CHEQUERED" => Flag.Chequered,
            "DOUBLE YELLOW" => Flag.DoubleYellow,
            "SLIPPERY SURFACE" => Flag.Surface,
            _ => Flag.None
        };

        return flag != Flag.None;
    }
}