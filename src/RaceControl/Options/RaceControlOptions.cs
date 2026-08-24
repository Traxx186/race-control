using System.Text.Json.Nodes;

namespace RaceControl.Options;

public record RaceControlOptions
{
    public const string Key = "RaceControl";

    public static string ConfigFilePath => GetConfigFilePath();

    /// <summary>
    /// The access token that is used in the connection to the SignalR Live Timing service.
    /// Providing this token allows you to access additional live timing feeds which may be used in some
    /// features of undercut-f1.
    /// </summary>
    public string? Formula1AccessToken { get; set; }

    private static string GetConfigFilePath()
    {
        var path = Path.Join(Environment.CurrentDirectory, "storage", "config.json");
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
}