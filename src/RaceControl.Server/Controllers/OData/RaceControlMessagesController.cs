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

public class RaceControlMessagesController(
    ILogger<RaceControlMessagesController> logger,
    RaceControlContext raceControlContext,
    IMapper mapper) : ODataController
{
    [HttpGet]
    [EnableQuery(PageSize = 100)]
    public IQueryable<RaceControlMessageDto> Get()
    {
        logger.LogInformation("[OData] Requesting registered race control messages");

        return raceControlContext.RacesControlMessages
            .ProjectTo<RaceControlMessageDto>(mapper.ConfigurationProvider)
            .AsSplitQuery();
    }

    [HttpGet]
    [EnableQuery]
    public SingleResult<RaceControlMessageDto> Get([FromRoute] int key)
    {
        logger.LogInformation("[OData] Requesting registered race control message with id {key}", key);

        var query = raceControlContext.RacesControlMessages
            .Where(c => c.Id == key)
            .ProjectTo<RaceControlMessageDto>(mapper.ConfigurationProvider)
            .AsSplitQuery();

        return SingleResult.Create(query);
    }
}