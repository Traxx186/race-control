using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using RaceControl.Data.Dtos;
using RaceControl.Database;

namespace RaceControl.Server.Controllers.OData;

public class ChampionshipsController(ILogger<ChampionshipsController> logger, RaceControlContext raceControlContext)
    : ODataController
{
    public ActionResult<IEnumerable<ChampionshipDto>> Get()
    {
        logger.LogInformation("[OData] Requesting registered championships");

        return raceControlContext.Championships
            .Select(championship => new ChampionshipDto(championship.Id, championship.Name))
            .ToArray();
    }

    public ActionResult<ChampionshipDto> Get([FromRoute] string key)
    {
        logger.LogInformation("[OData] Requesting registered championship with id {id}", key);
        var  championship = raceControlContext.Championships.SingleOrDefault(championship => championship.Id == key);
        if (championship is null)
            return NotFound();

        return new ChampionshipDto(championship.Id, championship.Name);
    }
}