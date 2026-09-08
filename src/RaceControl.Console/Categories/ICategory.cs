namespace RaceControl.Console.Categories;

public interface ICategory
{
    /// <summary>
    /// If the live timing API is active.
    /// </summary>
    bool Connected { get; }

    /// <summary>
    /// Sets up and starts the live timing service related to the category.
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Closes the connection to the live timing service related to the category.
    /// </summary>
    Task StopAsync();
}