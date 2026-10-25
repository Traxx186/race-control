using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using RaceControl.Data.Dtos;

namespace RaceControl.Server;

public static class EdmModelConfig
{
    public static IEdmModel GetEdmModel()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<ChampionshipDto>("Championships");
        builder.EntitySet<EventDto>("Events");
        builder.EntitySet<SessionDto>("Sessions");
        builder.EntitySet<RaceControlMessageDto>("RaceControlMessages");

        return builder.GetEdmModel();
    }
}