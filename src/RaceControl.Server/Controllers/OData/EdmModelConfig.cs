using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using RaceControl.Data.Dtos;

namespace RaceControl.Server.Controllers.OData;

public static class EdmModelConfig
{
    public static IEdmModel GetEdmModel()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<ChampionshipDto>("Championships");

        return builder.GetEdmModel();
    }
}