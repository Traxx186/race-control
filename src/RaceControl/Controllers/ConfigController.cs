using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using RaceControl.Data.Dtos;
using RaceControl.Options;

namespace RaceControl.Controllers;

[ApiController]
[Route("[controller]")]
public class ConfigController(ILogger<ConfigController> logger) : ControllerBase
{
    public IActionResult Create(ConfigDto configDto)
    {
        logger.LogInformation("[ConfigController] Setting new configuration.");
        var content = System.IO.File.ReadAllText(RaceControlOptions.ConfigFilePath);
        var config = JsonSerializer.Deserialize<JsonObject>(content);

        if (!string.IsNullOrWhiteSpace(configDto.Formula1AccessToken))
            config[RaceControlOptions.Key]["Formula1AccessToken"] = configDto.Formula1AccessToken;

        System.IO.File.WriteAllText(RaceControlOptions.ConfigFilePath, JsonSerializer.Serialize<JsonObject>(config));

        return NoContent();
    }
}