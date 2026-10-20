using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using RaceControl.Data.Dtos;
using RaceControl.Database;

namespace RaceControl.Server.Controllers.OData;

public class ChampionshipsController(ILogger<ChampionshipsController> logger, RaceControlContext raceControlContext, IMapper mapper)
    : ODataController
{
    public async Task<ActionResult<IEnumerable<ChampionshipDto>>> Get()
    {
        logger.LogInformation("[OData] Requesting registered championships");
        var championships = await raceControlContext
            .Championships
            .Include(championship => championship.Events)
            .ThenInclude(events => events.Sessions)
            .ToListAsync();

        return mapper.Map<List<ChampionshipDto>>(championships);
    }

    public ActionResult<ChampionshipDto> Get([FromRoute] string key)
    {
        logger.LogInformation("[OData] Requesting registered championship with id {id}", key);
        var championship = raceControlContext.Championships.SingleOrDefault(championship => championship.Id == key);
        if (championship is null)
            return NotFound();

        return mapper.Map<ChampionshipDto>(championship);
    }
}