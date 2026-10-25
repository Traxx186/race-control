using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using RaceControl.Data.Dtos;
using RaceControl.Database;

namespace RaceControl.Server.Controllers.OData;

public class SessionsController(
    ILogger<EventsController> logger,
    RaceControlContext raceControlContext,
    IMapper mapper) : ODataController
{
    [HttpGet]
    [EnableQuery(PageSize = 100)]
    public IQueryable<SessionDto> Get()
    {
        logger.LogInformation("[OData] Requesting registered sessions");

        return raceControlContext.Sessions
            .ProjectTo<SessionDto>(mapper.ConfigurationProvider)
            .AsSplitQuery();
    }

    [HttpGet]
    [EnableQuery]
    public SingleResult<SessionDto> Get([FromRoute] int key)
    {
        logger.LogInformation("[OData] Requesting registered session with id {key}", key);

        var query = raceControlContext.Sessions
            .Where(c => c.Id == key)
            .ProjectTo<SessionDto>(mapper.ConfigurationProvider)
            .AsSplitQuery();

        return SingleResult.Create(query);
    }
}