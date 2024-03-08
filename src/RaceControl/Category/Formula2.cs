using System.Text.RegularExpressions;
using RaceControl.Track;

namespace RaceControl.Category;

public partial class Formula2 : ICategory
{
    /// <summary>
    /// Regex for checking if a race control message contains the message that the race/session will not resume.
    /// </summary>
    [GeneratedRegex("(?:WILL NOT).*(?:RESUME)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex NotResumeRegex();

    /// <summary>
    /// The record of the latest parsed flag.
    /// </summary>
    private static FlagData? _parsedFlag = new() { Flag = Flag.Chequered };

    /// <summary>
    /// How many <see cref="Flag.Chequered"/> are shown in the current sessnion before the API connection
    /// needs to be closed.
    /// </summary>
    private int _numberOfChequered;

    /// <summary>
    /// To detect redundant calls.
    /// </summary>
    private bool _disposedValue;

    /// <summary>
    /// The URL to the live timing API.
    /// </summary>
    private readonly string _url;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event Action<FlagData> OnFlagParsed;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event Action OnSessionFinished;
    
    public Formula2(string url)
    {
        _url = url;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void Start(string session)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void Stop()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
            return;

        if (disposing)
        {
            // TODO: add API connection to be disposed
        }

        _disposedValue = true;
    }
}