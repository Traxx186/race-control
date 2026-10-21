using AutoMapper;
using RaceControl.Data.Dtos;
using RaceControl.Database.Entities;

namespace RaceControl.Server.Mapping;

public class EntityToDtoMapping : Profile
{
    public EntityToDtoMapping()
    {
        CreateMap<Championship, ChampionshipDto>();
        CreateMap<Event, EventDto>();
        CreateMap<Session, SessionDto>();
    }
}