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

public class EventsController(ILogger<EventsController> logger, RaceControlContext raceControlContext, IMapper mapper) : ODataController
{
    [HttpGet]
    [EnableQuery]
    public IQueryable<EventDto> Get()
    {
        logger.LogInformation("[OData] Requesting registered events");

        return raceControlContext.Events
            .ProjectTo<EventDto>(mapper.ConfigurationProvider)
            .AsSplitQuery();
    }

    [HttpGet]
    [EnableQuery]
    public SingleResult<EventDto> Get([FromRoute] int key)
    {
        logger.LogInformation("[OData] Requesting registered event with id {key}", key);

        var query = raceControlContext.Events
            .Where(c => c.Id == key)
            .ProjectTo<EventDto>(mapper.ConfigurationProvider)
            .AsSplitQuery();

        return SingleResult.Create(query);
    }
}