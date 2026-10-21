using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using RaceControl.Data.Dtos;
using RaceControl.Database;

namespace RaceControl.Server.Controllers.OData;

public class ChampionshipsController(ILogger<ChampionshipsController> logger, RaceControlContext raceControlContext, IMapper mapper)
    : ODataController
{
    [HttpGet]
    [EnableQuery]
    public IQueryable<ChampionshipDto> Get()
    {
        logger.LogInformation("[OData] Requesting registered championships");

        return raceControlContext.Championships
            .ProjectTo<ChampionshipDto>(mapper.ConfigurationProvider);
    }

    [HttpGet]
    [EnableQuery]
    public ActionResult<ChampionshipDto> Get([FromRoute] string key)
    {
        logger.LogInformation("[OData] Requesting registered championship with id {id}", key);
        var championship = raceControlContext.Championships.SingleOrDefault(championship => championship.Id == key);
        if (championship is null)
            return NotFound();

        return mapper.Map<ChampionshipDto>(championship);
    }
}