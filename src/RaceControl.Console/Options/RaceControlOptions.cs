using System.Text.Json.Nodes;

namespace RaceControl.Console.Options;

public record RaceControlOptions
{
    public const string Key = "RaceControl";

    public static string ConfigFilePath => GetConfigFilePath();
    public static string AppStoragePath => GetAppStoragePath();

    /// <summary>
    /// The host where the client will send the parsed race control messages to.
    /// </summary>
    public string? BroadcastHost { get; set; }

    private static string GetConfigFilePath()
    {
        string? path;
        if (OperatingSystem.IsWindows())
        {
            path = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData,
                Environment.SpecialFolderOption.Create
            );
        }
        else
        {
            path = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (string.IsNullOrWhiteSpace(path))
            {
                path = Path.Join(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".config"
                );
            }
        }

        path = Path.Join(path, "race-control", "config.json");
        if (File.Exists(path))
            return path;

        var content = new JsonObject
        {
            [Key] = new JsonObject()
        };

        Directory.CreateDirectory(Directory.GetParent(path)!.FullName);
        File.WriteAllText(path, content.ToJsonString());

        return path;
    }

    private static string GetAppStoragePath()
    {
        string? path;
        if (OperatingSystem.IsWindows())
        {
            path = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData,
                Environment.SpecialFolderOption.Create
            );
        }
        else
        {
            path = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (string.IsNullOrWhiteSpace(path))
            {
                path = Path.Join(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".local",
                    "share"
                );
            }
        }

        path = Path.Join(path, "race-control");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        return path;
    }
}