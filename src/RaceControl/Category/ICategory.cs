using RaceControl.Track;

namespace RaceControl.Category;

public interface ICategory
{
    /// <summary>
    /// Action that gets invoked when the active flag of the category has changed.
    /// </summary>
    event Action<FlagData> OnFlagParsed;

    /// <summary>
    /// Sets up and starts the live timing service related to the category.
    /// </summary>
    void Start();
}