using System.Text.RegularExpressions;
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

public partial class ChampionshipsController(ILogger<ChampionshipsController> logger, RaceControlContext raceControlContext, IMapper mapper)
    : ODataController
{
    [GeneratedRegex(@"[^\w\.@-]", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex SanitizeInputRegex();

    [HttpGet]
    [EnableQuery]
    public IQueryable<ChampionshipDto> Get()
    {
        logger.LogInformation("[OData] Requesting registered championships");

        return raceControlContext.Championships
            .ProjectTo<ChampionshipDto>(mapper.ConfigurationProvider)
            .AsSplitQuery();
    }

    [HttpGet]
    [EnableQuery]
    public SingleResult<ChampionshipDto> Get([FromRoute] string key)
    {
        key = SanitizeInputRegex().Replace(key, string.Empty);
        logger.LogInformation("[OData] Requesting registered championship with id {key}", key);

        var query = raceControlContext.Championships
            .Where(c => c.Id == key)
            .ProjectTo<ChampionshipDto>(mapper.ConfigurationProvider)
            .AsSplitQuery();

        return SingleResult.Create(query);
    }
}